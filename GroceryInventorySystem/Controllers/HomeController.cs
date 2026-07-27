using Microsoft.AspNetCore.Mvc;
using GroceryInventorySystem.Data;
using GroceryInventorySystem.Models;

namespace GroceryInventorySystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Dashboard Data from Database
            var products = _context.Products.ToList();
            var sales = _context.Sales.ToList();

            // AI: Low Stock Alerts
            var lowStockItems = products.Where(p => p.StockQuantity <= p.MinStockLevel).ToList();

            // Calculate stats
            ViewBag.TotalProducts = products.Count;
            ViewBag.TodaySales = sales
                .Where(s => s.SaleDate.Date == DateTime.Today)
                .Sum(s => s.TotalPrice);
            ViewBag.TotalRevenue = sales.Sum(s => s.TotalPrice);
            ViewBag.LowStockCount = lowStockItems.Count;
            ViewBag.LowStockItems = lowStockItems;

            return View();
        }
    }
}