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

        MathList _internalTimeList = new MathList();
        MathList _pingTimeList = new MathList();
        private AccountInfo _account;
        private string[] _symbols;
        private readonly List<string> _positions = new List<string>();

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
            Console.WriteLine("{0} ER: {1}", DateTime.UtcNow.ToString(_datetimeformat), ExecutionReportToString(e.Report));
            if (e.Report.OrderType == TradeRecordType.Position)
                PositionCreated.Set();
        }

        protected override void RunExample()
        {
            _tradeThread.Start(this);

            Console.WriteLine("Press any key to stop");
            Console.ReadKey();

            _stop = true;
            _tradeThread.Join();

            CloseAll();
        }

        private string ExecutionReportToString(ExecutionReport r)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}", r.OrderId);
            if (r.Created != null) sb.AppendFormat(" {0} ", r.Created.Value.ToString(_datetimeformat));
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

        private double TryGetOpenPrice(string symbol, TradeRecordSide side)
        {
            double price = 0;
            if (side == TradeRecordSide.Buy)
                this.Feed.Cache.TryGetAsk(symbol, out price);
            else
                this.Feed.Cache.TryGetBid(symbol, out price);
            return price;
        }

        private void SendMarketOrder()
        {
            string symbol = _symbols[0];
            double volume = 100000;
            TradeRecordSide side = TradeRecordSide.Buy;
            double price = TryGetOpenPrice(symbol, side);

            Console.WriteLine();
            Console.WriteLine("{0} SendMarketOrder: {1} {2} {3} at {4}", DateTime.UtcNow.ToString(_datetimeformat), side, volume, symbol, price);

            var tr = this.Trade.Server.SendOrder(symbol, TradeCommand.Market, side, price, volume, null, null, null, null);
            _positions.Add(tr.OrderId);
        }

        private void SendLimitOrder()
        {
        }

        private void SendIoCOrder()
        {
            string symbol = _symbols[0];
            double volume = 100000;
            TradeRecordSide side = TradeRecordSide.Buy;
            double price = TryGetOpenPrice(symbol, side);

            Console.WriteLine();
            Console.WriteLine("{0} SendIoCOrder: {1} {2} {3} at {4}", DateTime.UtcNow.ToString(_datetimeformat), side, volume, symbol, price);

            var tr = this.Trade.Server.SendOrder(symbol, TradeCommand.IoC, side, price, volume, null, null, null, null);
            _positions.Add(tr.OrderId);
        }

        private void CloseAll()
        {
            Console.WriteLine("CloseAll()");
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