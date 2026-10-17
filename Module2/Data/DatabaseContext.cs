using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using BootProject.Models;

namespace BootProject.Data
{
    public class DatabaseContext
    {
        private const string ConnectionString =
            @"Data Source=MarIA\MSSQLSERVER03;Initial Catalog=StoreShoes;Integrated Security=True;TrustServerCertificate=True;";

        public List<Product> GetProducts()
        {
            var products = new List<Product>();

            const string sql = @"
                SELECT 
                    p.ProductId,
                    p.ProductName,
                    p.Cost,
                    p.PictureHref, -- Забираем имя файла картинки
                    m.Name AS ManufacturerName,
                    c.CategoryName,
                    sc.SubCategoryName,
                    ISNULL(SUM(s.Quantity), 0) AS TotalQuantity
                FROM Product p
                LEFT JOIN ProductManufactor pm ON pm.ProductId = p.ProductId
                LEFT JOIN Manufactor m        ON m.ManufactorId = pm.ManufactorId
                LEFT JOIN ProductCategory pc  ON pc.ProductId = p.ProductId
                LEFT JOIN Category c          ON c.CategoryId = pc.CategoryId
                LEFT JOIN ProductSubCategory psc ON psc.ProductId = p.ProductId
                LEFT JOIN SubCategory sc      ON sc.SubCategoryId = psc.SubCategoryId
                LEFT JOIN Stock s             ON s.ProductId = p.ProductId
                GROUP BY 
                    p.ProductId, p.ProductName, p.Cost, p.PictureHref, 
                    m.Name, c.CategoryName, sc.SubCategoryName
                ORDER BY p.ProductName;";

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                using (var command = new SqlCommand(sql, connection))
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(new Product
                            {
                                ProductId = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Price = reader.GetDecimal(2),
                                Image = reader.IsDBNull(3) ? null : reader.GetString(3), // Имя файла
                                Manufacturer = reader.IsDBNull(4) ? "Неизвестно" : reader.GetString(4),
                                Category = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                SubCategory = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                TotalQuantity = reader.GetInt32(7),
                                Description = "Описание товара"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Ошибка БД: " + ex.Message, ex);
            }

            return products;
        }
    }
}