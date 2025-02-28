using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLSeed.Handlers;
using NLSeed.Interfaces;
using NLSeed.Services;

Console.WriteLine("Running N Lightning Seed daemon");

// Register signal handlers (requires .NET 6 or later)
PosixSignalRegistration.Create(PosixSignal.SIGTERM, HandleTermination);
PosixSignalRegistration.Create(PosixSignal.SIGQUIT, HandleTermination);

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var serviceCollection = new ServiceCollection();
serviceCollection.AddLogging(log =>
{
    log.AddConfiguration(configuration.GetSection("Logging"));
    log.AddConsole();
});
serviceCollection.AddSingleton(configuration);
serviceCollection.AddSingleton<ILightningService, LndService>();
serviceCollection.AddSingleton<NetworkViewService>();
serviceCollection.AddSingleton<SeederService>();
serviceCollection.AddSingleton<BackupService>();
serviceCollection.AddSingleton<DnsService>();

serviceCollection.AddScoped<LightningDnsHandler>();
serviceCollection.AddScoped<SrvQueryHandler>();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var serviceProvider = serviceCollection.BuildServiceProvider();

var seederService = serviceProvider.GetRequiredService<SeederService>();
var seederTask = seederService.RunAsync(cts.Token);

var dnsService = serviceProvider.GetRequiredService<DnsService>();
var udpTask = dnsService.RunUdpServerAsync(cts.Token);
var tcpTask = dnsService.RunTcpServerAsync(cts.Token);

Task.WaitAll(seederTask, udpTask, tcpTask);
return;

static void HandleTermination(PosixSignalContext context)
{
    Console.WriteLine($"Received {context.Signal} signal, starting graceful shutdown...");
    // Optionally, delay shutdown to allow cleanup tasks to complete.
    // For example, you might trigger a cancellation token here.
    context.Cancel = true; // Prevent default handling (if desired).
}