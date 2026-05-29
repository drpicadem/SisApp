namespace ŠišAppApi.Constants;

public static class AppointmentPaymentStatuses
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string RefundRequired = "RefundRequired";
}

public static class PaymentStatuses
{
    public const string Pending = "Pending";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string Expired = "Expired";
}

public static class PaymentMethods
{
    public const string Stripe = "Stripe";
    public const string PayPal = "PayPal";
}
