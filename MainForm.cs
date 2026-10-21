using System.ComponentModel;
using System.Text.Json;
using NeighborhoodInformant.Models;
using NeighborhoodInformant.Services;

namespace NeighborhoodInformant;

public sealed class MainForm : Form
{
    private readonly GoogleGeocodingService _geocodingService = new();
    private readonly BindingList<AddressResult> _results = new();
    private readonly TextBox _addressInput = new();
    private readonly DataGridView _resultsGrid = new();
    private readonly Button _lookupButton = new();
    private readonly Button _exportKmlButton = new();
    private readonly Button _exportExcelButton = new();
    private readonly Label _statusLabel = new();

    public MainForm()
    {
        Text = "Neighborhood Informant";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(850, 560);
        Size = new Size(1100, 700);
        Font = new Font("Segoe UI", 9F);

        BuildLayout();
        UpdateExportButtons();
        _statusLabel.Text = _geocodingService.IsConfigured
            ? "Enter one or more addresses, one per line."
            : "Set GOOGLE_MAPS_API_KEY and restart the app to enable lookups.";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _geocodingService.Dispose();
        }

        base.Dispose(disposing);
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 1,
            RowCount = 4
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var heading = new Label
        {
            Text = "Look up neighborhood addresses",
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8)
        };
        root.Controls.Add(heading, 0, 0);

        _addressInput.Dock = DockStyle.Fill;
        _addressInput.Multiline = true;
        _addressInput.ScrollBars = ScrollBars.Vertical;
        _addressInput.AcceptsReturn = true;
        _addressInput.PlaceholderText = "Enter an address per line";
        _addressInput.AccessibleName = "Addresses to look up";
        root.Controls.Add(_addressInput, 0, 1);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 8)
        };
        _lookupButton.Text = "Look up addresses";
        _lookupButton.AutoSize = true;
        _lookupButton.Click += async (_, _) => await LookUpAddressesAsync();
        _exportKmlButton.Text = "Export KML";
        _exportKmlButton.AutoSize = true;
        _exportKmlButton.Click += (_, _) => ExportKml();
        _exportExcelButton.Text = "Export Excel";
        _exportExcelButton.AutoSize = true;
        _exportExcelButton.Click += (_, _) => ExportExcel();
        _statusLabel.AutoSize = true;
        _statusLabel.Padding = new Padding(8, 6, 0, 0);
        actions.Controls.AddRange([_lookupButton, _exportKmlButton, _exportExcelButton, _statusLabel]);
        root.Controls.Add(actions, 0, 2);

        _resultsGrid.Dock = DockStyle.Fill;
        _resultsGrid.ReadOnly = true;
        _resultsGrid.AllowUserToAddRows = false;
        _resultsGrid.AllowUserToDeleteRows = false;
        _resultsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _resultsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _resultsGrid.MultiSelect = false;
        _resultsGrid.DataSource = _results;
        _resultsGrid.Columns[nameof(AddressResult.Query)]!.HeaderText = "Search";
        _resultsGrid.Columns[nameof(AddressResult.FormattedAddress)]!.HeaderText = "Formatted address";
        _resultsGrid.Columns[nameof(AddressResult.Latitude)]!.HeaderText = "Latitude";
        _resultsGrid.Columns[nameof(AddressResult.Longitude)]!.HeaderText = "Longitude";
        _resultsGrid.Columns[nameof(AddressResult.PlaceId)]!.HeaderText = "Place ID";
        root.Controls.Add(_resultsGrid, 0, 3);
    }

    private async Task LookUpAddressesAsync()
    {
        var addresses = _addressInput.Text
            .Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (addresses.Length == 0)
        {
            MessageBox.Show(this, "Enter at least one address.", "No addresses",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!_geocodingService.IsConfigured)
        {
            MessageBox.Show(this,
                "Google Maps API key is not configured. Set the GOOGLE_MAPS_API_KEY environment variable and restart the app.",
                "API key required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _lookupButton.Enabled = false;
        var found = 0;
        var notFound = 0;
        try
        {
            for (var index = 0; index < addresses.Length; index++)
            {
                _statusLabel.Text = $"Looking up {index + 1} of {addresses.Length}…";
                var result = await _geocodingService.GeocodeAsync(addresses[index]);
                if (result is null)
                {
                    notFound++;
                    continue;
                }

                _results.Add(result);
                found++;
            }

            _statusLabel.Text = $"Added {found} location(s); {notFound} address(es) had no match.";
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            _statusLabel.Text = "Lookup failed.";
            var message = exception is HttpRequestException
                ? "Could not connect to Google Geocoding. Check your internet connection and try again."
                : exception.Message;
            MessageBox.Show(this, message, "Address lookup failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or FormatException)
        {
            _statusLabel.Text = "Google returned an unexpected response.";
            MessageBox.Show(this, "Google Geocoding returned an invalid response. Please try again later.",
                "Address lookup failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _lookupButton.Enabled = true;
            UpdateExportButtons();
        }
    }

    private void ExportKml()
    {
        ExportWithDialog("KML files (*.kml)|*.kml", "neighborhood-locations.kml", LocationExporter.ExportKml);
    }

    private void ExportExcel()
    {
        ExportWithDialog("Excel workbooks (*.xlsx)|*.xlsx", "neighborhood-locations.xlsx", LocationExporter.ExportExcel);
    }

    private void ExportWithDialog(string filter, string fileName, Action<IEnumerable<AddressResult>, string> export)
    {
        using var dialog = new SaveFileDialog { Filter = filter, FileName = fileName, AddExtension = true };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            export(_results, dialog.FileName);
            _statusLabel.Text = $"Exported {Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            MessageBox.Show(this, exception.Message, "Export failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateExportButtons()
    {
        var hasResults = _results.Count > 0;
        _exportKmlButton.Enabled = hasResults;
        _exportExcelButton.Enabled = hasResults;
    }
}
