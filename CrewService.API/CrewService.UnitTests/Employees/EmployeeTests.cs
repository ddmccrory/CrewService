using CrewService.Domain.Models.Employees;
using Xunit;

namespace CrewService.UnitTests.Employees;

public class EmployeeTests
{
    [Fact]
    public void Create_SetsPropertiesAndRaisesEvent()
    {
        var emp = Employee.Create(1, "user1", "EMP001", "123-45-6789",
            Gender.Male, Race.White, new DateTime(1990, 1, 1), new DateTime(2020, 6, 15), 100,
            "user1@example.com", "user1", "User One");

        Assert.Equal("EMP001", emp.EmployeeNumber);
        Assert.Equal("user1", emp.UserId);
        Assert.Equal(Gender.Male, emp.Gender);
        Assert.True(emp.CallForOvertime);
        Assert.True(emp.ProcessPayroll);
        Assert.False(emp.AllowFMLAMarkOff);
        Assert.True(emp.DomainEvents.Count > 0);
    }

    [Fact]
    public void Update_ChangesOnlySpecifiedFields()
    {
        var emp = Employee.Create(1, "user1", "EMP001", "123-45-6789",
            Gender.Male, Race.White, new DateTime(1990, 1, 1), new DateTime(2020, 6, 15), 100,
            "user1@example.com", "user1", "User One");

        emp.Update(driversLicenseNumber: "DL12345", issuingState: "tx", callForOvertime: false);

        Assert.Equal("DL12345", emp.DriversLicenseNumber);
        Assert.Equal("TX", emp.IssuingState);
        Assert.False(emp.CallForOvertime);
        Assert.True(emp.ProcessPayroll);
    }

    [Fact]
    public void Update_NoChanges_DoesNotRaiseEvent()
    {
        var emp = Employee.Create(1, "user1", "EMP001", "123-45-6789",
            Gender.Male, Race.White, new DateTime(1990, 1, 1), new DateTime(2020, 6, 15), 100,
            "user1@example.com", "user1", "User One");
        var eventCountBefore = emp.DomainEvents.Count;

        emp.Update();

        Assert.Equal(eventCountBefore, emp.DomainEvents.Count);
    }

    [Fact]
    public void AddAddress_AddsToCollection()
    {
        var emp = Employee.Create(1, "user1", "EMP001", "123-45-6789",
            Gender.Male, Race.White, new DateTime(1990, 1, 1), new DateTime(2020, 6, 15), 100,
            "user1@example.com", "user1", "User One");

        var address = emp.AddAddress("123 Main St", "Dallas", "TX", "75001", 1);

        Assert.Single(emp.Addresses);
        Assert.Equal("123 Main St", address.Address1);
    }

    [Fact]
    public void AddPhoneNumber_AddsToCollection()
    {
        var emp = Employee.Create(1, "user1", "EMP001", "123-45-6789",
            Gender.Male, Race.White, new DateTime(1990, 1, 1), new DateTime(2020, 6, 15), 100,
            "user1@example.com", "user1", "User One");

        var phone = emp.AddPhoneNumber("555-0100", 1, true, 1);

        Assert.Single(emp.PhoneNumbers);
        Assert.Equal("555-0100", phone.Number);
    }

    [Fact]
    public void AddEmailAddress_AddsToCollection()
    {
        var emp = Employee.Create(1, "user1", "EMP001", "123-45-6789",
            Gender.Male, Race.White, new DateTime(1990, 1, 1), new DateTime(2020, 6, 15), 100,
            "user1@example.com", "user1", "User One");

        var email = emp.AddEmailAddress("test@example.com", 1);

        Assert.Single(emp.EmailAddresses);
        Assert.Equal("test@example.com", email.Email);
    }
}
