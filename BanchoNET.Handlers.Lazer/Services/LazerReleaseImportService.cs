using System.Text.Json;
using BanchoNET.Core.Utils;
using Microsoft.Extensions.Hosting;

namespace BanchoNET.Handlers.Lazer.Services;

public sealed class LazerReleaseImportService(
    ILogger logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken
    ) {
        var inbox = AppSettings.LazerReleaseInbox;
        if (string.IsNullOrWhiteSpace(inbox)) return;

        Directory.CreateDirectory(inbox);
        Directory.CreateDirectory(PublishedLazerReleases.Root);

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var directory in Directory.EnumerateDirectories(inbox, "*.ready").Order())
            {
                if (File.Exists(Path.Combine(directory, "result.json"))) continue;

                try
                {
                    await LazerReleasePublisher.Import(
                        directory,
                        PublishedLazerReleases.Root,
                        AppSettings.LazerName,
                        stoppingToken
                    );
                    await WriteResult(directory, true, "Published", stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError("Release import failed", ex, caller: nameof(LazerReleaseImportService));

                    try
                    {
                        await WriteResult(directory, false, ex.Message, stoppingToken);
                    }
                    catch (IOException resultException)
                    {
                        logger.LogError(
                            "Unable to write release import result",
                            resultException,
                            caller: nameof(LazerReleaseImportService)
                        );
                    }
                    catch (UnauthorizedAccessException resultException)
                    {
                        logger.LogError(
                            "Unable to write release import result",
                            resultException,
                            caller: nameof(LazerReleaseImportService)
                        );
                    }
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private static async Task WriteResult(
        string directory,
        bool success,
        string message,
        CancellationToken ct
    ) {
        var temporary = Path.Combine(directory, "result.tmp");
        await File.WriteAllTextAsync(
            temporary,
            JsonSerializer.Serialize(new { Success = success, Message = message }),
            ct
        );
        File.Move(temporary, Path.Combine(directory, "result.json"), true);
    }
}