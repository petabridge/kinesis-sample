using ClusterLightHouse;
var par = args.ToList();
var lighthouseService = new LighthouseService(actorSystemName: Environment.GetEnvironmentVariable("ACTORSYSTEM"));
lighthouseService.Start();

Console.WriteLine("Press Control + C to terminate.");
Console.CancelKeyPress += async (sender, eventArgs) =>
{
    await lighthouseService.StopAsync();
};
lighthouseService.TerminationHandle.Wait();