using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using CrewService.GrpcService;

namespace CrewService.UnitTests.Notifications;

public sealed class NotificationTypeConfigSeedDefaultsTests
{
    [Fact]
    public async Task SeedForRailroadAsync_AddsMissingDefaults()
    {
        var repo = new InMemoryNotificationTypeConfigRepository();
        var railroad = DynamicGroup.Create(
            groupTypeCtrlNbr: ControlNumber.Create(100),
            name: "CSX",
            parentGroupCtrlNbr: null,
            path: null,
            isWorkArea: false,
            code: "CSX",
            parentCtrlNbr: ControlNumber.Create(10));

        await NotificationTypeConfigSeedDefaults.SeedForRailroadAsync(repo, railroad, ct: TestContext.Current.CancellationToken);

        var configs = await repo.GetByRailroadAsync(railroad.CtrlNbr, TestContext.Current.CancellationToken);
        Assert.Equal(7, configs.Count);
        Assert.Contains(configs, c => c.Key == NotificationCategories.BulletinAward && c.RequiresAcknowledgementDefault);
        Assert.Contains(configs, c => c.Key == NotificationCategories.ForceAssign && c.RequiresAcknowledgementDefault);
        Assert.Contains(configs, c => c.Key == NotificationCategories.BulletinCancellation && !c.RequiresAcknowledgementDefault);
        Assert.Contains(configs, c => c.Key == NotificationCategories.SeniorityMove && c.RequiresAcknowledgementDefault);
        Assert.Contains(configs, c => c.Key == NotificationCategories.PositionChange && c.RequiresAcknowledgementDefault);
        Assert.Contains(configs, c => c.Key == NotificationCategories.BoardPlacement && !c.RequiresAcknowledgementDefault);
        Assert.Contains(configs, c => c.Key == NotificationCategories.GeneralInformation && !c.RequiresAcknowledgementDefault);
    }

    [Fact]
    public async Task SeedForRailroadAsync_IsIdempotent()
    {
        var repo = new InMemoryNotificationTypeConfigRepository();
        var railroad = DynamicGroup.Create(
            groupTypeCtrlNbr: ControlNumber.Create(100),
            name: "PTRA",
            parentGroupCtrlNbr: null,
            path: null,
            isWorkArea: true,
            code: "PTRA",
            parentCtrlNbr: ControlNumber.Create(11));

        await NotificationTypeConfigSeedDefaults.SeedForRailroadAsync(repo, railroad, ct: TestContext.Current.CancellationToken);
        await NotificationTypeConfigSeedDefaults.SeedForRailroadAsync(repo, railroad, ct: TestContext.Current.CancellationToken);

        var configs = await repo.GetByRailroadAsync(railroad.CtrlNbr, TestContext.Current.CancellationToken);
        Assert.Equal(7, configs.Count);
        Assert.Equal(7, configs.Select(c => c.Key).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task SeedForRailroadsAsync_SeedsEachRailroad()
    {
        var repo = new InMemoryNotificationTypeConfigRepository();
        var railroads = new[]
        {
            DynamicGroup.Create(ControlNumber.Create(100), "CSX", null, null, false, code: "CSX", parentCtrlNbr: ControlNumber.Create(10)),
            DynamicGroup.Create(ControlNumber.Create(100), "PTRA", null, null, true, code: "PTRA", parentCtrlNbr: ControlNumber.Create(11))
        };

        await NotificationTypeConfigSeedDefaults.SeedForRailroadsAsync(repo, railroads, ct: TestContext.Current.CancellationToken);

        var csxConfigs = await repo.GetByRailroadAsync(railroads[0].CtrlNbr, TestContext.Current.CancellationToken);
        var ptraConfigs = await repo.GetByRailroadAsync(railroads[1].CtrlNbr, TestContext.Current.CancellationToken);
        Assert.Equal(7, csxConfigs.Count);
        Assert.Equal(7, ptraConfigs.Count);
    }

    private sealed class InMemoryNotificationTypeConfigRepository : INotificationTypeConfigRepository
    {
        private readonly List<NotificationTypeConfig> _items = [];

        public Task AddAsync(NotificationTypeConfig entity, CancellationToken ct = default)
        {
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public void Add(NotificationTypeConfig entity) => _items.Add(entity);

        public Task<List<NotificationTypeConfig>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(_items.ToList());

        public Task<List<NotificationTypeConfig>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default)
            => Task.FromResult(_items.Skip(pageNumber * pageSize).Take(pageSize).ToList());

        public Task<NotificationTypeConfig?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult(_items.FirstOrDefault(i => i.CtrlNbr == ctrlNbr));

        public Task<NotificationTypeConfig?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult(_items.FirstOrDefault(i => i.CtrlNbr == ctrlNbr));

        public Task UpdateAsync(NotificationTypeConfig entity, CancellationToken ct = default) => Task.CompletedTask;
        public void Update(NotificationTypeConfig entity) { }

        public Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
        {
            _items.RemoveAll(i => i.CtrlNbr == ctrlNbr);
            return Task.CompletedTask;
        }

        public Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public void Remove(NotificationTypeConfig entity) => _items.Remove(entity);

        public Task<List<NotificationTypeConfig>> GetByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(_items.Where(i => i.RailroadCtrlNbr == railroadCtrlNbr).ToList());

        public Task<NotificationTypeConfig?> GetByRailroadAndKeyAsync(ControlNumber railroadCtrlNbr, string key, CancellationToken ct = default)
            => Task.FromResult(_items.FirstOrDefault(i => i.RailroadCtrlNbr == railroadCtrlNbr && string.Equals(i.Key, key, StringComparison.Ordinal)));
    }
}
