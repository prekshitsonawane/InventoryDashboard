using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace InventoryDashboard.Models
{
    /// <summary>
    /// Extended Identity user with company-specific fields.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>Employee's full display name.</summary>
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        /// <summary>Department this user belongs to.</summary>
        [MaxLength(100)]
        public string Department { get; set; } = string.Empty;

        /// <summary>Assigned role label (Admin / Editor / Viewer).</summary>
        [MaxLength(50)]
        public string Role { get; set; } = "Viewer";

        /// <summary>Account creation timestamp (UTC).</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Whether the user must change their password on next login.</summary>
        public bool MustChangePassword { get; set; } = false;

        // Navigation: items added by this user
        public ICollection<Item> Items { get; set; } = new List<Item>();
    }
}
