namespace CensudexOrders.Services.Interfaces;

/// <summary>
/// Service for loading and rendering email templates
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Renders an email template with the provided data
    /// </summary>
    /// <param name="templateName">Name of the template file (without .html extension)</param>
    /// <param name="data">Dictionary of placeholder values</param>
    /// <returns>Rendered HTML email</returns>
    Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> data);

    /// <summary>
    /// Renders the base email template with custom content
    /// </summary>
    /// <param name="title">Email title</param>
    /// <param name="preheader">Preheader text (shown in email preview)</param>
    /// <param name="content">HTML content to inject</param>
    /// <returns>Complete HTML email</returns>
    Task<string> RenderBaseTemplateAsync(string title, string preheader, string content);
}
