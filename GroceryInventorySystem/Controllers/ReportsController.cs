using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GroceryInventorySystem.Data;

namespace GroceryInventorySystem.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var sales = _context.Sales
                .Include(s => s.Product)
                .OrderByDescending(s => s.SaleDate)
                .ToList();

            // Best Selling Products
            var bestSelling = sales
                .GroupBy(s => s.ProductId)
                .Select(g => new
                {
                    ProductName = g.First().Product?.Name ?? "Unknown",
                    TotalQuantity = g.Sum(s => s.Quantity),
                    TotalRevenue = g.Sum(s => s.TotalPrice)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(5)
                .ToList();

            ViewBag.BestSelling = bestSelling;
            ViewBag.TotalSales = sales.Count;
            ViewBag.TotalRevenue = sales.Sum(s => s.TotalPrice);
            ViewBag.TotalQuantity = sales.Sum(s => s.Quantity);

            // Sales by Category
            var categorySales = sales
                .GroupBy(s => s.Product?.Category ?? "Unknown")
                .Select(g => new
                {
                    Category = g.Key,
                    Revenue = g.Sum(s => s.TotalPrice),
                    Count = g.Count()
                })
                .ToList();

            ViewBag.CategorySales = categorySales;

            // Monthly Sales (last 6 months)
            var monthlySales = sales
                .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
                .Select(g => new
                {
                    Month = $"{g.Key.Month:00}/{g.Key.Year}",
                    Revenue = g.Sum(s => s.TotalPrice),
                    Count = g.Count()
                })
                .OrderBy(x => x.Month)
                .Take(6)
                .ToList();

            ViewBag.MonthlySales = monthlySales;

            return View();
        }
    }
}