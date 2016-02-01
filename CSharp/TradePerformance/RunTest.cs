using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TradePerformance
{
    internal class RunTest
    {
        private string _password;
        private readonly int _stopAfterTime;
        private int _ordersPerSec;
        private int _ordersPersist;
        private string _server;
        private List<string> _accounts;

        public int TestNumber { get; }
        public List<TradeExample> TradeTests { get; }

        public RunTest(int number, string server, IEnumerable<string> accounts, string password, int ordersPerSec, int ordersPersist, int stopAfterTime)
        {
            _server = server;
            _accounts = accounts.ToList();
            _password = password;
            TestNumber = number;
            _ordersPerSec = ordersPerSec;
            _ordersPersist = ordersPersist;
            _stopAfterTime = stopAfterTime;
            TradeTests = new List<TradeExample>();
        }

        public void Run()
        {
            // new barier with _accounts.Count participants + this thread
            TradeExample.Barrier = new Barrier(_accounts.Count + 1);

            foreach (var account in _accounts)
            {
                TradeExample trade = new TradeExample(_server, account, _password, _ordersPerSec, _ordersPersist, _stopAfterTime);
                trade.Run();
                TradeTests.Add(trade);
            }

            Thread.Sleep(1000);
            // signal to start trading
            TradeExample.Barrier.SignalAndWait();

            Thread.Sleep(1000);
            // waiting for stop trading
            TradeExample.Barrier.SignalAndWait();

            TradeTests.ForEach(t => t.Dispose());
        }
    }
}
