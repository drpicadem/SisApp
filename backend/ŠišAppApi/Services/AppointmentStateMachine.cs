using ŠišAppApi.Constants;
using ŠišAppApi.Filters;

namespace ŠišAppApi.Services;

public static class AppointmentStateMachine
{
    public static bool CanTransition(string? fromStatus, string toStatus)
    {
        if (string.IsNullOrWhiteSpace(fromStatus) || string.IsNullOrWhiteSpace(toStatus))
            return false;

        if (string.Equals(fromStatus, toStatus, StringComparison.OrdinalIgnoreCase))
            return true;

        return fromStatus switch
        {
            AppointmentStatuses.Pending => toStatus is AppointmentStatuses.Confirmed or AppointmentStatuses.Cancelled or AppointmentStatuses.Active,
            AppointmentStatuses.Confirmed => toStatus is AppointmentStatuses.Cancelled or AppointmentStatuses.Active,
            AppointmentStatuses.Active => toStatus is AppointmentStatuses.Completed,
            AppointmentStatuses.Cancelled => false,
            AppointmentStatuses.Completed => false,
            _ => false
        };
    }

    public static void EnsureTransition(string? fromStatus, string toStatus, string? errorMessage = null)
    {
        if (CanTransition(fromStatus, toStatus))
            return;

        throw new UserException(errorMessage ?? $"Nedozvoljen prelaz statusa: {fromStatus} -> {toStatus}.");
    }

    public static bool CanCancel(string? status)
    {
        return status == AppointmentStatuses.Pending || status == AppointmentStatuses.Confirmed;
    }
}
