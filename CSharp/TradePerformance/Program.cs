using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace TradePerformance
{
    class Program
    {
        static void Main(string[] args)
        {
            using (StreamWriter writer = File.CreateText(Config.Default.ResultFile))
            {
                writer.WriteLine("TestNumber,Account,OrdPerSec,OrdPerSec_Mean,OrdPerSec_Sd,Order,New,Calculated1,Filled,Calculated2");
                var orderspersecs = Config.Default.OrdersPerSec.Split(',').Select(int.Parse);
                foreach (int orderspersec in orderspersecs)
                {
                    int count = Config.Default.TestsCount;
                    for (int i = 1; i <= count; i++)
                    {
                        var accounts = Config.Default.Accounts.Split(',');
                        var runTest = new RunTest(i, Config.Default.Server, accounts, Config.Default.Password, orderspersec, Config.Default.StopAfterTime);
                        runTest.Run();

                        foreach (var test in runTest.TradeTests)
                        {
                            foreach (var result in test.AccountTestResults.Results)
                            {
                                writer.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7}", i, test.AccountTestResults.Account,
                                    orderspersec,
                                    test.AccountTestResults.OrdersPerSec.Mean(),
                                    test.AccountTestResults.OrdersPerSec.Sd(),
                                    result.Value.Order,
                                    string.Join(",", result.Value.Latencies.Take(4)),
                                    result.Value.TotalLatency);
                            }
                        }

                        writer.Flush();
                        Thread.Sleep(3000);
                    }
                }
            }
            Console.WriteLine("Press any key to exit...");
            Console.Read();
        }
    }
}
