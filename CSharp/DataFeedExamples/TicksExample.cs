namespace DataFeedExamples
{
    using System;
    using SoftFX.Extended.Events;
    using System.Threading.Tasks;

    class TicksExample : Example
    {
        MathList internalTimeList = new MathList();
        MathList pingTimeList = new MathList();

        public TicksExample(string address, string username, string password)
            : base(address, username, password)
        {
            this.Feed.Logon += this.OnLogon;
            this.Feed.Tick += this.OnTick;
        }

        void OnLogon(object sender, LogonEventArgs e)
        {
            var symbols = new[]
            {
                "EURUSD",
                "EURJPY",
            };
                    
            // we should subscribe to quotes every time after logon event
            this.Feed.Server.SubscribeToQuotes(symbols, 3);
        }

        protected override void RunExample()
        {
            Console.WriteLine("Press any key to stop");
            Console.ReadKey();
        }

        long lastStatisticTime = 0;
        void OnTick(object sender, TickEventArgs e)
        {
            //Console.WriteLine("CreatingTime Time {2}. Sending Time {1}. OnTick(): {0}. ", e, e.SendingTime, e.Tick.CreatingTime);
            //Console.WriteLine("Server delay {0} ms. Receiving delay {1} ms.", (e.SendingTime- e.Tick.CreatingTime).Value.Milliseconds, (e.ReceivingTime - e.SendingTime).Value.TotalMilliseconds);
            internalTimeList.Add((e.SendingTime - e.Tick.CreatingTime).Value.Milliseconds);
            pingTimeList.Add((e.ReceivingTime - e.SendingTime).Value.TotalMilliseconds);

            if (lastStatisticTime == 0 || DateTime.Now.Ticks - lastStatisticTime > TimeSpan.FromSeconds(10).Ticks)
            {
                lastStatisticTime = DateTime.Now.Ticks;
                Console.WriteLine();
                Console.WriteLine("Internal delay: " + internalTimeList.ToString());
                Console.WriteLine("Ping delay: " + pingTimeList.ToString());
                internalTimeList.Clear();
                pingTimeList.Clear();
            }

        }
        
    }
}
