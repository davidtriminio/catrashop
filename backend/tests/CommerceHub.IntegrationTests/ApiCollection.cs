using Xunit;

namespace CommerceHub.IntegrationTests;

/// <summary>Boots the in-memory API host once and shares it across every integration test class.</summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<CommerceHubApiFactory>
{
    public const string Name = "CommerceHub API";
}
