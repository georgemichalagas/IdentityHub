using System.Text;
using System.Text.Json;

namespace IdentityHub.Tests.Infrastructure;

public static class TestHelpers
{
    public static StringContent CreateJsonContent(object obj)
    {
        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    public static async Task<T?> DeserializeResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        });
    }

    public static string GenerateRandomEmail() => $"test-{Guid.NewGuid():N}@example.com";
    
    public static string GenerateRandomUsername() => $"testuser{Guid.NewGuid():N}"[..15];
    
    public static string GenerateStrongPassword() => "TestPassword123!";
}
