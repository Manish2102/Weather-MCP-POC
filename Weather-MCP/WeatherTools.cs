using McpDotNet.Server;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Weather_MCP
{
    [McpToolType]
    public class WeatherTools
    {
        public static readonly HttpClient Client = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient() { BaseAddress = new Uri("https://api.weather.gov") };
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("weather-tool", "1.0"));
            return client;
        }

        [McpTool, Description("Get weather alerts for a US state code.")]
        public static async Task<string> GetAlerts(
            [Description("The US state code to get alerts for.")] string state)
        {
            using var jsonDocument = await Client.ReadJsonDocumentAsync($"/alerts/active/area/{state}");
            var jsonElement = jsonDocument.RootElement;
            var alerts = jsonElement.GetProperty("features").EnumerateArray();

            if (!alerts.Any())
            {
                return "No active alerts for this state.";
            }

            return string.Join("\n--\n", alerts.Select(alert =>
            {
                JsonElement properties = alert.GetProperty("properties");
                return $"""
                    Event: {properties.GetProperty("event").GetString()}
                    Area: {properties.GetProperty("areaDesc").GetString()}
                    Severity: {properties.GetProperty("severity").GetString()}
                    Description: {properties.GetProperty("description").GetString()}
                    Instruction: {properties.GetProperty("instruction").GetString()}
                    """;
            }));
        }

        [McpTool, Description("Get weather forecast for a location.")]
        public static async Task<string> GetForecast(
            [Description("Latitude of the location.")] double latitude,
            [Description("Longitude of the location.")] double longitude)
        {
            var pointUrl = string.Create(CultureInfo.InvariantCulture, $"/points/{latitude},{longitude}");
            using var jsonDocument = await Client.ReadJsonDocumentAsync(pointUrl);
            var forecastUrl = jsonDocument.RootElement.GetProperty("properties").GetProperty("forecast").GetString()
                ?? throw new Exception($"No forecast URL provided by {Client.BaseAddress}points/{latitude},{longitude}");

            using var forecastDocument = await Client.ReadJsonDocumentAsync(forecastUrl);
            var periods = forecastDocument.RootElement.GetProperty("properties").GetProperty("periods").EnumerateArray();

            return string.Join("\n---\n", periods.Select(period => $"""
                {period.GetProperty("name").GetString()}
                Temperature: {period.GetProperty("temperature").GetInt32()}°F
                Wind: {period.GetProperty("windSpeed").GetString()} {period.GetProperty("windDirection").GetString()}
                Forecast: {period.GetProperty("detailedForecast").GetString()}
                """));
        }
    }
}
