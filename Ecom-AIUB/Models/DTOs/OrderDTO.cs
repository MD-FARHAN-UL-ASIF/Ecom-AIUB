namespace Ecom_AIUB.Models.DTOs
{
    public class OrderDTO
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPostCode { get; set; } = string.Empty;
        public string CustomerCity { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateOnly OrderDate { get; set; }
        public string Response { get; set; } = string.Empty;
        public List<int> ProductsId { get; set; }
    }
}
