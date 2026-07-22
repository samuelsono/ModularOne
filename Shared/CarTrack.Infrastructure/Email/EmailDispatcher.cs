using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;

namespace CarTrack.Infrastructure.Email;

public interface IEmailDispatcher
{
    Task SendAsync(
        EmailRuntimeConfig config,
        string toAddress,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}

public sealed class EmailDispatcher(IHttpClientFactory httpClientFactory) : IEmailDispatcher
{
    public Task SendAsync(
        EmailRuntimeConfig config,
        string toAddress,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toAddress))
        {
            throw new ArgumentException("Recipient address is required.", nameof(toAddress));
        }

        return config.Provider switch
        {
            EmailProviderNames.SendGrid => SendViaSendGridAsync(config, toAddress, subject, htmlBody, cancellationToken),
            EmailProviderNames.Mailgun => SendViaMailgunAsync(config, toAddress, subject, htmlBody, cancellationToken),
            _ => SendViaSmtpAsync(config, toAddress, subject, htmlBody, cancellationToken),
        };
    }

    private static async Task SendViaSmtpAsync(
        EmailRuntimeConfig config,
        string toAddress,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.SmtpHost))
        {
            throw new InvalidOperationException("SMTP host is not configured.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(config.FromAddress, config.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(toAddress);

        using var client = new SmtpClient(config.SmtpHost, config.SmtpPort)
        {
            EnableSsl = config.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };

        if (!string.IsNullOrWhiteSpace(config.Username))
        {
            client.Credentials = new NetworkCredential(config.Username, config.Password);
        }

        await client.SendMailAsync(message, cancellationToken);
    }

    private async Task SendViaSendGridAsync(
        EmailRuntimeConfig config,
        string toAddress,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new InvalidOperationException("SendGrid API key is not configured.");
        }

        var payload = new
        {
            personalizations = new[]
            {
                new { to = new[] { new { email = toAddress } } },
            },
            from = new { email = config.FromAddress, name = config.FromName },
            subject,
            content = new[]
            {
                new { type = "text/html", value = htmlBody },
            },
        };

        var client = httpClientFactory.CreateClient(nameof(EmailDispatcher));
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(
            $"SendGrid returned {(int)response.StatusCode}: {(body.Length > 300 ? body[..300] : body)}");
    }

    private async Task SendViaMailgunAsync(
        EmailRuntimeConfig config,
        string toAddress,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new InvalidOperationException("Mailgun API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(config.MailgunDomain))
        {
            throw new InvalidOperationException("Mailgun domain is not configured.");
        }

        var domain = config.MailgunDomain.Trim();
        var client = httpClientFactory.CreateClient(nameof(EmailDispatcher));
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://api.mailgun.net/v3/{Uri.EscapeDataString(domain)}/messages");

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"api:{config.ApiKey}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        using var form = new MultipartFormDataContent
        {
            { new StringContent($"{config.FromName} <{config.FromAddress}>"), "from" },
            { new StringContent(toAddress), "to" },
            { new StringContent(subject), "subject" },
            { new StringContent(htmlBody), "html" },
        };
        request.Content = form;

        using var response = await client.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(
            $"Mailgun returned {(int)response.StatusCode}: {(body.Length > 300 ? body[..300] : body)}");
    }
}
