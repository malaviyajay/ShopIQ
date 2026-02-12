using EC.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace EC.Data
{
    public class DbHelper
    {
        private readonly string _con;

        public DbHelper(IConfiguration config)
        {
            _con = config.GetConnectionString("DefaultConnection");
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(_con);
        }   

        // ================= USERS =================

        public void RegisterUser(User user)
        {
            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand(
            @"INSERT INTO Users (Name, Email, Password,Address,Phone, IsAdmin)
              VALUES (@n,@e,@p,@d,@m,@a)", con);

            cmd.Parameters.AddWithValue("@n", user.Name);
            cmd.Parameters.AddWithValue("@e", user.Email);
            cmd.Parameters.AddWithValue("@p", user.Password);
            cmd.Parameters.AddWithValue("@d", user.Address);
            cmd.Parameters.AddWithValue("@m", user.Phone);
            cmd.Parameters.AddWithValue("@a", user.IsAdmin);

            cmd.ExecuteNonQuery();
        }

        public User? Login(string email, string password)
        {
            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand(
                "SELECT * FROM Users WHERE Email=@e AND Password=@p", con);

            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@p", password);

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return new User
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Name = Convert.ToString(reader["Name"]) ?? "",
                    Email = Convert.ToString(reader["Email"]) ?? "",
                    IsAdmin = Convert.ToBoolean(reader["IsAdmin"])

                };
            }
            return null;
        }

        public bool IsEmailExists(string email)
        {
            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Users WHERE Email=@e", con);

            cmd.Parameters.AddWithValue("@e", email);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        // ================= PROFILE =================

        public User GetUserProfile(int userId)
        {
            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand(@"
            SELECT U.Id,
                   U.Name,
                   U.Email,
                   ISNULL(P.Phone,'') AS Phone,
                   ISNULL(P.Address,'') AS Address
            FROM Users U
            LEFT JOIN UserProfiles P ON U.Id = P.UserId
            WHERE U.Id=@uid", con);

            cmd.Parameters.AddWithValue("@uid", userId);

            using var dr = cmd.ExecuteReader();

            if (!dr.Read())
                return new User();

            return new User
            {
                Id = Convert.ToInt32(dr["Id"]),
                Name = Convert.ToString(dr["Name"]),
                Email = Convert.ToString(dr["Email"]),
                Phone = Convert.ToString(dr["Phone"]),
                Address = Convert.ToString(dr["Address"])

            };
        }

        public void SaveUserProfile(User model)
        {
            using var con = GetConnection();
            con.Open();

            
            var userCmd = new SqlCommand(
                "UPDATE Users SET Name=@n WHERE Id=@uid", con);

            userCmd.Parameters.AddWithValue("@n", model.Name);
            userCmd.Parameters.AddWithValue("@uid", model.Id);
            userCmd.ExecuteNonQuery();

            var cmd = new SqlCommand(@"
            IF EXISTS (SELECT 1 FROM UserProfiles WHERE UserId=@uid)
                UPDATE UserProfiles
                SET Phone=@p, Address=@a
                WHERE UserId=@uid
            ELSE
                INSERT INTO UserProfiles(UserId,Phone,Address)
                VALUES(@uid,@p,@a)", con);

            cmd.Parameters.AddWithValue("@uid", model.Id);
            cmd.Parameters.AddWithValue("@p", model.Phone ?? "");
            cmd.Parameters.AddWithValue("@a", model.Address ?? "");

            cmd.ExecuteNonQuery();
        }

        // ================= CATEGORIES =================

        public List<Category> GetCategories()
        {
            var list = new List<Category>();

            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand("SELECT * FROM Categories", con);
            var rd = cmd.ExecuteReader();

            while (rd.Read())
            {
                list.Add(new Category
                {
                    Id = (int)rd["Id"],
                    Name = Convert.ToString(rd["Name"])

                });
            }
            return list;
        }

        // ================= PRODUCTS =================

        public void AddProduct(Product p)
        {
            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand(@"
            INSERT INTO Products (Name, Price, Image, CategoryId)
            VALUES (@n,@p,@i,@c)", con);

            cmd.Parameters.AddWithValue("@n", p.Name);
            cmd.Parameters.AddWithValue("@p", p.Price);
            cmd.Parameters.AddWithValue("@i", p.Image);
            cmd.Parameters.AddWithValue("@c", p.CategoryId);

            cmd.ExecuteNonQuery();
        }

        public Product? GetProduct(int id)
        {
            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand(
                "SELECT * FROM Products WHERE Id=@id", con);

            cmd.Parameters.AddWithValue("@id", id);

            var rd = cmd.ExecuteReader();

            if (rd.Read())
            {
                return new Product
                {
                    Id = (int)rd["Id"],
                    Name = rd["Name"].ToString(),
                    Price = (decimal)rd["Price"],
                    Image = rd["Image"].ToString(),
                    CategoryId = (int)rd["CategoryId"]
                };
            }
            return null;
        }

        // ================= ORDERS =================

        public List<Order> GetAllOrders()
        {
            var list = new List<Order>();

            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand("SELECT * FROM Orders", con);
            var rd = cmd.ExecuteReader();

            while (rd.Read())
            {
                list.Add(new Order
                {
                    Id = Convert.ToInt32(rd["Id"]),
                    UserId = Convert.ToInt32(rd["UserId"]),
                    UserName = Convert.ToString(rd["UserName"]),
                    TotalAmount = Convert.ToDecimal(rd["TotalAmount"]),
                    PaymentMethod = Convert.ToString(rd["PaymentMethod"]),
                    Status = Convert.ToString(rd["Status"]),
                    OrderDate = Convert.ToDateTime(rd["OrderDate"])

                });
            }
            return list;
        }
        // ================= PRODUCTS LIST =================
        public List<Product> GetProducts()
        {
            var list = new List<Product>();

            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand("SELECT * FROM Products", con);
            var rd = cmd.ExecuteReader();

            while (rd.Read())
            {
                list.Add(new Product
                {
                    Id = Convert.ToInt32(rd["Id"]),
                    Name = Convert.ToString(rd["Name"]) ?? "",
                    Price = Convert.ToDecimal(rd["Price"]),
                    Image = Convert.ToString(rd["Image"]) ?? "",
                    CategoryId = Convert.ToInt32(rd["CategoryId"])
                });
            }

            return list;
        }
        public User? ValidateUser(string email, string password)
        {
            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand(
                "SELECT * FROM Users WHERE Email=@e AND Password=@p", con);

            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@p", password);

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return new User
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Name = Convert.ToString(reader["Name"]) ?? "",
                    Email = Convert.ToString(reader["Email"]) ?? "",
                    IsAdmin = Convert.ToBoolean(reader["IsAdmin"])
                };
            }
            return null;
        }

        public void PlaceOrder(CheckoutViewModel model)
        {
            using var con = GetConnection();
            con.Open();

            // 1️⃣ Insert Order
            var orderCmd = new SqlCommand(@"
                           INSERT INTO Orders (UserId, UserName, TotalAmount, PaymentMethod, Status, OrderDate)
                           VALUES (@uid,@uname,@total,@pay,@status,GETDATE());
                       SELECT SCOPE_IDENTITY();", con);

            orderCmd.Parameters.AddWithValue("@uid", model.UserId);
            orderCmd.Parameters.AddWithValue("@uname", model.UserName ?? "");
            orderCmd.Parameters.AddWithValue("@total", model.TotalAmount);
            orderCmd.Parameters.AddWithValue("@pay", model.PaymentMethod ?? "COD");
            orderCmd.Parameters.AddWithValue("@status", "Pending");

            int orderId = Convert.ToInt32(orderCmd.ExecuteScalar());

            // 2️⃣ Insert Order Items
            foreach (var item in model.Items)
            {
                var itemCmd = new SqlCommand(@"
            INSERT INTO OrderItems (OrderId, ProductId, ProductName, Price, Quantity)
            VALUES (@oid,@pid,@name,@price,@qty)", con);

                itemCmd.Parameters.AddWithValue("@oid", orderId);
                itemCmd.Parameters.AddWithValue("@pid", item.ProductId);
                itemCmd.Parameters.AddWithValue("@name", item.ProductName ?? "");
                itemCmd.Parameters.AddWithValue("@price", item.Price);
                itemCmd.Parameters.AddWithValue("@qty", item.Quantity);

                itemCmd.ExecuteNonQuery();
            }
        }
        public List<CartItem> GetCartItemsFromCookie(string? cart)
        {
            var list = new List<CartItem>();

            if (string.IsNullOrEmpty(cart))
                return list;

            // Cookie format example:
            // "1:2,3:1,5:4"
            var items = cart.Split(',');

            foreach (var item in items)
            {
                var parts = item.Split(':');

                if (parts.Length == 2)
                {
                    int productId = Convert.ToInt32(parts[0]);
                    int qty = Convert.ToInt32(parts[1]);

                    var product = GetProduct(productId);

                    decimal price = product?.Price ?? 0;

                    list.Add(new CartItem
                    {
                        ProductId = productId,
                        Quantity = qty,
                        Price = price
                    });
                }
            }

            return list;
        }
        public List<Product> GetProductsByCategory(int categoryId)
        {
            var list = new List<Product>();

            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand(
                "SELECT * FROM Products WHERE CategoryId=@cid", con);

            cmd.Parameters.AddWithValue("@cid", categoryId);

            var rd = cmd.ExecuteReader();

            while (rd.Read())
            {
                list.Add(new Product
                {
                    Id = Convert.ToInt32(rd["Id"]),
                    Name = Convert.ToString(rd["Name"]),
                    Price = Convert.ToInt32(rd["Price"]),
                    Image = Convert.ToString(rd["Image"]),
                    CategoryId = Convert.ToInt32(rd["CategoryId"])

                });
            }

            return list;
        }
        public List<Order> GetOrdersByUser(int userId)
        {
            var list = new List<Order>();

            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand(
                "SELECT * FROM Orders ", con);

            var rd = cmd.ExecuteReader();

            while (rd.Read())
            {
                list.Add(new Order
                {
                    Id = Convert.ToInt32(rd["Id"]),
                    UserId = Convert.ToInt32(rd["UserId"]),
                    UserName = Convert.ToString(rd["UserName"]),
                    TotalAmount = Convert.ToDecimal(rd["TotalAmount"]),
                    PaymentMethod = Convert.ToString(rd["PaymentMethod"]),
                    Status = Convert.ToString(rd["Status"]),
                    OrderDate = Convert.ToDateTime(rd["OrderDate"])

                });
            }

            return list;
        }
        public void UpdateProduct(Product product)
        {
            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand(
                @"UPDATE Products
                 SET Name=@name,
                 Price=@price,
                 Image=@image,
                 CategoryId=@category
                 WHERE Id=@id", con);

            cmd.Parameters.AddWithValue("@name", product.Name);
            cmd.Parameters.AddWithValue("@price", product.Price);
            cmd.Parameters.AddWithValue("@image", product.Image);
            cmd.Parameters.AddWithValue("@category", product.CategoryId);
            cmd.Parameters.AddWithValue("@id", product.Id);

            cmd.ExecuteNonQuery();
        }
        // ================= DELETE PRODUCT =================
        public void DeleteProduct(int id)
        {
            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand(
                "DELETE FROM Products WHERE Id=@id", con);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }
        // ✅ Get products with category names
        public List<Product> GetProductsWithCategoryNames()
        {
            var list = new List<Product>();
            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand(@"
                SELECT P.Id, P.Name, P.Price, P.Image, P.CategoryId, C.Name AS CategoryName
                FROM Products P
                LEFT JOIN Categories C ON P.CategoryId = C.Id   ", con);

            var rd = cmd.ExecuteReader();

            while (rd.Read())
            {
                list.Add(new Product
                {
                    Id = Convert.ToInt32(rd["Id"]),
                    Name = Convert.ToString(rd["Name"]) ?? "",
                    Price = Convert.ToDecimal(rd["Price"]),
                    Image = Convert.ToString(rd["Image"]) ?? "",
                    CategoryId = Convert.ToInt32(rd["CategoryId"]),
                    CategoryName = Convert.ToString(rd["CategoryName"]) ?? ""
                });
            }

            return list;
        }
        public int GetProductCount()
        {
            int count = 0;
            using (SqlConnection conn = new SqlConnection(_con))
            {
                string query = "SELECT COUNT(*) FROM Products"; // Replace "Products" with your table name
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                count = (int)cmd.ExecuteScalar();
            }
            return count;
        }
        public int GetOrderCount()
        {
            using (SqlConnection conn = new SqlConnection(_con))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Orders", conn);
                return (int)cmd.ExecuteScalar();
            }
        }
        public int GetUserCount()
        {
            using (SqlConnection conn = new SqlConnection(_con))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Users", conn);
                return (int)cmd.ExecuteScalar();
            }
        }
        public decimal GetTotalRevenue()
        {
            using (SqlConnection conn = new SqlConnection(_con))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT ISNULL(SUM(TotalAmount),0) FROM Orders", conn);
                return (decimal)cmd.ExecuteScalar();
            }
        }

        public void UpdateUser(User user)
        {
            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand(
                @"UPDATE Users 
                 SET Name = @name, 
                 Email = @email, 
                 Phone = @phone, 
                 Address = @address
          WHERE Id = @id", con);

            cmd.Parameters.AddWithValue("@name", user.Name);
            cmd.Parameters.AddWithValue("@email", user.Email);
            cmd.Parameters.AddWithValue("@phone", user.Phone);
            cmd.Parameters.AddWithValue("@address", user.Address);
            cmd.Parameters.AddWithValue("@id", user.Id);

            cmd.ExecuteNonQuery();
        }

    }
}