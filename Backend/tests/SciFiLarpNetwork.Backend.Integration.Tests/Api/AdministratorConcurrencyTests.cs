using Marten;
using Microsoft.Extensions.DependencyInjection;
using SciFiLarpNetwork.Backend.Application.Administration;

namespace SciFiLarpNetwork.Backend.Tests.Integration.Api;

/// <summary>
/// Forces two administrator commands to overlap: the test clock makes each wait for the other after loading and
/// before saving, which is when a race between them can slip past the checks.
/// </summary>
[Collection("Backend database integration")]
public sealed class AdministratorConcurrencyTests : IAsyncLifetime
{
    private const string Password = "second-admin-password-1";
    private readonly AdministratorAuthenticationFactory _factory = new();

    private IAdministratorAccountService Accounts => _factory.Services.GetRequiredService<IAdministratorAccountService>();

    public async ValueTask InitializeAsync()
    {
        await _factory.InitializeAsync();
        await _factory.Services.GetRequiredService<IDocumentStore>().Advanced.ResetAllData();
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task CreatingAnAccountWhileBeingDemotedLetsOnlyOneSucceed()
    {
        var (first, second) = await TwoAdministratorsAsync();

        var (demotion, creation) = await OverlapAsync(
            () => Accounts.ChangeRoleAsync(second, first, AdministratorRoles.Reader, TestContext.Current.CancellationToken),
            async () => (await Accounts.CreateAccountAsync(first, "new-reader", Password, AdministratorRoles.Reader, TestContext.Current.CancellationToken)).Result);

        Assert.Single(new[] { demotion, creation }, result => result == AdministratorOperationResult.Succeeded);
    }

    [Fact]
    public async Task AdministratorsDemotingEachOtherKeepOneAdministrator()
    {
        var (first, second) = await TwoAdministratorsAsync();

        await OverlapAsync(
            () => Accounts.ChangeRoleAsync(first, second, AdministratorRoles.Reader, TestContext.Current.CancellationToken),
            () => Accounts.ChangeRoleAsync(second, first, AdministratorRoles.Reader, TestContext.Current.CancellationToken));

        Assert.Equal(1, await AdministratorCountAsync());
    }

    [Fact]
    public async Task LastTwoAdministratorsDemotingThemselvesKeepOneAdministrator()
    {
        var (first, second) = await TwoAdministratorsAsync();

        await OverlapAsync(
            () => Accounts.ChangeRoleAsync(first, first, AdministratorRoles.Reader, TestContext.Current.CancellationToken),
            () => Accounts.ChangeRoleAsync(second, second, AdministratorRoles.Reader, TestContext.Current.CancellationToken));

        Assert.Equal(1, await AdministratorCountAsync());
    }

    [Fact]
    public async Task AdministratorsCreatingDifferentAccountsDoNotBlockEachOther()
    {
        var (first, second) = await TwoAdministratorsAsync();

        var results = await OverlapAsync(
            async () => (await Accounts.CreateAccountAsync(first, "reader-one", Password, AdministratorRoles.Reader, TestContext.Current.CancellationToken)).Result,
            async () => (await Accounts.CreateAccountAsync(second, "reader-two", Password, AdministratorRoles.Reader, TestContext.Current.CancellationToken)).Result);

        Assert.Equal((AdministratorOperationResult.Succeeded, AdministratorOperationResult.Succeeded), results);
    }

    [Fact]
    public async Task ALostRoleChangeRaceIsReportedAsAConcurrentChange()
    {
        var (first, second) = await TwoAdministratorsAsync();
        var readerOne = await CreateAccountAsync(first, "reader-one", AdministratorRoles.Reader);
        var readerTwo = await CreateAccountAsync(first, "reader-two", AdministratorRoles.Reader);

        var results = await OverlapAsync(
            () => Accounts.ChangeRoleAsync(first, readerOne, AdministratorRoles.Administrator, TestContext.Current.CancellationToken),
            () => Accounts.ChangeRoleAsync(second, readerTwo, AdministratorRoles.Administrator, TestContext.Current.CancellationToken));

        Assert.Equal(
            [AdministratorOperationResult.Succeeded, AdministratorOperationResult.ConcurrentChange],
            new[] { results.First, results.Second }.Order());
    }

    [Fact]
    public async Task ALostAccountCreationRaceIsReportedAsAConcurrentChange()
    {
        var (first, _) = await TwoAdministratorsAsync();

        var results = await OverlapAsync(
            async () => (await Accounts.CreateAccountAsync(first, "reader-one", Password, AdministratorRoles.Reader, TestContext.Current.CancellationToken)).Result,
            async () => (await Accounts.CreateAccountAsync(first, "reader-two", Password, AdministratorRoles.Reader, TestContext.Current.CancellationToken)).Result);

        Assert.Equal(
            [AdministratorOperationResult.Succeeded, AdministratorOperationResult.ConcurrentChange],
            new[] { results.First, results.Second }.Order());
    }

    private async Task<Guid> CreateAccountAsync(Guid actor, string userName, string role) =>
        (await Accounts.CreateAccountAsync(actor, userName, Password, role, TestContext.Current.CancellationToken)).Account!.AccountId;

    private async Task<(T First, T Second)> OverlapAsync<T>(Func<Task<T>> first, Func<Task<T>> second)
    {
        using var overlap = new Barrier(2);
        _factory.Clock.ReadBarrier = overlap;
        try
        {
            var results = await Task.WhenAll(Task.Run(first), Task.Run(second));
            return (results[0], results[1]);
        }
        finally
        {
            _factory.Clock.ReadBarrier = null;
        }
    }

    private async Task<(Guid First, Guid Second)> TwoAdministratorsAsync()
    {
        await _factory.BootstrapAdministratorAsync();
        var first = (await _factory.LoadAccountDocumentsAsync()).Single().Id;
        var created = await Accounts.CreateAccountAsync(first, "second-admin", Password, AdministratorRoles.Administrator, TestContext.Current.CancellationToken);
        return (first, created.Account!.AccountId);
    }

    private async Task<int> AdministratorCountAsync() =>
        (await Accounts.GetAdministratorsAsync(TestContext.Current.CancellationToken))
        .Count(account => account.Role == AdministratorRoles.Administrator);
}
