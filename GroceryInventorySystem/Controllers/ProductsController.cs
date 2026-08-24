using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GroceryInventorySystem.Data;
using GroceryInventorySystem.Models;

namespace GroceryInventorySystem.Controllers
{
    [Authorize]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
            var products = _context.Products.ToList();
            return View(products);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                // Image Upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "products");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    product.ImageUrl = "/images/products/" + uniqueFileName;
                }
                else
                {
                    product.ImageUrl = "/images/placeholder-product.png";
                }

                product.CreatedAt = DateTime.Now;
                _context.Products.Add(product);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(product);
        }

        public IActionResult Edit(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile? imageFile)
        {
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var existingProduct = _context.Products.Find(product.Id);
                if (existingProduct == null) return NotFound();

                // Update image if new one uploaded
                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "products");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(existingProduct.ImageUrl) && existingProduct.ImageUrl != "/images/placeholder-product.png")
                    {
                        string oldPath = Path.Combine(_hostEnvironment.WebRootPath, existingProduct.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    existingProduct.ImageUrl = "/images/products/" + uniqueFileName;
                }

                existingProduct.Name = product.Name;
                existingProduct.Category = product.Category;
                existingProduct.Price = product.Price;
                existingProduct.StockQuantity = product.StockQuantity;
                existingProduct.MinStockLevel = product.MinStockLevel;
                existingProduct.Supplier = product.Supplier;

                _context.Products.Update(existingProduct);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(product);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var product = _context.Products.Find(id);
            if (product != null)
            {
                // Delete image file
                if (!string.IsNullOrEmpty(product.ImageUrl) && product.ImageUrl != "/images/placeholder-product.png")
                {
                    string filePath = Path.Combine(_hostEnvironment.WebRootPath, product.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }

                _context.Products.Remove(product);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}