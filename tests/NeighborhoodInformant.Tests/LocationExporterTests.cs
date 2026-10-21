using ClosedXML.Excel;
using NeighborhoodInformant.Models;
using NeighborhoodInformant.Services;
using System.Xml.Linq;
using Xunit;

namespace NeighborhoodInformant.Tests;

public sealed class LocationExporterTests
{
    private static readonly AddressResult Sample = new(
        "1 <Main> Street",
        "1 <Main> Street, Example",
        12.5,
        -45.25,
        "place-123");

    [Fact]
    public void ExportKml_WritesEscapedPlacemarkAndLongitudeFirstCoordinates()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.kml");
        try
        {
            LocationExporter.ExportKml([Sample], path);

            var document = XDocument.Load(path);
            XNamespace ns = "http://www.opengis.net/kml/2.2";
            var placemark = Assert.Single(document.Descendants(ns + "Placemark"));
            Assert.Equal(Sample.FormattedAddress, (string?)placemark.Element(ns + "name"));
            Assert.Equal("-45.25,12.5,0", (string?)placemark.Descendants(ns + "coordinates").Single());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ExportExcel_WritesColumnHeadersAndLocationValues()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");
        try
        {
            LocationExporter.ExportExcel([Sample], path);

            using var workbook = new XLWorkbook(path);
            var sheet = workbook.Worksheet("Locations");
            Assert.Equal("Search", sheet.Cell(1, 1).GetString());
            Assert.Equal(Sample.FormattedAddress, sheet.Cell(2, 2).GetString());
            Assert.Equal(Sample.Latitude, sheet.Cell(2, 3).GetDouble());
            Assert.Equal(Sample.Longitude, sheet.Cell(2, 4).GetDouble());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ExportKml_RejectsEmptyResults()
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => LocationExporter.ExportKml([], "unused.kml"));

        Assert.Contains("no locations", error.Message);
    }
}
