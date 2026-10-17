namespace BootProject.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Manufacturer { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Description { get; set; }

        public string Image { get; set; }

        public decimal Price { get; set; }
        public int TotalQuantity { get; set; }

        public string QuantityIndicator
        {
            get => TotalQuantity <= 3 ? "мало" : "много";
            set { }
        }

        public bool IsLowStock
        {
            get => TotalQuantity <= 3;
            set { }
        }

        public string ImagePath =>
            string.IsNullOrWhiteSpace(Image)
                ? "/image/picture.png"
                : $"/image/{Image}";
    }
}