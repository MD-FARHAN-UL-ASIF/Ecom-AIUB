using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ecom_AIUB.Models
{
    public class Address
    {
        [Key]
        public int Id { get; set; }
        public string ShippingAddress { get; set; }
        public string PostCode { get; set; }
        public string City { get; set; }
        [ForeignKey("User")]
        public int UserId { get; set; }
        public virtual User User { get; set; }
    }
}
