using System.Windows;
using System.Windows.Controls;
using BootProject.Data;
using BootProject.Models;

namespace BootProject
{
    public partial class OrdersManageWindow : Window
    {
        private readonly DatabaseContext _db = new DatabaseContext();
        private readonly User _currentUser;

        public OrdersManageWindow(User user)
        {
            InitializeComponent();
            _currentUser = user;
            LoadOrders();
        }

        private void LoadOrders()
        {
            var orders = _db.GetAllOrders();
            OrdersDataGrid.ItemsSource = orders;

            if (_currentUser.RoleName == "Менеджер")
            {
                OrdersDataGrid.Columns[2].Visibility = Visibility.Collapsed;
            }
        }

        private void EditOrder_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is OrderInfo selectedOrder)
            {
                var editWindow = new EditOrderWindow(selectedOrder) { Owner = this };
                if (editWindow.ShowDialog() == true)
                {
                    LoadOrders();
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}