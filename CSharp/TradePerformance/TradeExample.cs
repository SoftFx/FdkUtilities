using System;
using System.Linq;
using System.Text;
using System.Threading;
using SoftFX.Extended;
using SoftFX.Extended.Events;
using System.Collections.Generic;
using System.Diagnostics;

namespace TradePerformance
{
    class TradeExample : Example
    {
        public static Barrier Barrier { get; set; }

        private class NewOrder
        {
            public TradeCommand Command { get; set; }
            public string Symbol { get; set; }
            public double Volume { get; set; }
            public TradeRecordSide Side { get; set; }
            public double Price { get; set; }
        }

        private readonly AutoResetEvent TradeIsReady = new AutoResetEvent(false);

        public AccountTradeResults TradeResults { get; private set; }

        private int _ordersPerSec;
        private string[] _symbols;
        private bool _stop = false;
        private int _stopAfterTime;
        private List<string> _positions = new List<string>();
        private int _ordersPersist;

        public TradeExample(string address, string username, string password, int ordersPerSec, int ordersPersist, int stopAfterTime) : base(address, username, password)
        {
            _ordersPerSec = ordersPerSec;
            _ordersPersist = ordersPersist;
            _stopAfterTime = stopAfterTime;

            this.Feed.Logon += FeedOnLogon;

            this.TradeEnabled = true;
            this.Trade.Logon += OnLogon;
            this.Trade.Logout += OnLogout;
            this.Trade.SessionInfo += OnSessionInfo;
            this.Trade.AccountInfo += OnAccountInfo;
            this.Trade.ExecutionReport += OnExecutionReport;
        }

        private void FeedOnLogon(object sender, LogonEventArgs logonEventArgs)
        {
            _symbols = Config.Default.Symbols.Split(',');
            this.Feed.Server.SubscribeToQuotes(_symbols, Config.Default.Depth);
        }

        private void OnLogon(object sender, LogonEventArgs e)
        {
            if (ShowDebug) Console.WriteLine("{0} OnLogon(): Trade {1}", Username, e);
        }

        private void OnLogout(object sender, LogoutEventArgs e)
        {
            if (ShowDebug) Console.WriteLine("{0} OnLogout(): Trade {1}", Username, e);
        }

        private void OnSessionInfo(object sender, SessionInfoEventArgs e)
        {
            if (ShowDebug) Console.WriteLine("{0} TradingSessionId = {1}", Username, e.Information.TradingSessionId);
        }

        private void OnAccountInfo(object sender, AccountInfoEventArgs e)
        {
            TradeResults = new AccountTradeResults(e.Information.AccountId, _ordersPerSec);
            TradeIsReady.Set();
        }

        private void OnExecutionReport(object sender, ExecutionReportEventArgs e)
        {
            OpenOrderResult res = null;
            TradeResults.Results.TryGetValue(e.Report.ClientOrderId, out res);
            res?.Register(e.Report.OrderType == TradeRecordType.Position);
            //Console.WriteLine("{0} {1}", e.Report.ClientOrderId, e.Report.OrderStatus);
        }

        protected override void RunExample()
        {
            ThreadPool.QueueUserWorkItem(obj => TradeThread());
        }

        public void Stop()
        {
            _stop = true;
        }

        public void ShowResults()
        {
            Console.WriteLine();
            Console.WriteLine("{0,-8}  PerSec  New     Calc    Fill    Calc    Total", Username);
            //MathList[] stats = new MathList[5] { new MathList(), new MathList(), new MathList(), new MathList(), new MathList() };
            foreach (var result in TradeResults.Results)
            {
                //Console.WriteLine("{0,-8}  {1, -21}  {2}  {3}", result.Key, result.Value.SendingTime,
                //    String.Join("  ", result.Value.Latencies.Select(v => String.Format("{0,-6}", v))), result.Value.Latencies.Sum());
                TradeResults.Stats[0].Add(result.Value.Latencies[0]);
                TradeResults.Stats[1].Add(result.Value.Latencies[1]);
                TradeResults.Stats[2].Add(result.Value.Latencies[2]);
                TradeResults.Stats[3].Add(result.Value.Latencies[3]);
                TradeResults.Stats[4].Add(result.Value.TotalLatency);
            }
            Console.WriteLine("{0,-8}  {1,-6}  {2,-6}  {3,-6}  {4,-6}  {5,-6}  {6,-6}", "Mean",
                TradeResults.OrdersPerSecMean.ToString("F2"),
                TradeResults.Stats[0].Mean().ToString("F2"),
                TradeResults.Stats[1].Mean().ToString("F2"),
                TradeResults.Stats[2].Mean().ToString("F2"),
                TradeResults.Stats[3].Mean().ToString("F2"),
                TradeResults.Stats[4].Mean().ToString("F2"));
            Console.WriteLine("{0,-8}  {1,-6}  {2,-6}  {3,-6}  {4,-6}  {5,-6}  {6,-6}", "SD",
                TradeResults.OrdersPerSecSd.ToString("F2"),
                TradeResults.Stats[0].Sd().ToString("F2"),
                TradeResults.Stats[1].Sd().ToString("F2"),
                TradeResults.Stats[2].Sd().ToString("F2"),
                TradeResults.Stats[3].Sd().ToString("F2"),
                TradeResults.Stats[4].Sd().ToString("F2"));
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

        private bool SendIoCOrder()
        {
            bool isError = false;
            string opId = Guid.NewGuid().GetHashCode().ToString("X");
            string symbol = _symbols[0];
            double volume = 100000;
            TradeRecordSide side = TradeRecordSide.Buy;
            double price = TryGetOpenPrice(symbol, side, 10);
            try
            {
                TradeResults.Results.Add(opId, new OpenOrderResult());
                var tradeRecord = this.Trade.Server.SendOrderEx(opId, symbol, TradeCommand.IoC, side, price, volume, null, null, null, null);
                _positions.Add(tradeRecord.OrderId);
                TradeResults.Results[opId].Order = tradeRecord.OrderId;
                if (ShowDebug) Console.WriteLine("{0} SendIoCOrder(): {1} {2}", Username, TradeResults.Results[opId].SendingTime, opId);
                return true;
            }
            catch (Exception ex)
            {
                isError = true;
                if (ShowDebug) Console.WriteLine("{0} SendIoCThread() Exception: {1}", Username, ex.Message);
            }
            finally
            {
                if (TradeResults.Results.ContainsKey(opId))
                {
                    if (isError)
                        TradeResults.Results.Remove(opId);
                }
            }
            return false;
        }

        private void SendMarketOrder()
        {
            string symbol = _symbols[0];
            double volume = 100000;
            TradeRecordSide side = TradeRecordSide.Buy;
            double price = TryGetOpenPrice(symbol, side);

            DateTime sendTime = DateTime.UtcNow;
            var tradeRecord = this.Trade.Server.SendOrderEx(Trade.GenerateOperationId(), symbol, TradeCommand.Market, side, price, volume, null, null, null, null);
            if (ShowDebug) Console.WriteLine("SendMarketOrder(): {0} {1} {2} {3} at {4}", sendTime.ToString(Config.Default.DTFormat), side, volume, symbol, price);
        }

        private void SendLimitOrder()
        {
        }

        private void CloseAll()
        {
            int closed = 0;
            foreach (string id in _positions)
            {
                try
                {
                    this.Trade.Server.ClosePosition(id);
                    closed++;
                }
                catch
                {
                    // ignored
                }
            }
            if (ShowDebug) Console.Write("\n{0} CloseAll(): {1} positions closed, {2} failed\n", Username, closed, _positions.Count - closed);
        }

        private string ExecutionReportToString(ExecutionReport r)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}", r.OrderId);
            //if (r.Created != null) sb.AppendFormat(" {0} ", r.Created.Value.ToString(Settings1.Default.DTFormat));
            sb.AppendFormat(" {0}", r.OrderStatus);
            sb.AppendFormat(" {0}", r.ExecutionType);
            //sb.AppendFormat(" {0}", r.OrderType);
            //sb.AppendFormat(" {0}", r.OrderSide);
            //sb.AppendFormat(" {0}", r.LeavesVolume);
            //sb.AppendFormat(" {0}", r.Symbol);
            //sb.AppendFormat(" {0}", r.Price);
            //if (r.Modified != null) sb.AppendFormat(" {0} ", r.Modified.Value.ToString(Settings1.Default.DTFormat));
            return sb.ToString();
        }

        private void TradeThread()
        {
            // need to wait for AccountInfo before trade
            TradeIsReady.WaitOne(TimeSpan.FromSeconds(10));
            
            // signal that account is ready to start trading
            Barrier.SignalAndWait();

            Stopwatch opsStopwatch = new Stopwatch();
            DateTime startedTime = DateTime.Now;

            while (!_stop)
            {
                int sentOrders = 0;

                opsStopwatch.Restart();
                while (sentOrders < _ordersPerSec)
                {
                    if (SendIoCOrder())
                        sentOrders++;
                    if (opsStopwatch.ElapsedMilliseconds >= 1000)
                        break;
                }

                double ops;
                ops = sentOrders < _ordersPerSec
                    ? (sentOrders > 1 ? sentOrders : sentOrders*1000.0/opsStopwatch.ElapsedMilliseconds)
                    : sentOrders;
                TradeResults.AddOrdersPerSec(ops);

                if (1000 - (int)opsStopwatch.ElapsedMilliseconds > 0)
                    Thread.Sleep(1000 - (int)opsStopwatch.ElapsedMilliseconds);

                if (_stopAfterTime > 0 && (DateTime.Now - startedTime >= TimeSpan.FromMinutes(_stopAfterTime)))
                    break;
            }

            CloseAll();

            // signal that account stopped trading
            Barrier.SignalAndWait();
        }
    }
}