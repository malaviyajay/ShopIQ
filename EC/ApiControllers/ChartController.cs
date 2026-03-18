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
        public IActionResult GetSellerDashboard(DateTime? fromDate, DateTime? toDate)
        {
            int? sellerId = null;

            if (HttpContext.IsSeller())
            {
                sellerId = HttpContext.UserId();
            }

            
            DateTime startDate = fromDate ?? new DateTime(DateTime.Now.Year, 1, 1);
            DateTime endDate = toDate ?? new DateTime(DateTime.Now.Year, 12, 31);

            using (SqlConnection conn = new SqlConnection(_con))
            {
                string query = @"
        WITH Months AS (
            SELECT 1 AS MonthNum UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL
            SELECT 4 UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL
            SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9 UNION ALL
            SELECT 10 UNION ALL SELECT 11 UNION ALL SELECT 12
        ),
        SalesData AS (
            SELECT 
                MONTH(o.OrderDate) AS OrderMonth,
                SUM(oi.Quantity * oi.Price) AS Revenue,
                COUNT(DISTINCT o.ID) AS OrdersCount
            FROM Orders o
            INNER JOIN OrderItems oi ON o.Id = oi.OrderId
            INNER JOIN Products p ON p.Id = oi.ProductId
            WHERE 
                CAST(o.OrderDate AS DATE) BETWEEN @FromDate AND @ToDate
                AND (ISNULL(@SellerId, 0) = 0 OR p.SellerId = @SellerId)
            GROUP BY MONTH(o.OrderDate)
        )
        SELECT 
            FORMAT(DATEFROMPARTS(YEAR(@FromDate), m.MonthNum, 1), 'MMM') AS Month,
            ISNULL(s.Revenue, 0) AS Revenue,
            ISNULL(s.OrdersCount, 0) AS Orders
        FROM Months m
        LEFT JOIN SalesData s ON m.MonthNum = s.OrderMonth
        ORDER BY m.MonthNum
        ";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FromDate", startDate);
                    cmd.Parameters.AddWithValue("@ToDate", endDate);
                    cmd.Parameters.AddWithValue("@SellerId", sellerId ?? 0);

                    conn.Open();

                    var dt = new DataTable();
                    new SqlDataAdapter(cmd).Fill(dt);

                    var result = dt.AsEnumerable().Select(x => new
                    {
                        month = x.Field<string>("Month"),   // ✅ lowercase FIX
                        revenue = x.Field<decimal>("Revenue"),
                        orders = x.Field<int>("Orders"),
                        profit = x.Field<decimal>("Revenue") * 0.2m
                    }).ToList();

                    return Ok(result);
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