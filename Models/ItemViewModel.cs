using System.ComponentModel.DataAnnotations;

namespace InventoryDashboard.Models
{
    /// <summary>ViewModel used for displaying item rows in the list/table view.</summary>
    public class ItemViewModel
    {
        public int ItemID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string Department { get; set; } = string.Empty;
        public string? AssignedTo { get; set; }
        public string? Location { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? InstalledDate { get; set; }
        public DateTime? WarrantyExpiryDate { get; set; }
        public string Condition { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal? PurchasePrice { get; set; }
        public string? Vendor { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? Notes { get; set; }
        public string? AddedByUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsWarrantyExpiringSoon { get; set; }
        public bool IsWarrantyExpired { get; set; }
    }

    /// <summary>Paginated list of items with filter state.</summary>
    public class ItemListViewModel
    {
        public List<ItemViewModel> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; } = 15;
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public string? Search { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; }
        public string? Condition { get; set; }
        public string? Department { get; set; }
        public int StartItem => TotalCount == 0 ? 0 : (PageNumber - 1) * PageSize + 1;
        public int EndItem => Math.Min(PageNumber * PageSize, TotalCount);
    }
}
