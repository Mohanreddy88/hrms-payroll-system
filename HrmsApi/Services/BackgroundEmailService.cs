using System.Threading.Channels;

namespace HrmsApi.Services;

/// <summary>
/// Represents an email job queued for background delivery
/// </summary>
public class EmailJob
{
    public string   ToEmail        { get; set; } = string.Empty;
    public string   Subject        { get; set; } = string.Empty;
    public string   Body           { get; set; } = string.Empty;
    public byte[]?  AttachmentData { get; set; }
    public string?  AttachmentName { get; set; }
}

/// <summary>
/// Interface to enqueue email jobs from controllers
/// </summary>
public interface IEmailQueue
{
    void Enqueue(EmailJob job);
}

/// <summary>
/// Hosted background service that drains the email queue.
/// Uses a Channel<T> — thread-safe, no locks needed.
/// Lives for the lifetime of the application (singleton) so it is
/// never disposed mid-send, regardless of HTTP request lifetime.
/// </summary>
public class BackgroundEmailService : BackgroundService, IEmailQueue
{
    private readonly Channel<EmailJob>          _channel;
    private readonly IServiceScopeFactory       _scopeFactory;
    private readonly ILogger<BackgroundEmailService> _logger;

    public BackgroundEmailService(IServiceScopeFactory scopeFactory, ILogger<BackgroundEmailService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
        // Unbounded channel — emails queue up without blocking the caller
        _channel = Channel.CreateUnbounded<EmailJob>(new UnboundedChannelOptions
        {
            SingleReader = true,   // only this service reads
            SingleWriter = false   // many controllers can write
        });
    }

    /// <summary>
    /// Enqueue an email job — returns immediately (never blocks the HTTP request)
    /// </summary>
    public void Enqueue(EmailJob job)
    {
        _channel.Writer.TryWrite(job);
        _logger.LogInformation("[EmailQueue] Queued email to {ToEmail} subject: {Subject}", job.ToEmail, job.Subject);
    }

    /// <summary>
    /// Background loop — reads jobs from the channel and sends them one by one
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[EmailQueue] Background email service started");

        await foreach (var job in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("[EmailQueue] Sending email to {ToEmail}...", job.ToEmail);

                // Create a fresh DI scope for each send — IEmailService is scoped
                using var scope     = _scopeFactory.CreateScope();
                var emailService    = scope.ServiceProvider.GetRequiredService<IEmailService>();

                if (job.AttachmentData != null && job.AttachmentData.Length > 0)
                {
                    await emailService.SendEmailWithAttachmentAsync(
                        job.ToEmail, job.Subject, job.Body,
                        job.AttachmentData, job.AttachmentName!);
                }
                else
                {
                    await emailService.SendEmailAsync(job.ToEmail, job.Subject, job.Body);
                }

                _logger.LogInformation("[EmailQueue] Email sent successfully to {ToEmail}", job.ToEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EmailQueue] Failed to send email to {ToEmail}", job.ToEmail);
                // Continue processing remaining jobs — don't crash the service
            }
        }

        _logger.LogInformation("[EmailQueue] Background email service stopped");
    }
}
