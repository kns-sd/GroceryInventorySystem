using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GroceryInventorySystem.Data;
using GroceryInventorySystem.Models;
using GroceryInventorySystem.Services;

namespace GroceryInventorySystem.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SalesForecastService _forecastService;

        public HomeController(
            ApplicationDbContext context,
            SalesForecastService forecastService)
        {
            _context = context;
            _forecastService = forecastService;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _context.Products.ToListAsync();

            var sales = await _context.Sales
                .Include(s => s.Product)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            // Low Stock Alerts
            var lowStockItems = products
                .Where(p => p.StockQuantity <= p.MinStockLevel)
                .ToList();

            // Calculate Stats
            ViewBag.TotalProducts = products.Count;

            ViewBag.TodaySales = sales
                .Where(s => s.SaleDate.Date == DateTime.Today)
                .Sum(s => s.TotalPrice);

            ViewBag.TotalRevenue = sales
                .Sum(s => s.TotalPrice);

            ViewBag.LowStockCount = lowStockItems.Count;

            ViewBag.LowStockItems = lowStockItems;

            // Recent Transactions
            ViewBag.RecentSales = sales
                .Take(10)
                .ToList();

            // Last 7 Days Sales Chart
            var last7Days = Enumerable
                .Range(0, 7)
                .Select(i => DateTime.Today.AddDays(-6 + i))
                .ToList();

            var dailySales = last7Days
                .Select(date => new
                {
                    Day = date.ToString("ddd"),
                    Amount = sales
                        .Where(s => s.SaleDate.Date == date.Date)
                        .Sum(s => s.TotalPrice)
                })
                .ToList();

            ViewBag.ChartLabels = dailySales
                .Select(d => d.Day)
                .ToList();

            ViewBag.ChartData = dailySales
                .Select(d => d.Amount)
                .ToList();

            // Category Distribution
            var categoryData = products
                .GroupBy(p => p.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Count = g.Count()
                })
                .ToList();

            ViewBag.CategoryLabels = categoryData
                .Select(c => c.Category)
                .ToList();

            ViewBag.CategoryData = categoryData
                .Select(c => c.Count)
                .ToList();

            // ==========================================
            // ML.NET SALES FORECAST
            // ==========================================

            var forecastProducts = new List<object>();

            foreach (var product in products)
            {
                try
                {
                    var forecast = await _forecastService
                        .ForecastSales(product.Id, 7);

                    if (forecast != null && forecast.Count > 0)
                    {
                        forecastProducts.Add(new
                        {
                            ProductName = product.Name,
                            Dates = forecast
                                .Select(f => f.Date.ToString("dd MMM"))
                                .ToList(),
                            Predictions = forecast
                                .Select(f => Math.Round(
                                    (double)f.PredictedQuantity, 1))
                                .ToList(),
                            LowerBounds = forecast
                                .Select(f => Math.Round(
                                    (double)f.LowerBound, 1))
                                .ToList(),
                            UpperBounds = forecast
                                .Select(f => Math.Round(
                                    (double)f.UpperBound, 1))
                                .ToList(),
                            TotalForecast = Math.Round(
                                forecast.Sum(
                                    f => (double)f.PredictedQuantity), 1)
                        });
                    }
                }
                catch
                {
                    // Ignore products that do not have
                    // enough sales history for forecasting.
                }
            }

            ViewBag.ForecastProducts = forecastProducts;

            return View();
        }
    }
}