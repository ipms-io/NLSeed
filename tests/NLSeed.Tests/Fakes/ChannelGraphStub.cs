namespace NLSeed.Tests.Fakes;

using LnRpc;

public static class ChannelGraphStub
{
    private const string DEFAULT_NETWORK = "tcp";
    public const string NODE1_PUBKEY = "pubkey1";
    public const string NODE1_ADDRESS1 = "dnstest.ipms.io:64254";
    public const int NODE1_ADDRESS1_PORT = 64254;
    public const string NODE1_ADDRESS1_RESOLVED = "127.0.0.1";
    
    public static ChannelGraph GetEmptyChannelGraph()
    {
        return new ChannelGraph();
    }

    public static ChannelGraph GetSingleNodeSingleAddressChannelGraph()
    {
        return new ChannelGraph
        {
            Nodes =
            {
                new LightningNode
                {
                    PubKey = NODE1_PUBKEY,
                    Addresses = {
                        new NodeAddress
                        {
                            Network = DEFAULT_NETWORK,
                            Addr = NODE1_ADDRESS1
                        }
                    }
                }
            }
        };
    }
    
    public static ChannelGraph GetSingleNodePlusOnionAddressChannelGraph()
    {
        return new ChannelGraph
        {
            Nodes = {
                new LightningNode
                {
                    PubKey = NODE1_PUBKEY,
                    Addresses = { 
                        new NodeAddress
                        {
                            Network = DEFAULT_NETWORK,
                            Addr = NODE1_ADDRESS1
                        },
                        new NodeAddress
                        {
                            Network = DEFAULT_NETWORK,
                            Addr = "plhh65fhtzwbpm54foo77widlnt7375wtuhmpvprkzwt4kdcltt2c2ad.onion:9735"
                        }
                    }
                }
            }
        };
    }
}