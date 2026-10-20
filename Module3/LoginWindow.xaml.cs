using System.Windows;
using BootProject.Data;
using BootProject.Models;

namespace BootProject
{
    public partial class LoginWindow : Window
    {
        public User CurrentUser { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();
            LoginTextBox.Focus();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(login))
            {
                MessageBox.Show("Логин не может быть пустым.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var db = new DatabaseContext();
                var user = db.Authorize(login);

                if (user == null)
                {
                    MessageBox.Show("Пользователь с таким логином не найден.", "Ошибка авторизации",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                CurrentUser = user;
                DialogResult = true;
                Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Ошибка БД: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}