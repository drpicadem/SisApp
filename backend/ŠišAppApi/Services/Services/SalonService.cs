using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ŠišAppApi.Data;
using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Models.Requests;
using ŠišAppApi.Models.SearchObjects;
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
                .Include(s => s.Barbers)
                .Include(s => s.Services).AsQueryable();

            if (search != null)
            {
                if (!string.IsNullOrEmpty(search.Name))
                {
                    query = query.Where(s => s.Name.Contains(search.Name));
                }
                if (!string.IsNullOrEmpty(search.City))
                {
                    query = query.Where(s => s.City == search.City);
                }
                if (search.IsActive.HasValue)
                {
                    query = query.Where(s => s.IsActive == search.IsActive.Value);
                }
            }

            var list = await query.Select(s => new SalonDto
            {
                Id = s.Id,
                Name = s.Name,
                City = s.City,
                Address = s.Address,
                Phone = s.Phone,
                PostalCode = s.PostalCode,
                Country = s.Country,
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
                .Include(s => s.Barbers)
                .Include(s => s.Services)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (s == null) return null;

            return new SalonDto
            {
                Id = s.Id,
                Name = s.Name,
                City = s.City,
                Address = s.Address,
                Phone = s.Phone,
                PostalCode = s.PostalCode,
                Country = s.Country,
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

        public async Task<SalonDto> ToggleStatusAsync(int id)
        {
            var salon = await _context.Salons.FindAsync(id);
            if (salon == null) throw new Exception("Salon not found");

            salon.IsActive = !salon.IsActive;
            await _context.SaveChangesAsync();
            return await GetById(id);
        }

        public async Task<SalonDto> UpdateSalonImageAsync(int salonId, string imageId)
        {
            var salon = await _context.Salons.FindAsync(salonId);
            if (salon == null) throw new Exception("Salon not found");

            var imageIds = string.IsNullOrEmpty(salon.ImageIds) 
                ? new List<string>() 
                : JsonSerializer.Deserialize<List<string>>(salon.ImageIds) ?? new List<string>();
            
            imageIds.Add(imageId);
            salon.ImageIds = JsonSerializer.Serialize(imageIds);
            
            await _context.SaveChangesAsync();
            return await GetById(salonId);
        }
    }
}
