using System.ComponentModel.DataAnnotations;

namespace Ecom_AIUB.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PhoneNumber{ get; set; } = string.Empty;
        public int UserType { get; set; }
        //public string? Address { get; set; } = string.Empty;
        //public string? Image {  get; set; } = string.Empty;

    }
}
