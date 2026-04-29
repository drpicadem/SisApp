using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Constants;
using ŠišAppApi.Data;
using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;

using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Services;

public class RecommendationService : IRecommendationService
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
            .Include(a => a.Barber)
            .Include(a => a.Salon)
                .ThenInclude(s => s.CityRef)
            .Where(a => a.UserId == userId && !a.IsDeleted && a.Status != AppointmentStatuses.Cancelled)
            .ToListAsync();

        var userReviewsQuery = await _context.Reviews
            .Include(r => r.Appointment)
                .ThenInclude(a => a.Service)
            .Where(r => r.UserId == userId && !r.IsDeleted)
            .ToListAsync();

        if (!userAppointments.Any())
        {
            return await GetPopularRecommendations(maxResults);
        }

        var profile = BuildUserProfile(userAppointments);
        ApplyPositiveReviewSignals(profile, userReviewsQuery);
        var reviewMetricsPerSalon = CalculateSalonReviewMetrics(userReviewsQuery);

        var usedServiceIds = userAppointments.Select(a => a.ServiceId).Distinct().ToHashSet();

        var candidateServices = await _context.Services
            .Include(s => s.Salon)
                .ThenInclude(salon => salon.CityRef)
            .Include(s => s.BarberSpecialties)
            .Where(s => !s.IsDeleted && s.IsActive
                        && s.Salon != null && s.Salon.IsActive && !s.Salon.IsDeleted
                        && !usedServiceIds.Contains(s.Id))
            .ToListAsync();

        var scored = new List<(Service service, float score, string reason)>();

        foreach (var service in candidateServices)
        {
            var (score, reason) = ScoreService(service, profile, reviewMetricsPerSalon);
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
            SalonCity = x.service.Salon.CityRef != null ? x.service.Salon.CityRef.Name : string.Empty,
            SalonRating = x.service.Salon.Rating,
            SalonImageIds = x.service.Salon.ImageIds,
            Reason = x.reason,
            RelevanceScore = x.score
        }).ToList();
    }

    private Dictionary<int, SalonReviewMetrics> CalculateSalonReviewMetrics(List<Review> reviews)
    {
        var metrics = new Dictionary<int, SalonReviewMetrics>();
        var grouped = reviews.Where(r => r.Appointment != null).GroupBy(r => r.Appointment!.SalonId);

        var now = DateTime.UtcNow;

        foreach (var group in grouped)
        {
            var salonReviews = group.OrderBy(r => r.CreatedAt).ToList();

            float totalWeightedRating = 0;
            float totalWeights = 0;

            foreach (var r in salonReviews)
            {
                var ageMonths = (now - r.CreatedAt).TotalDays / 30.0;
                float weight = 1.0f;

                if (ageMonths > 12) weight = 0.4f;
                else if (ageMonths > 6) weight = 0.7f;

                totalWeightedRating += r.Rating * weight;
                totalWeights += weight;
            }

            float weightedAvg = totalWeights > 0 ? totalWeightedRating / totalWeights : 0;

            bool isTrendingUp = false;
            bool isTrendingDown = false;
            if (salonReviews.Count >= 2)
            {
                var firstRating = salonReviews.First().Rating;
                var lastRating = salonReviews.Last().Rating;
                if (lastRating > firstRating) isTrendingUp = true;
                if (lastRating < firstRating) isTrendingDown = true;
            }

            metrics[group.Key] = new SalonReviewMetrics
            {
                WeightedAverageScore = weightedAvg,
                IsTrendingUp = isTrendingUp,
                IsTrendingDown = isTrendingDown
            };
        }

        return metrics;
    }

    private UserProfile BuildUserProfile(List<Appointment> appointments)
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
                var city = apt.Salon.CityRef != null ? apt.Salon.CityRef.Name.ToLower() : string.Empty;
                if (!string.IsNullOrWhiteSpace(city))
                {
                    profile.CityFrequency.TryAdd(city, 0);
                    profile.CityFrequency[city]++;
                }
            }

            profile.SalonVisits.TryAdd(apt.SalonId, 0);
            profile.SalonVisits[apt.SalonId]++;

            profile.BarberVisits.TryAdd(apt.BarberId, 0);
            profile.BarberVisits[apt.BarberId]++;
        }

        return profile;
    }

    private (float score, string reason) ScoreService(
        Service service,
        UserProfile profile,
        Dictionary<int, SalonReviewMetrics> reviewMetrics)
    {
        float score = 0f;
        var reasons = new List<string>();

       if (reviewMetrics.TryGetValue(service.SalonId, out var metrics))
        {
            if (metrics.WeightedAverageScore >= 4.0f)
            {
                score += 15f;
                reasons.Add("Vaš omiljeni salon");
            }
            else if (metrics.WeightedAverageScore <= 2.5f)
            {
                score -= 20f;
            }

            if (metrics.IsTrendingUp)
            {
                score += 10f;
                if (!reasons.Contains("Vaš omiljeni salon"))
                {
                    reasons.Add("Rastući kvalitet usluge");
                }
            }

            if (metrics.IsTrendingDown)
            {
                score -= 8f;
            }
        }

        if (profile.SalonVisits.TryGetValue(service.SalonId, out var visitCount) && visitCount > 0)
        {
            score += Math.Min(visitCount * 4f, 16f);
            reasons.Add("Često birate ovaj salon");
        }

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

        var positiveKeywordHits = 0;
        foreach (var word in serviceWords)
        {
            if (profile.PositiveServiceKeywords.TryGetValue(word, out var freq))
            {
                positiveKeywordHits += freq;
            }
        }
        if (positiveKeywordHits > 0)
        {
            score += Math.Min(positiveKeywordHits * 12f, 36f);
            reasons.Add("Pozitivno ste ocjenjivali slične usluge");
        }

        if (service.BarberSpecialties != null && service.BarberSpecialties.Count > 0)
        {
            var preferredBarberMatches = service.BarberSpecialties
                .Count(bs => profile.BarberVisits.ContainsKey(bs.BarberId));
            if (preferredBarberMatches > 0)
            {
                score += Math.Min(preferredBarberMatches * 8f, 16f);
                reasons.Add("Frizeri koje često birate nude ovu uslugu");
            }
        }

        if (service.Salon != null && service.Salon.Rating >= 4.0)
        {
            score += (float)(service.Salon.Rating * 4);
            reasons.Add($"Visoko ocijenjen salon ({service.Salon.Rating:F1}★)");
        }

        if (service.Salon != null)
        {
            var salonCity = service.Salon.CityRef != null ? service.Salon.CityRef.Name.ToLower() : string.Empty;
            if (profile.CityFrequency.TryGetValue(salonCity, out var cityFreq))
            {
                score += Math.Min(cityFreq * 5f, 20f);
                reasons.Add($"U vašem gradu ({service.Salon.CityRef?.Name})");
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

    private void ApplyPositiveReviewSignals(UserProfile profile, List<Review> reviews)
    {
        foreach (var review in reviews)
        {
            if (review.Rating < 4 || review.Appointment?.Service == null) continue;

            var words = review.Appointment.Service.Name
                .ToLower()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var w in words)
            {
                profile.PositiveServiceKeywords.TryAdd(w, 0);
                profile.PositiveServiceKeywords[w]++;
            }
        }
    }

    private async Task<List<RecommendationDto>> GetPopularRecommendations(int maxResults)
    {
        var popular = await _context.Services
            .Include(s => s.Salon)
                .ThenInclude(salon => salon.CityRef)
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
                SalonCity = s.Salon.CityRef != null ? s.Salon.CityRef.Name : string.Empty,
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
        public Dictionary<string, int> PositiveServiceKeywords { get; set; } = new();
        public Dictionary<string, int> CityFrequency { get; set; } = new();
        public Dictionary<int, int> SalonVisits { get; set; } = new();
        public Dictionary<int, int> BarberVisits { get; set; } = new();
        public decimal TotalPrice { get; set; }
        public int PriceCount { get; set; }
        public int TotalDuration { get; set; }
        public int DurationCount { get; set; }
    }

    private class SalonReviewMetrics
    {
        public float WeightedAverageScore { get; set; }
        public bool IsTrendingUp { get; set; }
        public bool IsTrendingDown { get; set; }
    }
}
