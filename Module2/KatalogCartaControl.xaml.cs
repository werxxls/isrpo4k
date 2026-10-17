using System.Windows.Controls;
using System.Windows.Media;
using BootProject.Models;

namespace BootProject
{
    public partial class KatalogCartaControl : UserControl
    {
        public KatalogCartaControl(Product product)
        {
            InitializeComponent();

            DataContext = product;

            if (product != null && product.IsLowStock)
            {
                CardBorder.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x80, 0x80));
            }
        }
    }
}