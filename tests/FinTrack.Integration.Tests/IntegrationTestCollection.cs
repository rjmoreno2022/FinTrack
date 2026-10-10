using Xunit;

namespace FinTrack.Integration.Tests;

[CollectionDefinition("IntegrationTests", DisableParallelization = true)]
public class IntegrationTestCollection : ICollectionFixture<FinTrackApiFactory>
{
}
