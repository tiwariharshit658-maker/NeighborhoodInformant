using System.Globalization;
using System.Text;
using System.Xml;
using ClosedXML.Excel;
using NeighborhoodInformant.Models;

namespace NeighborhoodInformant.Services;

public static class LocationExporter
{
    private const string KmlNamespace = "http://www.opengis.net/kml/2.2";

    public static void ExportKml(IEnumerable<AddressResult> results, string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var locations = results.ToList();
        if (locations.Count == 0)
        {
            throw new InvalidOperationException("There are no locations to export.");
        }

        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true
        };

        using var writer = XmlWriter.Create(filePath, settings);
        writer.WriteStartDocument();
        writer.WriteStartElement("kml", KmlNamespace);
        writer.WriteStartElement("Document", KmlNamespace);
        writer.WriteElementString("name", KmlNamespace, "Neighborhood Informant locations");

        foreach (var result in locations)
        {
            writer.WriteStartElement("Placemark", KmlNamespace);
            writer.WriteElementString("name", KmlNamespace, result.FormattedAddress);
            writer.WriteElementString("description", KmlNamespace, $"Search: {result.Query}");
            writer.WriteStartElement("Point", KmlNamespace);
            writer.WriteElementString(
                "coordinates",
                KmlNamespace,
                string.Join(
                    ",",
                    result.Longitude.ToString("G17", CultureInfo.InvariantCulture),
                    result.Latitude.ToString("G17", CultureInfo.InvariantCulture),
                    "0"));
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    public static void ExportExcel(IEnumerable<AddressResult> results, string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var locations = results.ToList();
        if (locations.Count == 0)
        {
            throw new InvalidOperationException("There are no locations to export.");
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Locations");
        var headers = new[] { "Search", "Formatted address", "Latitude", "Longitude", "Place ID" };
        for (var column = 0; column < headers.Length; column++)
        {
            worksheet.Cell(1, column + 1).Value = headers[column];
        }

        for (var row = 0; row < locations.Count; row++)
        {
            var result = locations[row];
            var excelRow = row + 2;
            worksheet.Cell(excelRow, 1).Value = result.Query;
            worksheet.Cell(excelRow, 2).Value = result.FormattedAddress;
            worksheet.Cell(excelRow, 3).Value = result.Latitude;
            worksheet.Cell(excelRow, 4).Value = result.Longitude;
            worksheet.Cell(excelRow, 5).Value = result.PlaceId;
        }

        var header = worksheet.Range(1, 1, 1, headers.Length);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightBlue;
        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();
        worksheet.RangeUsed()!.SetAutoFilter();
        workbook.SaveAs(filePath);
    }
}
