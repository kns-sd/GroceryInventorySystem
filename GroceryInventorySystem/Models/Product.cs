using System.ComponentModel.DataAnnotations;

namespace GroceryInventorySystem.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [Display(Name = "Product Name")]
        public string Name { get; set; }

        [Display(Name = "Category")]
        public string Category { get; set; }

        [Range(0, 999999)]
        [Display(Name = "Price (৳)")]
        public decimal Price { get; set; }

        [Display(Name = "Stock Quantity")]
        public int StockQuantity { get; set; }

        [Display(Name = "Minimum Stock Level")]
        public int MinStockLevel { get; set; }

        [Display(Name = "Supplier")]
        public string Supplier { get; set; }

        [Display(Name = "Product Image")]
        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}