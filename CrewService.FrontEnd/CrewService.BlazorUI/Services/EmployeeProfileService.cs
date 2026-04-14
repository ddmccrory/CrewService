using CrewService.BlazorUI.Clients;
using CrewService.BlazorUI.Models;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Services;

/// <summary>
/// Shared service that encapsulates employee profile and sub-collection editing
/// logic used by both the Employee pages and the Account profile pages.
/// Handles loading from API responses, building update requests with admin-only
/// field guards, and executing CRUD for addresses, emails, and phone numbers.
/// </summary>
public class EmployeeProfileService(EmployeeClient employeeClient)
{
    // ── Profile ──

    /// <summary>
    /// Populates an <see cref="EmployeeProfileModel"/> from a <see cref="GetEmployeeResponse"/>.
    /// </summary>
    public static EmployeeProfileModel CreateFromEmployee(GetEmployeeResponse employee) => new()
    {
        FirstName = employee.FirstName,
        MiddleName = employee.MiddleName,
        LastName = employee.LastName,
        Gender = employee.Gender,
        Race = employee.Race,
        MaritalStatus = employee.MaritalStatus,
        DriversLicenseNumber = employee.DriversLicenseNumber,
        IssuingState = employee.IssuingState,
        AllowFmlaMarkOff = employee.AllowFmlaMarkOff,
        CallForOvertime = employee.CallForOvertime,
        ProcessPayroll = employee.ProcessPayroll,
        TieUpOffProperty = employee.TieUpOffProperty
    };

    /// <summary>
    /// Builds an <see cref="UpdateEmployeeRequest"/> from the model, applying
    /// admin-only field guards. Non-admin users get original values for
    /// AllowFmlaMarkOff, ProcessPayroll, and TieUpOffProperty.
    /// </summary>
    public static UpdateEmployeeRequest BuildUpdateRequest(
        long ctrlNbr,
        EmployeeProfileModel model,
        GetEmployeeResponse? original,
        bool isAdmin) => new()
    {
        CtrlNbr = ctrlNbr,
        FirstName = model.FirstName ?? string.Empty,
        MiddleName = model.MiddleName ?? string.Empty,
        LastName = model.LastName ?? string.Empty,
        Gender = model.Gender ?? string.Empty,
        Race = model.Race ?? string.Empty,
        MaritalStatus = model.MaritalStatus ?? string.Empty,
        DriversLicenseNumber = model.DriversLicenseNumber ?? string.Empty,
        IssuingState = model.IssuingState ?? string.Empty,
        AllowFmlaMarkOff = isAdmin ? model.AllowFmlaMarkOff : (original?.AllowFmlaMarkOff ?? false),
        CallForOvertime = model.CallForOvertime,
        ProcessPayroll = isAdmin ? model.ProcessPayroll : (original?.ProcessPayroll ?? false),
        TieUpOffProperty = isAdmin ? model.TieUpOffProperty : (original?.TieUpOffProperty ?? false)
    };

    /// <summary>
    /// Saves employee profile changes via the Employee gRPC service.
    /// </summary>
    public async Task<UpdateEmployeeResponse> SaveAsync(
        long ctrlNbr,
        EmployeeProfileModel model,
        GetEmployeeResponse? original,
        bool isAdmin)
    {
        var request = BuildUpdateRequest(ctrlNbr, model, original, isAdmin);
        return await employeeClient.UpdateAsync(request);
    }

    /// <summary>
    /// Masks a Social Security Number for display, showing only the last 4 digits.
    /// </summary>
    public static string MaskSsn(string ssn)
        => ssn.Length >= 4 ? $"***-**-{ssn[^4..]}" : ssn;

    // ── Address CRUD ──

    public static AddressInputModel CreateAddressModel(AddressResponse addr) => new()
    {
        AddressTypeCtrlNbr = addr.AddressTypeCtrlNbr,
        Address1 = addr.Address1,
        Address2 = addr.Address2,
        City = addr.City,
        State = addr.State,
        ZipCode = addr.ZipCode
    };

    public async Task<string> SaveAddressAsync(long employeeCtrlNbr, long editCtrlNbr, AddressInputModel model)
    {
        if (editCtrlNbr > 0)
        {
            await employeeClient.UpdateAddressAsync(new UpdateAddressRequest
            {
                EmployeeCtrlNbr = employeeCtrlNbr,
                CtrlNbr = editCtrlNbr,
                Address1 = model.Address1 ?? string.Empty,
                Address2 = model.Address2 ?? string.Empty,
                City = model.City ?? string.Empty,
                State = model.State ?? string.Empty,
                ZipCode = model.ZipCode ?? string.Empty
            });
            return "Address updated.";
        }

        await employeeClient.AddAddressAsync(new AddAddressRequest
        {
            EmployeeCtrlNbr = employeeCtrlNbr,
            AddressTypeCtrlNbr = model.AddressTypeCtrlNbr,
            Address1 = model.Address1 ?? string.Empty,
            Address2 = model.Address2 ?? string.Empty,
            City = model.City ?? string.Empty,
            State = model.State ?? string.Empty,
            ZipCode = model.ZipCode ?? string.Empty
        });
        return "Address added.";
    }

    public async Task DeleteAddressAsync(long employeeCtrlNbr, long ctrlNbr)
        => await employeeClient.DeleteAddressAsync(employeeCtrlNbr, ctrlNbr);

    // ── Email Address CRUD ──

    public static EmailInputModel CreateEmailModel(EmailAddressResponse email) => new()
    {
        EmailTypeCtrlNbr = email.EmailTypeCtrlNbr,
        Email = email.Email
    };

    public async Task<string> SaveEmailAsync(long employeeCtrlNbr, long editCtrlNbr, EmailInputModel model)
    {
        if (editCtrlNbr > 0)
        {
            await employeeClient.UpdateEmailAddressAsync(new UpdateEmailAddressRequest
            {
                EmployeeCtrlNbr = employeeCtrlNbr,
                CtrlNbr = editCtrlNbr,
                Email = model.Email ?? string.Empty
            });
            return "Email address updated.";
        }

        await employeeClient.AddEmailAddressAsync(new AddEmailAddressRequest
        {
            EmployeeCtrlNbr = employeeCtrlNbr,
            EmailTypeCtrlNbr = model.EmailTypeCtrlNbr,
            Email = model.Email ?? string.Empty
        });
        return "Email address added.";
    }

    public async Task DeleteEmailAsync(long employeeCtrlNbr, long ctrlNbr)
        => await employeeClient.DeleteEmailAddressAsync(employeeCtrlNbr, ctrlNbr);

    // ── Phone Number CRUD ──

    public static PhoneInputModel CreatePhoneModel(PhoneNumberResponse phone) => new()
    {
        PhoneTypeCtrlNbr = phone.PhoneTypeCtrlNbr,
        Number = phone.Number,
        CallingOrder = phone.CallingOrder,
        DialOne = phone.DialOne
    };

    public async Task<string> SavePhoneAsync(long employeeCtrlNbr, long editCtrlNbr, PhoneInputModel model)
    {
        if (editCtrlNbr > 0)
        {
            await employeeClient.UpdatePhoneNumberAsync(new UpdatePhoneNumberRequest
            {
                EmployeeCtrlNbr = employeeCtrlNbr,
                CtrlNbr = editCtrlNbr,
                Number = model.Number ?? string.Empty,
                CallingOrder = model.CallingOrder,
                DialOne = model.DialOne
            });
            return "Phone number updated.";
        }

        await employeeClient.AddPhoneNumberAsync(new AddPhoneNumberRequest
        {
            EmployeeCtrlNbr = employeeCtrlNbr,
            PhoneTypeCtrlNbr = model.PhoneTypeCtrlNbr,
            Number = model.Number ?? string.Empty,
            CallingOrder = model.CallingOrder,
            DialOne = model.DialOne
        });
        return "Phone number added.";
    }

    public async Task DeletePhoneAsync(long employeeCtrlNbr, long ctrlNbr)
        => await employeeClient.DeletePhoneNumberAsync(employeeCtrlNbr, ctrlNbr);
}
