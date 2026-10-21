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

        public User Authorize(string login)
        {
            const string sql = @"
                SELECT u.[UserLogin], u.[Lastname], u.[Firstname], u.[Patronymic], r.[RoleName]
                FROM [Users] u
                JOIN [UserRole] ur ON ur.[UserLogin] = u.[UserLogin]
                JOIN [Role] r      ON r.[RoleId] = ur.[RoleId]
                WHERE u.[UserLogin] = @login;";

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@login", login);
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read()) return null;
                        return new User
                        {
                            Login = reader.GetString(0),
                            Lastname = reader.GetString(1),
                            Firstname = reader.GetString(2),
                            Patronymic = reader.GetString(3),
                            RoleName = reader.GetString(4)
                        };
                    }
                }
            }
            catch (Exception ex) { throw new InvalidOperationException("SQL Ошибка: " + ex.Message, ex); }
        }

        public List<Product> GetProducts(bool isAuthorized, string login)
        {
            var products = new List<Product>();
            // ИЗМЕНЕНО: Проверяем заказы за последние 30 дней
            const string sql = @"
                SELECT p.ProductId, p.ProductName, p.Cost, p.PictureHref, p.Description,
                       m.Name AS ManufacturerName, c.CategoryName, 
                       ISNULL(SUM(s.Quantity), 0) AS TotalQuantity,
                       (SELECT COUNT(*) FROM Orders o 
                        WHERE o.ProductId = p.ProductId AND o.UserLogin = @login
                        AND o.OrderDate >= DATEADD(day, -30, GETDATE())) AS UserRecentOrders
                FROM Product p
                LEFT JOIN ProductManufactor pm ON pm.ProductId = p.ProductId
                LEFT JOIN Manufactor m        ON m.ManufactorId = pm.ManufactorId
                LEFT JOIN ProductCategory pc  ON pc.ProductId = p.ProductId
                LEFT JOIN Category c          ON c.CategoryId = pc.CategoryId
                LEFT JOIN Stock s             ON s.ProductId = p.ProductId
                GROUP BY p.ProductId, p.ProductName, p.Cost, p.PictureHref, p.Description, m.Name, c.CategoryName
                ORDER BY p.ProductName;";

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@login", login ?? "");
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            decimal basePrice = Convert.ToDecimal(reader["Cost"]);
                            int userRecentOrders = Convert.ToInt32(reader["UserRecentOrders"]);

                            // Если пользователь НЕ покупал этот товар за последние 30 дней, даем скидку 25%
                            decimal finalPrice = (isAuthorized && userRecentOrders == 0) ? basePrice * 0.75m : basePrice;

                            products.Add(new Product
                            {
                                ProductId = Convert.ToInt32(reader["ProductId"]),
                                Name = reader["ProductName"].ToString(),
                                Price = basePrice,
                                FinalPrice = finalPrice,
                                Image = reader["PictureHref"] != DBNull.Value ? reader["PictureHref"].ToString() : null,
                                Manufacturer = reader["ManufacturerName"] != DBNull.Value ? reader["ManufacturerName"].ToString() : "Неизвестно",
                                Category = reader["CategoryName"] != DBNull.Value ? reader["CategoryName"].ToString() : "Без категории",
                                Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : "",
                                Composition = "Натуральная кожа",
                                TotalQuantity = Convert.ToInt32(reader["TotalQuantity"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { throw new InvalidOperationException("Ошибка БД: " + ex.Message, ex); }
            return products;
        }

        public List<SizeInfo> GetSizes(int productId)
        {
            var list = new List<SizeInfo>();
            const string sql = "SELECT s.SizeId, s.SizeValue, st.Quantity FROM Stock st JOIN Size s ON s.SizeId = st.SizeId WHERE st.ProductId = @pid AND st.Quantity > 0 ORDER BY CAST(s.SizeValue AS float)";
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@pid", productId);
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new SizeInfo
                            {
                                SizeId = Convert.ToInt32(reader["SizeId"]),
                                SizeValue = reader["SizeValue"].ToString(),
                                Quantity = Convert.ToInt32(reader["Quantity"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { throw new InvalidOperationException("Ошибка размеров: " + ex.Message, ex); }
            return list;
        }

        public void CreateFullOrder(string login, List<CartItem> cartItems)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        DateTime currentTime = DateTime.Now;

                        foreach (var item in cartItems)
                        {
                            string updateStock = "UPDATE Stock SET Quantity = Quantity - @qty WHERE ProductId = @pid AND SizeId = @sid";
                            using (var cmd = new SqlCommand(updateStock, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@qty", item.Quantity);
                                cmd.Parameters.AddWithValue("@pid", item.ProductId);
                                cmd.Parameters.AddWithValue("@sid", item.SizeId);
                                cmd.ExecuteNonQuery();
                            }

                            string insertOrder = @"INSERT INTO Orders (OrderDate, UserLogin, ProductId, SizeId, Quantity) 
                                                   VALUES (@date, @login, @pid, @sid, @qty)";
                            using (var cmd = new SqlCommand(insertOrder, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@date", currentTime);
                                cmd.Parameters.AddWithValue("@login", login);
                                cmd.Parameters.AddWithValue("@pid", item.ProductId);
                                cmd.Parameters.AddWithValue("@sid", item.SizeId);
                                cmd.Parameters.AddWithValue("@qty", item.Quantity);
                                cmd.ExecuteNonQuery();
                            }

                            currentTime = currentTime.AddMilliseconds(1);
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new InvalidOperationException("Ошибка сохранения заказа: " + ex.Message, ex);
                    }
                }
            }
        }
    }
}