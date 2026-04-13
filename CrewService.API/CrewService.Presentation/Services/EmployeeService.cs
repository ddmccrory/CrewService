using CrewService.Domain.Exceptions;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
using Google.Protobuf.WellKnownTypes;
using CrewService.Infrastructure.Models.UserAccount;
using Grpc.Core;
using Microsoft.AspNetCore.Identity;

namespace CrewService.Presentation.Services;

public class EmployeeService(IEmployeeRepository employeeRepository, IOrchestrationUnitOfWorkFactory uowFactory, UserManager<User> userManager) : EmployeeSrvc.EmployeeSrvcBase
{
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly IOrchestrationUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly UserManager<User> _userManager = userManager;

    #region Employee Operations

    public override async Task<GetAllEmployeesResponse> GetAllEmployeesAsync(GetAllEmployeesRequest request, ServerCallContext context)
    {
        var response = new GetAllEmployeesResponse();

        var employees = request.ClientCtrlNbr > 0
            ? await _employeeRepository.GetByClientCtrlNbrAsync(ControlNumber.Create(request.ClientCtrlNbr))
            : request.PageSize > 0
                ? await _employeeRepository.GetAllAsync(request.PageNumber, request.PageSize)
                : await _employeeRepository.GetAllAsync();

        foreach (var employee in employees)
        {
            var mapped = MapToEmployeeResponse(employee);
            await EnrichWithUserNameAsync(mapped, employee.UserId);
            response.Employees.Add(mapped);
        }

        response.TotalCount = employees.Count;

        return response;
    }

    public override async Task<GetEmployeeResponse> GetEmployeeAsync(GetEmployeeRequest request, ServerCallContext context)
    {
        var employee = await _employeeRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with control number {request.CtrlNbr} was not found."));

        var resp = MapToEmployeeResponse(employee);
        await EnrichWithUserNameAsync(resp, employee.UserId);
        return resp;
    }

    public override async Task<GetEmployeeResponse> GetEmployeeByNumberAsync(GetEmployeeByNumberRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.EmployeeNumber))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid employee number."));

        var employee = await _employeeRepository.GetByEmployeeNumberAsync(request.EmployeeNumber)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with number {request.EmployeeNumber} was not found."));

        var resp = MapToEmployeeResponse(employee);
        await EnrichWithUserNameAsync(resp, employee.UserId);
        return resp;
    }

    public override async Task<CreateEmployeeResponse> CreateEmployeeAsync(CreateEmployeeRequest request, ServerCallContext context)
    {
        ValidateCreateRequest(request);

        var employee = Employee.Create(
            request.ClientCtrlNbr,
            request.UserId,
            request.EmployeeNumber,
            request.SocialSecurityNumber,
            System.Enum.Parse<Gender>(request.Gender, ignoreCase: true),
            System.Enum.Parse<Race>(request.Race, ignoreCase: true),
            request.BirthDate.ToDateTime(),
            request.EmploymentDate.ToDateTime(),
            request.EmploymentStatusCtrlNbr);

        if (!string.IsNullOrEmpty(request.DriversLicenseNumber))
        {
            employee.Update(
                driversLicenseNumber: request.DriversLicenseNumber,
                issuingState: request.IssuingState,
                maritalStatus: string.IsNullOrEmpty(request.MaritalStatus) ? null : System.Enum.Parse<MaritalStatus>(request.MaritalStatus, ignoreCase: true));
        }

        await using var uow = await _uowFactory.CreateAsync();
        uow.Employees.Add(employee);
        await uow.CommitAsync();

        return new CreateEmployeeResponse
        {
            CtrlNbr = employee.CtrlNbr.Value,
            EmployeeNumber = employee.EmployeeNumber,
            Success = true,
            Messages = { "Employee created successfully." }
        };
    }

    public override async Task<UpdateEmployeeResponse> UpdateEmployeeAsync(UpdateEmployeeRequest request, ServerCallContext context)
    {
        if (request.CtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid employee control number."));

        await using var uow = await _uowFactory.CreateAsync();

        var employee = await uow.Employees.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with control number {request.CtrlNbr} was not found."));

        employee.Update(
            driversLicenseNumber: string.IsNullOrEmpty(request.DriversLicenseNumber) ? null : request.DriversLicenseNumber,
            issuingState: string.IsNullOrEmpty(request.IssuingState) ? null : request.IssuingState,
            maritalStatus: string.IsNullOrEmpty(request.MaritalStatus) ? null : System.Enum.Parse<MaritalStatus>(request.MaritalStatus, ignoreCase: true),
            allowFMLAMarkOff: request.AllowFmlaMarkOff,
            callForOvertime: request.CallForOvertime,
            processPayroll: request.ProcessPayroll,
            tieUpOffProperty: request.TieUpOffProperty,
            gender: string.IsNullOrEmpty(request.Gender) ? null : System.Enum.Parse<Gender>(request.Gender, ignoreCase: true),
            race: string.IsNullOrEmpty(request.Race) ? null : System.Enum.Parse<Race>(request.Race, ignoreCase: true));

        uow.Employees.Update(employee);
        await uow.CommitAsync();

        return new UpdateEmployeeResponse
        {
            CtrlNbr = employee.CtrlNbr.Value,
            Success = true,
            Messages = { "Employee updated successfully." }
        };
    }

    public override async Task<DeleteEmployeeResponse> DeleteEmployeeAsync(DeleteEmployeeRequest request, ServerCallContext context)
    {
        if (request.CtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid employee control number."));

        await using var uow = await _uowFactory.CreateAsync();

        var employee = await uow.Employees.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with control number {request.CtrlNbr} was not found."));

        uow.Employees.Remove(employee);
        await uow.CommitAsync();

        return new DeleteEmployeeResponse
        {
            Success = true,
            Messages = { $"Employee {employee.EmployeeNumber} deleted successfully." }
        };
    }

    #endregion

    #region Address Operations

    public override async Task<AddressResponse> AddAddressAsync(AddAddressRequest request, ServerCallContext context)
    {
        await using var uow = await _uowFactory.CreateAsync();

        var employee = await uow.Employees.GetByCtrlNbrAsync(ControlNumber.Create(request.EmployeeCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with control number {request.EmployeeCtrlNbr} was not found."));

        var address = employee.AddAddress(
            request.Address1,
            request.City,
            request.State,
            request.ZipCode,
            request.AddressTypeCtrlNbr,
            string.IsNullOrEmpty(request.Address2) ? null : request.Address2);

        uow.Employees.Update(employee);
        await uow.CommitAsync();

        return new AddressResponse
        {
            CtrlNbr = address.CtrlNbr.Value,
            AddressTypeCtrlNbr = address.AddressTypeCtrlNbr.Value,
            Address1 = address.Address1,
            Address2 = address.Address2 ?? string.Empty,
            City = address.City,
            State = address.State,
            ZipCode = address.ZipCode,
            Success = true,
            Messages = { "Address added successfully." }
        };
    }

    public override async Task<AddressResponse> UpdateAddressAsync(UpdateAddressRequest request, ServerCallContext context)
    {
        await using var uow = await _uowFactory.CreateAsync();

        var employee = await uow.Employees.GetByCtrlNbrAsync(ControlNumber.Create(request.EmployeeCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with control number {request.EmployeeCtrlNbr} was not found."));

        var address = employee.Addresses.FirstOrDefault(a => a.CtrlNbr == ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Address with control number {request.CtrlNbr} was not found."));

        address.Update(
            address1: string.IsNullOrEmpty(request.Address1) ? null : request.Address1,
            address2: string.IsNullOrEmpty(request.Address2) ? null : request.Address2,
            city: string.IsNullOrEmpty(request.City) ? null : request.City,
            state: string.IsNullOrEmpty(request.State) ? null : request.State,
            zipCode: string.IsNullOrEmpty(request.ZipCode) ? null : request.ZipCode);

        uow.Employees.Update(employee);
        await uow.CommitAsync();

        return new AddressResponse
        {
            CtrlNbr = address.CtrlNbr.Value,
            AddressTypeCtrlNbr = address.AddressTypeCtrlNbr.Value,
            Address1 = address.Address1,
            Address2 = address.Address2 ?? string.Empty,
            City = address.City,
            State = address.State,
            ZipCode = address.ZipCode,
            Success = true,
            Messages = { "Address updated successfully." }
        };
    }

    public override async Task<DeleteResponse> DeleteAddressAsync(DeleteAddressRequest request, ServerCallContext context)
    {
        await using var uow = await _uowFactory.CreateAsync();

        var employee = await uow.Employees.GetByCtrlNbrAsync(ControlNumber.Create(request.EmployeeCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with control number {request.EmployeeCtrlNbr} was not found."));

        employee.RemoveAddress(ControlNumber.Create(request.CtrlNbr));

        uow.Employees.Update(employee);
        await uow.CommitAsync();

        return new DeleteResponse
        {
            Success = true,
            Messages = { "Address deleted successfully." }
        };
    }

    #endregion

    #region Phone Number Operations

    public override async Task<PhoneNumberResponse> AddPhoneNumberAsync(AddPhoneNumberRequest request, ServerCallContext context)
    {
        await using var uow = await _uowFactory.CreateAsync();

        var employee = await uow.Employees.GetByCtrlNbrAsync(ControlNumber.Create(request.EmployeeCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with control number {request.EmployeeCtrlNbr} was not found."));

        var phone = employee.AddPhoneNumber(
            request.Number,
            request.CallingOrder,
            request.DialOne,
            request.PhoneTypeCtrlNbr);

        uow.Employees.Update(employee);
        await uow.CommitAsync();

        return new PhoneNumberResponse
        {
            CtrlNbr = phone.CtrlNbr.Value,
            PhoneTypeCtrlNbr = phone.PhoneTypeCtrlNbr.Value,
            Number = phone.Number,
            CallingOrder = phone.CallingOrder,
            DialOne = phone.DialOne,
            Success = true,
            Messages = { "Phone number added successfully." }
        };
    }

    public override async Task<PhoneNumberResponse> UpdatePhoneNumberAsync(UpdatePhoneNumberRequest request, ServerCallContext context)
    {
        await using var uow = await _uowFactory.CreateAsync();

        var employee = await uow.Employees.GetByCtrlNbrAsync(ControlNumber.Create(request.EmployeeCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with control number {request.EmployeeCtrlNbr} was not found."));

        var phone = employee.PhoneNumbers.FirstOrDefault(p => p.CtrlNbr == ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Phone number with control number {request.CtrlNbr} was not found."));

        phone.Update(
            number: string.IsNullOrEmpty(request.Number) ? null : request.Number,
            callingOrder: request.CallingOrder > 0 ? request.CallingOrder : null,
            dialOne: request.DialOne);

        uow.Employees.Update(employee);
        await uow.CommitAsync();

        return new PhoneNumberResponse
        {
            CtrlNbr = phone.CtrlNbr.Value,
            PhoneTypeCtrlNbr = phone.PhoneTypeCtrlNbr.Value,
            Number = phone.Number,
            CallingOrder = phone.CallingOrder,
            DialOne = phone.DialOne,
            Success = true,
            Messages = { "Phone number updated successfully." }
        };
    }

    public override async Task<DeleteResponse> DeletePhoneNumberAsync(DeletePhoneNumberRequest request, ServerCallContext context)
    {
        await using var uow = await _uowFactory.CreateAsync();

        var employee = await uow.Employees.GetByCtrlNbrAsync(ControlNumber.Create(request.EmployeeCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with control number {request.EmployeeCtrlNbr} was not found."));

        employee.RemovePhoneNumber(ControlNumber.Create(request.CtrlNbr));

        uow.Employees.Update(employee);
        await uow.CommitAsync();

        return new DeleteResponse
        {
            Success = true,
            Messages = { "Phone number deleted successfully." }
        };
    }

    #endregion

    #region Email Address Operations

    public override async Task<EmailAddressResponse> AddEmailAddressAsync(AddEmailAddressRequest request, ServerCallContext context)
    {
        await using var uow = await _uowFactory.CreateAsync();

        var employee = await uow.Employees.GetByCtrlNbrAsync(ControlNumber.Create(request.EmployeeCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with control number {request.EmployeeCtrlNbr} was not found."));

        var email = employee.AddEmailAddress(request.Email, request.EmailTypeCtrlNbr);

        uow.Employees.Update(employee);
        await uow.CommitAsync();

        return new EmailAddressResponse
        {
            CtrlNbr = email.CtrlNbr.Value,
            EmailTypeCtrlNbr = email.EmailTypeCtrlNbr.Value,
            Email = email.Email,
            Success = true,
            Messages = { "Email address added successfully." }
        };
    }

    public override async Task<EmailAddressResponse> UpdateEmailAddressAsync(UpdateEmailAddressRequest request, ServerCallContext context)
    {
        await using var uow = await _uowFactory.CreateAsync();

        var employee = await uow.Employees.GetByCtrlNbrAsync(ControlNumber.Create(request.EmployeeCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with control number {request.EmployeeCtrlNbr} was not found."));

        var email = employee.EmailAddresses.FirstOrDefault(e => e.CtrlNbr == ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Email address with control number {request.CtrlNbr} was not found."));

        email.Update(string.IsNullOrEmpty(request.Email) ? null : request.Email);

        uow.Employees.Update(employee);
        await uow.CommitAsync();

        return new EmailAddressResponse
        {
            CtrlNbr = email.CtrlNbr.Value,
            EmailTypeCtrlNbr = email.EmailTypeCtrlNbr.Value,
            Email = email.Email,
            Success = true,
            Messages = { "Email address updated successfully." }
        };
    }

    public override async Task<DeleteResponse> DeleteEmailAddressAsync(DeleteEmailAddressRequest request, ServerCallContext context)
    {
        await using var uow = await _uowFactory.CreateAsync();

        var employee = await uow.Employees.GetByCtrlNbrAsync(ControlNumber.Create(request.EmployeeCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with control number {request.EmployeeCtrlNbr} was not found."));

        employee.RemoveEmailAddress(ControlNumber.Create(request.CtrlNbr));

        uow.Employees.Update(employee);
        await uow.CommitAsync();

        return new DeleteResponse
        {
            Success = true,
            Messages = { "Email address deleted successfully." }
        };
    }

    #endregion

    #region Private Helper Methods

    private static GetEmployeeResponse MapToEmployeeResponse(Employee employee)
    {
        var response = new GetEmployeeResponse
        {
            CtrlNbr = employee.CtrlNbr.Value,
            ClientCtrlNbr = employee.ClientCtrlNbr.Value,
            UserId = employee.UserId,
            EmployeeNumber = employee.EmployeeNumber,
            SocialSecurityNumber = employee.SocialSecurityNumber,
            Gender = employee.Gender.ToString(),
            Race = employee.Race.ToString(),
            BirthDate = Timestamp.FromDateTime(DateTime.SpecifyKind(employee.BirthDate, DateTimeKind.Utc)),
            EmploymentDate = Timestamp.FromDateTime(DateTime.SpecifyKind(employee.EmploymentDate, DateTimeKind.Utc)),
            EmploymentStatusCtrlNbr = employee.EmploymentStatusCtrlNbr.Value,
            DriversLicenseNumber = employee.DriversLicenseNumber ?? string.Empty,
            IssuingState = employee.IssuingState ?? string.Empty,
            MaritalStatus = employee.MaritalStatus.ToString(),
            AllowFmlaMarkOff = employee.AllowFMLAMarkOff,
            CallForOvertime = employee.CallForOvertime,
            ProcessPayroll = employee.ProcessPayroll,
            TieUpOffProperty = employee.TieUpOffProperty
        };

        // Map addresses
        foreach (var address in employee.Addresses)
        {
            response.Addresses.Add(new AddressResponse
            {
                CtrlNbr = address.CtrlNbr.Value,
                AddressTypeCtrlNbr = address.AddressTypeCtrlNbr.Value,
                Address1 = address.Address1,
                Address2 = address.Address2 ?? string.Empty,
                City = address.City,
                State = address.State,
                ZipCode = address.ZipCode
            });
        }

        // Map phone numbers
        foreach (var phone in employee.PhoneNumbers)
        {
            response.PhoneNumbers.Add(new PhoneNumberResponse
            {
                CtrlNbr = phone.CtrlNbr.Value,
                PhoneTypeCtrlNbr = phone.PhoneTypeCtrlNbr.Value,
                Number = phone.Number,
                CallingOrder = phone.CallingOrder,
                DialOne = phone.DialOne
            });
        }

        // Map email addresses
        foreach (var email in employee.EmailAddresses)
        {
            response.EmailAddresses.Add(new EmailAddressResponse
            {
                CtrlNbr = email.CtrlNbr.Value,
                EmailTypeCtrlNbr = email.EmailTypeCtrlNbr.Value,
                Email = email.Email
            });
        }

        return response;
    }

    private async Task EnrichWithUserNameAsync(GetEmployeeResponse response, string userId)
    {
        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _userManager.FindByIdAsync(userId);
            response.FullNameLnf = user?.FullNameLNF ?? string.Empty;
        }
    }

    private static void ValidateCreateRequest(CreateEmployeeRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.ClientCtrlNbr <= 0)
            errors.Add("ClientCtrlNbr", ["Must be greater than 0"]);

        if (string.IsNullOrEmpty(request.UserId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid user ID."));

        if (string.IsNullOrEmpty(request.EmployeeNumber))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid employee number."));

        if (string.IsNullOrEmpty(request.SocialSecurityNumber))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid social security number."));

        if (request.EmploymentStatusCtrlNbr <= 0)
            errors.Add("EmploymentStatusCtrlNbr", ["Must be greater than 0"]);

        if (errors.Count > 0)
            throw new ValidationException(errors);
    }

    #endregion
}