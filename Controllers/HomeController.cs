using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InventoryDashboard.Models;
using InventoryDashboard.Services;

namespace InventoryDashboard.Controllers
{
    /// <summary>
    /// Dashboard summary page shown after login.
    /// </summary>
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ItemService _itemService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ItemService itemService, ILogger<HomeController> logger)
        {
            _itemService = itemService;
            _logger = logger;
        }

        /// <summary>Renders the dashboard with summary statistics and charts.</summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Dashboard";
            var (total, active, inRepair, retired, disposed,
                recentlyAdded, expiringSoon, recentItems,
                expiringSoonItems, byCategory, byDepartment)
                = await _itemService.GetDashboardStatsAsync();

            var vm = new DashboardViewModel
            {
                TotalItems = total,
                ActiveItems = active,
                ItemsInRepair = inRepair,
                RetiredItems = retired,
                DisposedItems = disposed,
                RecentlyAdded = recentlyAdded,
                WarrantyExpiringSoon = expiringSoon,
                RecentItems = recentItems.Select(MapToVm).ToList(),
                ExpiringSoonItems = expiringSoonItems.Select(MapToVm).ToList(),
                ItemsByCategory = byCategory,
                ItemsByDepartment = byDepartment
            };

            return View(vm);
        }

        private static ItemViewModel MapToVm(Item i) => new()
        {
            ItemID = i.ItemID,
            Name = i.Name,
            SerialNumber = i.SerialNumber,
            Category = i.Category,
            Brand = i.Brand,
            Model = i.Model,
            Department = i.Department,
            AssignedTo = i.AssignedTo,
            Location = i.Location,
            PurchaseDate = i.PurchaseDate,
            InstalledDate = i.InstalledDate,
            WarrantyExpiryDate = i.WarrantyExpiryDate,
            Condition = i.Condition,
            Status = i.Status,
            PurchasePrice = i.PurchasePrice,
            Vendor = i.Vendor,
            InvoiceNumber = i.InvoiceNumber,
            Notes = i.Notes,
            AddedByUserName = i.AddedByUser?.FullName ?? i.AddedByUser?.UserName,
            CreatedAt = i.CreatedAt,
            UpdatedAt = i.UpdatedAt,
            IsWarrantyExpiringSoon = i.IsWarrantyExpiringSoon,
            IsWarrantyExpired = i.IsWarrantyExpired
        };
    }
}
