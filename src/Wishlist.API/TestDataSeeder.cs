using Wishlist.Application.UseCases.TestData;
using Wishlist.Infrastructure.Services;

namespace Wishlist.API;

/// <summary>
/// Test environments only: seeds test data once the app has started. Errors are
/// logged, never thrown, so a sleeping or unreachable database can't stop the app.
/// </summary>
public static class TestDataSeeder
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(TestDataSeeder));
        var inbox = services.GetRequiredService<EmailAllowList>().FirstPlainAddress;
        if (inbox is null)
        {
            logger.LogWarning("Test environment: no test data seeded; Smtp:AllowedRecipients has no plain address (name@domain) for the test users' +tag addresses.");
            return;
        }

        try
        {
            await using var scope = services.CreateAsyncScope();
            var created = await scope.ServiceProvider.GetRequiredService<SeedTestDataUseCase>().ExecuteAsync(inbox, ct);
            logger.LogInformation(created
                ? "Test environment: seeded test users, circles and wishes."
                : "Test environment: test data already there; nothing seeded.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Test environment: seeding test data failed.");
        }
    }
}
