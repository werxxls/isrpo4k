namespace BootProject.Models
{
    public class SizeInfo
    {
        public int SizeId { get; set; }
        public string SizeValue { get; set; }
        public int Quantity { get; set; }

        public override string ToString() => SizeValue;
    }
}