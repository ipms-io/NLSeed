namespace NLSeed.Tests.TestCollections;

using Fixtures;

[CollectionDefinition("regtest")]
public class LightningRegtestNetworkFixtureCollection : ICollectionFixture<LightningRegtestFixture>
{
    // This class has no code, and is never created. Its purpose is simply to be the place to
    // apply [CollectionDefinition] and all the ICollectionFixture<> interfaces.
}