using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Constants;
using ŠišAppApi.Data;
using ŠišAppApi.Filters;
using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Models.Requests;
using ŠišAppApi.Models.SearchObjects;
using ŠišAppApi.Services;
using ŠišAppApi.Services.Interfaces;
using System.Text.RegularExpressions;

namespace ŠišAppApi.Services.Services
{
    public class UserService : BaseCRUDService<UserDto, UserSearchObject, User, UserInsertRequest, UserUpdateRequest>, IUserService
    {
        public UserService(ApplicationDbContext context, IMapper mapper) : base(context, mapper)
        {
        }

        public override async Task<IEnumerable<UserDto>> Get(UserSearchObject? search = null)
        {
            var query = _context.Users.AsQueryable();
            var page = Math.Max(1, search?.Page ?? 1);
            var pageSize = Math.Clamp(search?.PageSize ?? 20, 1, 100);

            if (search?.IsDeleted.HasValue == true)
            {
                query = _context.Users
                    .IgnoreQueryFilters()
                    .Where(u => u.IsDeleted == search.IsDeleted.Value);
            }

            if (search != null)
            {
                if (!string.IsNullOrWhiteSpace(search.Q))
                {
                    var q = search.Q.Trim().ToLower();
                    query = query.Where(u =>
                        u.Username.ToLower().Contains(q) ||
                        u.Email.ToLower().Contains(q) ||
                        u.FirstName.ToLower().Contains(q) ||
                        u.LastName.ToLower().Contains(q));
                }
                if (!string.IsNullOrEmpty(search.Role))
                {
                    query = query.Where(u => u.Role == search.Role);
                }
                if (!string.IsNullOrEmpty(search.Username))
                {
                    query = query.Where(u => u.Username.Contains(search.Username));
                }
                if (!string.IsNullOrEmpty(search.Email))
                {
                    query = query.Where(u => u.Email.Contains(search.Email));
                }
            }

            var list = await query
                .OrderBy(u => u.Username)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return _mapper.Map<List<UserDto>>(list);
        }

        public override async Task<UserDto> Insert(UserInsertRequest request)
        {
            var entity = _mapper.Map<User>(request);
            entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            entity.CreatedAt = DateTime.UtcNow;
            entity.IsActive = true;
            entity.Role = AppRoles.User;
            entity.IsEmailVerified = false;
            entity.IsPhoneVerified = false;

            _context.Users.Add(entity);
            await _context.SaveChangesAsync();
            return _mapper.Map<UserDto>(entity);
        }

        public override async Task<UserDto> Update(int id, UserUpdateRequest request)
        {
            var entity = await _context.Users.FindAsync(id);
            if (entity == null)
                throw new UserException("Korisnik nije pronađen");


            entity.Username = request.Username.Trim();
            entity.Email = request.Email.Trim();
            entity.FirstName = request.FirstName.Trim();
            entity.LastName = request.LastName.Trim();
            entity.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
            if (request.IsActive.HasValue)
            {
                entity.IsActive = request.IsActive.Value;
            }

            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return _mapper.Map<UserDto>(entity);
        }

        public override async Task<UserDto> Delete(int id)
        {
            var entity = await _context.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == id);
            if (entity == null)
                throw new UserException("Korisnik nije pronađen");
            if (entity.IsDeleted)
                throw new UserException("Korisnik je već obrisan");

            entity.IsDeleted = true;
            entity.IsActive = false;
            entity.DeletedAt = DateTime.UtcNow;

            string deleteSuffix = "_del_" + Guid.NewGuid().ToString().Substring(0, 6);
            entity.Username = entity.Username + deleteSuffix;
            entity.Email = entity.Email + deleteSuffix;

            var futureAppointments = await _context.Appointments
                .Where(a => a.UserId == id
                    && a.AppointmentDateTime > DateTime.UtcNow
                    && (a.Status == AppointmentStatuses.Pending || a.Status == AppointmentStatuses.Confirmed))
                .ToListAsync();

            foreach (var apt in futureAppointments)
            {
                if (AppointmentStateMachine.CanTransition(apt.Status, AppointmentStatuses.Cancelled))
                {
                    apt.Status = AppointmentStatuses.Cancelled;
                    apt.CancelledAt = DateTime.UtcNow;
                    apt.CancellationReason = "Korisnički račun obrisan";
                }
            }

            await _context.SaveChangesAsync();
            return _mapper.Map<UserDto>(entity);
        }

        public async Task<UserDto> RestoreUser(int id)
        {
            var entity = await _context.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == id && u.IsDeleted);
            if (entity == null)
                throw new UserException("Korisnik nije pronađen ili nije obrisan");

            var restoredUsername = RemoveDeleteSuffix(entity.Username);
            var restoredEmail = RemoveDeleteSuffix(entity.Email);

            var usernameTaken = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Id != entity.Id && !u.IsDeleted && u.Username == restoredUsername);
            if (usernameTaken)
                throw new UserException($"Vraćanje nije moguće. Korisničko ime \"{restoredUsername}\" je već zauzeto.");

            var emailTaken = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Id != entity.Id && !u.IsDeleted && u.Email == restoredEmail);
            if (emailTaken)
                throw new UserException($"Vraćanje nije moguće. Email \"{restoredEmail}\" je već u upotrebi.");

            entity.IsDeleted = false;
            entity.IsActive = true;
            entity.DeletedAt = null;
            entity.Username = restoredUsername;
            entity.Email = restoredEmail;

            await _context.SaveChangesAsync();
            return _mapper.Map<UserDto>(entity);
        }

        private static string RemoveDeleteSuffix(string value)
        {
            return Regex.Replace(value, "_del_[0-9a-fA-F]{6}$", string.Empty);
        }

        public async Task SetPasswordByAdmin(int id, string newPassword, string confirmPassword)
        {
            var entity = await _context.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == id);
            if (entity == null)
                throw new UserException("Korisnik nije pronađen");
            if (entity.IsDeleted)
                throw new UserException("Nije moguće postaviti lozinku za obrisan korisnički račun");

            if (newPassword != confirmPassword)
                throw new UserException("Potvrda lozinke mora biti ista kao nova lozinka");

            entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            entity.UpdatedAt = DateTime.UtcNow;

            var activeRefreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == id && !rt.IsDeleted && rt.RevokedAt == null)
                .ToListAsync();
            foreach (var rt in activeRefreshTokens)
            {
                rt.RevokedAt = DateTime.UtcNow;
                rt.IsDeleted = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<UserDto> UpdateProfileImageAsync(int userId, string imageId)
        {
            var entity = await _context.Users.FindAsync(userId);
            if (entity == null)
                throw new UserException("Korisnik nije pronađen");

            entity.ImageId = imageId;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return _mapper.Map<UserDto>(entity);
        }
    }
}
