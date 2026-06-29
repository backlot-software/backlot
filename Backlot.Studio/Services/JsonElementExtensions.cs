using System.Text.Json;

namespace Backlot.Studio.Services;

public static class JsonElementExtensions
{
    public static JsonElement Unwrap(this JsonElement body, string propertyName)
    {
        if (body.ValueKind != JsonValueKind.Object)
            return body;

        if (body.TryGetProperty(propertyName, out var role) && role.ValueKind == JsonValueKind.Object)
            return role.Clone();

        return body;
    }
}