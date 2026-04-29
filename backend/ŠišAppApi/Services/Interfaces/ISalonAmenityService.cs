using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Models.Requests;
using ŠišAppApi.Models.SearchObjects;

namespace ŠišAppApi.Services.Interfaces;

public interface ISalonAmenityService : ICRUDService<SalonAmenityDto, SalonAmenitySearchObject, SalonAmenityInsertRequest, SalonAmenityUpdateRequest>
{
}
