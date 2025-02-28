// using System.Net;
// using System.Reflection;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.Logging;
// using Moq;
//
// namespace NLSeed.Tests.Services;
//
// using Fakes;
// using LnRpc;
// using Models;
// using NLSeed.Services;
//
// public class LndServiceTests
// {
//     private readonly IConfiguration _configuration;
//     private readonly ILogger<LndService> _logger;
//     private readonly FakeLnd _fakeLnd = new();
//     
//     public LndServiceTests()
//     {
//         // Configuration Stub
//         var inMemorySettings = new Dictionary<string, string?>
//         {
//             { "Lnd:Macaroon", CredentialsStub.FakeMacaroon },
//             { "Lnd:Cert", CredentialsStub.FakeCertificate },
//             { "Lnd:RpcAddress", "https://localhost:8080" }
//         };
//
//         _configuration = new ConfigurationBuilder()
//             .AddInMemoryCollection(inMemorySettings)
//             .Build();
//         
//         // Logger Mock
//         _logger = new Mock<ILogger<LndService>>().Object;
//     }
//
//     #region Constructor
//     [Fact]
//     public void Given_CorrectConfiguration_When_ConstructorIsCalled_Then_ServiceIsCreated()
//     {
//         // Given
//         // When
//         var lndService = new LndService(_configuration, _logger);
//         
//         // Then
//         Assert.NotNull(lndService);
//     }
//     #endregion
//
//     #region DescribeGraph
//     [Fact]
//     public async Task Given_NodeHasNoConnections_When_DescribeGraphAsyncIsCalled_Then_ReturnsEmptyList()
//     {
//         // Given
//         var lndService = new LndService(_configuration, _logger);
//         SetExpectedDescribeGraphResponse(lndService, ChannelGraphStub.GetEmptyChannelGraph());
//         
//         // When
//         var response = await lndService.DescribeGraphAsync();
//         
//         // Then
//         Assert.Empty(response);
//     }
//     
//     [Fact]
//     public async Task Given_GraphHasOneNodeWithOneAddress_When_DescribeGraphAsyncIsCalled_Then_ReturnsExpectedList()
//     {
//         // Given
//         List<Node> expectedResponse = [
//             new Node(ChannelGraphStub.NODE1_PUBKEY){Addresses = { new IPEndPoint(IPAddress.Parse(ChannelGraphStub.NODE1_ADDRESS1_RESOLVED), ChannelGraphStub.NODE1_ADDRESS1_PORT) }}
//         ];
//         var lndService = new LndService(_configuration, _logger);
//         SetExpectedDescribeGraphResponse(lndService, ChannelGraphStub.GetSingleNodeSingleAddressChannelGraph());
//         
//         // When
//         var response = await lndService.DescribeGraphAsync();
//         
//         // Then
//         Assert.Equal(expectedResponse, response.Values);
//     }
//     
//     [Fact]
//     public async Task Given_GraphHasOneNodeWithOnionAddress_When_DescribeGraphAsyncIsCalled_Then_ReturnsExpectedList()
//     {
//         // Given
//         List<Node> expectedResponse = [
//             new Node(ChannelGraphStub.NODE1_PUBKEY){Addresses = { new IPEndPoint(IPAddress.Parse(ChannelGraphStub.NODE1_ADDRESS1_RESOLVED), ChannelGraphStub.NODE1_ADDRESS1_PORT) }}
//         ];
//         var lndService = new LndService(_configuration, _logger);
//         SetExpectedDescribeGraphResponse(lndService, ChannelGraphStub.GetSingleNodeSingleAddressChannelGraph());
//         
//         // When
//         var response = await lndService.DescribeGraphAsync();
//         
//         // Then
//         Assert.Equal(expectedResponse, response.Values);
//     }
//
//     private void SetExpectedDescribeGraphResponse(LndService lndService, ChannelGraph expectedGraph)
//     {
//         _fakeLnd.SetExpectedChannelGraph(expectedGraph);
//         var rpcClientField = typeof(LndService).GetField("_rpcClient", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new NullReferenceException("Field no found");
//         rpcClientField.SetValue(lndService, _fakeLnd);
//     }
//     #endregion
// }