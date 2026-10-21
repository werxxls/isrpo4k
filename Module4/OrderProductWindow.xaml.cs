using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using BootProject.Data;
using BootProject.Models;

namespace BootProject
{
    public partial class OrderProductWindow : Window
    {
        private readonly DatabaseContext _db = new DatabaseContext();
        private readonly Product _product;
        private readonly User _user;
        private int _maxQuantity = 0;

        public static List<CartItem> SharedCart = new List<CartItem>();

        public OrderProductWindow(Product product, User user)
        {
            InitializeComponent();
            _product = product;
            _user = user;
            DataContext = _product;
            LoadSizes();
        }

        private void LoadSizes()
        {
            try
            {
                var sizes = _db.GetSizes(_product.ProductId);
                SizeComboBox.ItemsSource = sizes;
                if (sizes.Count > 0) SizeComboBox.SelectedIndex = 0;
            }
            catch (Exception ex) { MessageBox.Show("Ошибка загрузки размеров: " + ex.Message); }
        }

        private void SizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SizeComboBox.SelectedItem is SizeInfo size)
            {
                _maxQuantity = size.Quantity;
                StockInfoText.Text = $"На складе: {_maxQuantity} шт.";
                QuantityTextBox.Text = "1";
            }
        }

        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(QuantityTextBox.Text, out int currentQty) && currentQty > 1)
                QuantityTextBox.Text = (currentQty - 1).ToString();
        }

        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(QuantityTextBox.Text, out int currentQty))
            {
                if (currentQty < _maxQuantity)
                    QuantityTextBox.Text = (currentQty + 1).ToString();
                else
                    MessageBox.Show($"На складе только {_maxQuantity} шт.!");
            }
        }

        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            if (!(SizeComboBox.SelectedItem is SizeInfo selectedSize) || _maxQuantity == 0) return;
            if (!int.TryParse(QuantityTextBox.Text, out int qty) || qty <= 0) return;

            var existingItem = SharedCart.Find(c => c.ProductId == _product.ProductId && c.SizeId == selectedSize.SizeId);

            if (existingItem != null)
            {
                if (existingItem.Quantity + qty <= _maxQuantity)
                    existingItem.Quantity += qty;
                else
                {
                    MessageBox.Show("Превышено доступное количество на складе!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                SharedCart.Add(new CartItem
                {
                    ProductId = _product.ProductId,
                    SizeId = selectedSize.SizeId,
                    ProductName = _product.Name,
                    SizeValue = selectedSize.SizeValue,
                    Price = _product.FinalPrice,
                    Quantity = qty
                });
            }

            MessageBox.Show("Товар успешно добавлен в корзину!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}