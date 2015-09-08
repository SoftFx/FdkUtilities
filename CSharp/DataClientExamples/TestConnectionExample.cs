namespace DataClientExamples
{
    using System;
    using SoftFX.Extended;

    class TestConnectionExample
    {
        public static void Run()
        {
            var buidlers = ConnectionStringBuilder.TestConnections("ttdemo.fxopen.com");
            //var builders = FixConnectionStringBuilder.TestConnections("ttdemo.fxopen.com");
            Console.WriteLine("buildes.Length = {0}", buidlers.Length);
        }
    }
}
