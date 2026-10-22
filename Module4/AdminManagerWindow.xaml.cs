using System.Windows;
using BootProject.Models;

namespace BootProject
{
    public partial class AdminManagerWindow : Window
    {
        private readonly User _currentUser;

        public AdminManagerWindow(User user)
        {
            InitializeComponent();
            _currentUser = user;
            RoleText.Text = $"Пользователь: {_currentUser.FullName} ({_currentUser.RoleName})";
        }

        private void ProductsButton_Click(object sender, RoutedEventArgs e)
        {
            var authWindow = new AuthUserWindow(_currentUser);
            authWindow.Show();
            this.Hide();
        }

        private void OrdersButton_Click(object sender, RoutedEventArgs e)
        {
            var ordersWindow = new OrdersManageWindow(_currentUser) { Owner = this };
            ordersWindow.ShowDialog();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}