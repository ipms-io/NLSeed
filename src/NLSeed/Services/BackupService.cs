using System.Collections.Concurrent;
using MessagePack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NLSeed.Services;

using Models;

public class BackupService(IConfiguration configuration, ILogger<BackupService> logger)
{
    private const string AllNodesBpkFileName = "allNodes.bkp";
    private const string ReachableNodesBpkFileName = "reachableNodes.bkp";
    
    private readonly string? _basePath = configuration["Backup:BasePath"];

    public async Task BackupAllNodesInfoAsync(Dictionary<string, Node> allNodes)
    {
        if (!string.IsNullOrWhiteSpace(_basePath))
        {
            logger.LogTrace("Saving backups to {basePath}/{fileName}", _basePath, AllNodesBpkFileName);
            var allNodesFilePath = Path.Combine(_basePath, AllNodesBpkFileName);
            var serialized = MessagePackSerializer.Serialize(allNodes.Select(x=>x.Value).ToList(), cancellationToken: CancellationToken.None);
            await File.WriteAllBytesAsync(allNodesFilePath, serialized, cancellationToken: CancellationToken.None);
            logger.LogInformation("Saved {count} nodes to {allNodesFilePath}", allNodes.Count, allNodesFilePath);
        }
        else
        {
            logger.LogInformation("BasePath not provided. Not saving backups to file.");
        }
    }
    
    public async Task BackupReachableNodesInfoAsync(Dictionary<string, Node> reachableNodes)
    {
        if (!string.IsNullOrWhiteSpace(_basePath))
        {
            logger.LogTrace("Saving backups to {basePath}/{fileName}", _basePath, ReachableNodesBpkFileName);
            var reachableNodesFilePath = Path.Combine(_basePath, ReachableNodesBpkFileName);
            var serialized = MessagePackSerializer.Serialize(reachableNodes.Select(x=>x.Value).ToList(), cancellationToken: CancellationToken.None);
            await File.WriteAllBytesAsync(reachableNodesFilePath, serialized, cancellationToken: CancellationToken.None);
            logger.LogInformation("Saved {count} nodes to {reachableNodesFilePath}", reachableNodes.Count, reachableNodesFilePath);
        }
        else
        {
            logger.LogInformation("BasePath not provided. Not saving backups to file.");
        }
    }

    public ConcurrentDictionary<string, Node> LoadAllNodesFromBackup()
    {
        if (!string.IsNullOrEmpty(_basePath))
        {
            var allNodesFilePath = Path.Combine(_basePath, AllNodesBpkFileName);
            logger.LogTrace("Loading backups from {allNodesFilePath}", allNodesFilePath);
            if (File.Exists(allNodesFilePath))
            {
                var serialized = File.ReadAllBytes(allNodesFilePath);
                var nodeList = MessagePackSerializer.Deserialize<List<Node>>(serialized);
                logger.LogInformation("Loaded {count} nodes from {allNodesFilePath}", nodeList.Count, allNodesFilePath);

                return new ConcurrentDictionary<string, Node>(nodeList.ToDictionary(node => node.Id));
            }
        }
        
        logger.LogInformation("BasePath not provided. Not loading backups from file.");
        
        return new ConcurrentDictionary<string, Node>();
    }

    public ConcurrentDictionary<string, Node> LoadReachableNodesFromBackup()
    {
        if (!string.IsNullOrEmpty(_basePath))
        {
            var reachableNodesFilePath = Path.Combine(_basePath, ReachableNodesBpkFileName);
            logger.LogTrace("Loading backups from {reachableNodesFilePath}", reachableNodesFilePath);
            if (File.Exists(reachableNodesFilePath))
            {
                var serialized = File.ReadAllBytes(reachableNodesFilePath);
                var nodeList = MessagePackSerializer.Deserialize<List<Node>>(serialized);
                logger.LogInformation("Loaded {count} nodes from {reachableNodesFilePath}", nodeList.Count,
                    reachableNodesFilePath);

                return new ConcurrentDictionary<string, Node>(nodeList.ToDictionary(node => node.Id));
            }
        }
        
        logger.LogInformation("BasePath not provided. Not loading backups from file.");

        return new ConcurrentDictionary<string, Node>();
    }
}