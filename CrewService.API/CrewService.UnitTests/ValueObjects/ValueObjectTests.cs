using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.ValueObjects;

public class ControlNumberTests
{
    [Fact]
    public void Create_WithValue_ReturnsCorrectValue()
    {
        var ctrl = ControlNumber.Create(12345);

        Assert.Equal(12345, ctrl.Value);
    }

    [Fact]
    public void Create_Parameterless_ReturnsUniqueValues()
    {
        var ctrl1 = ControlNumber.Create();
        var ctrl2 = ControlNumber.Create();

        Assert.NotEqual(ctrl1.Value, ctrl2.Value);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var ctrl1 = ControlNumber.Create(100);
        var ctrl2 = ControlNumber.Create(100);

        Assert.Equal(ctrl1, ctrl2);
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var ctrl1 = ControlNumber.Create(100);
        var ctrl2 = ControlNumber.Create(200);

        Assert.NotEqual(ctrl1, ctrl2);
    }
}

public class NameTests
{
    [Fact]
    public void Create_ValidString_ReturnsCorrectValue()
    {
        var name = Name.Create("Test");

        Assert.Equal("Test", name.Value);
    }

    [Fact]
    public void Create_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Name.Create(null));
    }

    [Fact]
    public void Create_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() => Name.Create(string.Empty));
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var name1 = Name.Create("Alpha");
        var name2 = Name.Create("Alpha");

        Assert.Equal(name1, name2);
    }
}

public class AuditStampTests
{
    [Fact]
    public void Create_SetsNameAndDateTime()
    {
        var stamp = AuditStamp.Create("admin");

        Assert.Equal("admin", stamp.AuditName.Value);
        Assert.True(stamp.AuditDateTime <= DateTime.UtcNow);
    }
}
