using System;
using Bravo.Core.Policies;
using Bravo.Core.Updates;
using Bravo.Desktop.Hosting;
using Bravo.Desktop.Policies;
using Bravo.Desktop.Updates;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bravo.Desktop.Tests.Updates;

public sealed class UpdatesServiceCollectionExtensionsTests
{
    // The address that every installed copy of 1.0.x reads: a release must read the same one.
    private static readonly Uri s_releaseFeed = new("https://bravorelease.blob.core.windows.net/bravo-public/currentversion.json");
    private static readonly Uri s_developmentFeed = new("http://127.0.0.1:8099/currentversion.json");

    [Fact]
    public void AddUpdates_ReleaseBuild_ReadsTheFeedOfTheReleases()
    {
        var options = ResolveOptions(BuildIdentity.Release("1.1.0"), developmentFeedAddress: null);

        Assert.Equal(s_releaseFeed, options.FeedAddress);
    }

    [Fact]
    public void AddUpdates_DevelopmentBuildWithoutAFeed_HasNoFeed()
    {
        var options = ResolveOptions(BuildIdentity.Development("1.1.0"), developmentFeedAddress: null);

        Assert.Null(options.FeedAddress);
    }

    [Fact]
    public void AddUpdates_DevelopmentBuildWithAFeed_ReadsThatFeed()
    {
        var options = ResolveOptions(BuildIdentity.Development("1.1.0"), s_developmentFeed);

        Assert.Equal(s_developmentFeed, options.FeedAddress);
    }

    [Fact]
    public void AddUpdates_AnyBuild_ComparesWithItsFileVersionAndAsksForTheFileOfItsRuntime()
    {
        var identity = BuildIdentity.Release("1.1.0") with { FileVersion = new Version(1, 1, 0, 188), IncludesRuntime = true };

        var options = ResolveOptions(identity, developmentFeedAddress: null);

        Assert.Equal(new Version(1, 1, 0, 188), options.FileVersion);
        Assert.True(options.IncludesRuntime);
    }

    [Fact]
    public void AddUpdates_AnyBuild_CreatesTheUpdateCheck()
    {
        var services = new ServiceCollection()
            .AddInstallation()
            .AddPolicies(ApplicationPolicies.None)
            .AddUpdates(BuildIdentity.Release("1.1.0"), developmentFeedAddress: null);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        var updateCheck = provider.GetService<UpdateCheck>();

        Assert.NotNull(updateCheck);
    }

    private static UpdateCheckOptions ResolveOptions(BuildIdentity identity, Uri? developmentFeedAddress)
    {
        var services = new ServiceCollection().AddUpdates(identity, developmentFeedAddress);
        using var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<UpdateCheckOptions>();
    }
}
