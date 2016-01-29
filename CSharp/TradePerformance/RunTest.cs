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
        public int TestNumber { get; }
        private string _server { get; }
        private List<string> _accounts { get; }
        private string _password;
        private readonly int _stopAfterTime;
        private int _ordersPerSec { get; }
        public List<TradeExample> TradeTests { get; }

        public RunTest(int number, string server, IEnumerable<string> accounts, string password, int ordersPerSec, int stopAfterTime)
        {
            _server = server;
            _accounts = accounts.ToList();
            _password = password;
            TestNumber = number;
            _ordersPerSec = ordersPerSec;
            _stopAfterTime = stopAfterTime;
            TradeTests = new List<TradeExample>();
        }

        public void Run()
        {
            TradeExample.Barrier = new Barrier(_accounts.Count + 1);

            foreach (var account in _accounts)
            {
                TradeExample trade = new TradeExample(_server, account, _password, _ordersPerSec, _stopAfterTime);
                trade.Run();
                TradeTests.Add(trade);
            }

            TradeExample.Barrier.SignalAndWait();

            //if (!Config.Default.RunSilent)
            //{
            //    Console.WriteLine("\nPress any key to stop...");
            //    Console.ReadKey();
            //    TradeTests.ForEach(t => t.Stop());
            //}
            //else
                Thread.Sleep(1000);

            TradeExample.Barrier.SignalAndWait();

            //TradeTests.ForEach(t => t.ShowResults());

            //if (!Config.Default.RunSilent)
            //{
            //    Console.WriteLine();
            //    Console.WriteLine("Press any key to exit ...");
            //    Console.ReadKey();
            //}

            TradeTests.ForEach(t => t.Dispose());
        }
    }
}
