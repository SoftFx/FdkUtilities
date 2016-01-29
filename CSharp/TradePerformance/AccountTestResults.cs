using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradePerformance
{
    internal class AccountTestResults
    {
        private Stopwatch _watcher = new Stopwatch();
        private MathList _ordersPerSec = new MathList();
        private readonly Dictionary<string, OpenOrderResult> _results = new Dictionary<string, OpenOrderResult>();
        private MathList[] _stats = new MathList[5] { new MathList(), new MathList(), new MathList(), new MathList(), new MathList() };

        public string Account { get; private set; }
        public int OrdersPerSecInitial { get; private set; }

        public Dictionary<string, OpenOrderResult> Results
        {
            get { return _results; }
        }

        public MathList OrdersPerSec
        {
            get { return _ordersPerSec; }
        }

        public MathList[] Stats
        {
            get { return _stats; }
        }

        public Stopwatch Watcher
        {
            get { return _watcher; }
        }

        public bool OrdersPerSecStop
        {
            get { return _watcher.ElapsedMilliseconds >= 1000; }
        }

        public AccountTestResults(string account, int ordersPerSec)
        {
            Account = account;
            OrdersPerSecInitial = ordersPerSec;
        }

        public void OrdersPerSecRestart()
        {
            _watcher.Restart();
        }
    }
}
