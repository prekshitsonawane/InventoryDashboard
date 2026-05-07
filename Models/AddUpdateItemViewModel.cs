using System.ComponentModel.DataAnnotations;

namespace InventoryDashboard.Models
{
    /// <summary>ViewModel for Add and Update item forms.</summary>
    public class AddUpdateItemViewModel
    {
        public int ItemID { get; set; }

        [Required(ErrorMessage = "Item name is required.")]
        [MaxLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
        [Display(Name = "Item Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Serial number is required.")]
        [MaxLength(100, ErrorMessage = "Serial number cannot exceed 100 characters.")]
        [Display(Name = "Serial Number")]
        public string SerialNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required.")]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Brand { get; set; }

        [MaxLength(100)]
        public string? Model { get; set; }

        [Required(ErrorMessage = "Department is required.")]
        [MaxLength(100)]
        public string Department { get; set; } = string.Empty;

        [MaxLength(200)]
        [Display(Name = "Assigned To")]
        public string? AssignedTo { get; set; }

        [MaxLength(200)]
        public string? Location { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Purchase Date")]
        public DateTime? PurchaseDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Installed Date")]
        public DateTime? InstalledDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Warranty Expiry Date")]
        public DateTime? WarrantyExpiryDate { get; set; }

        [Required(ErrorMessage = "Condition is required.")]
        [MaxLength(50)]
        public string Condition { get; set; } = "New";

        [Required(ErrorMessage = "Status is required.")]
        [MaxLength(50)]
        public string Status { get; set; } = "Active";

        [Display(Name = "Purchase Price")]
        public string? PurchasePrice { get; set; }

        [MaxLength(200)]
        public string? Vendor { get; set; }

        [MaxLength(100)]
        [Display(Name = "Invoice Number")]
        public string? InvoiceNumber { get; set; }

        public string? Notes { get; set; }
    }

    /// <summary>ViewModel for user creation / management.</summary>
    public class CreateUserViewModel
    {
        [Required]
        [MaxLength(256)]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Department { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "Viewer";

        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>ViewModel for resetting a user's password.</summary>
    public class ResetPasswordViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>ViewModel for the dashboard summary page.</summary>
    public class DashboardViewModel
    {
        public int TotalItems { get; set; }
        public int ActiveItems { get; set; }
        public int ItemsInRepair { get; set; }
        public int RetiredItems { get; set; }
        public int RecentlyAdded { get; set; }
        public int WarrantyExpiringSoon { get; set; }
        public int DisposedItems { get; set; }
        public List<ItemViewModel> RecentItems { get; set; } = new();
        public List<ItemViewModel> ExpiringSoonItems { get; set; } = new();
        public Dictionary<string, int> ItemsByCategory { get; set; } = new();
        public Dictionary<string, int> ItemsByDepartment { get; set; } = new();
    }

    /// <summary>ViewModel used in bulk CSV import.</summary>
    public class ImportResultViewModel
    {
        public int ImportedCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
