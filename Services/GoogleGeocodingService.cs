using System.Net;
using System.Text.Json;
using NeighborhoodInformant.Models;

namespace NeighborhoodInformant.Services;

public sealed class GoogleGeocodingService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly bool _ownsHttpClient;

    public GoogleGeocodingService(string? apiKey = null, HttpClient? httpClient = null)
    {
        _apiKey = apiKey ?? Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY");
        _httpClient = httpClient ?? new HttpClient();
        _ownsHttpClient = httpClient is null;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<AddressResult?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Google Maps API key is not configured. Set the GOOGLE_MAPS_API_KEY environment variable and restart the app.");
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException("Enter an address to look up.", nameof(address));
        }

        var uri = new Uri(
            "https://maps.googleapis.com/maps/api/geocode/json?address=" +
            Uri.EscapeDataString(address.Trim()) +
            "&key=" + Uri.EscapeDataString(_apiKey!));

        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException(
                "Google Maps rejected the API key. Check that it is valid and that the Geocoding API is enabled.");
        }

        response.EnsureSuccessStatusCode();

        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
        var root = document.RootElement;
        var status = root.GetProperty("status").GetString();

        if (status == "ZERO_RESULTS")
        {
            return null;
        }

        if (status != "OK")
        {
            var reason = root.TryGetProperty("error_message", out var error)
                ? error.GetString()
                : null;
            var suffix = string.IsNullOrWhiteSpace(reason) ? string.Empty : $" Details: {reason}";
            throw new InvalidOperationException(
                $"Google Geocoding API returned {status ?? "an unknown status"}.{suffix}");
        }

        var firstResult = root.GetProperty("results").EnumerateArray().FirstOrDefault();
        if (firstResult.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Google Geocoding API returned no result for a successful response.");
        }

        var location = firstResult
            .GetProperty("geometry")
            .GetProperty("location");

        return new AddressResult(
            address.Trim(),
            firstResult.GetProperty("formatted_address").GetString()
                ?? throw new InvalidOperationException("Google Geocoding API returned an empty formatted address."),
            location.GetProperty("lat").GetDouble(),
            location.GetProperty("lng").GetDouble(),
            firstResult.TryGetProperty("place_id", out var placeId) ? placeId.GetString() ?? string.Empty : string.Empty);
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}
