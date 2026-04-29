using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Models.Requests;
using ŠišAppApi.Models.SearchObjects;

namespace ŠišAppApi.Services.Interfaces;

public interface IServiceCategoryService : ICRUDService<ServiceCategoryDto, ServiceCategorySearchObject, ServiceCategoryInsertRequest, ServiceCategoryUpdateRequest>
{
}
