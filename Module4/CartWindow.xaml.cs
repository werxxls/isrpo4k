using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BootProject.Data;
using BootProject.Models;

namespace BootProject
{
    public partial class CartWindow : Window
    {
        private readonly DatabaseContext _db = new DatabaseContext();
        private readonly User _user;
        public ObservableCollection<CartItem> CartItems { get; set; }

        public CartWindow(User user)
        {
            InitializeComponent();
            _user = user;

            CartItems = new ObservableCollection<CartItem>(OrderProductWindow.SharedCart);
            CartDataGrid.ItemsSource = CartItems;

            CartItems.CollectionChanged += (s, e) => UpdateTotalPrice();
            Loaded += (s, e) => UpdateTotalPrice();
        }

        private void UpdateTotalPrice()
        {
            decimal total = CartItems.Sum(item => item.Total);
            TotalPriceText.Text = $"{total:N0} ₽";
        }

        private void RemoveFromCart_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is CartItem item)
            {
                CartItems.Remove(item);
                OrderProductWindow.SharedCart.Remove(item);
                UpdateTotalPrice();
            }
        }

        private void ConfirmOrder_Click(object sender, RoutedEventArgs e)
        {
            if (CartItems.Count == 0)
            {
                MessageBox.Show("Корзина пуста.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var item in CartItems)
            {
                var sizes = _db.GetSizes(item.ProductId);
                var size = sizes.FirstOrDefault(s => s.SizeId == item.SizeId);
                if (size == null || item.Quantity > size.Quantity)
                {
                    MessageBox.Show($"Для товара '{item.ProductName}' (размер {item.SizeValue}) превышено доступное количество на складе!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            var result = MessageBox.Show($"Подтвердить заказ на сумму {TotalPriceText.Text}?", "Оформление", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                _db.CreateFullOrder(_user.Login, CartItems.ToList());
                MessageBox.Show("Заказ успешно оформлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                OrderProductWindow.SharedCart.Clear();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}