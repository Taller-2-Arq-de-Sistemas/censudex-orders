using CensudexOrders.Services.Interfaces;

namespace CensudexOrders.Services;

/// <summary>
/// Service for loading and rendering email templates
/// </summary>
public class EmailTemplateService : IEmailTemplateService
{
    private readonly ILogger<EmailTemplateService> _logger;
    private readonly string _templatesBasePath;
    private readonly string _emailsPath;

    public EmailTemplateService(ILogger<EmailTemplateService> logger)
    {
        _logger = logger;

        // Determine the correct path for templates
        _templatesBasePath = Path.Combine(Directory.GetCurrentDirectory(), "src", "Views", "Base");
        _emailsPath = Path.Combine(Directory.GetCurrentDirectory(), "src", "Views", "Emails");

        // Fallback for published/deployed scenarios
        if (!Directory.Exists(_templatesBasePath))
        {
            _templatesBasePath = Path.Combine(AppContext.BaseDirectory, "Views", "Base");
            _emailsPath = Path.Combine(AppContext.BaseDirectory, "Views", "Emails");
        }
    }

    public async Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> data)
    {
        try
        {
            // Load the specific email content template
            var templatePath = Path.Combine(_emailsPath, $"{templateName}.html");

            if (!File.Exists(templatePath))
            {
                _logger.LogWarning("Email template not found: {TemplatePath}", templatePath);
                throw new FileNotFoundException($"Email template '{templateName}' not found at {templatePath}");
            }

            var contentTemplate = await File.ReadAllTextAsync(templatePath);

            // Replace placeholders in content
            var renderedContent = ReplacePlaceholders(contentTemplate, data);

            // Get title and preheader from data or use defaults
            var title = data.ContainsKey("EmailTitle") ? data["EmailTitle"] : "Censudex Orders";
            var preheader = data.ContainsKey("PreheaderText") ? data["PreheaderText"] : "Important update about your order";

            // Wrap content in base template
            return await RenderBaseTemplateAsync(title, preheader, renderedContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering email template: {TemplateName}", templateName);
            throw;
        }
    }

    public async Task<string> RenderBaseTemplateAsync(string title, string preheader, string content)
    {
        try
        {
            // Load base template
            var baseTemplatePath = Path.Combine(_templatesBasePath, "EmailTemplate.html");

            if (!File.Exists(baseTemplatePath))
            {
                _logger.LogWarning("Base email template not found: {BaseTemplatePath}", baseTemplatePath);
                throw new FileNotFoundException($"Base email template not found at {baseTemplatePath}");
            }

            var baseTemplate = await File.ReadAllTextAsync(baseTemplatePath);

            // Replace base template placeholders
            var data = new Dictionary<string, string>
            {
                { "EmailTitle", title },
                { "PreheaderText", preheader },
                { "EmailContent", content },
                { "CurrentYear", DateTime.UtcNow.Year.ToString() }
            };

            return ReplacePlaceholders(baseTemplate, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering base email template");
            throw;
        }
    }

    /// <summary>
    /// Replaces all {{placeholder}} with corresponding values from data dictionary
    /// </summary>
    private string ReplacePlaceholders(string template, Dictionary<string, string> data)
    {
        var result = template;

        foreach (var kvp in data)
        {
            var placeholder = $"{{{{{kvp.Key}}}}}";
            result = result.Replace(placeholder, kvp.Value ?? string.Empty);
        }

        return result;
    }
}
