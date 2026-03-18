using EC.Helpers;
using EC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace EC.ApiControllers
{
   [Route("api/[controller]")]
    [ApiController]
    public class ChartController : ControllerBase
    {
        private readonly string _con;

        public ChartController(IConfiguration config)
        {
            _con = config.GetConnectionString("DefaultConnection");
        }

       [Authorize(Roles = "Seller, Admin")]
        [HttpGet("seller-admin-dashboard")]
        public IActionResult GetSellerDashboard(DateTime fromDate, DateTime toDate)
        {
            int? sellerId = null;

            if (HttpContext.IsSeller())
            {
                sellerId = HttpContext.UserId();
            }

            using (SqlConnection conn = new SqlConnection(_con))
            {
                string query = @"
                SELECT 
                    FORMAT(DATEFROMPARTS(OrderDateYear, OrderDateMonth, 1), 'MMM-yy') AS [Month], 
                    Revenue,
                    ProductQuatitySold,
                    OrdersCount
                FROM (
                    SELECT YEAR(OrderDate) OrderDateYear, MONTH(OrderDate) OrderDateMonth,
                           SUM(oi.Quantity * oi.Price) AS Revenue,
                           SUM(oi.Quantity) AS ProductQuatitySold, 
                           COUNT(DISTINCT o.ID) OrdersCount 
                    FROM Orders o
                    INNER JOIN OrderItems oi ON o.Id = oi.OrderId
                    INNER JOIN Products p ON p.Id = oi.ProductId
                    WHERE 1 = 1
                    AND Cast(o.OrderDate as date) BETWEEN @FromDate AND @ToDate
                    AND (ISNULL(@SellerId, 0) = 0 OR p.SellerId = @SellerId)
                    GROUP BY YEAR(o.OrderDate), MONTH(o.OrderDate)
                ) q
                ORDER BY OrderDateYear, OrderDateMonth
                ";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.Parameters.AddWithValue("@FromDate", fromDate);
                    cmd.Parameters.AddWithValue("@ToDate", toDate);
                    cmd.Parameters.AddWithValue("@SellerId", sellerId ?? 0);

                    using (var ada = new SqlDataAdapter(cmd))
                    {
                        try
                        {
                            conn.Open();

                            var dt = new DataTable();
                            ada.Fill(dt);

                            var result = dt.AsEnumerable().Select(x => new
                            {
                                Month = x.Field<string?>("Month") ?? default!,
                                Revenue = x.Field<decimal?>("Revenue") ?? default!,
                                ProductQuatitySold = x.Field<int?>("ProductQuatitySold") ?? default!,
                                Orders = x.Field<int?>("OrdersCount") ?? default!,
                                Profit = x.Field<decimal?>("Revenue") ?? default!,

                            }).ToList();


                            return Ok(result);
                        }
                        finally
                        {
                            conn.Close();
                        }
                        
                    }
                }
            }
        }

        // ================= YEARLY CHART =================
        [HttpGet("yearly")]
        public IActionResult GetYearly(int year, int? userId = null)
        {
            List<object> data = new List<object>();

            using (SqlConnection conn = new SqlConnection(_con))
            {
                conn.Open();

                string query = @"
                SELECT 
                    MONTH(OrderDate) AS Month,
                    SUM(TotalAmount) AS Revenue
                FROM Orders
                WHERE YEAR(OrderDate) = @Year
                " + (userId != null ? "AND UserId = @UserId" : "") + @"
                GROUP BY MONTH(OrderDate)
                ORDER BY Month";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Year", year);

                if (userId != null)
                    cmd.Parameters.AddWithValue("@UserId", userId);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    data.Add(new
                    {
                        month = reader["Month"],
                        revenue = reader["Revenue"]
                    });
                }
            }

            return Ok(data);
        }

        // ================= WEEKLY CHART =================
        [HttpGet("weekly")]
        public IActionResult GetWeekly(int? userId = null)
        {
            List<object> data = new List<object>();

            using (SqlConnection conn = new SqlConnection(_con))
            {
                conn.Open();

                string query = @"
                SELECT 
                    DATENAME(WEEKDAY, OrderDate) AS Day,
                    SUM(TotalAmount) AS Revenue
                FROM Orders 
                WHERE OrderDate >= DATEADD(DAY, -7, GETDATE())
                " + (userId != null ? "AND UserId = @UserId" : "") + @"
                GROUP BY DATENAME(WEEKDAY, OrderDate), DATEPART(WEEKDAY, OrderDate)
                ORDER BY DATEPART(WEEKDAY, OrderDate)";

                SqlCommand cmd = new SqlCommand(query, conn);

                if (userId != null)
                    cmd.Parameters.AddWithValue("@UserId", userId);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    data.Add(new
                    {
                        day = reader["Day"],
                        revenue = reader["Revenue"]
                    });
                }
            }

            return Ok(data);
        }
    }
}