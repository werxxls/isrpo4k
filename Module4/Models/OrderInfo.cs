using System;

namespace BootProject.Models
{
    public class OrderInfo
    {
        public DateTime OrderDate { get; set; }
        public string UserLogin { get; set; }
        public string UserFIO { get; set; }
        public string ProductName { get; set; }

        public string Manufacturer { get; set; }
        public string SizeValue { get; set; }

        public int Quantity { get; set; }
        public decimal Price { get; set; }

        public int ProductId { get; set; }
        public int SizeId { get; set; }

        public decimal Total => Price * Quantity;
    }
}