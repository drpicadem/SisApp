using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ŠišAppApi.Constants;
using ŠišAppApi.Data;
using ŠišAppApi.Filters;
using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Models.Requests;
using ŠišAppApi.Models.SearchObjects;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Services.Services
{
    public class ServiceService : BaseCRUDService<ServiceDto, ServiceSearchObject, Service, ServiceInsertRequest, ServiceUpdateRequest>, IServiceService
    {
        private readonly ILogger<ServiceService> _logger;

        public ServiceService(ApplicationDbContext context, IMapper mapper, ILogger<ServiceService> logger) : base(context, mapper)
        {
            _logger = logger;
        }

        public override async Task<IEnumerable<ServiceDto>> Get(ServiceSearchObject? search = null)
        {
            var query = _context.Services
                .Include(s => s.Category)
                .AsQueryable();

            var page = Math.Max(1, search?.Page ?? 1);
            var pageSize = Math.Clamp(search?.PageSize ?? 20, 1, 100);

            if (search != null)
            {
                if (!string.IsNullOrWhiteSpace(search.Q))
                {
                    var q = search.Q.Trim().ToLower();
                    query = query.Where(s =>
                        s.Name.ToLower().Contains(q) ||
                        (s.Description != null && s.Description.ToLower().Contains(q)));
                }
                if (search.SalonId.HasValue)
                {
                    query = query.Where(s => s.SalonId == search.SalonId.Value);
                }
                if (!string.IsNullOrEmpty(search.Name))
                {
                    query = query.Where(s => s.Name.Contains(search.Name));
                }
                if (search.IsActive.HasValue)
                {
                    query = query.Where(s => s.IsActive == search.IsActive.Value);
                }
                if (search.IsDeleted.HasValue)
                {
                    query = query.Where(s => s.IsDeleted == search.IsDeleted.Value);
                }
                else
                {
                    query = query.Where(s => !s.IsDeleted);
                }
            }
            else
            {
                query = query.Where(s => !s.IsDeleted);
            }

            var list = await query
                .OrderBy(s => s.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return list.Select(s => new ServiceDto
            {
                Id = s.Id,
                SalonId = s.SalonId,
                Name = s.Name,
                Description = s.Description,
                Price = s.Price,
                DurationMinutes = s.DurationMinutes,
                CategoryId = s.CategoryId,
                CategoryName = s.Category?.Name,
                CategoryDescription = s.Category?.Description,
                IsPopular = s.IsPopular,
                IsActive = s.IsActive
            }).ToList();
        }

        public override async Task<ServiceDto> Insert(ServiceInsertRequest request)
        {
            if (request.DurationMinutes <= 0)
                throw new UserException("Trajanje usluge mora biti veće od 0 minuta.");
            if (request.Price <= 0)
                throw new UserException("Cijena usluge mora biti veća od 0.");

            var salonExists = await _context.Salons.AnyAsync(s => s.Id == request.SalonId && !s.IsDeleted);
            if (!salonExists)
                throw new UserException("Odabrani salon ne postoji.");

            var entity = new Service
            {
                SalonId = request.SalonId,
                Name = request.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                DurationMinutes = request.DurationMinutes,
                Price = request.Price,
                CategoryId = request.CategoryId,
                IsPopular = request.IsPopular,
                DisplayOrder = request.DisplayOrder
            };
            entity.CreatedAt = DateTime.UtcNow;
            entity.IsActive = true;
            entity.IsDeleted = false;

            _context.Services.Add(entity);
            await _context.SaveChangesAsync();

            var created = await _context.Services
                .Include(s => s.Category)
                .FirstAsync(s => s.Id == entity.Id);

            return new ServiceDto
            {
                Id = created.Id,
                SalonId = created.SalonId,
                Name = created.Name,
                Description = created.Description,
                Price = created.Price,
                DurationMinutes = created.DurationMinutes,
                CategoryId = created.CategoryId,
                CategoryName = created.Category?.Name,
                CategoryDescription = created.Category?.Description,
                IsPopular = created.IsPopular,
                IsActive = created.IsActive
            };
        }

        public override async Task<ServiceDto> Update(int id, ServiceUpdateRequest request)
        {
            var entity = await _context.Services.FindAsync(id);
            if (entity == null)
                throw new UserException("Usluga nije pronađena");

            if (request.DurationMinutes <= 0)
                throw new UserException("Trajanje usluge mora biti veće od 0 minuta.");
            if (request.Price <= 0)
                throw new UserException("Cijena usluge mora biti veća od 0.");


            entity.Name = request.Name.Trim();
            entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            entity.DurationMinutes = request.DurationMinutes;
            entity.Price = request.Price;
            entity.CategoryId = request.CategoryId;
            entity.IsPopular = request.IsPopular;
            entity.DisplayOrder = request.DisplayOrder;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var updated = await _context.Services
                .Include(s => s.Category)
                .FirstAsync(s => s.Id == entity.Id);

            return new ServiceDto
            {
                Id = updated.Id,
                SalonId = updated.SalonId,
                Name = updated.Name,
                Description = updated.Description,
                Price = updated.Price,
                DurationMinutes = updated.DurationMinutes,
                CategoryId = updated.CategoryId,
                CategoryName = updated.Category?.Name,
                CategoryDescription = updated.Category?.Description,
                IsPopular = updated.IsPopular,
                IsActive = updated.IsActive
            };
        }

        public override async Task<ServiceDto> Delete(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            var entity = await _context.Services.FindAsync(id);
            if (entity == null)
                throw new UserException("Usluga nije pronađena");
            if (entity.IsDeleted)
                throw new UserException("Usluga je već obrisana");

            var bufferTime = DateTime.UtcNow.AddMinutes(-15);
            var hasActiveAppointments = await _context.Appointments
                .AnyAsync(a => a.ServiceId == id
                    && a.AppointmentDateTime > bufferTime
                    && (a.Status == AppointmentStatuses.Pending || a.Status == AppointmentStatuses.Confirmed));

            if (hasActiveAppointments)
            {
                var count = await _context.Appointments
                    .CountAsync(a => a.ServiceId == id
                        && a.AppointmentDateTime > bufferTime
                        && (a.Status == AppointmentStatuses.Pending || a.Status == AppointmentStatuses.Confirmed));

                throw new UserException($"Ne možete obrisati uslugu koja ima {count} zakazanih termina. Prvo ih otkažite.");
            }

            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.IsActive = false;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return _mapper.Map<ServiceDto>(entity);
        }

        public async Task<ServiceDto> DeleteAsBarber(int serviceId, int userId)
        {
            var isOwner = await _context.Services
                .AnyAsync(s => s.Id == serviceId && !s.IsDeleted && s.Salon.Barbers.Any(b => b.UserId == userId));

            if (!isOwner)
            {
                _logger.LogWarning("Unauthorized delete attempt by User {UserId} for Service {ServiceId}", userId, serviceId);
                throw new UserException("Možete brisati samo usluge iz svog salona.");
            }

            return await Delete(serviceId);
        }
    }
}

