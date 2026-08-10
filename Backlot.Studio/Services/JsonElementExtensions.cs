using System.Text.Json;

namespace Backlot.Studio.Services;

public static class JsonElementExtensions
{
    /// <summary>
    /// Attempts to extract a nested JSON object by its property name.
    /// </summary>
    /// <param name="body">The source <see cref="JsonElement"/>.</param>
    /// <param name="propertyName">The name of the property to unwrap.</param>
    /// <returns>
    /// The nested <see cref="JsonElement"/> if the property is found and is an object; 
    /// otherwise, the original <paramref name="body"/>.
    /// </returns>
    public static JsonElement Unwrap(this JsonElement body, string propertyName)
    {
        if (body.ValueKind != JsonValueKind.Object)
            return body;

        if (body.TryGetProperty(propertyName, out var role) && role.ValueKind == JsonValueKind.Object)
            return role.Clone();

        return body;
    }
}