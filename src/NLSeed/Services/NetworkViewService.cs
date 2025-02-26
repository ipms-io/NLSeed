using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NLSeed.Services;

using Models;

public class NetworkViewService
{
    private readonly BackupService _backupService;
    private readonly ILogger<NetworkViewService> _logger;
    private readonly SemaphoreSlim _semaphore = new(32, 32);
    
    public ConcurrentDictionary<string, Node> AllNodes { get; }
    public ConcurrentDictionary<string, Node> ReachableNodes { get; }

    public NetworkViewService(BackupService backupService, IConfiguration configuration, ILogger<NetworkViewService> logger)
    {
        _backupService = backupService;
        _logger = logger;
        
        // Load nodes from backup
        AllNodes = backupService.LoadAllNodesFromBackup();
        ReachableNodes = backupService.LoadReachableNodesFromBackup();
    }

    public void AddNode(Node node, CancellationToken cancellationToken)
    {
        if (AllNodes.TryAdd(node.Id, node))
        {
            Task.Run(async () =>
            {
                await _semaphore.WaitAsync(cancellationToken);

                try
                {
                    await ExtractReachableAddressesAsync(node, false, cancellationToken);
                }
                finally
                {
                    _semaphore.Release();
                }
            }, cancellationToken);
        }
    }
    
    public async Task ReachabilityPrunerAsync(CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();

        // Load reachable nodes as of now
        var reachableNodes = ReachableNodes.ToDictionary();
        
        foreach (var (_, value) in reachableNodes)
        {
            await _semaphore.WaitAsync(cancellationToken);
            
            var task = Task.Run(async () =>
            {
                try
                {
                    await ExtractReachableAddressesAsync(value, true, cancellationToken);
                }
                finally
                {
                    _semaphore.Release();
                }
            }, cancellationToken);

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        tasks.Clear();
        
        // Backup new list of reachable nodes
        await _backupService.BackupReachableNodesInfoAsync(reachableNodes);
        
        // Load all nodes as of now
        var allNodes = AllNodes.ToDictionary();
        
        foreach (var (_, value) in allNodes)
        {
            await _semaphore.WaitAsync(cancellationToken);
            
            var task = Task.Run(async () =>
            {
                try
                {
                    await ExtractReachableAddressesAsync(value, false, cancellationToken);
                }
                finally
                {
                    _semaphore.Release();
                }
            }, cancellationToken);

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        
        // Backup new list of reachable nodes
        await _backupService.BackupReachableNodesInfoAsync(reachableNodes);
        
        _logger.LogInformation("Total number of reachable nodes: {count}", ReachableNodes.Count);
    }

    public List<Node> RandomSample(byte query, int count)
    {
        return ReachableNodes.Where(x => query == 0 || x.Value.Type == query).Take(count).Select(x => x.Value).ToList();
    }

    private async Task<ICollection<IPEndPoint>> GetReachableAddressesFromNodeAsync(Node node, CancellationToken cancellationToken)
    {
        List<IPEndPoint> addresses = [];

        foreach (var address in node.Addresses)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;    
            }

            if (ReachableNodes.TryGetValue(node.Id, out var reachableNode) && reachableNode.Addresses.Any(x => x.Equals(address)))
            {
                continue;
            }
            
            _logger.LogTrace("Checking Node {id} for reachability @ {address}", node.Id, address);

            try
            {
                using var client = new TcpClient();
                using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
                await client.ConnectAsync(address, linkedCts.Token);
                client.Close();
                addresses.Add(address);
            }
            catch (OperationCanceledException)
            {
                _logger.LogTrace("Timeout while connecting to {id} @ {address}", node.Id, address);
            }
            catch (SocketException e)
            {
                _logger.LogTrace(e, "Error trying to reach {id} @ {address}", node.Id, address);
            }
        }

        return addresses;
    }

    private async Task ExtractReachableAddressesAsync(Node newNode, bool prune, CancellationToken cancellationToken)
    {
        var validAddresses = await GetReachableAddressesFromNodeAsync(newNode, cancellationToken);
        if (validAddresses.Count == 0)
        {
            _logger.LogTrace("Node({id}) has no reachable addresses, prune={prune}", newNode.Id, prune);

            if (prune)
            {
                ReachableNodes.TryRemove(newNode.Id, out _);
            }

            return;
        }

        newNode.Addresses = validAddresses;

        ReachableNodes.TryAdd(newNode.Id, newNode);
    }
}