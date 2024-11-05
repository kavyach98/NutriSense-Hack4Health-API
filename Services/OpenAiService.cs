using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RecipeApi.Models;
using System.Net;
using Microsoft.AspNetCore.Authentication;

namespace RecipeApi.Services
{
    public class OpenAiService
    {
private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    // Use a required property
    public required string ApiKey { get; init; }

    public OpenAiService(IConfiguration configuration)
    {
        _httpClient = new HttpClient();
        ApiKey = configuration["OpenAI:ApiKey"] 
                 ?? throw new ArgumentNullException("API key not found in configuration");
        _apiKey = ApiKey;
    }
    public async Task<JsonDocument> GetRecipesAsync(string bloodGlucoseLevel)
{
    var messages = new[]
    {
        new { role = "system", content = "You are a helpful assistant." },
        new { role = "user", content = $"Provide a complete valid JSON  array without introduction message of recipes suitable for a person with a blood glucose level of {bloodGlucoseLevel}. Each recipe should include 'RecipeName', 'Ingredients', 'Instructions'.Please make sure I can pass this to frontend directly in JSONDocument format" }
    };

    var requestBody = new
    {
        model = "gpt-4",
        messages = messages,
        max_tokens = 700,
        temperature = 0.7
    };

    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

    int maxRetries = 5;
    int delay = 1000; // Initial delay of 1 second

    for (int retry = 0; retry < maxRetries; retry++)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response: {responseBody},{JsonDocument.Parse(responseBody)}");

                try
                {
                    // Extract the content message
                    var recipesContent = JsonDocument.Parse(responseBody).RootElement
                        .GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                    // Locate the JSON array within the message
                    int jsonStartIndex = recipesContent!.IndexOf('[');
                    int jsonEndIndex = recipesContent.LastIndexOf(']');
                    
                    if (jsonStartIndex == -1 || jsonEndIndex == -1 || jsonEndIndex < jsonStartIndex)
                    {
                        throw new InvalidOperationException("JSON array not found or incomplete in the response.");
                    }

                    // Extract only the JSON array part
                    var jsonArray = recipesContent.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1).Trim();

                    // Parse the JSON array as JsonDocument
                    return JsonDocument.Parse(jsonArray);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing response: {ex.Message}");
                    throw new InvalidOperationException("Failed to parse recipe data.");
                }
            }
            else
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error: {response.StatusCode}, Details: {errorResponse}");
            }

            if (response.StatusCode == (HttpStatusCode)429)
            {
                Console.WriteLine($"Rate limit hit. Waiting {delay / 1000} seconds before retrying...");
            }

            if (response.Headers.TryGetValues("x-ratelimit-remaining", out var remainingRequests))
            {
                Console.WriteLine($"Remaining requests: {remainingRequests.FirstOrDefault()}");
            }
            if (response.Headers.TryGetValues("x-ratelimit-reset", out var resetTime))
            {
                Console.WriteLine($"Rate limit reset time: {resetTime.FirstOrDefault()}");
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode == (HttpStatusCode)429)
        {
            Console.WriteLine($"429 Too Many Requests. Retrying in {delay / 1000} seconds...");
        }

        // Exponential backoff
        await Task.Delay(delay);
        delay *= 2; // Double the delay for the next retry
    }

    throw new HttpRequestException("Max retries exceeded. Unable to fetch recipes due to repeated rate limiting.");
}

    }
}
