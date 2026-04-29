using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ŠišAppApi.Constants;
using ŠišAppApi.Data;
using ŠišAppApi.Filters;
using ŠišAppApi.Models;
using ŠišAppApi.Models.Authentication;
using ŠišAppApi.Models.DTOs.Auth;

namespace ŠišAppApi.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(
            ApplicationDbContext context,
            IConfiguration configuration,
            IEmailService emailService,
            ILogger<AuthenticationService> logger)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<TokenResponse?> Login(LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());

            if (user == null)
            {
                _logger.LogWarning("Login failed. User not found: {Username}", request.Username);
                return null;
            }

            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed. Password mismatch for user: {Username}", request.Username);
                return null;
            }

            if (user.IsDeleted || !user.IsActive)
            {
                _logger.LogWarning("Login failed. User inactive/deleted: {Username}", request.Username);
                throw new UserException("Korisnički račun je deaktiviran.");
            }

            if (string.Equals(user.Role, AppRoles.Barber, StringComparison.OrdinalIgnoreCase))
            {
                var barber = await _context.Barbers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.UserId == user.Id && !b.IsDeleted);

                if (barber == null)
                {
                    _logger.LogWarning("Login failed. Barber profile missing/inactive for user: {Username}", request.Username);
                    throw new UserException("Frizerski profil nije dostupan.");
                }

                var salonIsActive = await _context.Salons
                    .AsNoTracking()
                    .Where(s => s.Id == barber.SalonId && !s.IsDeleted)
                    .Select(s => s.IsActive)
                    .FirstOrDefaultAsync();

                if (!salonIsActive)
                {
                    _logger.LogWarning("Login blocked. Barber salon suspended for user: {Username}", request.Username);
                    throw new UserException("Salon je suspendovan. Prijava nije moguća.");
                }
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Login successful for user: {Username}", request.Username);
            return GenerateToken(user);
        }

        public async Task<TokenResponse> Register(RegisterDto request)
        {
            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == request.Username.ToLower()))
            {
                throw new UserException("Korisničko ime je već zauzeto");
            }

            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower()))
            {
                throw new UserException("Email adresa je već registrovana");
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                Role = AppRoles.User,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                IsEmailVerified = true
            };

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var customer = new Customer
                {
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Registration successful for user: {Username}", request.Username);
            return GenerateToken(user);
        }

        public async Task<TokenResponse?> RefreshToken(string refreshToken)
        {
            var tokenEntity = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsDeleted);

            if (tokenEntity == null || tokenEntity.ExpiresAt < DateTime.UtcNow || tokenEntity.RevokedAt != null)
            {
                return null;
            }

            return GenerateToken(tokenEntity.User);
        }

        public async Task<bool> RevokeToken(string? refreshToken, int userId, string? accessTokenJti)
        {
            if (string.IsNullOrWhiteSpace(refreshToken) && string.IsNullOrWhiteSpace(accessTokenJti))
            {
                return false;
            }

            var revokedAny = false;
            var now = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                var tokenEntity = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.UserId == userId && !rt.IsDeleted);

                if (tokenEntity != null)
                {
                    tokenEntity.RevokedAt = now;
                    tokenEntity.IsDeleted = true;
                    tokenEntity.DeletedAt = now;
                    tokenEntity.UpdatedAt = now;
                    tokenEntity.ReasonRevoked = "Logout";
                    revokedAny = true;
                }
            }

            if (!string.IsNullOrWhiteSpace(accessTokenJti))
            {
                var blacklistToken = BuildAccessTokenBlacklistKey(accessTokenJti);
                var alreadyBlacklisted = await _context.RefreshTokens
                    .AnyAsync(rt => rt.Token == blacklistToken && !rt.IsDeleted);

                if (!alreadyBlacklisted)
                {
                    var accessTokenLifetimeMinutes = GetAccessTokenLifetimeMinutes();
                    _context.RefreshTokens.Add(new RefreshToken
                    {
                        UserId = userId,
                        Token = blacklistToken,
                        ExpiresAt = now.AddMinutes(accessTokenLifetimeMinutes),
                        RevokedAt = now,
                        ReasonRevoked = "AccessTokenRevokedOnLogout",
                        CreatedAt = now
                    });
                }

                revokedAny = true;
            }

            if (!revokedAny)
            {
                return false;
            }

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task RequestPasswordReset(PasswordResetRequestDto request)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (user == null)
            {
                return;
            }

            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
            user.PasswordResetTokenHash = HashResetToken(token);
            user.PasswordResetRequestedAt = DateTime.UtcNow;
            user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
            user.PasswordResetUsedAt = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var subject = "Reset lozinke - ŠišApp";
            var body = $@"
                <h1>Reset lozinke</h1>
                <p>Primili smo zahtjev za resetiranje Vaše lozinke.</p>
                <p>Unesite sljedeći token u mobilnu aplikaciju:</p>
                <p><strong>{token}</strong></p>
                <p>Token važi 60 minuta.</p>
                <p>Ako niste Vi pokrenuli reset, zanemarite ovaj e-mail.</p>
                <hr/>
                <p>ŠišApp Team</p>";

            await _emailService.SendEmailAsync(user.Email, subject, body);
        }

        public async Task ResetPassword(PasswordResetConfirmDto request)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (user == null)
            {
                throw new UserException("Neispravan token ili email.");
            }

            if (string.IsNullOrWhiteSpace(user.PasswordResetTokenHash) ||
                user.PasswordResetTokenExpiresAt == null ||
                user.PasswordResetTokenExpiresAt <= DateTime.UtcNow ||
                user.PasswordResetUsedAt != null)
            {
                throw new UserException("Reset token je istekao ili nije validan.");
            }

            var incomingTokenHash = HashResetToken(request.Token.Trim());
            if (!string.Equals(incomingTokenHash, user.PasswordResetTokenHash, StringComparison.Ordinal))
            {
                throw new UserException("Neispravan token ili email.");
            }

            user.PasswordHash = HashPassword(request.NewPassword);
            user.PasswordResetUsedAt = DateTime.UtcNow;
            user.PasswordResetTokenHash = null;
            user.PasswordResetTokenExpiresAt = null;
            user.UpdatedAt = DateTime.UtcNow;

            var activeRefreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && !rt.IsDeleted && rt.RevokedAt == null)
                .ToListAsync();
            foreach (var rt in activeRefreshTokens)
            {
                rt.RevokedAt = DateTime.UtcNow;
                rt.IsDeleted = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task ChangePassword(int userId, ChangePasswordDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new UserException("Korisnik nije pronađen.");
            }

            if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                throw new UserException("Trenutna lozinka nije tačna.");
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                throw new UserException("Potvrda lozinke mora biti ista kao nova lozinka.");
            }

            if (VerifyPassword(request.NewPassword, user.PasswordHash))
            {
                throw new UserException("Nova lozinka mora biti različita od trenutne lozinke.");
            }

            user.PasswordHash = HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            var activeRefreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && !rt.IsDeleted && rt.RevokedAt == null)
                .ToListAsync();
            foreach (var rt in activeRefreshTokens)
            {
                rt.RevokedAt = DateTime.UtcNow;
                rt.IsDeleted = true;
            }

            await _context.SaveChangesAsync();
        }

        public TokenResponse GenerateToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpirationInMinutes"])),
                signingCredentials: credentials
            );

            var newRefreshToken = Guid.NewGuid().ToString();
            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };
            _context.RefreshTokens.Add(refreshTokenEntity);
            _context.SaveChanges();

            return new TokenResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = newRefreshToken,
                Expiration = token.ValidTo
            };
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }

        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private static string HashResetToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }

        private static string BuildAccessTokenBlacklistKey(string jti) => $"revoked-jti:{jti}";

        private int GetAccessTokenLifetimeMinutes()
        {
            var configured = _configuration["Jwt:ExpirationInMinutes"];
            if (double.TryParse(configured, out var minutes) && minutes > 0)
            {
                return (int)Math.Ceiling(minutes);
            }

            return 60;
        }
    }
} 