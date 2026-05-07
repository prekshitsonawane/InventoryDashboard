using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using InventoryDashboard.Models;
using InventoryDashboard.Services;

namespace InventoryDashboard.Controllers
{
    /// <summary>
    /// Admin-only controller: audit log viewer, user management, bulk CSV import.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ItemService _itemService;
        private readonly CsvExportService _csvExportService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ItemService itemService,
            CsvExportService csvExportService,
            UserManager<ApplicationUser> userManager,
            ILogger<AdminController> logger)
        {
            _itemService = itemService;
            _csvExportService = csvExportService;
            _userManager = userManager;
            _logger = logger;
        }

        // ─── AUDIT LOG ─────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> AuditLog(int page = 1)
        {
            ViewData["Title"] = "Audit Log";
            var (logs, total) = await _itemService.GetAuditLogsAsync(page, pageSize: 20);
            ViewBag.TotalCount = total;
            ViewBag.PageNumber = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / 20);
            return View(logs);
        }

        // ─── USER MANAGEMENT ───────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Users()
        {
            ViewData["Title"] = "User Management";
            var users = _userManager.Users.ToList();
            return View(users);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            ViewData["Title"] = "Create User";
            ViewBag.Roles = new List<string> { "Admin", "Editor", "Viewer" };
            return View(new CreateUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            ViewBag.Roles = new List<string> { "Admin", "Editor", "Viewer" };

            if (!ModelState.IsValid) return View(model);

            if (await _userManager.FindByNameAsync(model.UserName) != null)
            {
                ModelState.AddModelError(nameof(model.UserName), "Username already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                FullName = model.FullName,
                Department = model.Department,
                Role = model.Role,
                MustChangePassword = true,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, model.Role);
            _logger.LogInformation("Admin created user '{UserName}' with role {Role}.", model.UserName, model.Role);
            TempData["Success"] = $"User '{model.UserName}' created successfully.";
            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();
            ViewData["Title"] = $"Reset Password — {user.UserName}";
            return View(new ResetPasswordViewModel { UserId = userId, UserName = user.UserName ?? "" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            user.MustChangePassword = true;
            await _userManager.UpdateAsync(user);
            TempData["Success"] = $"Password for '{user.UserName}' has been reset.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var current = await _userManager.GetUserAsync(User);
            if (current?.Id == userId)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            await _userManager.DeleteAsync(user);
            TempData["Success"] = $"User '{user.UserName}' has been deleted.";
            return RedirectToAction(nameof(Users));
        }

        // ─── BULK CSV IMPORT ───────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Import()
        {
            ViewData["Title"] = "Bulk CSV Import";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile csvFile)
        {
            ViewData["Title"] = "Bulk CSV Import";

            if (csvFile == null || csvFile.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Please select a CSV file to upload.");
                return View();
            }

            if (!csvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "Only .csv files are accepted.");
                return View();
            }

            using var stream = csvFile.OpenReadStream();
            var (items, errors) = _csvExportService.ImportItemsFromCsv(stream);

            var user = await _userManager.GetUserAsync(User);
            var importedCount = 0;

            foreach (var item in items)
            {
                // Skip duplicate serial numbers
                if (await _itemService.SerialNumberExistsAsync(item.SerialNumber))
                {
                    errors.Add($"Skipped: Serial number '{item.SerialNumber}' already exists.");
                    continue;
                }
                await _itemService.AddAsync(item, user?.Id, user?.UserName);
                importedCount++;
            }

            var result = new ImportResultViewModel
            {
                ImportedCount = importedCount,
                ErrorCount = errors.Count,
                Errors = errors
            };

            _logger.LogInformation("Bulk import: {Imported} items imported, {Errors} errors.",
                importedCount, errors.Count);

            return View("ImportResult", result);
        }

        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            // Return a blank CSV template with just headers
            var header = "ItemID,Name,SerialNumber,Category,Brand,Model,Department,AssignedTo,Location," +
                         "PurchaseDate,InstalledDate,WarrantyExpiryDate,Condition,Status,PurchasePrice," +
                         "Vendor,InvoiceNumber,Notes,CreatedAt,UpdatedAt\r\n";
            var bytes = System.Text.Encoding.UTF8.GetBytes(header);
            return File(bytes, "text/csv", "InventoryImportTemplate.csv");
        }
    }
}
