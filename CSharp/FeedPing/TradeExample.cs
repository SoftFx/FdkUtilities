using System;
using System.Collections.Generic;
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
        private readonly Dictionary<string, MathList> _results = new Dictionary<string, MathList>();
        private DateTime _controlTime;

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
            DateTime utcNow = DateTime.UtcNow;
            int delta = (utcNow - _controlTime).Milliseconds;
            _controlTime = utcNow;

            SetResult(e.Report.OrderId, delta);

            if (e.Report.OrderType == TradeRecordType.Position)
                PositionCreated.Set();
        }

        protected override void RunExample()
        {
            _tradeThread.Start(this);

            Console.WriteLine("Press any key to stop...");
            Console.ReadKey();

            _stop = true;
            _tradeThread.Join();

            Console.WriteLine();
            foreach (var result in _results)
            {
                Console.WriteLine("Id: {0}  {1}\t{2}", result.Key, string.Join(" ", result.Value.Numbers), result.Value);
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
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

        private void SetResult(string orderId, double value)
        {
            if (!_results.ContainsKey(orderId))
            {
                var m = new MathList();
                m.Add(value);
                _results.Add(orderId, m);
            }
            else
            {
                _results[orderId].Add(value);
            }
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

            DateTime sendTime = _controlTime = DateTime.UtcNow;
            var tradeRecord = this.Trade.Server.SendOrder(symbol, TradeCommand.Market, side, price, volume, null, null, null, null);
            _positions.Add(tradeRecord.OrderId);
            Console.WriteLine("{0} SendMarketOrder(): {1} {2} {3} at {4}", sendTime.ToString(_datetimeformat), side, volume, symbol, price);
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

            DateTime sendTime = _controlTime = DateTime.UtcNow;
            var tradeRecord = this.Trade.Server.SendOrder(symbol, TradeCommand.IoC, side, price, volume, null, null, null, null);
            _positions.Add(tradeRecord.OrderId);
            Console.WriteLine("{0} SendIoCOrder(): {1} {2} {3} at {4}", sendTime.ToString(_datetimeformat), side, volume, symbol, price);
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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("TradeThread() Exception: {0}", ex.Message);
            }
            finally
            {
                Console.WriteLine("TradeThread(): stopped");
            }
        }
    }
}