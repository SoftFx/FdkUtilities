namespace TradeFeedExamples
{
    using SoftFX.Extended;

    class Program
    {
        static void Main(string[] args)
        {
            var address = "ttdemo.fxopen.com";
            var username = "100039";
            var password = "e2pllch2";

            var example = new StateCalculatorExample(address, username, password);

            using (example)
            {
                example.Run();
            }
        }
    }
}
