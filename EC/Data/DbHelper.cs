using EC.Models;
using Microsoft.Data.SqlClient;

namespace EC.Data
{
    public class DbHelper
    {
        public string _con;

        public object UserId { get; private set; }

        public DbHelper(IConfiguration config)
        {
            _con = config.GetConnectionString("DefaultConnection");
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(_con);
        }

        // ================= USERS =================

        public int RegisterUser(User user, List<int> roleIds)
        {
            using var con = GetConnection();
            con.Open();

            // Insert User
            var cmd = new SqlCommand(@"
        INSERT INTO Users (Name, Email, Password, Address, Phone)
        VALUES (@n,@e,@p,@d,@m);
        SELECT SCOPE_IDENTITY();", con);

            cmd.Parameters.AddWithValue("@n", user.Name);
            cmd.Parameters.AddWithValue("@e", user.Email);
            cmd.Parameters.AddWithValue("@p", user.Password);
            cmd.Parameters.AddWithValue("@d", user.Address);
            cmd.Parameters.AddWithValue("@m", user.Phone);
            cmd.Parameters.AddWithValue("@m", user.Phone);


            int userId = Convert.ToInt32(cmd.ExecuteScalar());

            // Insert Roles
            foreach (var roleId in roleIds)
            {
                var roleCmd = new SqlCommand(
                    "INSERT INTO UserRoles (UserId, RoleId) VALUES (@uid,@rid)", con);

                roleCmd.Parameters.AddWithValue("@uid", userId);
                roleCmd.Parameters.AddWithValue("@rid", roleId);
                roleCmd.ExecuteNonQuery();
            }

            return userId;
        }
        //==========================login=========================
        public User? Login(string email, string password)
        {
            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand(
                "SELECT * FROM Users WHERE Email=@e AND Password=@p", con);

            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@p", password);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            var user = new User
            {
                Id = Convert.ToInt32(reader["Id"]),
                Name = Convert.ToString(reader["Name"]) ?? "",
                Email = Convert.ToString(reader["Email"]) ?? ""
            };

            reader.Close();

            // Load Roles
            var roleCmd = new SqlCommand(@"
        SELECT R.RoleId, R.Name
        FROM Roles R
        INNER JOIN UserRoles UR ON R.RoleId = UR.RoleId
        WHERE UR.UserId=@uid", con);

            roleCmd.Parameters.AddWithValue("@uid", user.Id);

            var roleReader = roleCmd.ExecuteReader();

            while (roleReader.Read())
            {
                user.Roles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = Convert.ToInt32(roleReader["RoleId"])
                });
            }

            return user;
        }

        //==========================login=========================
        public List<UserRole> GetUserRole(int userId)
        {
            var userRoles = new List<UserRole>();

            using var con = GetConnection();
            con.Open();

            // Load Roles
            var roleCmd = new SqlCommand(@"
                SELECT R.RoleId, R.Name
                FROM Roles R
                INNER JOIN UserRoles UR ON R.RoleId = UR.RoleId
                WHERE UR.UserId=@uid", con);

            roleCmd.Parameters.AddWithValue("@uid", userId);

            var roleReader = roleCmd.ExecuteReader();

            while (roleReader.Read())
            {
                userRoles.Add(new UserRole
                {
                    UserId = userId,
                    Name = Convert.ToString(roleReader["Name"] ?? default!) ?? string.Empty,
                    RoleId = Convert.ToInt32(roleReader["RoleId"])
                });
            }

            return userRoles;
        }

        //============email verification===============
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
                   ISNULL(P.Address,'') AS Address,
                   U.StripeCustomerId
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
                Address = Convert.ToString(dr["Address"]),
                StripeCustomerId = Convert.ToString(dr["StripeCustomerId"]),
            };
        }
        //=======================save profile data========================

        public void SaveUserProfile(User model)
        {
            using var con = GetConnection();
            con.Open();

            // =================Update Users table (Name + Email)
            var userCmd = new SqlCommand(
                "UPDATE Users SET Name=@n, Email=@e WHERE Id=@uid", con);

            userCmd.Parameters.AddWithValue("@n", model.Name);
            userCmd.Parameters.AddWithValue("@e", model.Email);
            userCmd.Parameters.AddWithValue("@uid", model.Id);
            userCmd.ExecuteNonQuery();

            //========================== Insert or Update UserProfiles table
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
        public void UpdateStripeCustId(int UserID, string StripeCustomerId)
        {
            using var con = GetConnection();
            con.Open();


            var userCmd = new SqlCommand(
                "UPDATE Users SET StripeCustomerId=@n WHERE Id=@uid", con);

            userCmd.Parameters.AddWithValue("@n", StripeCustomerId);
            userCmd.Parameters.AddWithValue("@uid", UserID);
            userCmd.ExecuteNonQuery();
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
        INSERT INTO Products (Name, Price, Image, CategoryId, Quantity)
        VALUES (@n,@p,@i,@c,@q)", con);

            cmd.Parameters.AddWithValue("@n", p.Name);
            cmd.Parameters.AddWithValue("@p", p.Price);
            cmd.Parameters.AddWithValue("@i", p.Image);
            cmd.Parameters.AddWithValue("@c", p.CategoryId);
            cmd.Parameters.AddWithValue("@q", p.Quantity);

            cmd.ExecuteNonQuery();
        }

        public Product? GetProduct(int id)
        {
            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand(
                "SELECT * FROM Products WHERE Id=@id", con);

            cmd.Parameters.AddWithValue("@id", id);

            using var rd = cmd.ExecuteReader();

            if (rd.Read())
            {
                return new Product
                {
                    Id = Convert.ToInt32(rd["Id"]),
                    Name = Convert.ToString(rd["Name"]) ?? "",
                    Price = Convert.ToDecimal(rd["Price"]),
                    Image = Convert.ToString(rd["Image"]) ?? "",
                    CategoryId = Convert.ToInt32(rd["CategoryId"]),
                    Quantity = Convert.ToInt32(rd["Quantity"]) // ✅ IMPORTANT FIX
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
            var cmd = new SqlCommand("SELECT * FROM Orders ORDER BY OrderDate DESC", con);
            using var rd = cmd.ExecuteReader();
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

            using (SqlConnection con = GetConnection())
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT Id, Name, Price, Image, CategoryId, Quantity FROM Products", con))
                {
                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            list.Add(new Product
                            {
                                Id = rd["Id"] != DBNull.Value ? Convert.ToInt32(rd["Id"]) : 0,
                                Name = rd["Name"] != DBNull.Value ? rd["Name"].ToString() ?? "" : "",
                                Price = rd["Price"] != DBNull.Value ? Convert.ToDecimal(rd["Price"]) : 0,
                                Image = rd["Image"] != DBNull.Value ? rd["Image"].ToString() ?? "" : "",
                                CategoryId = rd["CategoryId"] != DBNull.Value ? Convert.ToInt32(rd["CategoryId"]) : 0,
                                Quantity = rd["Quantity"] != DBNull.Value ? Convert.ToInt32(rd["Quantity"]) : 0
                            });
                        }
                    }
                }
            }

            return list;
        }

        public Product? GetProducts(int id)
        {
            var list = new List<Product>();

            using (SqlConnection con = GetConnection())
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT Id, Name, Price, Image, CategoryId, Quantity FROM Products WHERE Id=@Id", con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);

                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            list.Add(new Product
                            {
                                Id = rd["Id"] != DBNull.Value ? Convert.ToInt32(rd["Id"]) : 0,
                                Name = rd["Name"] != DBNull.Value ? rd["Name"].ToString() ?? "" : "",
                                Price = rd["Price"] != DBNull.Value ? Convert.ToDecimal(rd["Price"]) : 0,
                                Image = rd["Image"] != DBNull.Value ? rd["Image"].ToString() ?? "" : "",
                                CategoryId = rd["CategoryId"] != DBNull.Value ? Convert.ToInt32(rd["CategoryId"]) : 0,
                                Quantity = rd["Quantity"] != DBNull.Value ? Convert.ToInt32(rd["Quantity"]) : 0
                            });
                        }
                    }
                }
            }

            return list.FirstOrDefault();
        }

        public Product? GetProductById(int id)
        {
            return GetProducts(id);
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

                };
            }
            return null;
        }
        //=========================place order================================
        public int PlaceOrder(CheckoutViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Status))
                model.Status = "Pending";

            if (string.IsNullOrWhiteSpace(model.PaymentMethod))
                model.PaymentMethod = "Online";

            using var con = GetConnection();
            con.Open();

            var orderCmd = new SqlCommand(@"
        INSERT INTO Orders 
        (UserId, UserName, TotalAmount, PaymentMethod, Status, OrderDate, 
         StripeCheckoutSessionId, StripePaymentId)
        VALUES 
        (@uid,@uname,@total,@pay,@status,GETDATE(),
         @StripeCheckoutSessionId,@StripePaymentId);

        SELECT SCOPE_IDENTITY();", con);

            orderCmd.Parameters.AddWithValue("@uid", model.UserId);
            orderCmd.Parameters.AddWithValue("@uname", model.UserName ?? "");
            orderCmd.Parameters.AddWithValue("@total", model.TotalAmount);
            orderCmd.Parameters.AddWithValue("@pay", model.PaymentMethod);
            orderCmd.Parameters.AddWithValue("@status", model.Status);

            // ✅ Fix for NULL values (important)
            orderCmd.Parameters.AddWithValue("@StripeCheckoutSessionId",
                (object?)model.StripeCheckoutSessionId ?? DBNull.Value);

            orderCmd.Parameters.AddWithValue("@StripePaymentId",
                (object?)model.StripePaymentId ?? DBNull.Value);

            int orderId = Convert.ToInt32(orderCmd.ExecuteScalar());

            foreach (var item in model.Items)
            {
                var itemCmd = new SqlCommand(@"
            INSERT INTO OrderItems 
            (OrderId, ProductId, ProductName, Price, Quantity)
            VALUES (@oid,@pid,@name,@price,@qty)", con);

                itemCmd.Parameters.AddWithValue("@oid", orderId);
                itemCmd.Parameters.AddWithValue("@pid", item.ProductId);
                itemCmd.Parameters.AddWithValue("@name", item.ProductName ?? "");
                itemCmd.Parameters.AddWithValue("@price", item.Price);
                itemCmd.Parameters.AddWithValue("@qty", item.Quantity);

                itemCmd.ExecuteNonQuery();
            }

            return orderId;
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
        // ================= ORDERS =================
        public List<Order> GetOrdersByUser(int userId)
        {
            var list = new List<Order>();
            using var con = GetConnection();
            con.Open();
            var cmd = new SqlCommand("SELECT * FROM Orders WHERE UserId=@uid ORDER BY OrderDate DESC", con);
            cmd.Parameters.AddWithValue("@uid", userId);
            using var rd = cmd.ExecuteReader();
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
        //================= update product =======================
        public void UpdateProduct(Product product)
        {
            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand(
                @"UPDATE Products
                  SET Name = @name,
                      Price = @price,
                      Image = @image,
                      CategoryId = @category,
                      Quantity = @qty
                  WHERE Id = @id", con);

            cmd.Parameters.AddWithValue("@name", product.Name);
            cmd.Parameters.AddWithValue("@price", product.Price);
            cmd.Parameters.AddWithValue("@image", product.Image);
            cmd.Parameters.AddWithValue("@category", product.CategoryId);
            cmd.Parameters.AddWithValue("@qty", product.Quantity);
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
        // =================Get products with category names========================
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
                    Category = Convert.ToString(rd["CategoryName"]) ?? ""
                });
            }

            return list;
        }
        // ====================GetProductCount====================
        public int GetProductCount()
        {
            int count = 0;
            using (SqlConnection conn = new SqlConnection(_con))
            {
                string query = "SELECT COUNT(*) FROM Products";
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
        //============GetTotalRevenue=========================
        public decimal GetTotalRevenue()
        {
            using (SqlConnection conn = new SqlConnection(_con))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT ISNULL(SUM(TotalAmount),0) FROM Orders", conn);
                return (decimal)cmd.ExecuteScalar();
            }
        }
        //=====================UpdateUser profile =====================
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
        public User GetUserByEmail(string email)
        {
            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand(
                "SELECT * FROM Users WHERE Email=@e", con);
            cmd.Parameters.AddWithValue("@e", email);

            var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return new User
                {
                    Id = (int)reader["Id"],
                    Email = reader["Email"].ToString()
                };
            }
            return null;
        }
        //================SaveResetToken=====================
        public void SaveResetToken(int userId, string token, DateTime dateTime)
        {
            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand(
            @"UPDATE Users
      SET ResetToken=@t,
          TokenExpiry=DATEADD(hour,1,GETDATE())
      WHERE Id=@id", con);

            cmd.Parameters.AddWithValue("@t", token);
            cmd.Parameters.AddWithValue("@id", userId);

            cmd.ExecuteNonQuery();
        }

        //===============Validate token=========================
        public string? ValidateResetToken(string token)
        {
            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand(
            @"SELECT Email
            FROM Users
            WHERE ResetToken=@t
         AND TokenExpiry > GETDATE()", con);

            cmd.Parameters.AddWithValue("@t", token);

            var result = cmd.ExecuteScalar();
            return result?.ToString();
        }

        //=================== Update password==================
        public bool UpdatePasswordByToken(string token, string newPass)
        {
            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand(
                    @"UPDATE Users
                    SET Password=@p,
                     ResetToken=NULL,
                     TokenExpiry=NULL
                     WHERE ResetToken=@t
                     AND TokenExpiry > GETDATE()", con);

            cmd.Parameters.AddWithValue("@p", newPass);
            cmd.Parameters.AddWithValue("@t", token);

            return cmd.ExecuteNonQuery() > 0;
        }

        public async Task SaveChangesAsync()
        {

            await Task.Delay(10);

            Console.WriteLine("Changes saved successfully.");
        }
        //public List<OrderDetailViewModel> GetOrderItems(int orderId, int? userId = null)
        //{
        //    var items = new List<OrderDetailViewModel>();
        //    using var con = GetConnection();
        //    con.Open();

        //    var sql = @"
        //        SELECT OD.ProductId, OD.ProductName, OD.Quantity, OD.Price, U.Name AS UserName
        //        FROM OrderItems OD
        //        INNER JOIN Orders O ON O.Id = OD.OrderId
        //        INNER JOIN Users U ON U.Id = O.UserId
        //        WHERE OD.OrderId=@orderId";

        //    if (userId.HasValue)
        //        sql += " AND O.UserId=@userId";

        //    var cmd = new SqlCommand(sql, con);
        //    cmd.Parameters.AddWithValue("@orderId", orderId);
        //    if (userId.HasValue)
        //        cmd.Parameters.AddWithValue("@userId", userId.Value);

        //    using var reader = cmd.ExecuteReader();
        //    while (reader.Read())
        //    {
        //        items.Add(new OrderDetailViewModel
        //        {
        //            ProductId = Convert.ToInt32(reader["ProductId"]),
        //            ProductName = Convert.ToString(reader["ProductName"]),
        //            Quantity = Convert.ToInt32(reader["Quantity"]),
        //            Price = Convert.ToDecimal(reader["Price"]),
        //            UserName = Convert.ToString(reader["UserName"])
        //        });
        //    }
        //    return items;
        //}
        //    public List<OrderDetailViewModel> GetOrderItems(int orderId)
        //    {
        //        var items = new List<OrderDetailViewModel>();

        //        using var con = GetConnection();
        //        con.Open();

        //        var cmd = new SqlCommand(@"
        //    SELECT ProductId, ProductName, Quantity, Price, U.Name AS UserName
        //    FROM OrderItems OD
        //    INNER JOIN Orders O ON O.Id = OD.OrderId
        //    INNER JOIN Users U ON U.Id = O.UserId
        //    WHERE OD.OrderId = @orderId
        //", con);

        //        cmd.Parameters.AddWithValue("@orderId", orderId);
        //        cmd.Parameters.AddWithValue("@userId", UserId);

        //        using var reader = cmd.ExecuteReader();
        //        while (reader.Read())
        //        {
        //            items.Add(new OrderDetailViewModel
        //            {
        //                ProductId = Convert.ToInt32(reader["ProductId"]),
        //                ProductName = Convert.ToString(reader["ProductName"]),
        //                Quantity = Convert.ToInt32(reader["Quantity"]),
        //                Price = Convert.ToDecimal(reader["Price"]),
        //                UserName = Convert.ToString(reader["UserName"])
        //            });
        //        }

        //        return items;
        //    }
        // ================= ORDER DETAILS (FINAL FIX) =================
        public List<OrderDetailViewModel> GetOrderItems(int orderId, int? userId = null)
        {
            var items = new List<OrderDetailViewModel>();

            using var con = GetConnection();
            con.Open();

            string sql = @"
                 SELECT 
                     OD.ProductId,
                     OD.ProductName,
                     OD.Quantity,
                     OD.Price,
                     U.Name AS UserName
                      FROM OrderItems OD
                      INNER JOIN Orders O ON O.Id = OD.OrderId
                      INNER JOIN Users U ON U.Id = O.UserId
                      WHERE OD.OrderId = @orderId";

            if (userId.HasValue)
                sql += " AND O.UserId = @userId";

            using var cmd = new SqlCommand(sql, con);

            cmd.Parameters.AddWithValue("@orderId", orderId);

            if (userId.HasValue)
                cmd.Parameters.AddWithValue("@userId", userId.Value);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                items.Add(new OrderDetailViewModel
                {
                    ProductId = Convert.ToInt32(reader["ProductId"]),
                    ProductName = Convert.ToString(reader["ProductName"]) ?? "",
                    Quantity = Convert.ToInt32(reader["Quantity"]),
                    Price = Convert.ToDecimal(reader["Price"]),
                    UserName = Convert.ToString(reader["UserName"]) ?? ""
                });
            }

            return items;
        }
        // ================= DASHBOARD FULL DATA =================
        public DashboardViewModel GetDashboardData()
        {
            var model = new DashboardViewModel();

            using var con = GetConnection();
            con.Open();

            // SUMMARY
            model.ProductCount = GetProductCount();
            model.OrderCount = GetOrderCount();
            model.UserCount = GetUserCount();
            model.Revenue = GetTotalRevenue();
            model.Profit = model.Revenue * 0.20m;
            // ================= LOW STOCK COUNT =================
            var lowStockCmd = new SqlCommand(
                "SELECT COUNT(*) FROM Products WHERE Quantity <= 5", con);
            model.LowStockCount = Convert.ToInt32(lowStockCmd.ExecuteScalar());

            // MONTHLY DATA
            var cmd = new SqlCommand(@"
        SELECT MONTH(OrderDate) M,
               SUM(TotalAmount) Revenue,
               COUNT(*) Orders
        FROM Orders
        GROUP BY MONTH(OrderDate)
        ORDER BY M", con);

            // ================= CATEGORY SALES (FOR PIE CHART) =================
            var categoryCmd = new SqlCommand(@"
        SELECT C.Name, SUM(OI.Quantity * OI.Price) AS TotalSales
        FROM OrderItems OI
        INNER JOIN Products P ON OI.ProductId = P.Id
        INNER JOIN Categories C ON P.CategoryId = C.Id
        GROUP BY C.Name", con);

            using (var cr = categoryCmd.ExecuteReader())
            {
                while (cr.Read())
                {
                    string category = cr["Name"].ToString() ?? "";
                    decimal total = Convert.ToDecimal(cr["TotalSales"]);
                    model.CategorySales.Add(category, total);
                }
            }

            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                decimal rev = Convert.ToDecimal(rd["Revenue"]);
                int ord = Convert.ToInt32(rd["Orders"]);

                model.MonthlyRevenue.Add(rev);
                model.MonthlyProfit.Add(rev * 0.2m);
                model.MonthlyOrders.Add(ord);
            }
            rd.Close();

            // TOP PRODUCTS
            var prodCmd = new SqlCommand(@"
                SELECT TOP 5 ProductName, COUNT(*) Sold
                FROM OrderItems
                GROUP BY ProductName
                ORDER BY Sold DESC", con);

            using var pr = prodCmd.ExecuteReader();
            while (pr.Read())
            {
                model.TopProducts.Add(new TopCountry.TopProduct
                {
                    Name = pr["ProductName"].ToString() ?? "",
                    Total = Convert.ToInt32(pr["Sold"])
                });
            }
            pr.Close();

            // RECENT ORDERS
            var recentCmd = new SqlCommand(@"
        SELECT TOP 5 UserName, OrderDate, TotalAmount
        FROM Orders
        ORDER BY OrderDate DESC", con);

            using var rr = recentCmd.ExecuteReader();
            while (rr.Read())
            {
                model.RecentOrders.Add(new RecentOrder
                {
                    ProductName = rr["UserName"].ToString() ?? "",
                    OrderDate = Convert.ToDateTime(rr["OrderDate"]),
                    Amount = Convert.ToDecimal(rr["TotalAmount"])
                });
            }

            return model;
        }

        //================UpdateStripeSession=====================
        public void UpdateStripeSession(int orderId, string sessionId)
        {
            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand(
                "UPDATE Orders SET StripeCheckoutSessionId=@sid WHERE Id=@id", con);

            cmd.Parameters.AddWithValue("@sid", sessionId);
            cmd.Parameters.AddWithValue("@id", orderId);

            cmd.ExecuteNonQuery();
        }
        //==============MarkOrderPaid=================
        public void MarkOrderPaid(int orderId)
        {
            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand(
                @"UPDATE Orders 
          SET Status='Paid',
              PaymentMethod='Online'
          WHERE Id=@id", con);

            cmd.Parameters.AddWithValue("@id", orderId);
            cmd.ExecuteNonQuery();
        }
        //===================MarkOrderCancelled================
        public void MarkOrderCancelled(int orderId)
        {
            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand(
                @"UPDATE Orders 
          SET Status='Cancelled'
          WHERE Id=@id", con);

            cmd.Parameters.AddWithValue("@id", orderId);
            cmd.ExecuteNonQuery();
        }
        public Order GetOrderById(int orderId)
        {
            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand(@"
        SELECT O.Id, O.UserName, O.TotalAmount, U.Email
        FROM Orders O
        INNER JOIN Users U ON O.UserId = U.Id
        WHERE O.Id=@id", con);

            cmd.Parameters.AddWithValue("@id", orderId);

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return new Order
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    UserName = Convert.ToString(reader["UserName"]),
                    Email = Convert.ToString(reader["Email"]),
                    TotalAmount = Convert.ToDecimal(reader["TotalAmount"])
                };
            }

            return null;
        }
        //======================DeleteOrder===================
        public void DeleteOrder(int orderId)
        {
            using var con = GetConnection();
            con.Open();

            // First delete order items (because of Foreign Key)
            var cmdItems = new SqlCommand(
                "DELETE FROM OrderItems WHERE OrderId = @id", con);

            cmdItems.Parameters.AddWithValue("@id", orderId);
            cmdItems.ExecuteNonQuery();

            // Then delete order
            var cmdOrder = new SqlCommand(
                "DELETE FROM Orders WHERE Id = @id", con);

            cmdOrder.Parameters.AddWithValue("@id", orderId);
            cmdOrder.ExecuteNonQuery();
        }
      public SellerDashboardViewModel GetSellerDashboardData(int sellerId)
        {
            // Example: fetch dashboard data for seller
            var products = GetProductsBySeller(sellerId);
            var orders = GetOrdersBySeller(sellerId);

            return new SellerDashboardViewModel
            {
                SellerName = $"Seller {sellerId}",
                SellerId = sellerId,
                ProductCount = products.Count,
                Order = orders.Count,
                Revenue = orders.Sum(o => o.TotalAmount),
                Product = products,
                Orders = orders
            };
        }
        public List<Product> GetProductsBySeller(int sellerId)
        {
            var list = new List<Product>();
            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand("SELECT * FROM Products WHERE SellerId=@sid", con);
            cmd.Parameters.AddWithValue("@sid", sellerId);

            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(new Product
                {
                    Id = Convert.ToInt32(rd["Id"]),
                    Name = Convert.ToString(rd["Name"]) ?? "",
                    Price = Convert.ToDecimal(rd["Price"]),
                    Image = Convert.ToString(rd["Image"]) ?? "",
                    CategoryId = Convert.ToInt32(rd["CategoryId"]),
                    Quantity = Convert.ToInt32(rd["Quantity"]),
                    SellerId = Convert.ToInt32(rd["SellerId"]) 
                });
            }

            return list;
        }
        public List<Order> GetOrdersBySeller(int sellerId)
        {
            var list = new List<Order>();
            using var con = GetConnection();
            con.Open();

            var cmd = new SqlCommand("SELECT * FROM Orders WHERE SellerId=@sid ORDER BY OrderDate DESC", con);
            cmd.Parameters.AddWithValue("@sid", sellerId);

            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(new Order
                {
                    Id = Convert.ToInt32(rd["Id"]),
                    UserId = Convert.ToInt32(rd["UserId"]),
                    UserName = Convert.ToString(rd["UserName"]) ?? "",
                    TotalAmount = Convert.ToDecimal(rd["TotalAmount"]),
                    PaymentMethod = Convert.ToString(rd["PaymentMethod"]) ?? "",
                    Status = Convert.ToString(rd["Status"]) ?? "",
                    OrderDate = Convert.ToDateTime(rd["OrderDate"])
                });
            }
            return list;
        }

        
    }
}