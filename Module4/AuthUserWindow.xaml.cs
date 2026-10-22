using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BootProject.Data;
using BootProject.Models;

namespace BootProject
{
    public partial class AuthUserWindow : Window
    {
        private readonly DatabaseContext _db = new DatabaseContext();
        private readonly User _currentUser;
        private List<Product> _allProducts;

        public AuthUserWindow(User user)
        {
            InitializeComponent();
            _currentUser = user;
            UserFullNameText.Text = _currentUser.FullName;

            if (_currentUser.RoleName == "Администратор" || _currentUser.RoleName == "Менеджер")
            {
                AdminPanelButton.Visibility = Visibility.Visible;
            }

            Loaded += (s, e) => LoadCatalog();
        }

        private void LoadCatalog()
        {
            try
            {
                _allProducts = _db.GetProducts(true, _currentUser.Login);

                CategoryComboBox.Items.Clear();
                CategoryComboBox.Items.Add("Все категории");
                var uniqueCategories = _allProducts.Select(p => p.Category).Distinct().OrderBy(c => c);
                foreach (var cat in uniqueCategories) CategoryComboBox.Items.Add(cat);
                CategoryComboBox.SelectedIndex = 0;

                ApplyFiltersAndSort();
            }
            catch (Exception ex) { MessageBox.Show("Ошибка загрузки: " + ex.Message); }
        }

        private void ApplyFiltersAndSort()
        {
            if (_allProducts == null) return;
            var query = _allProducts.AsEnumerable();

            string searchText = SearchTextBox.Text?.Trim().ToLower();
            if (!string.IsNullOrEmpty(searchText))
                query = query.Where(p => (p.Name != null && p.Name.ToLower().Contains(searchText)) || (p.Description != null && p.Description.ToLower().Contains(searchText)));

            string selectedCategory = CategoryComboBox.SelectedItem as string;
            if (!string.IsNullOrEmpty(selectedCategory) && selectedCategory != "Все категории")
                query = query.Where(p => p.Category == selectedCategory);

            if (SortComboBox.SelectedIndex == 1) query = query.OrderBy(p => p.FinalPrice);
            else if (SortComboBox.SelectedIndex == 2) query = query.OrderByDescending(p => p.FinalPrice);

            CatalogPanel.Children.Clear();
            foreach (var product in query)
            {
                var card = new KatalogCartaControl(product);
                card.MouseDoubleClick += Card_MouseDoubleClick;
                CatalogPanel.Children.Add(card);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFiltersAndSort();
        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFiltersAndSort();
        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFiltersAndSort();

        private void Card_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is KatalogCartaControl card && card.Product != null)
            {
                var productWindow = new OrderProductWindow(card.Product, _currentUser) { Owner = this };
                productWindow.ShowDialog();
                LoadCatalog();
            }
        }

        private void AdminPanelButton_Click(object sender, RoutedEventArgs e)
        {
            var adminWindow = new AdminManagerWindow(_currentUser);
            adminWindow.Show();
            this.Close();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}