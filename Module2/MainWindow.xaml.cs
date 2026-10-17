using System;
using System.Windows;
using BootProject.Data;
using BootProject.Models;

namespace BootProject
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseContext _db = new DatabaseContext();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCatalog();
        }

        /// <summary>
        /// Загрузка списка товаров из БД и вывод их в виде карточек юзерконтролов на панель.
        /// </summary>
        private void LoadCatalog()
        {
            try
            {
                CatalogPanel.Children.Clear();

                var products = _db.GetProducts();

                if (products.Count == 0)
                {
                    MessageBox.Show("В базе данных нет товаров.", "Пусто",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                foreach (var product in products)
                {
                    var card = new KatalogCartaControl(product);
                    CatalogPanel.Children.Add(card);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить данные.\n\n" + ex.ToString(),
                    "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}