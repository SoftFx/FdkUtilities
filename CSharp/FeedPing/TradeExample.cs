using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SoftFX.Extended;
using SoftFX.Extended.Events;

namespace DataFeedExamples
{
    class TradeExample : Example
    {
        private static string _datetimeformat = "yyyMMdd-hh:mm:ss.fff";
        private static bool _stop;
        private static readonly AutoResetEvent TradeIsReady = new AutoResetEvent(false);
        private static readonly AutoResetEvent PositionCreated = new AutoResetEvent(false);

        private readonly Thread _tradeThread = new Thread(TradeThread);
        private AccountInfo _account;
        private string[] _symbols;
        private readonly List<string> _positions = new List<string>();
        private readonly Dictionary<string, List<long>> _results = new Dictionary<string, List<long>>();
        private static Stopwatch _stopwatch = new Stopwatch();
        private static bool _isError;

        public TradeExample(string address, string username, string password) : base(address, username, password)
        {
            this.Feed.Logon += FeedOnLogon;

            this.TradeEnabled = true;
            this.Trade.Logon += OnLogon;
            this.Trade.Logout += OnLogout;
            this.Trade.AccountInfo += OnAccountInfo;
            this.Trade.ExecutionReport += OnExecutionReport;
        }

        private void FeedOnLogon(object sender, LogonEventArgs logonEventArgs)
        {
            _symbols = Settings1.Default.SubscribeToSymbols.Split(',');
            this.Feed.Server.SubscribeToQuotes(_symbols, Settings1.Default.Depth);
        }

        private void OnLogon(object sender, LogonEventArgs e)
        {
            Console.WriteLine("OnLogon(): Trade {0}", e);
        }

        private void OnLogout(object sender, LogoutEventArgs e)
        {
            Console.WriteLine("OnLogout(): Trade {0}", e);
        }

        private void OnAccountInfo(object sender, AccountInfoEventArgs e)
        {
            Console.WriteLine("OnAccountInfo(): {0}", e.Information);
            _account = e.Information;
            TradeIsReady.Set();
        }

        private void OnExecutionReport(object sender, ExecutionReportEventArgs e)
        {
            long elapsed = _stopwatch.ElapsedMilliseconds;
            SetResult(e.Report.OrderId, elapsed);

            if (e.Report.OrderType == TradeRecordType.Position)
                PositionCreated.Set();

            _stopwatch.Restart();
        }

        protected override void RunExample()
        {
            _tradeThread.Start(this);

            Console.WriteLine("Press any key to stop...");
            Console.ReadKey();

            _stop = true;
            _tradeThread.Join();

            if (!_isError)
            {
                MathList[] stats = new MathList[4] {new MathList(), new MathList(), new MathList(), new MathList()};

                Console.WriteLine();
                Console.WriteLine("Order\t| New\t\t| Calculated\t| Filled\t| Calculated");
                int i = 0;
                foreach (var result in _results)
                {
                    Console.WriteLine("{0}\t| {1}", result.Key, string.Join("\t\t| ", result.Value));

                    stats[0].Add(result.Value[0]);
                    stats[1].Add(result.Value[1]);
                    stats[2].Add(result.Value[2]);
                    stats[3].Add(result.Value[3]);
                }
                Console.WriteLine();
                Console.WriteLine("Mean\t| {0}\t\t| {1}\t\t| {2}\t\t| {3}", stats[0].Mean(), stats[1].Mean(), stats[2].Mean(), stats[3].Mean());
                Console.WriteLine("Sd\t| {0:F2}\t\t| {1:F2}\t\t| {2:F2}\t\t| {3:F2}", stats[0].Sd(), stats[1].Sd(), stats[2].Sd(), stats[3].Sd());

                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                _stopwatch.Stop();
            }
            CloseAll();
        }

        private string ExecutionReportToString(ExecutionReport r)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}", r.OrderId);
            //if (r.Created != null) sb.AppendFormat(" {0} ", r.Created.Value.ToString(_datetimeformat));
            sb.AppendFormat(" {0}", r.OrderStatus);
            sb.AppendFormat(" {0}", r.ExecutionType);
            //sb.AppendFormat(" {0}", r.OrderType);
            //sb.AppendFormat(" {0}", r.OrderSide);
            //sb.AppendFormat(" {0}", r.LeavesVolume);
            //sb.AppendFormat(" {0}", r.Symbol);
            //sb.AppendFormat(" {0}", r.Price);
            //if (r.Modified != null) sb.AppendFormat(" {0} ", r.Modified.Value.ToString(_datetimeformat));
            return sb.ToString();
        }

        private void SetResult(string orderId, long value)
        {
            if (!_results.ContainsKey(orderId))
                _results.Add(orderId, new List<long> {value});
            else
                _results[orderId].Add(value);
        }

        private double TryGetOpenPrice(string symbol, TradeRecordSide side, int points = 0)
        {
            double price = 0;
            if (side == TradeRecordSide.Buy)
                this.Feed.Cache.TryGetAsk(symbol, out price);
            else
                this.Feed.Cache.TryGetBid(symbol, out price);

            if (points != 0)
            {
                var symbolInfo = this.Feed.Cache.Symbols.FirstOrDefault(s => s.Name == symbol);
                if (symbolInfo != null)
                    price = price + Math.Pow(10, -symbolInfo.Precision) * points;
            }

            return price;
        }

        private void SendMarketOrder()
        {
            string symbol = _symbols[0];
            double volume = 100000;
            TradeRecordSide side = TradeRecordSide.Buy;
            double price = TryGetOpenPrice(symbol, side);

            DateTime sendTime = DateTime.UtcNow;
            var tradeRecord = this.Trade.Server.SendOrder(symbol, TradeCommand.Market, side, price, volume, null, null, null, null);
            _positions.Add(tradeRecord.OrderId);
            Console.WriteLine("SendMarketOrder(): {0} {1} {2} {3} at {4}", sendTime.ToString(_datetimeformat), side, volume, symbol, price);
        }

        private void SendLimitOrder()
        {
        }

        private void SendIoCOrder()
        {
            string symbol = _symbols[0];
            double volume = 100000;
            TradeRecordSide side = TradeRecordSide.Buy;
            double price = TryGetOpenPrice(symbol, side, 5);

            _stopwatch.Restart();
            DateTime sendTime = DateTime.UtcNow;
            var tradeRecord = this.Trade.Server.SendOrder(symbol, TradeCommand.IoC, side, price, volume, null, null, null, null);
            _positions.Add(tradeRecord.OrderId);
            Console.WriteLine("SendIoCOrder(): {0} {1} {2} {3} at {4}", sendTime.ToString(_datetimeformat), side, volume, symbol, price);
        }

        private void CloseAll()
        {
            foreach (string id in _positions)
            {
                this.Trade.Server.ClosePosition(id);
            }
            Console.WriteLine("CloseAll(): {0} positions closed", _positions.Count);

            _positions.Clear();
        }

        private static void TradeThread(object state)
        {
            try
            {
                // need to wait for AccountInfo before trade
                TradeIsReady.WaitOne(TimeSpan.FromSeconds(10));

                var example = state as TradeExample;
                if (example == null)
                    return;

                int count = Settings1.Default.OrdersCount;
                while (!_stop && count > 0)
                {
                    PositionCreated.Reset();

                    example.SendIoCOrder();
                    //example.SendMarketOrder();

                    PositionCreated.WaitOne();

                    count--;
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("TradeThread() Exception: {0}", ex.Message);
                _isError = true;
            }
            finally
            {
                Console.WriteLine("TradeThread(): stopped");
            }
        }
    }
}