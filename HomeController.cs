using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GroceryInventorySystem.Data;
using GroceryInventorySystem.Models;

namespace GroceryInventorySystem.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int days = 7)
        {
            // Validate days parameter
            if (days != 7 && days != 30)
                days = 7;

            var products = _context.Products.ToList();
            var sales = _context.Sales
                .Include(s => s.Product)
                .OrderByDescending(s => s.SaleDate)
                .ToList();

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

            // REAL Recent Transactions
            ViewBag.RecentSales = sales.Take(10).ToList();

            // DYNAMIC CHART DATA - Selected days range
            var dateRange = Enumerable.Range(0, days)
                .Select(i => DateTime.Today.AddDays(-(days - 1) + i))
                .ToList();

            var dailySales = dateRange.Select(date => new
            {
                Day = date.ToString("ddd"),
                FullDate = date.ToString("dd MMM"),
                Amount = sales.Where(s => s.SaleDate.Date == date.Date).Sum(s => s.TotalPrice)
            }).ToList();

            ViewBag.ChartLabels = dailySales.Select(d => d.Day).ToList();
            ViewBag.ChartData = dailySales.Select(d => d.Amount).ToList();
            ViewBag.ChartFullDates = dailySales.Select(d => d.FullDate).ToList();
            ViewBag.SelectedDays = days;

            // Category Distribution (REAL)
            var categoryData = products.GroupBy(p => p.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToList();

            ViewBag.CategoryLabels = categoryData.Select(c => c.Category).ToList();
            ViewBag.CategoryData = categoryData.Select(c => c.Count).ToList();

            return View();
        }
    }
}