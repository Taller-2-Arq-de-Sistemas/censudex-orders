namespace CensudexOrders.Services.Interfaces;

public interface ISendGridService
{
    /// <summary>
    /// Sends a simple email with plain text body
    /// </summary>
    Task SendEmailAsync(string toEmail, string subject, string body);

    /// <summary>
    /// Sends an email using a template with data placeholders
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="templateName">Name of the template file (without .html)</param>
    /// <param name="templateData">Dictionary of placeholder values</param>
    Task SendTemplatedEmailAsync(string toEmail, string subject, string templateName, Dictionary<string, string> templateData);

    /// <summary>
    /// Sends order confirmation email
    /// </summary>
    Task SendOrderConfirmationAsync(string toEmail, string customerName, int orderNumber, string orderDate, string orderStatus, decimal totalAmount);

    /// <summary>
    /// Sends order status update email
    /// </summary>
    Task SendOrderStatusUpdateAsync(string toEmail, string customerName, int orderNumber, string newStatus);

    /// <summary>
    /// Sends order cancellation email
    /// </summary>
    Task SendOrderCancellationAsync(string toEmail, string customerName, int orderNumber, string cancellationReason);
}