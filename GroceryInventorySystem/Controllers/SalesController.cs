using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GroceryInventorySystem.Data;
using GroceryInventorySystem.Models;

namespace GroceryInventorySystem.Controllers
{
    [Authorize]
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Sales (POS Page)
        public IActionResult Index()
        {
            var products = _context.Products.Where(p => p.StockQuantity > 0).ToList();
            return View(products);
        }

        // POST: /Sales/CreateSale
        [HttpPost]
        public IActionResult CreateSale([FromBody] List<SaleItem> cart)
        {
            if (cart == null || cart.Count == 0)
            {
                return Json(new { success = false, message = "Cart is empty!" });
            }

            decimal grandTotal = 0;
            var sales = new List<Sale>();

            foreach (var item in cart)
            {
                var product = _context.Products.Find(item.ProductId);
                if (product == null || product.StockQuantity < item.Quantity)
                {
                    return Json(new { success = false, message = $"Not enough stock for {product?.Name}!" });
                }

                decimal total = product.Price * item.Quantity;
                grandTotal += total;

                // Deduct stock
                product.StockQuantity -= item.Quantity;

                // Create Sale record
                sales.Add(new Sale
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    TotalPrice = total,
                    SaleDate = DateTime.Now
                });
            }

            _context.Sales.AddRange(sales);
            _context.SaveChanges();

            return Json(new { success = true, message = "Sale completed successfully!", total = grandTotal });
        }

        // GET: /Sales/History
        public IActionResult History()
        {
            var sales = _context.Sales
                .Include(s => s.Product)
                .OrderByDescending(s => s.SaleDate)
                .Take(50)
                .ToList();
            return View(sales);
        }
    }

    // View Model for Cart Items
    public class SaleItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}