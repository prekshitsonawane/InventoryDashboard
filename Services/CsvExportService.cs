using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;
using InventoryDashboard.Models;

namespace InventoryDashboard.Services
{
    /// <summary>
    /// Generates CSV export files from inventory item collections using CsvHelper.
    /// </summary>
    public class CsvExportService
    {
        private readonly ILogger<CsvExportService> _logger;

        public CsvExportService(ILogger<CsvExportService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Converts a collection of Items to a UTF-8 CSV byte array.
        /// Column order matches the specification.
        /// </summary>
        public byte[] ExportItems(IEnumerable<Item> items)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            });

            csv.Context.RegisterClassMap<ItemCsvMap>();
            csv.WriteHeader<ItemCsvRecord>();
            csv.NextRecord();

            foreach (var item in items)
            {
                csv.WriteRecord(new ItemCsvRecord
                {
                    ItemID = item.ItemID,
                    Name = item.Name,
                    SerialNumber = item.SerialNumber,
                    Category = item.Category,
                    Brand = item.Brand ?? "",
                    Model = item.Model ?? "",
                    Department = item.Department,
                    AssignedTo = item.AssignedTo ?? "",
                    Location = item.Location ?? "",
                    PurchaseDate = item.PurchaseDate?.ToString("yyyy-MM-dd") ?? "",
                    InstalledDate = item.InstalledDate?.ToString("yyyy-MM-dd") ?? "",
                    WarrantyExpiryDate = item.WarrantyExpiryDate?.ToString("yyyy-MM-dd") ?? "",
                    Condition = item.Condition,
                    Status = item.Status,
                    PurchasePrice = item.PurchasePrice?.ToString("F2") ?? "",
                    Vendor = item.Vendor ?? "",
                    InvoiceNumber = item.InvoiceNumber ?? "",
                    Notes = item.Notes ?? "",
                    CreatedAt = item.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    UpdatedAt = item.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                });
                csv.NextRecord();
            }

            writer.Flush();
            _logger.LogInformation("Exported {Count} items to CSV.", items.Count());
            return memoryStream.ToArray();
        }

        /// <summary>
        /// Parses an uploaded CSV file and returns a list of items ready for import.
        /// Returns parsed items and any per-row error messages.
        /// </summary>
        public (List<Item> Items, List<string> Errors) ImportItemsFromCsv(Stream stream)
        {
            var items = new List<Item>();
            var errors = new List<string>();

            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null
            });

            csv.Context.RegisterClassMap<ItemCsvMap>();

            var row = 1;
            while (csv.Read())
            {
                row++;
                try
                {
                    var record = csv.GetRecord<ItemCsvRecord>();
                    if (record == null) continue;

                    if (string.IsNullOrWhiteSpace(record.Name))
                    { errors.Add($"Row {row}: Name is required."); continue; }
                    if (string.IsNullOrWhiteSpace(record.SerialNumber))
                    { errors.Add($"Row {row}: Serial Number is required."); continue; }

                    var item = new Item
                    {
                        Name = record.Name,
                        SerialNumber = record.SerialNumber,
                        Category = string.IsNullOrWhiteSpace(record.Category) ? "Other" : record.Category,
                        Brand = record.Brand,
                        Model = record.Model,
                        Department = string.IsNullOrWhiteSpace(record.Department) ? "General" : record.Department,
                        AssignedTo = record.AssignedTo,
                        Location = record.Location,
                        PurchaseDate = TryParseDate(record.PurchaseDate),
                        InstalledDate = TryParseDate(record.InstalledDate),
                        WarrantyExpiryDate = TryParseDate(record.WarrantyExpiryDate),
                        Condition = string.IsNullOrWhiteSpace(record.Condition) ? "Good" : record.Condition,
                        Status = string.IsNullOrWhiteSpace(record.Status) ? "Active" : record.Status,
                        PurchasePrice = TryParseDecimal(record.PurchasePrice),
                        Vendor = record.Vendor,
                        InvoiceNumber = record.InvoiceNumber,
                        Notes = record.Notes
                    };
                    items.Add(item);
                }
                catch (Exception ex)
                {
                    errors.Add($"Row {row}: {ex.Message}");
                }
            }

            return (items, errors);
        }

        private static DateTime? TryParseDate(string? val)
        {
            if (string.IsNullOrWhiteSpace(val)) return null;
            return DateTime.TryParse(val, out var d) ? d : null;
        }

        private static decimal? TryParseDecimal(string? val)
        {
            if (string.IsNullOrWhiteSpace(val)) return null;
            return decimal.TryParse(val, out var d) ? d : null;
        }
    }

    // ─── CSV Record / Map ──────────────────────────────────────────────────────

    /// <summary>Flat record class matching CSV column order.</summary>
    public class ItemCsvRecord
    {
        public int ItemID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string PurchaseDate { get; set; } = string.Empty;
        public string InstalledDate { get; set; } = string.Empty;
        public string WarrantyExpiryDate { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PurchasePrice { get; set; } = string.Empty;
        public string Vendor { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }

    /// <summary>CsvHelper class map defining column order and names.</summary>
    public class ItemCsvMap : ClassMap<ItemCsvRecord>
    {
        public ItemCsvMap()
        {
            Map(m => m.ItemID).Name("ItemID").Index(0);
            Map(m => m.Name).Name("Name").Index(1);
            Map(m => m.SerialNumber).Name("SerialNumber").Index(2);
            Map(m => m.Category).Name("Category").Index(3);
            Map(m => m.Brand).Name("Brand").Index(4);
            Map(m => m.Model).Name("Model").Index(5);
            Map(m => m.Department).Name("Department").Index(6);
            Map(m => m.AssignedTo).Name("AssignedTo").Index(7);
            Map(m => m.Location).Name("Location").Index(8);
            Map(m => m.PurchaseDate).Name("PurchaseDate").Index(9);
            Map(m => m.InstalledDate).Name("InstalledDate").Index(10);
            Map(m => m.WarrantyExpiryDate).Name("WarrantyExpiryDate").Index(11);
            Map(m => m.Condition).Name("Condition").Index(12);
            Map(m => m.Status).Name("Status").Index(13);
            Map(m => m.PurchasePrice).Name("PurchasePrice").Index(14);
            Map(m => m.Vendor).Name("Vendor").Index(15);
            Map(m => m.InvoiceNumber).Name("InvoiceNumber").Index(16);
            Map(m => m.Notes).Name("Notes").Index(17);
            Map(m => m.CreatedAt).Name("CreatedAt").Index(18);
            Map(m => m.UpdatedAt).Name("UpdatedAt").Index(19);
        }
    }
}
