using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Google.GenAI;
using Google.GenAI.Types;
using GroceryInventorySystem.Data;
using GroceryInventorySystem.Services;

namespace GroceryInventorySystem.Controllers
{
    public class AIController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SalesForecastService _forecastService;

        public AIController(
            ApplicationDbContext context,
            SalesForecastService forecastService)
        {
            _context = context;
            _forecastService = forecastService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Chat(
            string message,
            bool dashboard = false)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                if (dashboard)
                {
                    return Json(new
                    {
                        success = false,
                        error = "Please enter a question."
                    });
                }

                return View("Index");
            }

            string? apiKey =
                System.Environment.GetEnvironmentVariable("GEMINI_API_KEY");

            if (string.IsNullOrEmpty(apiKey))
            {
                if (dashboard)
                {
                    return Json(new
                    {
                        success = false,
                        error = "Gemini API key is not configured."
                    });
                }

                ViewBag.Error = "Gemini API key is not configured.";
                return View("Index");
            }

            try
            {
                DateTime today = DateTime.Today;
                DateTime tomorrow = today.AddDays(1);
                DateTime sevenDaysAgo = today.AddDays(-7);
                DateTime thirtyDaysAgo = today.AddDays(-30);

                // Today's Revenue
                decimal todayRevenue = await _context.Sales
                    .Where(s =>
                        s.SaleDate >= today &&
                        s.SaleDate < tomorrow)
                    .SumAsync(s => (decimal?)s.TotalPrice) ?? 0;

                // Total Revenue
                decimal totalRevenue = await _context.Sales
                    .SumAsync(s => (decimal?)s.TotalPrice) ?? 0;

                // Last 7 Days Revenue
                decimal last7DaysRevenue = await _context.Sales
                    .Where(s => s.SaleDate >= sevenDaysAgo)
                    .SumAsync(s => (decimal?)s.TotalPrice) ?? 0;

                // Last 30 Days Revenue
                decimal last30DaysRevenue = await _context.Sales
                    .Where(s => s.SaleDate >= thirtyDaysAgo)
                    .SumAsync(s => (decimal?)s.TotalPrice) ?? 0;

                // Top Selling Products
                var topProducts = await _context.Sales
                    .Include(s => s.Product)
                    .GroupBy(s => new
                    {
                        s.ProductId,
                        ProductName = s.Product.Name
                    })
                    .Select(g => new
                    {
                        ProductName = g.Key.ProductName,
                        TotalQuantity = g.Sum(s => s.Quantity),
                        TotalRevenue = g.Sum(s => s.TotalPrice)
                    })
                    .OrderByDescending(x => x.TotalQuantity)
                    .Take(10)
                    .ToListAsync();

                string topProductsData = string.Join(
                    "\n",
                    topProducts.Select((p, index) =>
                        $"{index + 1}. {p.ProductName} - " +
                        $"Sold: {p.TotalQuantity} units, " +
                        $"Revenue: ৳{p.TotalRevenue:F2}")
                );

                if (string.IsNullOrEmpty(topProductsData))
                {
                    topProductsData = "No sales data available.";
                }

                // Low Stock Products
                var lowStockProducts = await _context.Products
                    .Where(p => p.StockQuantity <= p.MinStockLevel)
                    .OrderBy(p => p.StockQuantity)
                    .ToListAsync();

                string lowStockData = string.Join(
                    "\n",
                    lowStockProducts.Select(p =>
                        $"{p.Name} - " +
                        $"Current stock: {p.StockQuantity}, " +
                        $"Minimum required: {p.MinStockLevel}, " +
                        $"Supplier: {p.Supplier}")
                );

                if (string.IsNullOrEmpty(lowStockData))
                {
                    lowStockData =
                        "No products are currently low in stock.";
                }

                // Inventory Information
                var products = await _context.Products
                    .ToListAsync();

                int totalProducts = products.Count;

                int totalStockUnits =
                    products.Sum(p => p.StockQuantity);

                decimal inventoryValue =
                    products.Sum(p => p.Price * p.StockQuantity);

                // Category Performance
                var categoryPerformance = await _context.Sales
                    .Include(s => s.Product)
                    .GroupBy(s => s.Product.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        QuantitySold = g.Sum(s => s.Quantity),
                        Revenue = g.Sum(s => s.TotalPrice)
                    })
                    .OrderByDescending(x => x.Revenue)
                    .ToListAsync();

                string categoryData = string.Join(
                    "\n",
                    categoryPerformance.Select(c =>
                        $"{c.Category} - " +
                        $"Units sold: {c.QuantitySold}, " +
                        $"Revenue: ৳{c.Revenue:F2}")
                );

                if (string.IsNullOrEmpty(categoryData))
                {
                    categoryData =
                        "No category sales data available.";
                }

                // Recent Sales
                var recentSales = await _context.Sales
                    .Include(s => s.Product)
                    .OrderByDescending(s => s.SaleDate)
                    .Take(20)
                    .Select(s => new
                    {
                        Product = s.Product.Name,
                        Quantity = s.Quantity,
                        Revenue = s.TotalPrice,
                        Date = s.SaleDate
                    })
                    .ToListAsync();

                string recentSalesData = string.Join(
                    "\n",
                    recentSales.Select(s =>
                        $"{s.Date:yyyy-MM-dd HH:mm} - " +
                        $"{s.Product} - " +
                        $"{s.Quantity} units - " +
                        $"৳{s.Revenue:F2}")
                );

                if (string.IsNullOrEmpty(recentSalesData))
                {
                    recentSalesData = "No sales recorded.";
                }

                // Sales Forecast
                string forecastData =
                    "No sales forecast was requested.";

                string lowerMessage =
                    message.ToLower();

                bool forecastRequested =
                    lowerMessage.Contains("forecast") ||
                    lowerMessage.Contains("predict") ||
                    lowerMessage.Contains("prediction") ||
                    lowerMessage.Contains("next 7 days") ||
                    lowerMessage.Contains("next week") ||
                    lowerMessage.Contains("future sales") ||
                    lowerMessage.Contains("will sell");

                if (forecastRequested)
                {
                    var matchingProduct = products
                        .OrderByDescending(p => p.Name.Length)
                        .FirstOrDefault(p =>
                            lowerMessage.Contains(
                                p.Name.ToLower()));

                    if (matchingProduct != null)
                    {
                        try
                        {
                            var forecast =
                                await _forecastService.ForecastSales(
                                    matchingProduct.Id,
                                    7);

                            forecastData =
                                $"PRODUCT: {matchingProduct.Name}\n" +
                                "7-DAY SALES FORECAST\n" +
                                string.Join(
                                    "\n",
                                    forecast.Select(f =>
                                        $"{f.Date:yyyy-MM-dd} - " +
                                        $"Predicted: " +
                                        $"{f.PredictedQuantity:F1} units " +
                                        $"(Range: " +
                                        $"{f.LowerBound:F1} - " +
                                        $"{f.UpperBound:F1})")
                                );

                            double totalForecast =
                                forecast.Sum(
                                    f => f.PredictedQuantity);

                            forecastData +=
                                $"\nTotal predicted sales for " +
                                $"next 7 days: " +
                                $"{totalForecast:F1} units";
                        }
                        catch (Exception ex)
                        {
                            forecastData =
                                $"Forecast unavailable for " +
                                $"{matchingProduct.Name}. " +
                                $"Reason: {ex.Message}";
                        }
                    }
                    else
                    {
                        forecastData =
                            "The user requested a forecast, " +
                            "but no specific product was identified.";
                    }
                }

                // Business Data
                string businessData = $"""
                    BUSINESS INFORMATION

                    Today's Revenue:
                    ৳{todayRevenue:F2}

                    Total Revenue:
                    ৳{totalRevenue:F2}

                    Revenue - Last 7 Days:
                    ৳{last7DaysRevenue:F2}

                    Revenue - Last 30 Days:
                    ৳{last30DaysRevenue:F2}

                    Total Products:
                    {totalProducts}

                    Total Stock Units:
                    {totalStockUnits}

                    Current Inventory Value:
                    ৳{inventoryValue:F2}


                    TOP SELLING PRODUCTS
                    {topProductsData}


                    LOW STOCK PRODUCTS
                    {lowStockData}


                    CATEGORY PERFORMANCE
                    {categoryData}


                    RECENT SALES
                    {recentSalesData}


                    SALES FORECAST
                    {forecastData}
                    """;

                // Gemini
                Client client = new(
                    apiKey: apiKey
                );

                string prompt = $"""
                    You are an AI Business Assistant for a grocery
                    inventory management system.

                    Your job is to help the business owner understand
                    sales, inventory, forecasting and business
                    performance.

                    REAL BUSINESS DATA:
                    {businessData}


                    USER QUESTION:
                    {message}


                    RULES:

                    1. Use the provided business data when answering
                       business questions.

                    2. Never invent business numbers.

                    3. If requested information is not available,
                       clearly say that it is not currently available.

                    4. Give practical recommendations when appropriate.

                    5. When discussing money, use Bangladeshi Taka (৳).

                    6. Keep answers clear and easy for a business owner
                       to understand.

                    7. For low-stock questions, use the provided
                       current stock and minimum stock level.

                    8. For best-selling questions, use the provided
                       sales quantity and revenue.

                    9. For forecasting questions, use the ML.NET
                       forecast provided under SALES FORECAST.

                    10. Clearly explain that forecasts are estimates,
                        not guaranteed future sales.

                    11. If a forecast range is provided, explain it
                        as an estimated range.

                    12. If the forecast is unavailable, do not invent
                        a prediction.

                    13. You may combine business data and forecast
                        information to give useful recommendations.

                    14. Do not expose database implementation details
                        unless the user specifically asks.

                    Answer the user's question naturally.
                    """;

                GenerateContentResponse response =
                    await client.Models.GenerateContentAsync(
                        model: "gemini-3.6-flash",
                        contents: prompt
                    );

                string aiResponse =
                    response.Text ?? "No response received.";

                // Dashboard request → return JSON
                if (dashboard)
                {
                    return Json(new
                    {
                        success = true,
                        response = aiResponse
                    });
                }

                // Normal AI page request
                ViewBag.UserMessage = message;
                ViewBag.AIResponse = aiResponse;

                return View("Index");
            }
            catch (Exception ex)
            {
                if (dashboard)
                {
                    return Json(new
                    {
                        success = false,
                        error = ex.Message
                    });
                }

                ViewBag.Error = ex.Message;

                return View("Index");
            }
        }
    }
}