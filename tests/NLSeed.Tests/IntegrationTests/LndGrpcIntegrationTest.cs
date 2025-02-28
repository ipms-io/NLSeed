// using LNUnit.LND;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.Logging;
// using Moq;
//
// namespace NLSeed.Tests.IntegrationTests;
//
// using Lnrpc;
// using Fixtures;
// using NLSeed.Services;
//
// [Collection("regtest")]
// public class LndGrpcIntegrationTest
// {
//     private readonly LightningRegtestFixture _lightningRegtestFixture;
//     private readonly LNDNodeConnection _alice;
//     private readonly LndService _lndService;
//     private readonly NetworkViewService _networkViewService;
//     
//     public LndGrpcIntegrationTest(LightningRegtestFixture fixture)
//     {
//         _lightningRegtestFixture = fixture;
//
//         _alice = _lightningRegtestFixture.Builder?.LNDNodePool?.ReadyNodes.First(x => x.LocalAlias == "alice") ?? throw new Exception("Alice was not ready.");
//         
//         var inMemorySettings = new Dictionary<string, string?>{
//             { "Lnd:Macaroon", _alice.Settings.MacaroonBase64 },
//             { "Lnd:Cert", _alice.Settings.TLSCertBase64 },
//             { "Lnd:RpcAddress", _alice.Host }
//         };
//
//         var configuration = new ConfigurationBuilder()
//             .AddInMemoryCollection(inMemorySettings)
//             .Build();
//
//         _networkViewService = new NetworkViewService(configuration, new Mock<ILogger<NetworkViewService>>().Object);
//
//         _lndService = new LndService(configuration, new Mock<ILogger<LndService>>().Object);
//     }
//     
//     #region LndService
//     [Fact]
//     public async Task Given_NodeIsRunning_When_CheckConnection_Then_ExpectResponse()
//     {
//         // Arrange
//         // Act
//         var isConnected = await _lndService.CheckConnectionAsync();
//
//         // Assert
//         Assert.True(isConnected);
//     }
//
//     [Fact]
//     public async Task Given_NodeHasConnections_When_ListPeersIsCalled_Then_NodeListIsReturned()
//     {
//         // Arrange
//         
//         // Act
//         var nodes = await _lndService.ListPeersAsync();
//         
//         // Assert
//         Assert.Equal(2, nodes.Count);
//     }
//     
//     [Fact]
//     public async Task Given_NodeHasConnections_When_DescribeGraphIsCalled_Then_NodesAreAddedToNetworkViewService()
//     {
//         // Arrange
//         
//         // Act
//         await _lndService.DescribeGraphAsync();
//     }
//     #endregion
//     
//     #region NetworkViewService
//     // [Fact]
//     // public async Task Given_NodeHasConnections_When_
//     #endregion
// }