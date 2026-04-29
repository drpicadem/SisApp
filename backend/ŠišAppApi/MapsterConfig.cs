using Mapster;
using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;

namespace ŠišAppApi;

public class MapsterConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ServiceCategory, ServiceCategoryDto>()
            .Map(dest => dest.ParentCategoryName, src => src.ParentCategory != null ? src.ParentCategory.Name : null);
    }
}
