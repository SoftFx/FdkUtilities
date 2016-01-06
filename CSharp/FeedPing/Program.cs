namespace DataFeedExamples
{
    using System;
    using SoftFX.Extended;

    class Program
    {
        static void Main()
        {
            //
            var address = Settings1.Default.server;
            var username = Settings1.Default.login;
            var password = Settings1.Default.password;

            Console.WriteLine("[1] - Feed Example");
            Console.WriteLine("[2] - Trade Example");
            Console.Write("Enter your choice or any key to exit: ");

            var key = Console.ReadKey();
            switch (key.KeyChar)
            {
                case '1':
                    using (var example = new TicksExample(address, username, password))
                    {
                        Console.WriteLine();
                        example.Run();
                    }
                    break;
                case '2': using (var example = new TradeExample(address, username, password))
                    {
                        Console.WriteLine();
                        example.Run();
                    }
                    break;
                default:
                    return;
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit ...");
            Console.ReadKey();
        }
    }
}
