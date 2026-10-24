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

            const string sql = @"
                SELECT p.ProductId, p.ProductName, p.Cost, p.PictureHref, p.Description,
                       m.Name AS ManufacturerName, c.CategoryName, 
                       ISNULL(SUM(s.Quantity), 0) AS TotalQuantity,
                       (SELECT COUNT(*) FROM Orders o 
                        WHERE o.ProductId = p.ProductId AND o.UserLogin = @login
                        AND o.OrderDate >= DATEADD(day, -30, GETDATE())) AS RecentOrders
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
                            int recentOrders = Convert.ToInt32(reader["RecentOrders"]);

                            decimal finalPrice = (isAuthorized && recentOrders == 0) ? basePrice * 0.75m : basePrice;

                            products.Add(new Product
                            {
                                ProductId = Convert.ToInt32(reader["ProductId"]),
                                Name = Convert.ToString(reader["ProductName"]),
                                Price = basePrice,
                                FinalPrice = finalPrice,
                                Image = reader["PictureHref"] != DBNull.Value ? Convert.ToString(reader["PictureHref"]) : null,
                                Manufacturer = reader["ManufacturerName"] != DBNull.Value ? Convert.ToString(reader["ManufacturerName"]) : "Неизвестно",
                                Category = reader["CategoryName"] != DBNull.Value ? Convert.ToString(reader["CategoryName"]) : "Без категории",
                                Description = reader["Description"] != DBNull.Value ? Convert.ToString(reader["Description"]) : "",
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

        public void CreateOrder(string login, int productId, int sizeId, int quantity)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string updateStock = "UPDATE Stock SET Quantity = Quantity - @qty WHERE ProductId = @pid AND SizeId = @sid";
                        using (var cmd = new SqlCommand(updateStock, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@qty", quantity);
                            cmd.Parameters.AddWithValue("@pid", productId);
                            cmd.Parameters.AddWithValue("@sid", sizeId);
                            cmd.ExecuteNonQuery();
                        }
                        string insertOrder = "INSERT INTO Orders (OrderDate, UserLogin, ProductId, SizeId, Quantity) VALUES (SYSDATETIME(), @login, @pid, @sid, @qty)";
                        using (var cmd = new SqlCommand(insertOrder, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@login", login);
                            cmd.Parameters.AddWithValue("@pid", productId);
                            cmd.Parameters.AddWithValue("@sid", sizeId);
                            cmd.Parameters.AddWithValue("@qty", quantity);
                            cmd.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new InvalidOperationException("Ошибка заказа: " + ex.Message, ex);
                    }
                }
            }
        }


        public List<OrderInfo> GetAllOrders()
        {
            var list = new List<OrderInfo>();
            const string sql = @"
                SELECT o.OrderDate, o.UserLogin, 
                       u.Lastname + ' ' + u.Firstname + ' ' + u.Patronymic AS FIO,
                       p.ProductName, m.Name AS Manufacturer, s.SizeValue, 
                       o.Quantity, p.Cost, o.ProductId, o.SizeId
                FROM Orders o
                JOIN Users u ON u.UserLogin = o.UserLogin
                JOIN Product p ON p.ProductId = o.ProductId
                JOIN Size s ON s.SizeId = o.SizeId
                JOIN ProductManufactor pm ON pm.ProductId = p.ProductId
                JOIN Manufactor m ON m.ManufactorId = pm.ManufactorId
                ORDER BY o.OrderDate DESC;";

            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    conn.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new OrderInfo
                            {
                                OrderDate = Convert.ToDateTime(r["OrderDate"]),
                                UserLogin = r["UserLogin"].ToString(),
                                UserFIO = r["FIO"].ToString(),
                                ProductName = r["ProductName"].ToString(),
                                Manufacturer = r["Manufacturer"].ToString(),
                                SizeValue = r["SizeValue"].ToString(),
                                Quantity = Convert.ToInt32(r["Quantity"]),
                                Price = Convert.ToDecimal(r["Cost"]),
                                ProductId = Convert.ToInt32(r["ProductId"]),
                                SizeId = Convert.ToInt32(r["SizeId"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { throw new InvalidOperationException("Ошибка загрузки заказов: " + ex.Message, ex); }
            return list;
        }

        public void UpdateOrder(DateTime oldDate, string userLogin, int productId, int sizeId, DateTime newDate, int newQty)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        if (newQty <= 0)
                        {
                            string delSql = "DELETE FROM Orders WHERE OrderDate = @oldDate AND UserLogin = @user AND ProductId = @pid AND SizeId = @sid";
                            using (var cmd = new SqlCommand(delSql, conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@oldDate", oldDate);
                                cmd.Parameters.AddWithValue("@user", userLogin);
                                cmd.Parameters.AddWithValue("@pid", productId);
                                cmd.Parameters.AddWithValue("@sid", sizeId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            string updSql = "UPDATE Orders SET OrderDate = @newDate, Quantity = @qty WHERE OrderDate = @oldDate AND UserLogin = @user AND ProductId = @pid AND SizeId = @sid";
                            using (var cmd = new SqlCommand(updSql, conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@newDate", newDate);
                                cmd.Parameters.AddWithValue("@qty", newQty);
                                cmd.Parameters.AddWithValue("@oldDate", oldDate);
                                cmd.Parameters.AddWithValue("@user", userLogin);
                                cmd.Parameters.AddWithValue("@pid", productId);
                                cmd.Parameters.AddWithValue("@sid", sizeId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        tran.Commit();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        throw new InvalidOperationException("Ошибка обновления заказа: " + ex.Message, ex);
                    }
                }
            }
        }

        public int GetAvailableStock(int productId, int sizeId)
        {
            const string sql = "SELECT Quantity FROM Stock WHERE ProductId = @pid AND SizeId = @sid";
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@pid", productId);
                cmd.Parameters.AddWithValue("@sid", sizeId);
                conn.Open();
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }
    }
}