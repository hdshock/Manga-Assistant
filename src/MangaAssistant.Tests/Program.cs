using System;
using System.Threading.Tasks;

namespace MangaAssistant.Tests
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting metadata search test...");
            await MetadataSearchTest.RunTest();
            Console.WriteLine("Test completed. Check the logs folder for detailed information.");
            Console.ReadKey();
        }
    }
}
