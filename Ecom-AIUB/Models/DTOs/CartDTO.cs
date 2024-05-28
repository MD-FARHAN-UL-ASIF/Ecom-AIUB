using System.ComponentModel.DataAnnotations.Schema;

namespace Ecom_AIUB.Models.DTOs
{
    public class CartDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public virtual Product Product { get; set; }
        public virtual User User { get; set; }
    }
}
