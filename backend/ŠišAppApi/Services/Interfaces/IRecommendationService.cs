using ŠišAppApi.Models.DTOs;

namespace ŠišAppApi.Services.Interfaces
{
    public interface IRecommendationService
    {
        Task<List<RecommendationDto>> GetRecommendations(int userId, int maxResults = 10);
    }
}
