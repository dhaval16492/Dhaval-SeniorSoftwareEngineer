using System;
using System.Collections.Generic;
using System.Linq;

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
    public List<Match> Matches { get; set; }
}

public class Match
{
    public string OrderId { get; set; }
    public int Volume { get; set; }
    public decimal Notional { get; set; }
}

public class OrderBookMatcher
{
    /// <summary>
    /// Match orders as per pro-rata algorithm
    /// </summary>
    /// <param name="orders"></param>
    public static void MatchOrders(List<Order> orders)
    {
        var buyOrders = orders.Where(o => o.Direction == "Buy" && o.Volume > 0).OrderByDescending(o => o.Notional).ThenBy(o => o.OrderDateTime);
        var sellOrders = orders.Where(o => o.Direction == "Sell" && o.Volume > 0).OrderBy(o => o.Notional).ThenBy(o => o.OrderDateTime);

        int totalBuyVolume = buyOrders.Sum(o => o.Volume);

        foreach (var sellOrder in sellOrders)
        {
            var remainingVolume = sellOrder.Volume;
            int totalmatchVolume = 0;
            foreach (var buyOrder in buyOrders)
            {
                int initialBuyVolume = buyOrder.Volume;
                if (buyOrder.Volume > 0 && remainingVolume > 0)
                {
                    var buyPercentage = (decimal)buyOrder.Volume / totalBuyVolume;
                    var matchVolume = (int)Math.Round(buyPercentage * sellOrder.Volume);


                    totalmatchVolume += matchVolume;
                    buyOrder.Volume -= matchVolume;
                    remainingVolume -= matchVolume;
                    DisplayMatchDetails(buyOrder, sellOrder, matchVolume);

                    if (buyOrder.Volume == 0)
                    {
                        // Move to the next buy order
                        break;
                    }

                }
            }
            DisplayOrderBook(sellOrder, GetMatchState(totalmatchVolume, sellOrder.Volume));
            foreach (var buyOrder in buyOrders)
            {
                foreach (var matches in buyOrder.Matches)
                {
                    if (sellOrder.OrderId == matches.OrderId)
                    {
                        Console.WriteLine($"  {buyOrder.OrderId} - {matches.Volume}");
                    }
                }
            }


        }
    }

    /// <summary>
    /// Display match details of orders
    /// </summary>
    /// <param name="buyOrder"></param>
    /// <param name="sellOrder"></param>
    /// <param name="matchVolume"></param>
    private static void DisplayMatchDetails(Order buyOrder, Order sellOrder, int matchVolume)
    {
        int totalbuy = buyOrder.Volume + matchVolume;
        if (buyOrder.Matches != null && buyOrder.Matches.Count > 0)
        {
            totalbuy = buyOrder.Matches?.Sum(x => x.Volume) ?? 0 + buyOrder.Volume;
        }
        Console.WriteLine($"{buyOrder.OrderId} - {GetMatchState(buyOrder.Volume, totalbuy)}");
        Console.WriteLine($"  {sellOrder.OrderId} - {matchVolume}");
        if (buyOrder.Matches == null)
        {
            buyOrder.Matches = new List<Match>();
        }

        buyOrder.Matches.Add(new Match
        {
            OrderId = sellOrder.OrderId,
            Volume = matchVolume,
            Notional = sellOrder.Notional
        });
    }

    /// <summary>
    /// Display order book
    /// </summary>
    /// <param name="order"></param>
    /// <param name="matchState"></param>
    private static void DisplayOrderBook(Order order, MatchState matchState)
    {

        Console.WriteLine($"{order.OrderId} - {matchState}");
        if (order.Matches != null)
        {
            foreach (var match in order.Matches)
            {
                Console.WriteLine($"  {match.OrderId} - {match.Volume}");
            }
        }
    }

    /// <summary>
    /// Get match state
    /// </summary>
    /// <param name="orderVolume"></param>
    /// <param name="initialVolume"></param>
    /// <returns></returns>
    private static MatchState GetMatchState(int orderVolume, int initialVolume)
    {
        if (orderVolume == initialVolume)
        {
            return MatchState.FullMatch;
        }
        else if (orderVolume > 0 && orderVolume < initialVolume)
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
            new Order { CompanyId = "A", OrderId = "A1", Direction = "Buy", Volume = 50, Notional = 5.00m, OrderDateTime = DateTime.Parse("09:27:43") },
            new Order { CompanyId = "B", OrderId = "B1", Direction = "Buy", Volume = 200, Notional = 5.00m, OrderDateTime = DateTime.Parse("10:21:46") },
            new Order { CompanyId = "C", OrderId = "C1", Direction = "Sell", Volume = 200, Notional = 5.00m, OrderDateTime = DateTime.Parse("10:26:18") },
          };

        OrderBookMatcher.MatchOrders(orders);
    }
}
