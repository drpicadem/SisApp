namespace ŠišAppApi.Services.Interfaces
{
    public interface IFavoriteService
    {
        Task<IEnumerable<int>> GetFavoriteSalonIdsAsync(int userId, int page, int pageSize);
        Task<bool> ToggleFavoriteSalonAsync(int userId, int salonId);
    }
}
