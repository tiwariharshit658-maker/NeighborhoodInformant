# Neighborhood Informant

Neighborhood Informant is a Windows desktop app for looking up one or more street addresses with the Google Maps Geocoding API, reviewing the returned coordinates, and exporting locations as KML or Excel workbooks.

## Requirements

- Windows 10 or later
- .NET 8 SDK
- A Google Maps Platform API key with the Geocoding API enabled and billing configured

## Configure the API key

The app reads its key from `GOOGLE_MAPS_API_KEY`. It is never stored in the project or source files.

Set it for the current PowerShell window before launching:

```powershell
$env:GOOGLE_MAPS_API_KEY = "YOUR_API_KEY"
dotnet run --project .\NeighborhoodInformant.csproj
```

To save it for your Windows user account, run this once and restart the app:

```powershell
[Environment]::SetEnvironmentVariable("GOOGLE_MAPS_API_KEY", "YOUR_API_KEY", "User")
```

Restrict the key to the Geocoding API and set reasonable API quotas in Google Cloud Console. A key used by a desktop app can be extracted by its user; for deployments where that is not acceptable, put the Google API call behind a secured server instead of distributing a key with the desktop client.

## Use

Enter one address per line and select **Look up addresses**. Successful matches appear in the results table; unmatched addresses are counted in the status message. Use **Export KML** to create a `.kml` file with placemarks or **Export Excel** to create a `.xlsx` workbook with the query, formatted address, latitude, longitude, and place ID.

The app reports a clear configuration message when the key is missing and surfaces Google API errors without logging the key.

## Build and test

Run these commands from the repository root on Windows with the .NET 8 SDK installed:

```powershell
dotnet build .\NeighborhoodInformant.csproj
dotnet test .\tests\NeighborhoodInformant.Tests\NeighborhoodInformant.Tests.csproj
```

The tests cover Google response mapping and address URL encoding, the missing-key behavior, KML coordinate order and XML escaping, Excel output, and empty-export handling.
