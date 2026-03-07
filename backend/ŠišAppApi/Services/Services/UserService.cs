using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Data;
using ŠišAppApi.Filters;
using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Models.Requests;
using ŠišAppApi.Models.SearchObjects;
using ŠišAppApi.Services.Interfaces;

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

            if (search != null)
            {
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

            var list = await query.ToListAsync();
            return _mapper.Map<List<UserDto>>(list);
        }

        public override async Task<UserDto> Insert(UserInsertRequest request)
        {
            var entity = _mapper.Map<User>(request);
            entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            entity.CreatedAt = DateTime.UtcNow;
            entity.IsActive = true;

            _context.Users.Add(entity);
            await _context.SaveChangesAsync();
            return _mapper.Map<UserDto>(entity);
        }

        public override async Task<UserDto> Update(int id, UserUpdateRequest request)
        {
            var entity = await _context.Users.FindAsync(id);
            if (entity == null)
                throw new UserException("Korisnik nije pronađen");

            _mapper.Map(request, entity);

            if (!string.IsNullOrEmpty(request.Password))
            {
                entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
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
                    && (a.Status == "Pending" || a.Status == "Confirmed"))
                .ToListAsync();

            foreach (var apt in futureAppointments)
            {
                apt.Status = "Cancelled";
                apt.CancelledAt = DateTime.UtcNow;
                apt.CancellationReason = "Korisnički račun obrisan";
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

            entity.IsDeleted = false;
            entity.IsActive = true;
            entity.DeletedAt = null;

            await _context.SaveChangesAsync();
            return _mapper.Map<UserDto>(entity);
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
