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
            
            var example = new TicksExample(address, username, password);

            using (example)
            {
                example.Run();
                Console.WriteLine("Press any key to continue ...");
                Console.ReadKey();
            }
        }
    }
}
