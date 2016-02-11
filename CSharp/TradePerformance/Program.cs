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
            string resultFile = $"{DateTime.UtcNow.ToString("yyyyMMdd-hhmmss")}_{Config.Default.ResultFile}";

            using (StreamWriter writer = File.CreateText(resultFile))
            {
                var orderspersecs = Config.Default.OrdersPerSec.Split(',').Select(int.Parse);
                var accounts = Config.Default.Accounts.Split(',').ToList();
                int testsCount = accounts.Count;

                writer.WriteLine("TestNumber,Account,OPS_Req,OPS_Mean,OPS_Sd,Order,New,Calculated1,Filled,Calculated2,Total");
                foreach (int orderspersec in orderspersecs)
                {
                    for (int i = 1; i <= testsCount; i++)
                    {
                        var accountsToTest = accounts.Take(i).ToList();

                        var runTest = new RunTest(i, Config.Default.Server, accountsToTest, Config.Default.Password, orderspersec, Config.Default.OrdersPersist, Config.Default.StopAfterTime);
                        runTest.Run();

                        foreach (var test in runTest.TradeTests)
                        {
                            foreach (var result in test.TradeResults.Results)
                            {
                                writer.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7}",
                                    i,
                                    test.TradeResults.Account,
                                    orderspersec,
                                    test.TradeResults.OrdersPerSecMean.ToString("F2"),
                                    test.TradeResults.OrdersPerSecSd.ToString("F2"),
                                    result.Value.Order,
                                    string.Join(",", result.Value.Latencies.Take(4)),
                                    result.Value.TotalLatency);
                            }
                        }

                        writer.Flush();
                    }
                }
            }
            Console.WriteLine("Press any key to exit...");
            Console.Read();
        }
    }
}
