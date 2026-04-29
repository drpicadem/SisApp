using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;

namespace ŠišAppApi.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ??
            Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ??
            TryReadFromDotEnv("DB_CONNECTION_STRING") ??
            throw new InvalidOperationException(
                "DB connection string nije pronađen. Postavi ConnectionStrings__DefaultConnection ili DB_CONNECTION_STRING.");

        optionsBuilder.UseSqlServer(connectionString);
        return new ApplicationDbContext(optionsBuilder.Options);
    }

    private static string? TryReadFromDotEnv(string key)
    {
        var cwd = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(cwd, ".env"),
            Path.Combine(cwd, "..", ".env"),
            Path.Combine(cwd, "..", "..", ".env")
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path))
                continue;

            foreach (var rawLine in File.ReadLines(path))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                    continue;

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                    continue;

                var currentKey = line[..separatorIndex].Trim();
                if (!string.Equals(currentKey, key, StringComparison.Ordinal))
                    continue;

                var value = line[(separatorIndex + 1)..].Trim();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }

        return null;
    }
}
