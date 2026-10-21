using System.Net;
using System.Text;
using NeighborhoodInformant.Services;
using Xunit;

namespace NeighborhoodInformant.Tests;

public sealed class GoogleGeocodingServiceTests
{
    [Fact]
    public async Task GeocodeAsync_MapsGoogleResponseAndEscapesAddress()
    {
        var handler = new StubHttpMessageHandler("""
            {
              "status": "OK",
              "results": [{
                "formatted_address": "1 Main St, Example",
                "place_id": "place-123",
                "geometry": { "location": { "lat": 12.5, "lng": -45.25 } }
              }]
            }
            """);
        using var client = new HttpClient(handler);
        using var service = new GoogleGeocodingService("test-key", client);

        var result = await service.GeocodeAsync("1 Main St & 2nd Ave");

        Assert.NotNull(result);
        Assert.Equal("1 Main St & 2nd Ave", result.Query);
        Assert.Equal("1 Main St, Example", result.FormattedAddress);
        Assert.Equal(12.5, result.Latitude);
        Assert.Equal(-45.25, result.Longitude);
        Assert.Equal("place-123", result.PlaceId);
        Assert.Contains("address=1%20Main%20St%20%26%202nd%20Ave", handler.RequestUri!.Query);
        Assert.Contains("key=test-key", handler.RequestUri.Query);
    }

    [Fact]
    public async Task GeocodeAsync_ReturnsNullWhenGoogleFindsNoResults()
    {
        using var client = new HttpClient(new StubHttpMessageHandler("""{"status":"ZERO_RESULTS","results":[]}"""));
        using var service = new GoogleGeocodingService("test-key", client);

        var result = await service.GeocodeAsync("unknown address");

        Assert.Null(result);
    }

    [Fact]
    public async Task GeocodeAsync_FailsClearlyWhenApiKeyIsMissing()
    {
        using var client = new HttpClient(new StubHttpMessageHandler("{}"));
        using var service = new GoogleGeocodingService(apiKey: string.Empty, httpClient: client);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GeocodeAsync("1 Main St"));

        Assert.Contains("GOOGLE_MAPS_API_KEY", error.Message);
    }

    private sealed class StubHttpMessageHandler(string responseBody) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });
        }
    }
}
