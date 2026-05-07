using Microsoft.EntityFrameworkCore;
using InventoryDashboard.Data;
using InventoryDashboard.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InventoryDashboard.Services
{
    /// <summary>
    /// Provides all CRUD, filter, pagination, and audit operations for inventory items.
    /// </summary>
    public class ItemService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ItemService> _logger;

        public ItemService(ApplicationDbContext context, ILogger<ItemService> logger)
        {
            _context = context;
            _logger = logger;
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = false
        };

        // ─── READ ──────────────────────────────────────────────────────────────

        public async Task<(List<Item> Items, int TotalCount)> GetPagedItemsAsync(
            string? search, string? category, string? status,
            string? condition, string? department,
            int page = 1, int pageSize = 15)
        {
            var query = _context.Items
                .Include(i => i.AddedByUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(i =>
                    i.Name.ToLower().Contains(s) ||
                    i.SerialNumber.ToLower().Contains(s) ||
                    i.Department.ToLower().Contains(s) ||
                    (i.AssignedTo != null && i.AssignedTo.ToLower().Contains(s)) ||
                    (i.Brand != null && i.Brand.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(i => i.Category == category);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(i => i.Status == status);

            if (!string.IsNullOrWhiteSpace(condition))
                query = query.Where(i => i.Condition == condition);

            if (!string.IsNullOrWhiteSpace(department))
                query = query.Where(i => i.Department == department);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<List<Item>> GetFilteredItemsAsync(
            string? search, string? category, string? status,
            string? condition = null, string? department = null)
        {
            var query = _context.Items.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(i =>
                    i.Name.ToLower().Contains(s) ||
                    i.SerialNumber.ToLower().Contains(s) ||
                    i.Department.ToLower().Contains(s) ||
                    (i.AssignedTo != null && i.AssignedTo.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(category)) query = query.Where(i => i.Category == category);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(i => i.Status == status);
            if (!string.IsNullOrWhiteSpace(condition)) query = query.Where(i => i.Condition == condition);
            if (!string.IsNullOrWhiteSpace(department)) query = query.Where(i => i.Department == department);

            return await query.OrderByDescending(i => i.CreatedAt).ToListAsync();
        }

        public async Task<Item?> GetByIdAsync(int id)
        {
            return await _context.Items
                .Include(i => i.AddedByUser)
                .FirstOrDefaultAsync(i => i.ItemID == id);
        }

        public async Task<(int total, int active, int inRepair, int retired, int disposed,
            int recentlyAdded, int expiringSoon,
            List<Item> recentItems, List<Item> expiringSoonItems,
            Dictionary<string, int> byCategory, Dictionary<string, int> byDepartment)>
            GetDashboardStatsAsync()
        {
            var all = await _context.Items.ToListAsync();
            var total = all.Count;
            var active = all.Count(i => i.Status == "Active");
            var inRepair = all.Count(i => i.Status == "In Repair");
            var retired = all.Count(i => i.Status == "Retired");
            var disposed = all.Count(i => i.Status == "Disposed");
            var recentlyAdded = all.Count(i => i.CreatedAt >= DateTime.UtcNow.AddDays(-7));
            var expiringSoon = all.Count(i => i.IsWarrantyExpiringSoon);

            var recentItems = all.OrderByDescending(i => i.CreatedAt).Take(5).ToList();
            var expiringSoonItems = all.Where(i => i.IsWarrantyExpiringSoon)
                .OrderBy(i => i.WarrantyExpiryDate).Take(5).ToList();

            var byCategory = all.GroupBy(i => i.Category)
                .ToDictionary(g => g.Key, g => g.Count());
            var byDepartment = all.GroupBy(i => i.Department)
                .ToDictionary(g => g.Key, g => g.Count());

            return (total, active, inRepair, retired, disposed,
                recentlyAdded, expiringSoon, recentItems, expiringSoonItems,
                byCategory, byDepartment);
        }

        // ─── CREATE ─────────────────────────────────────────────────────────────

        public async Task<Item> AddAsync(Item item, string? userId, string? userName)
        {
            item.CreatedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
            item.AddedByUserID = userId;
            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            await LogAuditAsync(item.ItemID, "Added", userId, userName,
                oldValues: null, newValues: JsonSerializer.Serialize(item, JsonOptions), itemName: item.Name);

            _logger.LogInformation("Item {ItemID} '{Name}' added by {User}.", item.ItemID, item.Name, userName);
            return item;
        }

        // ─── UPDATE ─────────────────────────────────────────────────────────────

        public async Task UpdateAsync(Item updated, string? userId, string? userName)
        {
            var existing = await _context.Items.FindAsync(updated.ItemID)
                ?? throw new InvalidOperationException($"Item {updated.ItemID} not found.");

            var oldJson = JsonSerializer.Serialize(existing, JsonOptions);

            existing.Name = updated.Name;
            existing.SerialNumber = updated.SerialNumber;
            existing.Category = updated.Category;
            existing.Brand = updated.Brand;
            existing.Model = updated.Model;
            existing.Department = updated.Department;
            existing.AssignedTo = updated.AssignedTo;
            existing.Location = updated.Location;
            existing.PurchaseDate = updated.PurchaseDate;
            existing.InstalledDate = updated.InstalledDate;
            existing.WarrantyExpiryDate = updated.WarrantyExpiryDate;
            existing.Condition = updated.Condition;
            existing.Status = updated.Status;
            existing.PurchasePrice = updated.PurchasePrice;
            existing.Vendor = updated.Vendor;
            existing.InvoiceNumber = updated.InvoiceNumber;
            existing.Notes = updated.Notes;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await LogAuditAsync(existing.ItemID, "Updated", userId, userName,
                oldValues: oldJson, newValues: JsonSerializer.Serialize(existing, JsonOptions), itemName: existing.Name);

            _logger.LogInformation("Item {ItemID} '{Name}' updated by {User}.", existing.ItemID, existing.Name, userName);
        }

        // ─── DELETE ─────────────────────────────────────────────────────────────

        public async Task<bool> DeleteAsync(int id, string? userId, string? userName)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null) return false;

            var oldJson = JsonSerializer.Serialize(item, JsonOptions);
            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            await LogAuditAsync(id, "Deleted", userId, userName,
                oldValues: oldJson, newValues: null, itemName: item.Name);

            _logger.LogInformation("Item {ItemID} '{Name}' deleted by {User}.", id, item.Name, userName);
            return true;
        }

        // ─── AUDIT ──────────────────────────────────────────────────────────────

        private async Task LogAuditAsync(int itemId, string action,
            string? userId, string? userName,
            string? oldValues, string? newValues, string? itemName)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                ItemID = itemId,
                Action = action,
                ChangedByUserID = userId,
                ChangedByUserName = userName,
                ChangedAt = DateTime.UtcNow,
                OldValues = oldValues,
                NewValues = newValues,
                ItemName = itemName
            });
            await _context.SaveChangesAsync();
        }

        public async Task<(List<AuditLog> Logs, int TotalCount)> GetAuditLogsAsync(int page = 1, int pageSize = 20)
        {
            var total = await _context.AuditLogs.CountAsync();
            var logs = await _context.AuditLogs
                .OrderByDescending(l => l.ChangedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (logs, total);
        }

        public async Task<List<string>> GetDistinctCategoriesAsync()
            => await _context.Items.Select(i => i.Category).Distinct().OrderBy(x => x).ToListAsync();

        public async Task<List<string>> GetDistinctDepartmentsAsync()
            => await _context.Items.Select(i => i.Department).Distinct().OrderBy(x => x).ToListAsync();

        public async Task<bool> SerialNumberExistsAsync(string serialNumber, int? excludeId = null)
        {
            var query = _context.Items.Where(i => i.SerialNumber == serialNumber);
            if (excludeId.HasValue) query = query.Where(i => i.ItemID != excludeId.Value);
            return await query.AnyAsync();
        }
    }
}
