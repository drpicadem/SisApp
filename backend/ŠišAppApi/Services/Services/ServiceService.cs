using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
            var query = _context.Services.AsQueryable();

            if (search != null)
            {
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

            var list = await query.ToListAsync();
            return _mapper.Map<List<ServiceDto>>(list);
        }

        public override async Task<ServiceDto> Insert(ServiceInsertRequest request)
        {
            var entity = _mapper.Map<Service>(request);
            entity.CreatedAt = DateTime.UtcNow;
            entity.IsActive = true;
            entity.IsDeleted = false;

            _context.Services.Add(entity);
            await _context.SaveChangesAsync();
            return _mapper.Map<ServiceDto>(entity);
        }

        public override async Task<ServiceDto> Update(int id, ServiceUpdateRequest request)
        {
            var entity = await _context.Services.FindAsync(id);
            if (entity == null)
                throw new UserException("Usluga nije pronađena");

            _mapper.Map(request, entity);
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return _mapper.Map<ServiceDto>(entity);
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
                    && (a.Status == "Pending" || a.Status == "Confirmed"));

            if (hasActiveAppointments)
            {
                var count = await _context.Appointments
                    .CountAsync(a => a.ServiceId == id
                        && a.AppointmentDateTime > bufferTime
                        && (a.Status == "Pending" || a.Status == "Confirmed"));

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

