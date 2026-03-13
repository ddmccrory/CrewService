using CrewService.Domain.Modules.RailroadInfo;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.RailroadInfo;

public class RailroadInformationTests
{
    [Fact]
    public void Create_DefaultsToDraft()
    {
        var info = RailroadInformation.Create(1, "Bulletin", "Test Subject", "Test Body");

        Assert.Equal("Draft", info.Status);
        Assert.Null(info.PublishedAtUtc);
        Assert.Null(info.ClosedAtUtc);
        Assert.True(info.DomainEvents.Count > 0);
    }

    [Fact]
    public void Publish_FromDraft_SetsPublished()
    {
        var info = RailroadInformation.Create(1, "Bulletin", "Test", "Body");

        info.Publish();

        Assert.Equal("Published", info.Status);
        Assert.NotNull(info.PublishedAtUtc);
    }

    [Fact]
    public void Publish_FromPublished_Throws()
    {
        var info = RailroadInformation.Create(1, "Bulletin", "Test", "Body");
        info.Publish();

        Assert.Throws<InvalidOperationException>(() => info.Publish());
    }

    [Fact]
    public void Close_FromPublished_SetsClosed()
    {
        var info = RailroadInformation.Create(1, "Notice", "Test", "Body");
        info.Publish();

        info.Close();

        Assert.Equal("Closed", info.Status);
        Assert.NotNull(info.ClosedAtUtc);
    }

    [Fact]
    public void Close_FromDraft_Throws()
    {
        var info = RailroadInformation.Create(1, "Notice", "Test", "Body");

        Assert.Throws<InvalidOperationException>(() => info.Close());
    }

    [Fact]
    public void Cancel_FromDraft_SetsCancelled()
    {
        var info = RailroadInformation.Create(1, "Notice", "Test", "Body");

        info.Cancel();

        Assert.Equal("Cancelled", info.Status);
        Assert.NotNull(info.ClosedAtUtc);
    }

    [Fact]
    public void Cancel_FromClosed_Throws()
    {
        var info = RailroadInformation.Create(1, "Notice", "Test", "Body");
        info.Publish();
        info.Close();

        Assert.Throws<InvalidOperationException>(() => info.Cancel());
    }

    [Fact]
    public void Update_FromDraft_Succeeds()
    {
        var info = RailroadInformation.Create(1, "Bulletin", "Old", "OldBody");

        info.Update("New Subject", "New Body", "Notice");

        Assert.Equal("New Subject", info.Subject);
        Assert.Equal("New Body", info.Body);
        Assert.Equal("Notice", info.InformationType);
    }

    [Fact]
    public void Update_FromPublished_Throws()
    {
        var info = RailroadInformation.Create(1, "Bulletin", "Test", "Body");
        info.Publish();

        Assert.Throws<InvalidOperationException>(() => info.Update("X", "Y", "Z"));
    }
}

public class RailroadInformationReadReceiptTests
{
    [Fact]
    public void Create_SetsPropertiesCorrectly()
    {
        var infoCtrl = ControlNumber.Create(100);
        var empCtrl = ControlNumber.Create(200);

        var receipt = RailroadInformationReadReceipt.Create(infoCtrl, empCtrl);

        Assert.Equal(infoCtrl, receipt.InformationCtrlNbr);
        Assert.Equal(empCtrl, receipt.EmployeeCtrlNbr);
        Assert.True(receipt.ReadAtUtc <= DateTime.UtcNow);
    }
}
