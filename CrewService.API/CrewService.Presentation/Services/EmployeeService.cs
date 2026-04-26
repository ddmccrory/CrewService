using CrewService.Domain.Exceptions;
using CrewService.Presentation.Services;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;
using CrewService.Application.Employees;
using CrewService.Application.Modules.UserAccount;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class EmployeeService(
    EmployeeAppService employeeAppService,
    IUserAccountService userAccountService) : EmployeeSrvc.EmployeeSrvcBase
{
    private readonly EmployeeAppService _employeeAppService = employeeAppService;
    private readonly IUserAccountService _userAccountService = userAccountService;
    #region Employee Operations

    public override async Task<GetAllEmployeesResponse> GetAllEmployeesAsync(GetAllEmployeesRequest request, ServerCallContext context)
    {
        var response = new GetAllEmployeesResponse();

        ControlNumber? clientCtrlNbr = request.ClientCtrlNbr > 0
            ? ControlNumber.Create(request.ClientCtrlNbr) : null;

        var employees = await _employeeAppService.GetAllAsync(
            clientCtrlNbr, request.PageNumber, request.PageSize, context.CancellationToken);

        var userIds = employees.Select(e => e.UserId).Where(id => !string.IsNullOrEmpty(id)).Distinct().Cast<string>().ToList();
        var userList = await _userAccountService.GetNamesByIdsAsync(userIds);
        var userDict = userList.ToDictionary(u => u.Id, StringComparer.Ordinal);

        foreach (var employee in employees)
        {
            var mapped = MapToEmployeeResponse(employee);
            if (!string.IsNullOrEmpty(employee.UserId) && userDict.TryGetValue(employee.UserId, out var user))
            {
                mapped.FullNameLnf = EmployeeNameService.FormatFullNameLnf(user.FirstName ?? string.Empty, user.MiddleName ?? string.Empty, user.LastName ?? string.Empty);
                mapped.FirstName = user.FirstName ?? string.Empty;
                mapped.MiddleName = user.MiddleName ?? string.Empty;
                mapped.LastName = user.LastName ?? string.Empty;
            }
            response.Employees.Add(mapped);
        }

        response.TotalCount = employees.Count;
        return response;
    }

    public override async Task<GetEmployeeResponse> GetEmployeeAsync(GetEmployeeRequest request, ServerCallContext context)
    {
        Employee employee;
        try { employee = await _employeeAppService.GetAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken); }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }

        var resp = MapToEmployeeResponse(employee);
        await EnrichWithUserNameAsync(resp, employee.UserId);
        return resp;
    }

    public override async Task<GetEmployeeResponse> GetEmployeeByNumberAsync(GetEmployeeByNumberRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.EmployeeNumber))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid employee number."));

        var employee = await _employeeAppService.GetByNumberAsync(request.EmployeeNumber, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with number {request.EmployeeNumber} was not found."));

        var resp = MapToEmployeeResponse(employee);
        await EnrichWithUserNameAsync(resp, employee.UserId);
        return resp;
    }

    public override async Task<CreateEmployeeResponse> CreateEmployeeAsync(CreateEmployeeRequest request, ServerCallContext context)
    {
        ValidateCreateRequest(request);

        MaritalStatus? maritalStatus = string.IsNullOrEmpty(request.MaritalStatus)
            ? null : System.Enum.Parse<MaritalStatus>(request.MaritalStatus, ignoreCase: true);

        var employee = await _employeeAppService.CreateAsync(
            ControlNumber.Create(request.ClientCtrlNbr),
            request.UserId,
            request.EmployeeNumber,
            request.SocialSecurityNumber,
            System.Enum.Parse<Gender>(request.Gender, ignoreCase: true),
            System.Enum.Parse<Race>(request.Race, ignoreCase: true),
            request.BirthDate.ToDateTime(),
            request.EmploymentDate.ToDateTime(),
            ControlNumber.Create(request.EmploymentStatusCtrlNbr),
            string.IsNullOrEmpty(request.DriversLicenseNumber) ? null : request.DriversLicenseNumber,
            string.IsNullOrEmpty(request.IssuingState) ? null : request.IssuingState,
            maritalStatus,
            context.CancellationToken);

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

        Employee employee;
        try
        {
            employee = await _employeeAppService.UpdateAsync(
                ControlNumber.Create(request.CtrlNbr),
                string.IsNullOrEmpty(request.DriversLicenseNumber) ? null : request.DriversLicenseNumber,
                string.IsNullOrEmpty(request.IssuingState) ? null : request.IssuingState,
                string.IsNullOrEmpty(request.MaritalStatus) ? null : System.Enum.Parse<MaritalStatus>(request.MaritalStatus, ignoreCase: true),
                request.AllowFmlaMarkOff,
                request.CallForOvertime,
                request.ProcessPayroll,
                request.TieUpOffProperty,
                string.IsNullOrEmpty(request.Gender) ? null : System.Enum.Parse<Gender>(request.Gender, ignoreCase: true),
                string.IsNullOrEmpty(request.Race) ? null : System.Enum.Parse<Race>(request.Race, ignoreCase: true),
                context.CancellationToken);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }

        if (!string.IsNullOrEmpty(employee.UserId))
        {
            var fullName = EmployeeNameService.FormatFullName(request.FirstName, request.MiddleName, request.LastName);
            var fullNameLNF = EmployeeNameService.FormatFullNameLnf(request.FirstName, request.MiddleName, request.LastName);
            await _userAccountService.UpdateProfileAsync(
                employee.UserId, request.FirstName, request.MiddleName, request.LastName, fullName, fullNameLNF);
        }

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

        Employee employee;
        try { employee = await _employeeAppService.DeleteAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken); }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }

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
        var (_, address) = await _employeeAppService.AddAddressAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr),
            request.Address1, request.City, request.State, request.ZipCode,
            request.AddressTypeCtrlNbr,
            string.IsNullOrEmpty(request.Address2) ? null : request.Address2,
            context.CancellationToken);

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
        var (_, address) = await _employeeAppService.UpdateAddressAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr),
            ControlNumber.Create(request.CtrlNbr),
            string.IsNullOrEmpty(request.Address1) ? null : request.Address1,
            string.IsNullOrEmpty(request.Address2) ? null : request.Address2,
            string.IsNullOrEmpty(request.City) ? null : request.City,
            string.IsNullOrEmpty(request.State) ? null : request.State,
            string.IsNullOrEmpty(request.ZipCode) ? null : request.ZipCode,
            context.CancellationToken);

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
        await _employeeAppService.DeleteAddressAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr),
            ControlNumber.Create(request.CtrlNbr),
            context.CancellationToken);

        return new DeleteResponse { Success = true, Messages = { "Address deleted successfully." } };
    }

    #endregion

    #region Phone Number Operations

    public override async Task<PhoneNumberResponse> AddPhoneNumberAsync(AddPhoneNumberRequest request, ServerCallContext context)
    {
        var (_, phone) = await _employeeAppService.AddPhoneNumberAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr),
            request.Number, request.CallingOrder, request.DialOne,
            request.PhoneTypeCtrlNbr, context.CancellationToken);

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
        var (_, phone) = await _employeeAppService.UpdatePhoneNumberAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr),
            ControlNumber.Create(request.CtrlNbr),
            string.IsNullOrEmpty(request.Number) ? null : request.Number,
            request.CallingOrder > 0 ? request.CallingOrder : null,
            request.DialOne,
            context.CancellationToken);

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
        await _employeeAppService.DeletePhoneNumberAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr),
            ControlNumber.Create(request.CtrlNbr),
            context.CancellationToken);

        return new DeleteResponse { Success = true, Messages = { "Phone number deleted successfully." } };
    }

    #endregion

    #region Email Address Operations

    public override async Task<EmailAddressResponse> AddEmailAddressAsync(AddEmailAddressRequest request, ServerCallContext context)
    {
        var (_, email) = await _employeeAppService.AddEmailAddressAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr),
            request.Email, request.EmailTypeCtrlNbr, context.CancellationToken);

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
        var (_, email) = await _employeeAppService.UpdateEmailAddressAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr),
            ControlNumber.Create(request.CtrlNbr),
            string.IsNullOrEmpty(request.Email) ? null : request.Email,
            context.CancellationToken);

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
        await _employeeAppService.DeleteEmailAddressAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr),
            ControlNumber.Create(request.CtrlNbr),
            context.CancellationToken);

        return new DeleteResponse { Success = true, Messages = { "Email address deleted successfully." } };
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
            var user = await _userAccountService.FindByIdAsync(userId);
            if (user is not null)
            {
                response.FullNameLnf = EmployeeNameService.FormatFullNameLnf(
                    user.FirstName ?? string.Empty,
                    user.MiddleName ?? string.Empty,
                    user.LastName ?? string.Empty);
                response.FirstName = user.FirstName ?? string.Empty;
                response.MiddleName = user.MiddleName ?? string.Empty;
                response.LastName = user.LastName ?? string.Empty;
            }
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