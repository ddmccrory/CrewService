using CrewService.Domain.DomainEvents.Notifications;
using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.Notifications;

public class EmployeeNotificationTests
{
    private static readonly ControlNumber Railroad = ControlNumber.Create(1);
    private static readonly ControlNumber Employee = ControlNumber.Create(100);

    [Fact]
    public void Create_RaisesEmployeeNotifiedDomainEvent()
    {
        var notification = EmployeeNotification.Create(
            Railroad, Employee, NotificationCategories.BulletinAward, "You won.", requiresAcknowledgement: true);

        var evt = Assert.Single(notification.DomainEvents);
        var notified = Assert.IsType<EmployeeNotifiedDomainEvent>(evt);
        Assert.Equal("EmployeeNotification", notified.AggregateType);
        Assert.Equal(notification.CtrlNbr.Value, notified.AggregateId);
    }

    [Fact]
    public void Create_RequiresAcknowledgement_NotAcknowledgedUntilConfirmed()
    {
        var notification = EmployeeNotification.Create(
            Railroad, Employee, NotificationCategories.SeniorityMove, "Move complete.", requiresAcknowledgement: true);

        Assert.False(notification.IsAcknowledged);

        notification.AcknowledgeElectronically("user1");

        Assert.True(notification.IsAcknowledged);
    }

    [Fact]
    public void Create_NoAcknowledgementRequired_IsAlwaysAcknowledged()
    {
        var notification = EmployeeNotification.Create(
            Railroad, Employee, NotificationCategories.BulletinAward, "You were not awarded.", requiresAcknowledgement: false);

        Assert.True(notification.IsAcknowledged);
    }
}
