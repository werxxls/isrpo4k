using System;
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

        private void Order_Click(object sender, RoutedEventArgs e)
        {
            if (!(SizeComboBox.SelectedItem is SizeInfo selectedSize) || _maxQuantity == 0) return;
            if (!int.TryParse(QuantityTextBox.Text, out int qty) || qty <= 0) return;

            if (qty > _maxQuantity)
            {
                MessageBox.Show($"На складе только {_maxQuantity} шт.!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal totalPrice = _product.FinalPrice * qty;

            var result = MessageBox.Show(
                $"Вы действительно хотите заказать {qty} шт. размера {selectedSize.SizeValue}?\n\n" +
                $"Сумма заказа: {totalPrice:N0} ₽",
                "Подтверждение заказа",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                _db.CreateOrder(_user.Login, _product.ProductId, selectedSize.SizeId, qty);
                MessageBox.Show("Заказ успешно создан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}