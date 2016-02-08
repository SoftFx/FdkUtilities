using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using SoftFX.Extended;
using SoftFX.Extended.Events;

namespace DataFeedExamples
{
    class TradeExample : Example
    {
        private class Result : Tuple<Stopwatch, List<long>, string>
        {
            public Result() : base(new Stopwatch(), new List<long>(), DateTime.UtcNow.ToString(Settings1.Default.DTFormat))
            {
                Item1.Start();
            }

            public void Register(bool end)
            {
                Item2.Add(Item1.ElapsedMilliseconds);

                if (end) Item1.Stop();
                else Item1.Restart();
            }
        }

        private class NewOrder
        {
            public TradeCommand Command { get; set; }
            public string Symbol { get; set; }
            public double Volume { get; set; }
            public TradeRecordSide Side { get; set; }
            public double Price { get; set; }
        }

        private static readonly AutoResetEvent TradeIsReady = new AutoResetEvent(false);

        private AccountInfo _account;
        private string[] _symbols;
        private readonly List<string> _positions = new List<string>();
        private readonly Dictionary<string, Result> _results = new Dictionary<string, Result>();
        private static NewOrder _newOrder = null;

        public TradeExample(string address, string username, string password) : base(address, username, password)
        {
            this.Feed.Logon += FeedOnLogon;

            this.TradeEnabled = true;
            this.Trade.Logon += OnLogon;
            this.Trade.Logout += OnLogout;
            this.Trade.SessionInfo += delegate(object sender, SessionInfoEventArgs e)
            {
                Console.WriteLine("TradingSessionId = {0}", e.Information.TradingSessionId);
            };
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
            Result res = null;
            _results.TryGetValue(e.Report.ClientOrderId, out res);
            res?.Register(e.Report.OrderType == TradeRecordType.Position);
            //Console.WriteLine("{0} {1}", opId, e.Report.OrderStatus);
        }

        protected override void RunExample()
        {
            // need to wait for AccountInfo before trade
            TradeIsReady.WaitOne(TimeSpan.FromSeconds(10));

            int count = Settings1.Default.OrdersCount;
            Console.Write("\nSend {0} IoC orders: [t] - in threads, [s] - sequentially, [any] - exit ", count);
            var key = Console.ReadKey();
            Console.WriteLine();

            if (key.KeyChar != 't' && key.KeyChar != 's')
                return;
            bool inthread = key.KeyChar == 't';

            _newOrder = new NewOrder
            {
                //Command = TradeCommand.IoC,
                Symbol = _symbols[0],
                Volume = 100000,
                Side = TradeRecordSide.Buy,
                Price = TryGetOpenPrice(_symbols[0], TradeRecordSide.Buy, 20)
            };

            if (inthread)
            {
                while (count > 0)
                {
                    ThreadPool.QueueUserWorkItem(obj => SendIoCOrder());
                    count--;
                }
            }
            else
            {
                while (count > 0)
                {
                    SendIoCOrder();
                    count--;
                }
            }

            Console.WriteLine("\nPress any key to stop...");
            Console.ReadKey();
            Console.WriteLine();
            Console.WriteLine("ClOrdId  | Time                  | New    | Calc   | Fill   | Calc   | Total");
            MathList[] stats = new MathList[5] { new MathList(), new MathList(), new MathList(), new MathList(), new MathList() };
            foreach (var result in _results)
            {
                Console.WriteLine("{0,-8} | {1, -21} | {2} | {3}", result.Key, result.Value.Item3,
                    string.Join(" | ", result.Value.Item2.Select(v => string.Format("{0,-6}", v))), result.Value.Item2.Sum());
                stats[0].Add(result.Value.Item2[0]);
                stats[1].Add(result.Value.Item2[1]);
                stats[2].Add(result.Value.Item2[2]);
                stats[3].Add(result.Value.Item2[3]);
                stats[4].Add(result.Value.Item2.Sum());
            }
            Console.WriteLine();
            Console.WriteLine("{0,-32} | {1,-6} | {2,-6} | {3,-6} | {4,-6} | {5,-6}", "Mean", stats[0].Mean().ToString("F2"), stats[1].Mean().ToString("F2"),
                stats[2].Mean().ToString("F2"), stats[3].Mean().ToString("F2"), stats[4].Mean().ToString("F2"));
            Console.WriteLine("{0,-32} | {1,-6} | {2,-6} | {3,-6} | {4,-6} | {5,-6}", "SD", stats[0].Sd().ToString("F2"), stats[1].Sd().ToString("F2"),
                stats[2].Sd().ToString("F2"), stats[3].Sd().ToString("F2"), stats[4].Sd().ToString("F2"));

            Console.ReadKey();
            CloseAll();
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

        private void SendIoCOrder()
        {
            bool isError = false;
            string opId = Guid.NewGuid().GetHashCode().ToString("X");
            try
            {
                _results.Add(opId, new Result());
                //_newOrder.Price = TryGetOpenPrice(_newOrder.Symbol, _newOrder.Side, 10);
                var tradeRecord = this.Trade.Server.SendOrderEx(opId, _newOrder.Symbol, TradeCommand.IoC, _newOrder.Side,
                    _newOrder.Price, _newOrder.Volume, null, null, null, null);
                _positions.Add(tradeRecord.OrderId);
            }
            catch (Exception ex)
            {
                isError = true;
                Console.WriteLine("SendIoCThread() Exception: {0}", ex.Message);
            }
            finally
            {
                if (_results.ContainsKey(opId))
                {
                    if (isError)
                        _results.Remove(opId);
                    else
                        Console.WriteLine("SendIoCOrder(): {0} {1}", _results[opId].Item3, opId);
                }
            }
        }

        private void SendMarketOrder()
        {
            string symbol = _symbols[0];
            double volume = 100000;
            TradeRecordSide side = TradeRecordSide.Buy;
            double price = TryGetOpenPrice(symbol, side);

            DateTime sendTime = DateTime.UtcNow;
            var tradeRecord = this.Trade.Server.SendOrderEx(Trade.GenerateOperationId(), symbol, TradeCommand.Market, side, price, volume, null, null, null, null);
            _positions.Add(tradeRecord.OrderId);
            Console.WriteLine("SendMarketOrder(): {0} {1} {2} {3} at {4}", sendTime.ToString(Settings1.Default.DTFormat), side, volume, symbol, price);
        }

        private void SendLimitOrder()
        {
        }

        private void CloseAll()
        {
            Console.Write("\nCloseAll(): ");
            foreach (string id in _positions)
            {
                Console.Write(id);
                this.Trade.Server.ClosePosition(id);
                Console.SetCursorPosition(Console.CursorLeft-7, Console.CursorTop);
            }
            Console.Write("{0} positions closed\n", _positions.Count);

            _positions.Clear();
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
    }
}