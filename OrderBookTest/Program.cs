using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public enum MatchState
{
    NoMatch,
    FullMatch,
    PartialMatch,
    InvalidOrder
}

public class Order
{
    public string CompanyId { get; set; }
    public string OrderId { get; set; }
    public string Direction { get; set; }
    public int Volume { get; set; }
    public decimal Notional { get; set; }
    public DateTime OrderDateTime { get; set; }
}

public class Match
{
    public string OrderId { get; set; }
    public int Volume { get; set; }
    public decimal Notional { get; set; }
}

public class OrderBookMatcher
{
    public static List<string> OutputString = new List<string>();

    /// <summary>
    /// Match orders as per price-time-priority algorithm
    /// </summary>
    /// <param name="orders"></param>
    public static void MatchOrders(List<Order> orders)
    {
        var buyOrders = orders.Where(o => o.Direction == "Buy" && o.Volume > 0).OrderByDescending(o => o.Notional).ThenBy(o => o.OrderDateTime);
        var sellOrders = orders.Where(o => o.Direction == "Sell" && o.Volume > 0).OrderBy(o => o.Notional).ThenBy(o => o.OrderDateTime);

        foreach (var buyOrder in buyOrders)
        {
            int initialBuyVolume = buyOrder.Volume;
            foreach (var sellOrder in sellOrders)
            {
                int initialSellVolume = sellOrder.Volume;
                if (buyOrder.Notional >= sellOrder.Notional && buyOrder.Volume > 0 && sellOrder.Volume > 0)
                {
                    var matchVolume = Math.Min(buyOrder.Volume, sellOrder.Volume);

                    DisplayMatchDetails(buyOrder, sellOrder, matchVolume);
                    DisplayMatchDetails(sellOrder, buyOrder, matchVolume);

                    buyOrder.Volume -= matchVolume;
                    sellOrder.Volume -= matchVolume;

                    if (buyOrder.Volume == 0)
                    {
                        break; // Move to the next buy order
                    }
                    DisplayOrderBook(sellOrder, GetMatchState(sellOrder, initialSellVolume));

                }
            }
            DisplayOrderBook(buyOrder, GetMatchState(buyOrder, initialBuyVolume));

        }


    }

    /// <summary>
    /// Display Match details
    /// </summary>
    /// <param name="buyOrder"></param>
    /// <param name="sellOrder"></param>
    /// <param name="matchVolume"></param>
    private static void DisplayMatchDetails(Order buyOrder, Order sellOrder, int matchVolume)
    {
        OutputString.Add($"{buyOrder.OrderId} - {sellOrder.OrderId} - {matchVolume}");
    }

    /// <summary>
    /// Display order book
    /// </summary>
    /// <param name="order"></param>
    /// <param name="matchState"></param>
    private static void DisplayOrderBook(Order order, MatchState matchState)
    {
        string state = matchState == MatchState.NoMatch ?
            $"{matchState.ToString()} - the higher priced orders took priority" : matchState.ToString();
        OutputString.Add($"{order.OrderId} - {state}");
    }

    /// <summary>
    /// Get match state order
    /// </summary>
    /// <param name="order"></param>
    /// <param name="initialVolume"></param>
    /// <returns></returns>
    private static MatchState GetMatchState(Order order, int initialVolume)
    {
        if (order.Volume == 0)
        {
            return MatchState.FullMatch;
        }
        else if (order.Volume > 0 && order.Volume < initialVolume)
        {
            return MatchState.PartialMatch;
        }
        else
        {
            return MatchState.NoMatch;
        }
    }
}

class Program
{
    static void Main()
    {
        List<Order> orders = new List<Order>
        {
            new Order { CompanyId = "A", OrderId = "A1", Direction = "Buy", Volume = 100, Notional = 4.99m, OrderDateTime = DateTime.Parse("09:27:43") },
            new Order { CompanyId = "B", OrderId = "B1", Direction = "Buy", Volume = 200, Notional = 5.00m, OrderDateTime = DateTime.Parse("10:21:46") },
            new Order { CompanyId = "C", OrderId = "C1", Direction = "Buy", Volume = 150, Notional = 5.00m, OrderDateTime = DateTime.Parse("10:26:18") },
            new Order { CompanyId = "D", OrderId = "D1", Direction = "Sell", Volume = 150, Notional = 5.00m, OrderDateTime = DateTime.Parse("10:32:41") },
            new Order { CompanyId = "E", OrderId = "E1", Direction = "Sell", Volume = 100, Notional = 5.00m, OrderDateTime = DateTime.Parse("10:33:07") }
        };

        OrderBookMatcher.MatchOrders(orders);

        StringBuilder sb = new StringBuilder();

        List<string> orderList = OrderBookMatcher.OutputString.OrderBy(x=>x).ToList();
        orderList.ForEach((output) =>
        {
            sb.AppendLine(output);
        });
        Console.WriteLine(sb.ToString());
    }
}
