namespace EC.Models
{
    public class SellerDashboardViewModel

    {
        public int ProductCount { get; set; }
        public int Order { get; set; }
        public int LowStockCount { get; set; }

        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }

        // CHART DATA
        public List<decimal> MonthlyRevenue { get; set; } = new();
        public List<decimal> MonthlyProfit { get; set; } = new();
        public List<int> MonthlyOrders { get; set; } = new();

        // CATEGORY SALES
        public Dictionary<string, decimal> CategorySales { get; set; } = new();

        // TABLE DATA
        public List<TopProduct> TopProducts { get; set; } = new List<TopProduct>();
        public List<RecentOrder> RecentOrders { get; set; } = new();

        public int SellerId { get; set; }
        public string SellerName { get; set; } = default!;
        public List<Product> Product { get; set; } = [];
        public List<Order> Orders { get; set; } = [];

    }

    public class TopProduct
    {
        public string TopProductName { get; set; } = "";
        public int TotalSold { get; set; }


    }

    public class RecentOrder
    {
        public string ProductName { get; set; } = "";
        public DateTime OrderDate { get; set; }

        public decimal Total { get; set; }
        public decimal Amount { get; internal set; }
    }
}