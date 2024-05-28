using System.ComponentModel.DataAnnotations.Schema;

namespace Ecom_AIUB.Models.DTOs
{
    public class ProductDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; } = 0;
        public string Manufacturer { get; set; } = string.Empty;
        public int Category_Id { get; set; }
        public int Quantity { get; set; }
        public IFormFile Image { get; set; }
        public virtual Category Category { get; set; }
    }
}
