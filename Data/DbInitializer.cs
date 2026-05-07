using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InventoryDashboard.Models;

namespace InventoryDashboard.Data
{
    /// <summary>
    /// Seeds the database with initial users and sample inventory items on first run.
    /// </summary>
    public static class DbInitializer
    {
        /// <summary>
        /// Applies pending migrations, seeds roles, users, and sample items.
        /// </summary>
        public static async Task Initialize(IServiceProvider serviceProvider, ILogger logger)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            try
            {
                // Apply any pending migrations
                await context.Database.MigrateAsync();

                // ─── Seed Roles ───────────────────────────────────────────────
                string[] roles = { "Admin", "Editor", "Viewer" };
                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                        logger.LogInformation("Created role: {Role}", role);
                    }
                }

                // ─── Seed Admin User ──────────────────────────────────────────
                const string adminUser = "admin";
                const string adminPass = "Admin@1234";

                if (await userManager.FindByNameAsync(adminUser) == null)
                {
                    var admin = new ApplicationUser
                    {
                        UserName = adminUser,
                        FullName = "System Administrator",
                        Department = "IT",
                        Role = "Admin",
                        MustChangePassword = false,
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(admin, adminPass);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(admin, "Admin");
                        logger.LogInformation("Seeded admin user.");
                    }
                }

                // ─── Seed Editor User ─────────────────────────────────────────
                const string editorUser = "editor";
                const string editorPass = "Editor@1234";

                if (await userManager.FindByNameAsync(editorUser) == null)
                {
                    var editor = new ApplicationUser
                    {
                        UserName = editorUser,
                        FullName = "John Editor",
                        Department = "Operations",
                        Role = "Editor",
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(editor, editorPass);
                    if (result.Succeeded)
                        await userManager.AddToRoleAsync(editor, "Editor");
                }

                // ─── Seed Viewer User ─────────────────────────────────────────
                const string viewerUser = "viewer";
                const string viewerPass = "Viewer@1234";

                if (await userManager.FindByNameAsync(viewerUser) == null)
                {
                    var viewer = new ApplicationUser
                    {
                        UserName = viewerUser,
                        FullName = "Jane Viewer",
                        Department = "Finance",
                        Role = "Viewer",
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(viewer, viewerPass);
                    if (result.Succeeded)
                        await userManager.AddToRoleAsync(viewer, "Viewer");
                }

                // ─── Seed Sample Items ─────────────────────────────────────────
                if (!context.Items.Any())
                {
                    var adminId = (await userManager.FindByNameAsync(adminUser))?.Id;

                    var items = new List<Item>
                    {
                        new Item { Name = "Dell OptiPlex 7090", SerialNumber = "SN-001-DELL", Category = "Hardware", Brand = "Dell", Model = "OptiPlex 7090", Department = "IT", AssignedTo = "Alice Johnson", Location = "Room 101", PurchaseDate = DateTime.UtcNow.AddYears(-2), InstalledDate = DateTime.UtcNow.AddYears(-2), WarrantyExpiryDate = DateTime.UtcNow.AddYears(1), Condition = "Good", Status = "Active", PurchasePrice = 85000, Vendor = "Dell Technologies", InvoiceNumber = "INV-2022-001", Notes = "Primary workstation for IT department.", AddedByUserID = adminId },
                        new Item { Name = "HP LaserJet Pro M404n", SerialNumber = "SN-002-HP", Category = "Hardware", Brand = "HP", Model = "LaserJet Pro M404n", Department = "Finance", AssignedTo = "Finance Team", Location = "Finance Office", PurchaseDate = DateTime.UtcNow.AddYears(-1), InstalledDate = DateTime.UtcNow.AddYears(-1), WarrantyExpiryDate = DateTime.UtcNow.AddDays(25), Condition = "Good", Status = "Active", PurchasePrice = 22000, Vendor = "HP India", InvoiceNumber = "INV-2023-045", Notes = "Shared network printer.", AddedByUserID = adminId },
                        new Item { Name = "Cisco Catalyst 2960", SerialNumber = "SN-003-CISCO", Category = "Hardware", Brand = "Cisco", Model = "Catalyst 2960-24TT", Department = "IT", AssignedTo = "Network Team", Location = "Server Room", PurchaseDate = DateTime.UtcNow.AddYears(-3), InstalledDate = DateTime.UtcNow.AddYears(-3), WarrantyExpiryDate = DateTime.UtcNow.AddDays(-30), Condition = "Fair", Status = "Active", PurchasePrice = 45000, Vendor = "Cisco Systems", InvoiceNumber = "INV-2021-012", Notes = "24-port managed switch. Warranty expired.", AddedByUserID = adminId },
                        new Item { Name = "Microsoft Office 365 License", SerialNumber = "SN-004-MS365", Category = "Software", Brand = "Microsoft", Model = "Office 365 Business", Department = "HR", AssignedTo = "All Staff", Location = "Cloud", PurchaseDate = DateTime.UtcNow.AddMonths(-6), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(6), Condition = "New", Status = "Active", PurchasePrice = 15000, Vendor = "Microsoft India", InvoiceNumber = "INV-2024-099", Notes = "Annual subscription. 50 user licenses.", AddedByUserID = adminId },
                        new Item { Name = "Ergonomic Office Chair", SerialNumber = "SN-005-CHR", Category = "Furniture", Brand = "Herman Miller", Model = "Aeron", Department = "Management", AssignedTo = "CEO Office", Location = "Executive Floor", PurchaseDate = DateTime.UtcNow.AddYears(-1), Condition = "New", Status = "Active", PurchasePrice = 55000, Vendor = "Office Essentials Ltd.", InvoiceNumber = "INV-2023-200", AddedByUserID = adminId },
                        new Item { Name = "LG UltraWide Monitor 34\"", SerialNumber = "SN-006-LG", Category = "Hardware", Brand = "LG", Model = "34WN80C-B", Department = "Design", AssignedTo = "Bob Smith", Location = "Design Studio", PurchaseDate = DateTime.UtcNow.AddMonths(-8), InstalledDate = DateTime.UtcNow.AddMonths(-8), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(16), Condition = "Good", Status = "Active", PurchasePrice = 38000, Vendor = "LG Electronics", InvoiceNumber = "INV-2023-310", AddedByUserID = adminId },
                        new Item { Name = "Toyota Innova Crysta", SerialNumber = "SN-007-TYT", Category = "Vehicle", Brand = "Toyota", Model = "Innova Crysta 2.4 GX", Department = "Admin", AssignedTo = "Driver Pool", Location = "Parking Bay A", PurchaseDate = DateTime.UtcNow.AddYears(-2), InstalledDate = DateTime.UtcNow.AddYears(-2), WarrantyExpiryDate = DateTime.UtcNow.AddYears(1), Condition = "Good", Status = "Active", PurchasePrice = 1850000, Vendor = "Toyota Dealer", InvoiceNumber = "INV-2022-VEH-01", Notes = "Company vehicle. Registration: MH12 AB 1234.", AddedByUserID = adminId },
                        new Item { Name = "UPS APC Smart-UPS 1500", SerialNumber = "SN-008-APC", Category = "Electronics", Brand = "APC", Model = "SMT1500I", Department = "IT", AssignedTo = "Server Room", Location = "Server Room Rack B", PurchaseDate = DateTime.UtcNow.AddYears(-4), WarrantyExpiryDate = DateTime.UtcNow.AddDays(-365), Condition = "Fair", Status = "In Repair", PurchasePrice = 28000, Vendor = "APC by Schneider Electric", InvoiceNumber = "INV-2020-078", Notes = "Battery replacement needed.", AddedByUserID = adminId },
                        new Item { Name = "Canon EOS R50 Camera", SerialNumber = "SN-009-CAN", Category = "Electronics", Brand = "Canon", Model = "EOS R50", Department = "Marketing", AssignedTo = "Marketing Team", Location = "Marketing Office", PurchaseDate = DateTime.UtcNow.AddMonths(-3), InstalledDate = DateTime.UtcNow.AddMonths(-3), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(21), Condition = "New", Status = "Active", PurchasePrice = 62000, Vendor = "Canon India", InvoiceNumber = "INV-2024-CAM-01", Notes = "Includes 18-55mm kit lens and memory card.", AddedByUserID = adminId },
                        new Item { Name = "Cisco IP Phone 8841", SerialNumber = "SN-010-VOIP", Category = "Electronics", Brand = "Cisco", Model = "CP-8841-K9", Department = "Sales", AssignedTo = "Sales Team", Location = "Sales Floor", PurchaseDate = DateTime.UtcNow.AddYears(-2), WarrantyExpiryDate = DateTime.UtcNow.AddYears(-1), Condition = "Good", Status = "Retired", PurchasePrice = 12000, Vendor = "Cisco Systems", InvoiceNumber = "INV-2022-PH-10", Notes = "Replaced with softphone solution.", AddedByUserID = adminId },
                    };

                    context.Items.AddRange(items);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded {Count} sample inventory items.", items.Count);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
            }
        }
    }
}
