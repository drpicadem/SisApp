using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Data;

namespace ŠišAppApi.Services;

public class RevokedTokenCleanupWorker : BackgroundService
{
    private const string RevokedJtiPrefix = "revoked-jti:";
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RevokedTokenCleanupWorker> _logger;

    public RevokedTokenCleanupWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<RevokedTokenCleanupWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredRevokedTokens(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while cleaning revoked JWT records.");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task CleanupExpiredRevokedTokens(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;

        var expiredRevokedJtis = await context.RefreshTokens
            .Where(rt =>
                !rt.IsDeleted &&
                rt.Token.StartsWith(RevokedJtiPrefix) &&
                rt.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        if (expiredRevokedJtis.Count == 0)
        {
            return;
        }

        foreach (var token in expiredRevokedJtis)
        {
            token.IsDeleted = true;
            token.DeletedAt = now;
            token.UpdatedAt = now;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
