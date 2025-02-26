namespace NLSeed.Interfaces;

using Models;

public interface ILightningService
{
    Task<IDictionary<string, Node>> DescribeGraphAsync(CancellationToken cancellationToken);
    Task<IDictionary<string, Node>> ListPeersAsync(CancellationToken cancellationToken);
    Task<bool> CheckConnectionAsync(CancellationToken cancellationToken);
}