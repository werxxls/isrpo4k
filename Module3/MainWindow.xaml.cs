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
    public partial class MainWindow : Window
    {
        private readonly DatabaseContext _db = new DatabaseContext();
        private List<Product> _allProducts; // ПЕРЕМЕННАЯ

        public MainWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadCatalog();
        }

        private void LoadCatalog()
        {
            try
            {
                // ЗДЕСЬ МЫ ПРИСВАИВАЕМ ЗНАЧЕНИЕ
                _allProducts = _db.GetProducts(false, "");

                CategoryComboBox.Items.Clear();
                CategoryComboBox.Items.Add("Все категории");

                if (_allProducts != null)
                {
                    var uniqueCategories = _allProducts.Select(p => p.Category).Distinct().OrderBy(c => c);
                    foreach (var cat in uniqueCategories)
                        CategoryComboBox.Items.Add(cat);
                }

                CategoryComboBox.SelectedIndex = 0;

                ApplyFiltersAndSort();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить данные.\n\n" + ex.Message, "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

            if (SortComboBox.SelectedIndex == 1) query = query.OrderBy(p => p.Price);
            else if (SortComboBox.SelectedIndex == 2) query = query.OrderByDescending(p => p.Price);

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
            MessageBox.Show("Для оформления заказа необходимо авторизоваться!", "Требуется авторизация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow { Owner = this };
            if (loginWindow.ShowDialog() == true)
            {
                var authWindow = new AuthUserWindow(loginWindow.CurrentUser);
                authWindow.Show();
                this.Close();
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    }
}