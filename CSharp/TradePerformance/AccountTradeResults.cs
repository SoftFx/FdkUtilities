using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradePerformance
{
    internal class AccountTradeResults
    {
        private MathList _ordersPerSec = new MathList();
        private readonly Dictionary<string, OpenOrderResult> _results = new Dictionary<string, OpenOrderResult>();
        private MathList[] _stats = new MathList[5] { new MathList(), new MathList(), new MathList(), new MathList(), new MathList() };

        public string Account { get; private set; }
        public int OrdersPerSecInitial { get; private set; }

        public Dictionary<string, OpenOrderResult> Results
        {
            get { return _results; }
        }

        public double OrdersPerSecMean
        {
            get { return _ordersPerSec.Mean(); }
        }

        public double OrdersPerSecSd
        {
            get { return _ordersPerSec.Sd(); }
        }

        public MathList[] Stats
        {
            get { return _stats; }
        }

        public AccountTradeResults(string account, int ordersPerSec)
        {
            Account = account;
            OrdersPerSecInitial = ordersPerSec;
        }

        public void AddOrdersPerSec(double ordersPerSec)
        {
            _ordersPerSec.Add(ordersPerSec);
        }
    }
}
