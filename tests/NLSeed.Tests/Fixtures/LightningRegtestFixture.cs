using Docker.DotNet;
using Docker.DotNet.Models;
using LNUnit.Setup;

namespace NLSeed.Tests.Fixtures;

// ReSharper disable once ClassNeverInstantiated.Global
public class LightningRegtestFixture : IDisposable
{
    private readonly DockerClient _client = new DockerClientConfiguration().CreateClient();

    public LightningRegtestFixture()
    {
        SetupNetwork().Wait();
    }

    public LNUnitBuilder? Builder { get; private set; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        // Remove containers
        RemoveContainer("miner").Wait();
        RemoveContainer("alice").Wait();
        RemoveContainer("bob").Wait();
        RemoveContainer("carol").Wait();
        RemoveContainer("dave").Wait();
        RemoveContainer("ellen").Wait();
        RemoveContainer("frank").Wait();

        Builder?.Destroy();
        _client.Dispose();
    }

    private async Task SetupNetwork()
    {
        await RemoveContainer("miner");
        await RemoveContainer("alice");
        await RemoveContainer("bob");
        await RemoveContainer("carol");
        await RemoveContainer("dave");
        await RemoveContainer("ellen");
        await RemoveContainer("frank");

        // await _client.CreateDockerImageFromPath("../../../../Docker/custom_lnd", ["custom_lnd", "custom_lnd:latest"]);

        Builder = new LNUnitBuilder();

        Builder.AddBitcoinCoreNode();

        Builder.AddPolarLNDNode("alice",
        [
            new LNUnitNetworkDefinition.Channel
            {
                ChannelSize = 10_000_000, //10MSat
                RemoteName = "bob"
            }
        ], imageName: "custom_lnd", tagName: "latest", pullImage: false);

        Builder.AddPolarLNDNode("bob",
        [
            new LNUnitNetworkDefinition.Channel
            {
                ChannelSize = 10_000_000, //10MSat
                RemotePushOnStart = 1_000_000, // 1MSat
                RemoteName = "alice"
            },
            new LNUnitNetworkDefinition.Channel
            {
                ChannelSize = 10_000_000, //10MSat
                RemotePushOnStart = 1_000_000, // 1MSat
                RemoteName = "carol"
            }
        ], imageName: "custom_lnd", tagName: "latest", pullImage: false);

        Builder.AddPolarLNDNode("carol",
        [
            new LNUnitNetworkDefinition.Channel
            {
                ChannelSize = 10_000_000, //10MSat
                RemotePushOnStart = 1_000_000, // 1MSat
                RemoteName = "bob"
            }
        ], imageName: "custom_lnd", tagName: "latest", pullImage: false);

        Builder.AddPolarLNDNode("dave",
        [
            new LNUnitNetworkDefinition.Channel
            {
                ChannelSize = 10_000_000, //10MSat
                RemotePushOnStart = 1_000_000, // 1MSat
                RemoteName = "carol"
            }
        ], imageName: "custom_lnd", tagName: "latest", pullImage: false);

        Builder.AddPolarLNDNode("ellen",
        [
            new LNUnitNetworkDefinition.Channel
            {
                ChannelSize = 10_000_000, //10MSat
                RemotePushOnStart = 1_000_000, // 1MSat
                RemoteName = "dave"
            },
            new LNUnitNetworkDefinition.Channel
            {
            ChannelSize = 10_000_000, //10MSat
            RemotePushOnStart = 1_000_000, // 1MSat
            RemoteName = "frank"
            }
        ], imageName: "custom_lnd", tagName: "latest", pullImage: false);

        Builder.AddPolarLNDNode("frank",
        [
            new LNUnitNetworkDefinition.Channel
            {
                ChannelSize = 10_000_000, //10MSat
                RemotePushOnStart = 1_000_000, // 1MSat
                RemoteName = "bob"
            }
        ], imageName: "custom_lnd", tagName: "latest", pullImage: false);

        await Builder.Build();
    }

    private async Task RemoveContainer(string name)
    {
        try
        {
            await _client.Containers.RemoveContainerAsync(name, new ContainerRemoveParameters { Force = true, RemoveVolumes = true });
        }
        catch
        {
            // ignored
        }
    }
}