using Microsoft.Extensions.Logging;

namespace NLSeed.Services;

using Interfaces;

public class SeederService(BackupService backupService, ILightningService lightningService, ILogger<SeederService> logger, NetworkViewService networkViewService)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        // // Check connection status
        // if (!await lightningService.CheckConnectionAsync(cancellationToken))
        // {
        //     throw new Exception("Cannot establish connection to the node");
        // }
        //
        // // Run jobs
        // await Task.Run(() => Task.WaitAll(CollectDataAsync(cancellationToken), PruneNetworkView(cancellationToken)), CancellationToken.None);
        //
        // // Backup node list
        // await backupService.BackupAllNodesInfoAsync(networkViewService.AllNodes.ToDictionary());
        // await backupService.BackupReachableNodesInfoAsync(networkViewService.ReachableNodes.ToDictionary());
    }

    private async Task CollectDataAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var currentGraph = await lightningService.DescribeGraphAsync(cancellationToken);
                if (currentGraph.Count == 0)
                {
                    currentGraph = await lightningService.ListPeersAsync(cancellationToken);
                }

                foreach (var node in currentGraph.Values)
                {
                    networkViewService.AddNode(node, cancellationToken);
                }
            }
            catch (Exception e)
            {
                // Log or handle the exception as needed.
                logger.LogError(e, "An error occurred while collecting data");
            }

            // Wait for the specified interval before repeating.
            try
            {
                await backupService.BackupAllNodesInfoAsync(networkViewService.AllNodes.ToDictionary());
                await backupService.BackupReachableNodesInfoAsync(networkViewService.ReachableNodes.ToDictionary());
                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // Task was canceled, so exit gracefully.
                break;
            }
        }
    }

    private async Task PruneNetworkView(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await networkViewService.ReachabilityPrunerAsync(cancellationToken);
            }
            catch (Exception e)
            {
                // Log or handle the exception as needed.
                logger.LogError(e, "An error occurred while pruning the network");
            }

            // Wait for the specified interval before repeating.
            try
            {
                await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
                // await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // Task was canceled, so exit gracefully.
                break;
            }
        }
    }
}