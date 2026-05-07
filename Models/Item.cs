using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryDashboard.Models
{
    /// <summary>
    /// Represents a company asset / inventory item.
    /// </summary>
    public class Item
    {
        [Key]
        public int ItemID { get; set; }

        [Required, MaxLength(200)]
        [Display(Name = "Item Name")]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        [Display(Name = "Serial Number")]
        public string SerialNumber { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Brand { get; set; }

        [MaxLength(100)]
        public string? Model { get; set; }

        [Required, MaxLength(100)]
        public string Department { get; set; } = string.Empty;

        [MaxLength(200)]
        [Display(Name = "Assigned To")]
        public string? AssignedTo { get; set; }

        [MaxLength(200)]
        public string? Location { get; set; }

        [Display(Name = "Purchase Date")]
        public DateTime? PurchaseDate { get; set; }

        [Display(Name = "Installed Date")]
        public DateTime? InstalledDate { get; set; }

        [Display(Name = "Warranty Expiry Date")]
        public DateTime? WarrantyExpiryDate { get; set; }

        [MaxLength(50)]
        public string Condition { get; set; } = "New";

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Active";

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Purchase Price")]
        public decimal? PurchasePrice { get; set; }

        [MaxLength(200)]
        public string? Vendor { get; set; }

        [MaxLength(100)]
        [Display(Name = "Invoice Number")]
        public string? InvoiceNumber { get; set; }

        public string? Notes { get; set; }

        [MaxLength(450)]
        public string? AddedByUserID { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Last Updated")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(AddedByUserID))]
        public ApplicationUser? AddedByUser { get; set; }

        // Computed property: is warranty expiring within 30 days?
        [NotMapped]
        public bool IsWarrantyExpiringSoon =>
            WarrantyExpiryDate.HasValue &&
            WarrantyExpiryDate.Value > DateTime.UtcNow &&
            WarrantyExpiryDate.Value <= DateTime.UtcNow.AddDays(30);

        [NotMapped]
        public bool IsWarrantyExpired =>
            WarrantyExpiryDate.HasValue && WarrantyExpiryDate.Value < DateTime.UtcNow;
    }
}
