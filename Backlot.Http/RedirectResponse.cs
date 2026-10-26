namespace Backlot.Http;

/// <summary>
/// Scenario result that sends the browser on to another url (302 Found) instead of returning a body,
/// e.g. the return from an external identity provider.
/// </summary>
public sealed record RedirectResponse(string Location);
