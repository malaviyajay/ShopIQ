namespace EC.Models
{
    public class SellerDashboardViewModel
    {
        public int ProductCount { get; set; }
        public int Order { get; set; }
        public int LowStockCount { get; set; }
        public List<Product> LowStockProducts { get; set; } = new List<Product>();
      
        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }

        public List<decimal> MonthlyRevenue { get; set; } = new();
        public List<decimal> MonthlyProfit { get; set; } = new();
        public List<int> MonthlyOrders { get; set; } = new();


        public List<RecentOrder> Recentorders { get; set; } = new();
        public List<TopProduct> TopProducts { get; set; } = new();

        public Dictionary<string, decimal> CategorySales { get; set; } = new();

        public int SellerId { get; set; }
        public string SellerName { get; set; } = "";
        public List<Product> Product { get; set; } = new();
        public List<Order> Orders { get; set; } = new();
    }

    public class TopProduct
    {
        public string TopProductName { get; set; } = "";
        public int TotalSold { get; set; }
    }

    public class RecentOrder
    {
        public string Name { get; set; } = "";
        public DateTime OrderDate { get; set; }
        public decimal Amount { get; set; }
    }
}