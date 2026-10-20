namespace BootProject.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Manufacturer { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string Composition { get; set; }
        public string Image { get; set; }
        public decimal Price { get; set; }
        public decimal FinalPrice { get; set; }
        public int TotalQuantity { get; set; }

        public bool IsDiscounted => FinalPrice < Price;

        public string QuantityIndicator
        {
            get => TotalQuantity <= 5 ? "мало" : "много";
            set { }
        }

        public bool IsLowStock
        {
            get => TotalQuantity <= 5;
            set { }
        }

        public string ImagePath =>
            string.IsNullOrWhiteSpace(Image) ? "/Image/picture.png" : $"/Image/{Image}";
    }
}