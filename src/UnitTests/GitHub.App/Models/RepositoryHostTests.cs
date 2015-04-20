﻿using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;
using GitHub.Api;
using GitHub.Authentication;
using GitHub.Caches;
using GitHub.Models;
using GitHub.Primitives;
using GitHub.Services;
using NSubstitute;
using Octokit;
using UnitTests.Helpers;
using Xunit;

public class RepositoryHostTests
{
    public class TheLoginMethod
    {
        [Fact]
        public async Task SetsTheLoggedInBitWhenLoginIsSuccessful()
        {
            var apiClient = Substitute.For<IApiClient>();
            apiClient.HostAddress.Returns(HostAddress.GitHubDotComHostAddress);
            apiClient.GetOrCreateApplicationAuthenticationCode(Arg.Any<Func<TwoFactorAuthorizationException, IObservable<TwoFactorChallengeResult>>>(), Args.Boolean)
                .Returns(Observable.Return(new ApplicationAuthorization("1234")));
            apiClient.GetUser().Returns(Observable.Return(new User()));
            var modelService = Substitute.For<IModelService>();
            var loginCache = new TestLoginCache();
            var host = new RepositoryHost(apiClient, modelService, loginCache, Substitute.For<ITwoFactorChallengeHandler>());

            await host.LogIn("aUsername", "aPassowrd");

            Assert.True(host.IsLoggedIn);
        }

        [Fact]
        public async Task CachesTheLoggedInUserWhenLoginIsSuccessful()
        {
            var apiClient = Substitute.For<IApiClient>();
            apiClient.HostAddress.Returns(HostAddress.GitHubDotComHostAddress);
            apiClient.GetOrCreateApplicationAuthenticationCode(Arg.Any<Func<TwoFactorAuthorizationException, IObservable<TwoFactorChallengeResult>>>(), Args.Boolean)
                .Returns(Observable.Return(new ApplicationAuthorization("S3CR3TS")));
            apiClient.GetUser().Returns(Observable.Return(CreateOctokitUser("lagavulin")));
            var hostCache = new InMemoryBlobCache();
            var modelService = new ModelService(apiClient, hostCache, Substitute.For<IAvatarProvider>());
            var loginCache = new TestLoginCache();
            var host = new RepositoryHost(apiClient, modelService, loginCache, Substitute.For<ITwoFactorChallengeHandler>());

            await host.LogIn("aUsername", "aPassword");

            var user = await hostCache.GetObject<AccountCacheItem>("user");
            Assert.NotNull(user);
            Assert.Equal("lagavulin", user.Login);
        }
    }

    static User CreateOctokitUser(string login)
    {
        return new User("https://url", "", "", 1, "GitHub", DateTimeOffset.UtcNow, 0, "email", 100, 100, true, "http://url", 10, 42, "somewhere", login, "Who cares", 1, new Plan(), 1, 1, 1, "https://url", false);
    }

    static Organization CreateOctokitOrganization(string login)
    {
        return new Organization("https://url", "", "", 1, "GitHub", DateTimeOffset.UtcNow, 0, "email", 100, 100, true, "http://url", 10, 42, "somewhere", login, "Who cares", 1, new Plan(), 1, 1, 1, "https://url", "billing");
    }
}
