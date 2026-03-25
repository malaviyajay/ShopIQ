
namespace EC.Models
{
    public class Products
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public string? Image { get; set; }
        public int CategoryId { get; set; }
        public int Quantity { get; set; }
        public string? Category { get; set; }
    
    }

    public class DashboardViewModel
    {
       
        public int ProductCount { get; set; }
        public int OrderCount { get; set; }
        public int UserCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }
        public decimal RevenueGrowth { get; set; }
        public decimal OrderGrowth { get; set; }
        public decimal UserGrowth { get; set; }
        public decimal ProductGrowth { get; set; }
        public int PendingReviewCount { get; set; }


        public List<decimal> MonthlyRevenue { get; set; } = new();
        public List<decimal> MonthlyProfit { get; set; } = new();
        public List<int> MonthlyOrders { get; set; } = new();

      
        public List<RecentOrder> RecentOrders { get; set; } = new();
        public List<TopCountry.TopProduct> TopProducts { get; set; } = new();

        public int LowStockCount { get; set; }
        public Dictionary<string, decimal> CategorySales { get; set; } = new();
        public decimal ThisMonthRevenue { get; set; }
        public decimal LastMonthRevenue { get; set; }
        public int LatestOrderId { get; set; }
    }

    public class TopCountry
    {
        public string Country { get; set; } = "";
        public int TotalOrders { get; set; }

        public class TopProduct
        {
            public int ProductId { get; set; }
            public string Name { get; set; } = "";
            public int Quantity { get; set; }
            public decimal Total { get; set; }
        }
    }

}