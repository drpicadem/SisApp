using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ŠišAppApi.Data;
using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Models.Requests;
using ŠišAppApi.Models.SearchObjects;
using ŠišAppApi.Filters;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Services.Services
{
    public class SalonService : BaseCRUDService<SalonDto, SalonSearchObject, Salon, SalonInsertRequest, SalonUpdateRequest>, ISalonService
    {
        public SalonService(ApplicationDbContext context, IMapper mapper) : base(context, mapper)
        {
        }

        public override async Task<IEnumerable<SalonDto>> Get(SalonSearchObject? search = null)
        {
            var query = _context.Salons
                .Include(s => s.CityRef)
                .Include(s => s.Barbers)
                .Include(s => s.Services).AsQueryable();

            var page = Math.Max(1, search?.Page ?? 1);
            var pageSize = Math.Clamp(search?.PageSize ?? 20, 1, 100);

            if (search != null)
            {
                if (!string.IsNullOrWhiteSpace(search.Q))
                {
                    var q = search.Q.Trim().ToLower();
                    query = query.Where(s =>
                        s.Name.ToLower().Contains(q) ||
                        s.Address.ToLower().Contains(q) ||
                        (s.CityRef != null && s.CityRef.Name.ToLower().Contains(q)));
                }
                if (!string.IsNullOrEmpty(search.Name))
                {
                    query = query.Where(s => s.Name.Contains(search.Name));
                }
                if (!string.IsNullOrEmpty(search.City))
                {
                    query = query.Where(s => s.CityRef != null && s.CityRef.Name == search.City);
                }
                if (search.IsActive.HasValue)
                {
                    query = query.Where(s => s.IsActive == search.IsActive.Value);
                }
            }

            var list = await query
                .OrderBy(s => s.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new SalonDto
            {
                Id = s.Id,
                Name = s.Name,
                CityId = s.CityId,
                City = s.CityRef != null ? s.CityRef.Name : string.Empty,
                Address = s.Address,
                Phone = s.Phone,
                PostalCode = s.PostalCode,
                Website = s.Website,
                ImageIds = s.ImageIds,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                Services = s.Services.Where(serv => !serv.IsDeleted && serv.IsActive).Select(serv => serv.Name).ToList(),
                EmployeeCount = s.Barbers.Count(b => !b.IsDeleted),
                Rating = s.Rating,
                IsActive = s.IsActive
            }).ToListAsync();

            return list;
        }

        public override async Task<SalonDto> GetById(int id)
        {
            var s = await _context.Salons
                .Include(s => s.CityRef)
                .Include(s => s.Barbers)
                .Include(s => s.Services)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (s == null) return null;

            return new SalonDto
            {
                Id = s.Id,
                Name = s.Name,
                CityId = s.CityId,
                City = s.CityRef != null ? s.CityRef.Name : string.Empty,
                Address = s.Address,
                Phone = s.Phone,
                PostalCode = s.PostalCode,
                Website = s.Website,
                ImageIds = s.ImageIds,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                Services = s.Services.Where(serv => !serv.IsDeleted && serv.IsActive).Select(serv => serv.Name).ToList(),
                EmployeeCount = s.Barbers.Count(b => !b.IsDeleted),
                Rating = s.Rating,
                IsActive = s.IsActive
            };
        }

        public override async Task<SalonDto> Insert(SalonInsertRequest request)
        {
            ValidateSalonRequest(request.Name, request.Address, request.CityId, request.PostalCode, request.Phone);
            await EnsureCityExists(request.CityId);

            var entity = new Salon
            {
                Name = request.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Address = request.Address.Trim(),
                CityId = request.CityId,
                PostalCode = request.PostalCode.Trim(),
                Phone = request.Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                Website = string.IsNullOrWhiteSpace(request.Website) ? null : request.Website.Trim(),
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                BusinessHours = request.BusinessHours,
                Amenities = request.Amenities,
                SocialMedia = request.SocialMedia,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Salons.Add(entity);
            await _context.SaveChangesAsync();
            return await GetById(entity.Id);
        }

        public override async Task<SalonDto> Update(int id, SalonUpdateRequest request)
        {
            var entity = await _context.Salons.FindAsync(id);
            if (entity == null)
                throw new NotFoundException("Salon not found");

            ValidateSalonRequest(request.Name, request.Address, request.CityId, request.PostalCode, request.Phone);
            await EnsureCityExists(request.CityId);


            entity.Name = request.Name.Trim();
            entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            entity.Address = request.Address.Trim();
            entity.CityId = request.CityId;
            entity.PostalCode = request.PostalCode.Trim();
            entity.Phone = request.Phone.Trim();
            entity.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
            entity.Website = string.IsNullOrWhiteSpace(request.Website) ? null : request.Website.Trim();
            entity.Latitude = request.Latitude;
            entity.Longitude = request.Longitude;
            entity.BusinessHours = request.BusinessHours;
            entity.Amenities = request.Amenities;
            entity.SocialMedia = request.SocialMedia;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetById(id);
        }

        public async Task<SalonDto> ToggleStatusAsync(int id)
        {
            var salon = await _context.Salons.FindAsync(id);
            if (salon == null) throw new NotFoundException("Salon not found");

            salon.IsActive = !salon.IsActive;
            await _context.SaveChangesAsync();
            return await GetById(id);
        }

        public async Task<bool> CanBarberUpdateSalonAsync(int userId, int salonId)
        {
            var barber = await _context.Barbers
                .FirstOrDefaultAsync(b => b.UserId == userId && !b.IsDeleted);
            return barber != null && barber.SalonId == salonId;
        }

        public async Task<SalonDto> UpdateSalonImageAsync(int salonId, string imageId)
        {
            var salon = await _context.Salons.FindAsync(salonId);
            if (salon == null) throw new NotFoundException("Salon not found");

            var imageIds = string.IsNullOrEmpty(salon.ImageIds)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(salon.ImageIds) ?? new List<string>();

            imageIds.Add(imageId);
            salon.ImageIds = JsonSerializer.Serialize(imageIds);

            await _context.SaveChangesAsync();
            return await GetById(salonId);
        }

        private static void ValidateSalonRequest(string name, string address, int cityId, string postalCode, string phone)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(address) || cityId <= 0
                || string.IsNullOrWhiteSpace(postalCode))
            {
                throw new BusinessException("Obavezna polja salona nisu validna.");
            }

            if (string.IsNullOrWhiteSpace(phone) || phone.Length < 6)
            {
                throw new BusinessException("Telefon salona nije validan.");
            }
        }

        private async Task EnsureCityExists(int cityId)
        {
            var exists = await _context.Cities.AnyAsync(c => c.Id == cityId);
            if (!exists)
            {
                throw new BusinessException("Odabrani grad nije validan.");
            }
        }
    }
}
