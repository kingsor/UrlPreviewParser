namespace ConsoleSample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var client = new PreviewClient();
            client.Start().GetAwaiter().GetResult();
        }
    }
}
