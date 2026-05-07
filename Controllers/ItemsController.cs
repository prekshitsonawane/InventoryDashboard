using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using InventoryDashboard.Models;
using InventoryDashboard.Services;

namespace InventoryDashboard.Controllers
{
    /// <summary>
    /// Full CRUD controller for inventory items, including CSV export.
    /// Role-based access: Admin=full, Editor=no delete, Viewer=read-only.
    /// </summary>
    [Authorize]
    public class ItemsController : Controller
    {
        private readonly ItemService _itemService;
        private readonly CsvExportService _csvExportService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ItemsController> _logger;

        private static readonly List<string> Categories =
            new() { "Hardware", "Software", "Furniture", "Vehicle", "Electronics", "Other" };
        private static readonly List<string> Statuses =
            new() { "Active", "In Repair", "Retired", "Disposed" };
        private static readonly List<string> Conditions =
            new() { "New", "Good", "Fair", "Damaged", "Retired" };

        public ItemsController(
            ItemService itemService,
            CsvExportService csvExportService,
            UserManager<ApplicationUser> userManager,
            ILogger<ItemsController> logger)
        {
            _itemService = itemService;
            _csvExportService = csvExportService;
            _userManager = userManager;
            _logger = logger;
        }

        // ─── INDEX ─────────────────────────────────────────────────────────────

        /// <summary>Paginated, searchable, filterable item list.</summary>
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search, string? category, string? status,
            string? condition, string? department, int page = 1)
        {
            ViewData["Title"] = "All Inventory Items";

            var (items, totalCount) = await _itemService.GetPagedItemsAsync(
                search, category, status, condition, department, page, pageSize: 15);

            var categories = await _itemService.GetDistinctCategoriesAsync();
            var departments = await _itemService.GetDistinctDepartmentsAsync();

            ViewBag.Categories = categories;
            ViewBag.Departments = departments;
            ViewBag.AllStatuses = Statuses;
            ViewBag.AllConditions = Conditions;

            var vm = new ItemListViewModel
            {
                Items = items.Select(MapToVm).ToList(),
                TotalCount = totalCount,
                PageNumber = page,
                Search = search,
                Category = category,
                Status = status,
                Condition = condition,
                Department = department
            };

            return View(vm);
        }

        // ─── DETAILS ───────────────────────────────────────────────────────────

        /// <summary>Full detail view for a single item.</summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var item = await _itemService.GetByIdAsync(id);
            if (item == null) return NotFound();

            ViewData["Title"] = $"Asset Details — {item.Name}";
            return View(MapToVm(item));
        }

        // ─── ADD ───────────────────────────────────────────────────────────────

        [HttpGet]
        [Authorize(Roles = "Admin,Editor")]
        public IActionResult Add()
        {
            ViewData["Title"] = "Add New Item";
            PopulateDropdownsForForm();
            return View(new AddUpdateItemViewModel());
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Editor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add([Bind(Prefix="")] AddUpdateItemViewModel model)
        {
            _logger.LogInformation("Add POST received. Form Keys: {Keys}", string.Join(", ", Request.Form.Keys));
            foreach (var key in Request.Form.Keys)
            {
                _logger.LogInformation("Form Data - {Key}: {Value}", key, Request.Form[key]);
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => new { x.Key, Errors = x.Value?.Errors.Select(e => e.ErrorMessage) });

                foreach (var err in errors)
                {
                    _logger.LogWarning("Validation Error - Key: {Key}, Errors: {Errors}", 
                        err.Key, string.Join(", ", err.Errors ?? Array.Empty<string>()));
                }

                TempData["Error"] = "Please correct the errors in the form and try again.";
                PopulateDropdownsForForm();
                return View(model);
            }

            // Duplicate serial number check
            if (await _itemService.SerialNumberExistsAsync(model.SerialNumber))
            {
                ModelState.AddModelError(nameof(model.SerialNumber),
                    "This serial number is already registered in the system.");
                PopulateDropdownsForForm();
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            var item = MapFromVm(model);
            var created = await _itemService.AddAsync(item, user?.Id, user?.UserName);

            TempData["Success"] = $"Item '{created.Name}' was added successfully.";
            return RedirectToAction(nameof(Details), new { id = created.ItemID });
        }

        // ─── UPDATE ────────────────────────────────────────────────────────────

        [HttpGet]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> Update(int id)
        {
            var item = await _itemService.GetByIdAsync(id);
            if (item == null) return NotFound();

            ViewData["Title"] = $"Update Item — {item.Name}";
            PopulateDropdownsForForm();

            var vm = new AddUpdateItemViewModel
            {
                ItemID = item.ItemID,
                Name = item.Name,
                SerialNumber = item.SerialNumber,
                Category = item.Category,
                Brand = item.Brand,
                Model = item.Model,
                Department = item.Department,
                AssignedTo = item.AssignedTo,
                Location = item.Location,
                PurchaseDate = item.PurchaseDate,
                InstalledDate = item.InstalledDate,
                WarrantyExpiryDate = item.WarrantyExpiryDate,
                Condition = item.Condition,
                Status = item.Status,
                PurchasePrice = item.PurchasePrice?.ToString(),
                Vendor = item.Vendor,
                InvoiceNumber = item.InvoiceNumber,
                Notes = item.Notes
            };
            return View(vm);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Editor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, [Bind(Prefix="")] AddUpdateItemViewModel model)
        {
            if (id != model.ItemID) return BadRequest();

            if (!ModelState.IsValid)
            {
                PopulateDropdownsForForm();
                return View(model);
            }

            // Duplicate serial number check (exclude current item)
            if (await _itemService.SerialNumberExistsAsync(model.SerialNumber, excludeId: id))
            {
                ModelState.AddModelError(nameof(model.SerialNumber),
                    "This serial number is already registered on another item.");
                PopulateDropdownsForForm();
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            await _itemService.UpdateAsync(MapFromVm(model), user?.Id, user?.UserName);

            TempData["Success"] = "Item updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ─── DELETE ────────────────────────────────────────────────────────────

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var success = await _itemService.DeleteAsync(id, user?.Id, user?.UserName);

            if (!success) return NotFound();

            TempData["Success"] = "Item has been permanently deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ─── CSV EXPORT ────────────────────────────────────────────────────────

        /// <summary>Exports all items matching current filters to CSV.</summary>
        [HttpGet]
        public async Task<IActionResult> ExportAll(
            string? search, string? category, string? status,
            string? condition, string? department)
        {
            var items = await _itemService.GetFilteredItemsAsync(
                search, category, status, condition, department);
            var bytes = _csvExportService.ExportItems(items);
            return File(bytes, "text/csv",
                $"InventoryExport_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        /// <summary>Exports a single item to a one-row CSV.</summary>
        [HttpGet]
        public async Task<IActionResult> ExportSingle(int id)
        {
            var item = await _itemService.GetByIdAsync(id);
            if (item == null) return NotFound();
            var bytes = _csvExportService.ExportItems(new[] { item });
            return File(bytes, "text/csv",
                $"Item_{item.SerialNumber}_{DateTime.Now:yyyyMMdd}.csv");
        }

        // ─── HELPERS ───────────────────────────────────────────────────────────

        private void PopulateDropdownsForForm()
        {
            ViewBag.Categories = Categories;
            ViewBag.Statuses = Statuses;
            ViewBag.Conditions = Conditions;
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

        private static Item MapFromVm(AddUpdateItemViewModel vm)
        {
            var item = new Item
            {
                ItemID = vm.ItemID,
                Name = vm.Name,
                SerialNumber = vm.SerialNumber,
                Category = vm.Category,
                Brand = vm.Brand,
                Model = vm.Model,
                Department = vm.Department,
                AssignedTo = vm.AssignedTo,
                Location = vm.Location,
                PurchaseDate = vm.PurchaseDate,
                InstalledDate = vm.InstalledDate,
                WarrantyExpiryDate = vm.WarrantyExpiryDate,
                Condition = vm.Condition,
                Status = vm.Status,
                Vendor = vm.Vendor,
                InvoiceNumber = vm.InvoiceNumber,
                Notes = vm.Notes
            };

            // Safe parsing for Price
            if (decimal.TryParse(vm.PurchasePrice, out decimal price))
                item.PurchasePrice = price;

            return item;
        }
    }
}
