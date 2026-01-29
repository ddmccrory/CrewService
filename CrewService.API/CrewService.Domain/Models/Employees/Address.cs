using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.Employees;

public sealed class Address : Entity
{
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public ControlNumber AddressTypeCtrlNbr { get; private set; }
    public string Address1 { get; private set; } = string.Empty;
    public string? Address2 { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string ZipCode { get; private set; } = string.Empty;

    private Address()
    {
        EmployeeCtrlNbr = null!;
        AddressTypeCtrlNbr = null!;
    }

    private Address(
        ControlNumber employeeCtrlNbr,
        ControlNumber addressTypeCtrlNbr,
        string address1,
        string city,
        string state,
        string zipCode,
        string? address2)
    {
        EmployeeCtrlNbr = employeeCtrlNbr;
        AddressTypeCtrlNbr = addressTypeCtrlNbr;
        Address1 = address1;
        City = city;
        State = state.ToUpperInvariant();
        ZipCode = zipCode;
        Address2 = address2;
    }

    internal static Address Create(
        ControlNumber employeeCtrlNbr,
        long addressTypeCtrlNbr,
        string address1,
        string city,
        string state,
        string zipCode,
        string? address2 = null)
    {
        return new Address(
            employeeCtrlNbr,
            ControlNumber.Create(addressTypeCtrlNbr),
            address1,
            city,
            state,
            zipCode,
            address2);
    }

    public Address Update(string? address1 = null, string? address2 = null, string? city = null, string? state = null, string? zipCode = null)
    {
        if (address1 is not null) Address1 = address1;
        if (address2 is not null) Address2 = address2;
        if (city is not null) City = city;
        if (state is not null) State = state.ToUpperInvariant();
        if (zipCode is not null) ZipCode = zipCode;

        return this;
    }
}