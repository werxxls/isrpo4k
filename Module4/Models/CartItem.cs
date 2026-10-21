using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BootProject.Models
{
    public class CartItem : INotifyPropertyChanged
    {
        public int ProductId { get; set; }
        public int SizeId { get; set; }
        public string ProductName { get; set; }
        public string SizeValue { get; set; }
        public decimal Price { get; set; }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Total));
            }
        }

        public decimal Total => Price * Quantity;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}