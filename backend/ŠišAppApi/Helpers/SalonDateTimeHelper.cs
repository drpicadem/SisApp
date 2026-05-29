namespace ŠišAppApi.Helpers;


public static class SalonDateTimeHelper
{
    public const string TimeZoneId = "Europe/Sarajevo";
    public const string FallbackTimeZoneId = "Central European Standard Time";

    public static TimeZoneInfo GetSalonTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId); }
        catch
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(FallbackTimeZoneId); }
            catch { return TimeZoneInfo.Utc; }
        }
    }

   
    public static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    public static DateTime ToSalonLocal(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(NormalizeToUtc(utc), GetSalonTimeZone());

    public static DateTime SalonLocalToUtc(DateTime salonLocal)
    {
        var unspecified = DateTime.SpecifyKind(salonLocal, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, GetSalonTimeZone());
    }

    public static string FormatForDisplay(DateTime utc)
        => ToSalonLocal(utc).ToString("dd.MM.yyyy HH:mm");
}
