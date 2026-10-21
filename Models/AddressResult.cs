namespace NeighborhoodInformant.Models;

public sealed record AddressResult(
    string Query,
    string FormattedAddress,
    double Latitude,
    double Longitude,
    string PlaceId);
