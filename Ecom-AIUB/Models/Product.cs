using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ecom_AIUB.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Description { get; set; } = string.Empty;
        [Required]
        public decimal Price { get; set; } = 0;
        [Required]
        public string Manufacturer {  get; set; } = string.Empty;
        [Required]
        [ForeignKey("Category")]
        public int Category_Id { get; set; }
        [Required]
        public int Quantity { get; set; }
        public string Image {  get; set; }
        public virtual Category Category { get; set; }
    }
}
