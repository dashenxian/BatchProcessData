namespace BatchProcessData;

internal class Program
{
    static async Task Main(string[] args)
    {
        var batcher = new Batcher<string>(
            processor: Process,
            batchSize: 10,
            interval: TimeSpan.FromSeconds(5));
        var random = new Random();
        while (true)
        {
            var count = random.Next(1, 4);
            for (var i = 0; i < count; i++)
            {
                batcher.Add(Guid.NewGuid().ToString());
            }
            await Task.Delay(1000);
        }

    }
    static void Process(Batch<string> batch)
    {
        using (batch)
        {
            Console.WriteLine($"[{DateTimeOffset.Now}]{batch.Count} items are delivered.");
        }
    }

}