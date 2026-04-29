namespace ŠišAppApi.Constants;

public static class AppRoles
{
    public const string User = "User";
    public const string Barber = "Barber";
    public const string Admin = "Admin";

    public const string AdminOrBarber = $"{Admin},{Barber}";
}
