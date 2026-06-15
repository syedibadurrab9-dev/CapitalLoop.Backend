using System;

namespace Domain.Exceptions;

/// <summary>
/// Thrown when an automated Playwright, Puppeteer, or HTTP scraping routine intercepts a structural shift, 
/// selector mutation, or data geometry variation on an external upstream financial data hub portal [CL-206].
/// </summary>
public class ScraperMutationException : Exception
{
    public string TargetNodeName { get; private set; } = string.Empty;

    public ScraperMutationException()
        : base("An unhandled DOM structural mutation or markup variation was intercepted on a downstream tracking feed.") { }

    // 🚀 ADDED: Single string constructor to support quick validation breaks inside your scraper engine loops
    public ScraperMutationException(string message)
        : base(message) { }

    public ScraperMutationException(string nodeName, string description)
        : base($"Downstream breaking scraper structural layout shift caught on [{nodeName}]: {description}")
    {
        TargetNodeName = nodeName;
    }

    public ScraperMutationException(string message, Exception innerException)
        : base(message, innerException) { }
}