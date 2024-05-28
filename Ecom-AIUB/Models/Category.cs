using System.ComponentModel.DataAnnotations;

namespace Ecom_AIUB.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; }

    }
}
