using ŠišAppApi.Models.DTOs;

namespace ŠišAppApi.Services.Interfaces
{
    public interface IReviewService
    {
        Task<ReviewDto> CreateReviewAsync(int userId, CreateReviewDto dto);
        Task<ReviewDto> UpdateReviewAsync(int id, int userId, CreateReviewDto dto);
        Task<IEnumerable<ReviewDto>> GetMyReviewsAsync(int userId, int page, int pageSize);
        Task<IEnumerable<ReviewDto>> GetMyBarberReviewsAsync(int userId, int page, int pageSize);
        Task<ReviewDto> RespondToReviewAsync(int id, int userId, string response);
        Task<IEnumerable<ReviewDto>> GetReviewsForBarberAsync(int barberId, int page, int pageSize);
        Task<IEnumerable<ReviewDto>> GetReviewsForSalonAsync(int salonId, int page, int pageSize);
        Task MarkAsHelpfulAsync(int id);
        Task VerifyReviewAsync(int id, int adminUserId, string? ipAddress, string? userAgent);
        Task HideReviewAsync(int id, int adminUserId, string reason, string? ipAddress, string? userAgent);
    }
}
