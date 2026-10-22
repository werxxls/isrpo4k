using System;
using System.Windows;
using BootProject.Data;
using BootProject.Models;

namespace BootProject
{
    public partial class EditOrderWindow : Window
    {
        private readonly DatabaseContext _db = new DatabaseContext();
        private readonly OrderInfo _order;
        private int _maxAvailableStock;

        public EditOrderWindow(OrderInfo order)
        {
            InitializeComponent();
            _order = order;

            OrderDatePicker.SelectedDate = _order.OrderDate.Date;
            QtyTextBox.Text = _order.Quantity.ToString();

            _maxAvailableStock = _db.GetAvailableStock(_order.ProductId, _order.SizeId);

            int totalPossible = _maxAvailableStock + _order.Quantity;
            StockHintText.Text = $"Доступно на складе: {_maxAvailableStock} шт. (с учетом текущего заказа: {totalPossible} шт.)";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (OrderDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DateTime newDate = OrderDatePicker.SelectedDate.Value;

            if (newDate.Date > DateTime.Today)
            {
                MessageBox.Show("Дата заказа не может быть позже сегодняшнего дня!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            
            if (!int.TryParse(QtyTextBox.Text, out int newQty) || newQty < 0)
            {
                MessageBox.Show("Неверное количество!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (newQty > _order.Quantity)
            {
                int extraNeeded = newQty - _order.Quantity;
                if (extraNeeded > _maxAvailableStock)
                {
                    MessageBox.Show($"На складе не хватает товара! Доступно для добавления: {_maxAvailableStock} шт.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            try
            {
                _db.UpdateOrder(_order.OrderDate, _order.UserLogin, _order.ProductId, _order.SizeId, newDate, newQty);

                MessageBox.Show(newQty == 0 ? "Заказ удален." : "Заказ обновлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}