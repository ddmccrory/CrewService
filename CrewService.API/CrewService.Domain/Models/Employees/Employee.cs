using CrewService.Domain.DomainEvents.Employees;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;
using System.Collections.ObjectModel;

namespace CrewService.Domain.Models.Employees;

public sealed class Employee : Entity
{
    private readonly Collection<Address> _addresses = [];
    private readonly Collection<PhoneNumber> _phoneNumbers = [];
    private readonly Collection<EmailAddress> _emailAddresses = [];

    public ControlNumber ClientCtrlNbr { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string EmployeeNumber { get; private set; } = string.Empty;
    public string SocialSecurityNumber { get; private set; } = string.Empty;
    public string? DriversLicenseNumber { get; private set; }
    public string? IssuingState { get; private set; }
    public string Gender { get; private set; } = string.Empty;
    public string Race { get; private set; } = string.Empty;
    public bool MarriageStatus { get; private set; }
    public DateTime BirthDate { get; private set; }
    public DateTime EmploymentDate { get; private set; }
    public ControlNumber EmploymentStatusCtrlNbr { get; private set; }
    public bool AllowFMLAMarkOff { get; private set; }
    public bool CallForOvertime { get; private set; }
    public bool ProcessPayroll { get; private set; }
    public bool TieUpOffProperty { get; private set; }

    public IReadOnlyList<Address> Addresses => [.. _addresses];
    public IReadOnlyList<PhoneNumber> PhoneNumbers => [.. _phoneNumbers];
    public IReadOnlyList<EmailAddress> EmailAddresses => [.. _emailAddresses];

    private Employee()
    {
        ClientCtrlNbr = null!;
        EmploymentStatusCtrlNbr = null!;
    }

    private Employee(
        ControlNumber clientCtrlNbr,
        string userId,
        string employeeNumber,
        string ssn,
        string gender,
        string race,
        DateTime birthDate,
        DateTime employmentDate,
        ControlNumber employmentStatusCtrlNbr)
    {
        ClientCtrlNbr = clientCtrlNbr;
        UserId = userId;
        EmployeeNumber = employeeNumber;
        SocialSecurityNumber = ssn;
        Gender = gender;
        Race = race;
        BirthDate = birthDate;
        EmploymentDate = employmentDate;
        EmploymentStatusCtrlNbr = employmentStatusCtrlNbr;
        IssuingState = "TX";
        AllowFMLAMarkOff = false;
        CallForOvertime = true;
        ProcessPayroll = true;
        TieUpOffProperty = false;
    }

    public static Employee Create(
        long clientCtrlNbr,
        string userId,
        string employeeNumber,
        string ssn,
        string gender,
        string race,
        DateTime birthDate,
        DateTime employmentDate,
        long employmentStatusCtrlNbr)
    {
        var employee = new Employee(
            ControlNumber.Create(clientCtrlNbr),
            userId,
            employeeNumber,
            ssn,
            gender,
            race,
            birthDate,
            employmentDate,
            ControlNumber.Create(employmentStatusCtrlNbr));

        employee.Raise(new EmployeeCreatedDomainEvent(employee.CtrlNbr));

        return employee;
    }

    public Employee Update(
        string? driversLicenseNumber = null,
        string? issuingState = null,
        bool? marriageStatus = null,
        bool? allowFMLAMarkOff = null,
        bool? callForOvertime = null,
        bool? processPayroll = null,
        bool? tieUpOffProperty = null)
    {
        var changes = new Dictionary<string, object?>();

        if (driversLicenseNumber is not null)
        {
            DriversLicenseNumber = driversLicenseNumber;
            changes["driversLicenseNumber"] = driversLicenseNumber;
        }

        if (issuingState is not null)
        {
            IssuingState = issuingState.ToUpperInvariant();
            changes["issuingState"] = IssuingState;
        }

        if (marriageStatus.HasValue)
        {
            MarriageStatus = marriageStatus.Value;
            changes["marriageStatus"] = marriageStatus.Value;
        }

        if (allowFMLAMarkOff.HasValue)
        {
            AllowFMLAMarkOff = allowFMLAMarkOff.Value;
            changes["allowFMLAMarkOff"] = allowFMLAMarkOff.Value;
        }

        if (callForOvertime.HasValue)
        {
            CallForOvertime = callForOvertime.Value;
            changes["callForOvertime"] = callForOvertime.Value;
        }

        if (processPayroll.HasValue)
        {
            ProcessPayroll = processPayroll.Value;
            changes["processPayroll"] = processPayroll.Value;
        }

        if (tieUpOffProperty.HasValue)
        {
            TieUpOffProperty = tieUpOffProperty.Value;
            changes["tieUpOffProperty"] = tieUpOffProperty.Value;
        }

        if (changes.Count > 0)
        {
            Raise(new EmployeeUpdatedDomainEvent(CtrlNbr, payload: new { Changes = changes }));
        }

        return this;
    }

    public Address AddAddress(string address1, string city, string state, string zipCode, long addressTypeCtrlNbr, string? address2 = null)
    {
        var address = Address.Create(CtrlNbr, addressTypeCtrlNbr, address1, city, state, zipCode, address2);
        _addresses.Add(address);
        Raise(new EmployeeUpdatedDomainEvent(CtrlNbr, payload: new { Action = "AddAddress", AddressCtrlNbr = address.CtrlNbr.Value }));
        return address;
    }

    public PhoneNumber AddPhoneNumber(string number, int callingOrder, bool dialOne, long phoneTypeCtrlNbr)
    {
        var phone = PhoneNumber.Create(CtrlNbr, phoneTypeCtrlNbr, number, callingOrder, dialOne);
        _phoneNumbers.Add(phone);
        Raise(new EmployeeUpdatedDomainEvent(CtrlNbr, payload: new { Action = "AddPhone", PhoneCtrlNbr = phone.CtrlNbr.Value }));
        return phone;
    }

    public EmailAddress AddEmailAddress(string email, long emailTypeCtrlNbr)
    {
        var emailAddress = EmailAddress.Create(CtrlNbr, emailTypeCtrlNbr, email);
        _emailAddresses.Add(emailAddress);
        Raise(new EmployeeUpdatedDomainEvent(CtrlNbr, payload: new { Action = "AddEmail", EmailCtrlNbr = emailAddress.CtrlNbr.Value }));
        return emailAddress;
    }

    public void RemoveAddress(ControlNumber addressCtrlNbr)
    {
        var address = _addresses.FirstOrDefault(a => a.CtrlNbr == addressCtrlNbr);
        if (address is not null)
        {
            _addresses.Remove(address);
            Raise(new EmployeeUpdatedDomainEvent(CtrlNbr, payload: new { Action = "RemoveAddress", AddressCtrlNbr = addressCtrlNbr.Value }));
        }
    }

    public void RemovePhoneNumber(ControlNumber phoneCtrlNbr)
    {
        var phone = _phoneNumbers.FirstOrDefault(p => p.CtrlNbr == phoneCtrlNbr);
        if (phone is not null)
        {
            _phoneNumbers.Remove(phone);
            Raise(new EmployeeUpdatedDomainEvent(CtrlNbr, payload: new { Action = "RemovePhone", PhoneCtrlNbr = phoneCtrlNbr.Value }));
        }
    }

    public void RemoveEmailAddress(ControlNumber emailCtrlNbr)
    {
        var email = _emailAddresses.FirstOrDefault(e => e.CtrlNbr == emailCtrlNbr);
        if (email is not null)
        {
            _emailAddresses.Remove(email);
            Raise(new EmployeeUpdatedDomainEvent(CtrlNbr, payload: new { Action = "RemoveEmail", EmailCtrlNbr = emailCtrlNbr.Value }));
        }
    }

    public void Delete()
    {
        Raise(new EmployeeDeletedDomainEvent(CtrlNbr, payload: new { DeletedAt = DateTime.UtcNow }));
    }
}