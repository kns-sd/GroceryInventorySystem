using System.ComponentModel.DataAnnotations;

namespace GroceryInventorySystem.Models
{
    public class Sale
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Range(1, 9999)]
        public int Quantity { get; set; }

        [Display(Name = "Total Price (৳)")]
        public decimal TotalPrice { get; set; }

        public DateTime SaleDate { get; set; } = DateTime.Now;
    }
}
