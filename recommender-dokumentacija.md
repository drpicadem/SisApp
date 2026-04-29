
ŠišApp uses content-based recommendation logic. There is no separate ML training pipeline or model file.  
Recommendations are calculated directly from data users already generate through appointments and reviews.

- Backend service: `backend/ŠišAppApi/Services/RecommendationService.cs` (`GetRecommendations`)
- API: `GET /api/Recommendations`
- `top` parameter: if `< 1`, backend sets it to `10`; hard limit is `50`

## Where System Reads Data From

System uses only "real" records for current user:

- `Appointments`: user-owned, not deleted, excluding `Cancelled`
- `Reviews`: user-owned, not deleted
- `Services`: active and not deleted
- `Salons`: candidate service salon must be active and not deleted
- scoring-related relations: salon/city, barber specialties, appointment history

In practice: system builds preference profile from user behavior, then compares profile with currently available services.

## When User Has No History (Cold Start)

If user has no relevant appointments yet:

- fallback `GetPopularRecommendations` is used
- priority goes to higher `Salon.Rating`
- then to services marked as `IsPopular`
- `Reason` is set to popular fallback label
- `RelevanceScore` is tied to salon rating (`Salon.Rating * 10`)

This gives new users meaningful first results instead of an empty recommendations list.

## How User Profile Is Built

Signals extracted from user history:

- service-name keywords (`ServiceKeywords`)
- cities user most often chooses (`CityFrequency`)
- repeated visits to same salon (`SalonVisits`)
- repeated selection of same barber (`BarberVisits`)
- typical appointment price and duration

From reviews with rating `>= 4`, system also extracts positive keywords from related services (`PositiveServiceKeywords`).

## How Candidate Gets Score

Each candidate service (not already booked by user) accumulates points from multiple sources:

- salon quality based on user reviews (weighted avg + trend)
- prior visits to same salon
- keyword match with appointment history
- keyword match with positive reviews
- barber affinity via `BarberSpecialties`
- overall salon rating (`Salon.Rating`)
- city match (`CityFrequency`)
- price and duration similarity vs user averages

Implementation weighting details:

- poor past experiences can reduce score (for example very low avg)
- good experiences and upward quality trend increase score
- caps exist so one signal cannot dominate all others
- candidate enters result list only if `score > 0`

## Why User Gets This Recommendation

`Reason` is not generic text; it is built from actual scoring hits.
Backend keeps first 2 strongest reasons (`reasons.Take(2)`), for example:

- "Similar services you used before"
- "Similar price range"
- "In your city (...)"

If signals are insufficient, fallback is generic "Recommended" label.
When stored in DB, reason text is trimmed to max 100 characters.

## What API Returns

`GET /api/Recommendations` returns `List<RecommendationDto>` with:

- service data (`ServiceId`, `ServiceName`, `ServiceDescription`, `Price`, `DurationMinutes`)
- salon data (`SalonId`, `SalonName`, `SalonCity`, `SalonRating`, `SalonImageIds`)
- explanation (`Reason`)
- score value (`RelevanceScore`)

## Database Persistence

After each calculation:

- old user recommendations are deleted from `Recommendations`
- new recommendation snapshot is inserted (`RecommendedServiceId`, `Reason`, `RelevanceScore`, `CreatedAt`)

This ensures frontend always receives fresh set instead of mixed old/new recommendations.

## Project Note

In this project, recommender is designed to be:

- fast (on-demand calculation, no additional training service)
- explainable (`Reason` always accompanies recommendation)
- robust for both cold-start and users with rich history
