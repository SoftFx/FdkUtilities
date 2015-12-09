namespace DataFeedExamples
{
    using System;
    using SoftFX.Extended;

    class Program
    {
        static void Main()
        {
            //Library.Path = "<FRE>";
            var address = Settings1.Default.server;
            var username = Settings1.Default.login;
            var password = Settings1.Default.password;
            
            //Library.WriteFullDumpOnError(@"D:\full.dmp");

            //var example = new SymbolInfoExample(address, username, password);
            var example = new TicksExample(address, username, password);
            //var example = new BarsHistoryExample(address, username, password);
            //var example = new StorageTicksHistoryExample(address, username, password);
            //var example = new StorageTicksRangeIteratorHistoryExample(address, username, password);
            //var example = new StorageBarsHistoryExample(address, username, password);
            //var example = new StorageUpdatingExample(address, username, password);
            // var example = new ImportQuotesExample();
            //var example = new StorageMultithreadingExample(address, username, password);

            using (example)
            {
                example.Run();
                Console.WriteLine("Press any key to continue ...");
                Console.ReadKey();
            }
        }
    }
}
