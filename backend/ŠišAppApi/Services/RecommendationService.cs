using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Data;
using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;

namespace ŠišAppApi.Services;

public class RecommendationService
{
    private readonly ApplicationDbContext _context;

    public RecommendationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RecommendationDto>> GetRecommendations(int userId, int maxResults = 10)
    {
        var userAppointments = await _context.Appointments
            .Include(a => a.Service)
            .Include(a => a.Salon)
            .Where(a => a.UserId == userId && !a.IsDeleted && a.Status != "Cancelled")
            .ToListAsync();

        var userReviews = await _context.Reviews
            .Where(r => r.UserId == userId && !r.IsDeleted)
            .ToDictionaryAsync(r => r.AppointmentId, r => r.Rating);
        
        if (!userAppointments.Any())
        {
            return await GetPopularRecommendations(maxResults);
        }

        var profile = BuildUserProfile(userAppointments, userReviews);

        var usedServiceIds = userAppointments.Select(a => a.ServiceId).Distinct().ToHashSet();

        var candidateServices = await _context.Services
            .Include(s => s.Salon)
            .Where(s => !s.IsDeleted && s.IsActive
                        && s.Salon != null && s.Salon.IsActive && !s.Salon.IsDeleted
                        && !usedServiceIds.Contains(s.Id))
            .ToListAsync();

        var scored = new List<(Service service, float score, string reason)>();

        foreach (var service in candidateServices)
        {
            var (score, reason) = ScoreService(service, profile);
            if (score > 0)
            {
                scored.Add((service, score, reason));
            }
        }

        var topResults = scored
            .OrderByDescending(x => x.score)
            .Take(maxResults)
            .ToList();

        await PersistRecommendations(userId, topResults);

        return topResults.Select(x => new RecommendationDto
        {
            ServiceId = x.service.Id,
            ServiceName = x.service.Name,
            ServiceDescription = x.service.Description,
            Price = x.service.Price,
            DurationMinutes = x.service.DurationMinutes,
            SalonId = x.service.SalonId,
            SalonName = x.service.Salon!.Name,
            SalonCity = x.service.Salon.City,
            SalonRating = x.service.Salon.Rating,
            SalonImageIds = x.service.Salon.ImageIds,
            Reason = x.reason,
            RelevanceScore = x.score
        }).ToList();
    }


    private UserProfile BuildUserProfile(List<Appointment> appointments, Dictionary<int, int> reviews)
    {
        var profile = new UserProfile();

        foreach (var apt in appointments)
        {
            if (apt.Service == null) continue;

            var words = apt.Service.Name.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var w in words)
                profile.ServiceKeywords.TryAdd(w, 0);
            foreach (var w in words)
                profile.ServiceKeywords[w]++;

            profile.TotalPrice += apt.Service.Price;
            profile.PriceCount++;

            profile.TotalDuration += apt.Service.DurationMinutes;
            profile.DurationCount++;

            if (apt.Salon != null)
            {
                var city = apt.Salon.City.ToLower();
                profile.CityFrequency.TryAdd(city, 0);
                profile.CityFrequency[city]++;
            }

            profile.SalonVisits.TryAdd(apt.SalonId, 0);
            profile.SalonVisits[apt.SalonId]++;

            if (reviews.TryGetValue(apt.Id, out var rating))
            {
                profile.AvgRating += rating;
                profile.RatingCount++;
            }
        }

        return profile;
    }

    private (float score, string reason) ScoreService(Service service, UserProfile profile)
    {
        float score = 0f;
        var reasons = new List<string>();

        var serviceWords = service.Name.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int keywordHits = 0;
        foreach (var word in serviceWords)
        {
            if (profile.ServiceKeywords.TryGetValue(word, out var freq))
            {
                keywordHits += freq;
            }
        }
        if (keywordHits > 0)
        {
            score += Math.Min(keywordHits * 10f, 40f);
            reasons.Add("Slične usluge koje ste koristili");
        }

        if (service.Salon != null && service.Salon.Rating >= 4.0)
        {
            score += (float)(service.Salon.Rating * 4);
            reasons.Add($"Visoko ocijenjen salon ({service.Salon.Rating:F1}★)");
        }

        if (service.Salon != null)
        {
            var salonCity = service.Salon.City.ToLower();
            if (profile.CityFrequency.TryGetValue(salonCity, out var cityFreq))
            {
                score += Math.Min(cityFreq * 5f, 20f);
                reasons.Add($"U vašem gradu ({service.Salon.City})");
            }
        }

        if (profile.PriceCount > 0)
        {
            var avgPrice = profile.TotalPrice / profile.PriceCount;
            var priceDiff = Math.Abs(service.Price - avgPrice);
            if (priceDiff <= avgPrice * 0.3m) 
            {
                score += 10f;
                reasons.Add("Sličan cjenovni rang");
            }
            else if (priceDiff <= avgPrice * 0.6m)
            {
                score += 5f;
            }
        }

        
        if (profile.DurationCount > 0)
        {
            var avgDuration = profile.TotalDuration / profile.DurationCount;
            var durationDiff = Math.Abs(service.DurationMinutes - avgDuration);
            if (durationDiff <= 15)
            {
                score += 10f;
                reasons.Add("Slično trajanje tretmana");
            }
            else if (durationDiff <= 30)
            {
                score += 5f;
            }
        }

        var reason = reasons.Any() ? string.Join(", ", reasons.Take(2)) : "Preporučeno";
        return (score, reason);
    }

    private async Task<List<RecommendationDto>> GetPopularRecommendations(int maxResults)
    {
        var popular = await _context.Services
            .Include(s => s.Salon)
            .Where(s => !s.IsDeleted && s.IsActive
                        && s.Salon != null && s.Salon.IsActive && !s.Salon.IsDeleted)
            .OrderByDescending(s => s.Salon!.Rating)
            .ThenByDescending(s => s.IsPopular)
            .Take(maxResults)
            .Select(s => new RecommendationDto
            {
                ServiceId = s.Id,
                ServiceName = s.Name,
                ServiceDescription = s.Description,
                Price = s.Price,
                DurationMinutes = s.DurationMinutes,
                SalonId = s.SalonId,
                SalonName = s.Salon!.Name,
                SalonCity = s.Salon.City,
                SalonRating = s.Salon.Rating,
                SalonImageIds = s.Salon.ImageIds,
                Reason = "Popularno",
                RelevanceScore = (float)s.Salon.Rating * 10
            })
            .ToListAsync();

        return popular;
    }

    private async Task PersistRecommendations(int userId, List<(Service service, float score, string reason)> results)
    {
        var old = await _context.Recommendations
            .Where(r => r.UserId == userId)
            .ToListAsync();
        _context.Recommendations.RemoveRange(old);
        foreach (var (service, score, reason) in results)
        {
            _context.Recommendations.Add(new Recommendation
            {
                UserId = userId,
                RecommendedServiceId = service.Id,
                Reason = reason.Length > 100 ? reason[..100] : reason,
                RelevanceScore = score,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
    }



    private class UserProfile
    {
        public Dictionary<string, int> ServiceKeywords { get; set; } = new();
        public Dictionary<string, int> CityFrequency { get; set; } = new();
        public Dictionary<int, int> SalonVisits { get; set; } = new();
        public decimal TotalPrice { get; set; }
        public int PriceCount { get; set; }
        public int TotalDuration { get; set; }
        public int DurationCount { get; set; }
        public int AvgRating { get; set; }
        public int RatingCount { get; set; }
    }
}
