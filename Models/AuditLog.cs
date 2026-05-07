using System.ComponentModel.DataAnnotations;

namespace InventoryDashboard.Models
{
    /// <summary>Records every insert, update, or delete on Items.</summary>
    public class AuditLog
    {
        [Key]
        public int LogID { get; set; }

        public int? ItemID { get; set; }

        [MaxLength(50)]
        public string Action { get; set; } = string.Empty; // Added / Updated / Deleted

        [MaxLength(450)]
        public string? ChangedByUserID { get; set; }

        public string? ChangedByUserName { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public string? OldValues { get; set; }  // JSON

        public string? NewValues { get; set; }  // JSON

        public string? ItemName { get; set; }
    }
}
