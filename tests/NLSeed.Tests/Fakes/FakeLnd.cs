using Grpc.Core;

namespace NLSeed.Tests.Fakes;

using LnRpc;

public class FakeLnd : Lightning.LightningClient
{
    private ChannelGraph? _channelGraph;

    public void SetExpectedChannelGraph(ChannelGraph graph)
    {
        _channelGraph = graph;
    }

    public override AsyncUnaryCall<ChannelGraph> DescribeGraphAsync(ChannelGraphRequest request, CallOptions options)
    {
        if (_channelGraph is null)
            throw new Exception("Please, provide a ChannelGraphResponse");
        
        return new AsyncUnaryCall<ChannelGraph>(Task.FromResult(_channelGraph),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => [],
            () => { }
        );
    }
}