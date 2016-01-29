using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TradePerformance
{
    internal class OpenOrderResult
    {
        private readonly Stopwatch _watchser;
        public string Order { get; set; }
        public string SendingTime { get; set; }
        public List<long> Latencies { get; set; }
        public long TotalLatency
        {
            get { return Latencies.Any() ? Latencies.Sum() : 0; }
        }

        public OpenOrderResult()
        {
            SendingTime = DateTime.UtcNow.ToString(Config.Default.DTFormat);
            Latencies = new List<long>();
            _watchser = Stopwatch.StartNew();
        }

        public void Register(bool end)
        {
            Latencies.Add(_watchser.ElapsedMilliseconds);

            if (end) _watchser.Stop();
            else _watchser.Restart();
        }
    }
}