using CensudexOrders.Services.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Configuration;
using CensudexOrders.Exceptions;

namespace CensudexOrders.Services;

public class SendGridService : ISendGridService
{
    private readonly SendGridClient _client;
    private readonly string _fromEmail;
    private readonly IEmailTemplateService _templateService;
    private readonly ILogger<SendGridService> _logger;

    public SendGridService(
        IConfiguration configuration,
        IEmailTemplateService templateService,
        ILogger<SendGridService> logger)
    {
        var apiKey = configuration["SendGrid:ApiKey"]
            ?? throw new InternalServerException("SendGrid API key is not configured.");

        _fromEmail = configuration["SendGrid:FromEmail"]
            ?? throw new InternalServerException("SendGrid FromEmail is not configured.");

        _client = new SendGridClient(apiKey);
        _templateService = templateService;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            // Use simple template for backward compatibility
            var htmlContent = await _templateService.RenderBaseTemplateAsync(
                subject,
                body.Length > 100 ? body.Substring(0, 100) + "..." : body,
                $"<p style='font-family: Helvetica, sans-serif; font-size: 16px; font-weight: normal; margin: 0; margin-bottom: 16px;'>{body}</p>"
            );

            await SendEmailInternalAsync(toEmail, subject, body, htmlContent);
        }
        catch (Exception ex) when (ex is not InternalServerException)
        {
            _logger.LogError(ex, "Error sending email to {ToEmail}", toEmail);
            throw new InternalServerException($"Error sending email: {ex.Message}");
        }
    }

    public async Task SendTemplatedEmailAsync(string toEmail, string subject, string templateName, Dictionary<string, string> templateData)
    {
        try
        {
            // Render the template with data
            var htmlContent = await _templateService.RenderTemplateAsync(templateName, templateData);

            // Get plain text version (simplified)
            var plainText = $"{subject}\n\n{string.Join("\n", templateData.Values)}";

            await SendEmailInternalAsync(toEmail, subject, plainText, htmlContent);
        }
        catch (Exception ex) when (ex is not InternalServerException)
        {
            _logger.LogError(ex, "Error sending templated email to {ToEmail} using template {TemplateName}", toEmail, templateName);
            throw new InternalServerException($"Error sending templated email: {ex.Message}");
        }
    }

    public async Task SendOrderConfirmationAsync(string toEmail, string customerName, int orderNumber, string orderDate, string orderStatus, decimal totalAmount)
    {
        var subject = $"Order Confirmation - Order #{orderNumber}";

        var templateData = new Dictionary<string, string>
        {
            { "EmailTitle", "Order Confirmation" },
            { "PreheaderText", $"Your order #{orderNumber} has been confirmed!" },
            { "CustomerName", customerName },
            { "OrderNumber", orderNumber.ToString() },
            { "OrderDate", orderDate },
            { "OrderStatus", orderStatus },
            { "TotalAmount", totalAmount.ToString("F2") }
        };

        await SendTemplatedEmailAsync(toEmail, subject, "OrderConfirmation", templateData);
    }

    public async Task SendOrderStatusUpdateAsync(string toEmail, string customerName, int orderNumber, string newStatus)
    {
        var subject = $"Order Status Update - Order #{orderNumber}";

        // Format status for display
        var formattedStatus = FormatOrderStatus(newStatus);

        var templateData = new Dictionary<string, string>
        {
            { "EmailTitle", "Order Status Update" },
            { "PreheaderText", $"Order #{orderNumber} status: {formattedStatus}" },
            { "CustomerName", customerName },
            { "OrderNumber", orderNumber.ToString() },
            { "NewStatus", formattedStatus }
        };

        await SendTemplatedEmailAsync(toEmail, subject, "OrderStatusUpdate", templateData);
    }

    public async Task SendOrderCancellationAsync(string toEmail, string customerName, int orderNumber, string cancellationReason)
    {
        var subject = $"Order Cancelled - Order #{orderNumber}";

        var templateData = new Dictionary<string, string>
        {
            { "EmailTitle", "Order Cancelled" },
            { "PreheaderText", $"Order #{orderNumber} has been cancelled" },
            { "CustomerName", customerName },
            { "OrderNumber", orderNumber.ToString() },
            { "CancellationReason", cancellationReason }
        };

        await SendTemplatedEmailAsync(toEmail, subject, "OrderCancellation", templateData);
    }

    /// <summary>
    /// Internal method to send email through SendGrid
    /// </summary>
    private async Task SendEmailInternalAsync(string toEmail, string subject, string plainText, string htmlContent)
    {
        var from = new EmailAddress(_fromEmail, "Censudex Orders");
        var to = new EmailAddress(toEmail);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainText, htmlContent);

        var response = await _client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Body.ReadAsStringAsync();
            _logger.LogError("Failed to send email to {ToEmail}. Status: {StatusCode}, Response: {Response}",
                toEmail, response.StatusCode, responseBody);
            throw new InternalServerException($"Failed to send email. Status: {response.StatusCode}");
        }

        _logger.LogInformation("Email sent successfully to {ToEmail} with subject '{Subject}'", toEmail, subject);
    }

    /// <summary>
    /// Formats order status for display (capitalizes first letter)
    /// </summary>
    private static string FormatOrderStatus(string status)
    {
        return status switch
        {
            "pendiente" => "Pendiente",
            "en procesamiento" => "En Procesamiento",
            "enviado" => "Enviado",
            "entregado" => "Entregado",
            "cancelado" => "Cancelado",
            _ => status
        };
    }
}