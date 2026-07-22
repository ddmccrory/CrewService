using CrewService.Domain.Exceptions;
using CrewService.Presentation.Services;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;
using CrewService.Application.Employees;
using CrewService.Application.Authorization;
using CrewService.Application.Time;
using CrewService.Application.Modules.UserAccount;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Authorization;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace CrewService.Presentation.Services;

public class EmployeeService(
    EmployeeAppService employeeAppService,
    IUserAccountService userAccountService,
    IOrchestrationUnitOfWorkFactory uowFactory,
    IRequestActorContextResolver actorContextResolver,
    IRequestActorContextPolicy actorContextPolicy,
    IWorkAreaClock workAreaClock,
    ILogger<EmployeeService> logger) : EmployeeSrvc.EmployeeSrvcBase
{
    private const string EmployeeFeatureKey = "employees";

    private readonly EmployeeAppService _employeeAppService = employeeAppService;
    private readonly IUserAccountService _userAccountService = userAccountService;
    private readonly IOrchestrationUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly IRequestActorContextResolver _actorContextResolver = actorContextResolver;
    private readonly IRequestActorContextPolicy _actorContextPolicy = actorContextPolicy;
    private readonly IWorkAreaClock _workAreaClock = workAreaClock;
    private readonly ILogger<EmployeeService> _logger = logger;
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

    public override async Task<GetAllEmployeesResponse> GetEligibleAbsenceEmployeesAsync(GetEligibleAbsenceEmployeesRequest request, ServerCallContext context)
    {
        if (request.ParentCtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid parent control number."));

        if (request.RailroadCtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid railroad control number."));

        var parentCtrlNbr = ControlNumber.Create(request.ParentCtrlNbr);
        var railroadCtrlNbr = ControlNumber.Create(request.RailroadCtrlNbr);
        ControlNumber? craftCtrlNbr = request.CraftCtrlNbr > 0 ? ControlNumber.Create(request.CraftCtrlNbr) : null;
        ControlNumber? departmentCtrlNbr = request.DepartmentCtrlNbr > 0 ? ControlNumber.Create(request.DepartmentCtrlNbr) : null;

        var employees = await _employeeAppService.GetEligibleAbsenceEmployeesAsync(
            parentCtrlNbr,
            railroadCtrlNbr,
            craftCtrlNbr,
            departmentCtrlNbr,
            context.CancellationToken);

        var response = new GetAllEmployeesResponse();
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

        response.TotalCount = response.Employees.Count;
        return response;
    }

    public override async Task<GetEmployeeResponse> GetEmployeeAsync(GetEmployeeRequest request, ServerCallContext context)
    {
        Employee employee;
        try { employee = await _employeeAppService.GetAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken); }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }

        var resp = MapToEmployeeResponse(employee);
        await EnrichWithUserNameAsync(resp, employee.UserId);
        var canEditEmployeeDetail = await CanEditEmployeeDetailAsync(employee.CtrlNbr.Value, context);
        resp.CanEditEmployeeDetail = canEditEmployeeDetail;
        resp.Actions = new EmployeeActions
        {
            CanEditDetail = canEditEmployeeDetail
        };
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
        var canEditEmployeeDetail = await CanEditEmployeeDetailAsync(employee.CtrlNbr.Value, context);
        resp.CanEditEmployeeDetail = canEditEmployeeDetail;
        resp.Actions = new EmployeeActions
        {
            CanEditDetail = canEditEmployeeDetail
        };
        return resp;
    }

    public override async Task<CreateEmployeeResponse> CreateEmployeeAsync(CreateEmployeeRequest request, ServerCallContext context)
    {
        ValidateCreateRequest(request);

        try
        {
            MaritalStatus? maritalStatus = string.IsNullOrEmpty(request.MaritalStatus)
                ? null : System.Enum.Parse<MaritalStatus>(request.MaritalStatus, ignoreCase: true);

            var employee = await _employeeAppService.CreateAsync(
                ControlNumber.Create(request.ClientCtrlNbr),
                request.Email,
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
                string.IsNullOrEmpty(request.FirstName) ? null : request.FirstName,
                string.IsNullOrEmpty(request.MiddleName) ? null : request.MiddleName,
                string.IsNullOrEmpty(request.LastName) ? null : request.LastName,
                ct: context.CancellationToken);

            return new CreateEmployeeResponse
            {
                CtrlNbr = employee.CtrlNbr.Value,
                EmployeeNumber = employee.EmployeeNumber,
                Success = true,
                Messages = { "Employee created successfully." }
            };
        }
        catch (RpcException) { throw; }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message)); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in CreateEmployeeAsync");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    public override async Task<UpdateEmployeeResponse> UpdateEmployeeAsync(UpdateEmployeeRequest request, ServerCallContext context)
    {
        if (request.CtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid employee control number."));

        await EnsureCanEditEmployeeDetailAsync(request.CtrlNbr, context);

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
                string.IsNullOrEmpty(request.Race) ? null : ParseRace(request.Race),
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
        await EnsureCanEditEmployeeDetailAsync(request.EmployeeCtrlNbr, context);

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
        await EnsureCanEditEmployeeDetailAsync(request.EmployeeCtrlNbr, context);

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
        await EnsureCanEditEmployeeDetailAsync(request.EmployeeCtrlNbr, context);

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
        await EnsureCanEditEmployeeDetailAsync(request.EmployeeCtrlNbr, context);

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
        await EnsureCanEditEmployeeDetailAsync(request.EmployeeCtrlNbr, context);

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
        await EnsureCanEditEmployeeDetailAsync(request.EmployeeCtrlNbr, context);

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
        await EnsureCanEditEmployeeDetailAsync(request.EmployeeCtrlNbr, context);

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
        await EnsureCanEditEmployeeDetailAsync(request.EmployeeCtrlNbr, context);

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
        await EnsureCanEditEmployeeDetailAsync(request.EmployeeCtrlNbr, context);

        await _employeeAppService.DeleteEmailAddressAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr),
            ControlNumber.Create(request.CtrlNbr),
            context.CancellationToken);

        return new DeleteResponse { Success = true, Messages = { "Email address deleted successfully." } };
    }

    #endregion

    #region Private Helper Methods

    private static Race ParseRace(string value)
    {
        var normalized = value.Replace(" ", string.Empty, StringComparison.Ordinal);
        return System.Enum.Parse<Race>(normalized, ignoreCase: true);
    }

    private async Task EnsureCanEditEmployeeDetailAsync(long requestedEmployeeCtrlNbr, ServerCallContext context)
    {
        if (requestedEmployeeCtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "EmployeeCtrlNbr must be greater than zero."));

        if (!await CanEditEmployeeDetailAsync(requestedEmployeeCtrlNbr, context))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "You do not have permission to edit this employee detail."));
    }

    private async Task<bool> CanEditEmployeeDetailAsync(long requestedEmployeeCtrlNbr, ServerCallContext context)
    {
        var actorContext = await _actorContextResolver.ResolveAsync(
            requestedEmployeeCtrlNbr,
            ct: context.CancellationToken);

        var hasFullRoleAccess = await HasFullEmployeeDetailRoleAccessAsync(context, actorContext.ParentCtrlNbr, context.CancellationToken);
        var allowOnBehalf = actorContext.IsLinkedEmployee && hasFullRoleAccess;
        return _actorContextPolicy.CanAccessRequestedEmployee(actorContext, allowOnBehalf);
    }

    private async Task<bool> HasFullEmployeeDetailRoleAccessAsync(ServerCallContext context, long? parentCtrlNbr, CancellationToken ct)
    {
        var user = context.GetHttpContext().User;
        if (user.Identity?.IsAuthenticated != true)
            return false;

        await using var uow = await _uowFactory.CreateAsync(cancellationToken: ct);
        var feature = await uow.Features.GetByKeyAsync(EmployeeFeatureKey, ct);
        if (feature is null)
            return false;

        var parent = parentCtrlNbr.HasValue ? ControlNumber.Create(parentCtrlNbr.Value) : null;
        var roleNames = user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var roleName in roleNames)
        {
            var role = await uow.Roles.GetByNameAsync(roleName, ct);
            if (role is null)
                continue;

            var permissions = await uow.Permissions.GetEffectivePermissionsAsync(role.CtrlNbr, parent, craftCtrlNbr: null, ct);
            var hasFullAccess = permissions.Any(p => p.FeatureCtrlNbr == feature.CtrlNbr && p.AccessLevel == AccessLevel.FullAccess);
            if (hasFullAccess)
                return true;
        }

        return false;
    }

    private static string FormatRace(Race race) => race switch
    {
        Race.BlackOrAfricanAmerican => "Black or African American",
        Race.AmericanIndianOrAlaskaNative => "American Indian or Alaska Native",
        Race.NativeHawaiianOrPacificIslander => "Native Hawaiian or Pacific Islander",
        Race.TwoOrMoreRaces => "Two or More Races",
        Race.PreferNotToSay => "Prefer Not to Say",
        _ => race.ToString()
    };

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
            Race = FormatRace(employee.Race),
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
                response.ThemeName = user.ThemeName ?? string.Empty;
                response.ThemeMode = user.ThemeMode ?? string.Empty;
            }
        }
    }

    private static void ValidateCreateRequest(CreateEmployeeRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.ClientCtrlNbr <= 0)
            errors.Add("ClientCtrlNbr", ["Must be greater than 0"]);

        if (string.IsNullOrEmpty(request.Email))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid email address."));

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

    #region Work Profile

    public override async Task<EmployeeWorkProfileResponse> GetEmployeeWorkProfile(
        GetEmployeeWorkProfileRequest request, ServerCallContext context)
    {
        var parentCtrlNbr   = request.ParentCtrlNbr   > 0 ? ControlNumber.Create(request.ParentCtrlNbr)   : (ControlNumber?)null;
        var railroadCtrlNbr = request.RailroadCtrlNbr  > 0 ? ControlNumber.Create(request.RailroadCtrlNbr) : (ControlNumber?)null;

        var result = await _employeeAppService.GetEmployeeWorkProfileAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr),
            parentCtrlNbr, railroadCtrlNbr,
            context.CancellationToken);

        var response = new EmployeeWorkProfileResponse
        {
            Role               = result.Role,
            EmploymentDate     = result.EmploymentDate,
            EmploymentStatus   = result.EmploymentStatus,
            CanBidOnBulletins  = result.CanBidOnBulletins,
        };

        foreach (var s in result.SeniorityEntries)
        {
            response.SeniorityEntries.Add(new WorkProfileSeniorityEntry
            {
                CtrlNbr               = s.CtrlNbr.Value,
                RosterCtrlNbr         = s.RosterCtrlNbr.Value,
                RosterName            = s.RosterName,
                RosterDate            = s.RosterDate,
                Rank                  = s.Rank,
                SeniorityStateCtrlNbr = s.SeniorityStateCtrlNbr.Value,
                SeniorityStateName    = s.SeniorityStateName,
                LastActiveRoster      = s.LastActiveRoster,
                PositionName          = s.PositionName,
                PositionType          = s.PositionType,
                PositionAssignedDate  = s.PositionAssignedDate,
                CraftCtrlNbr          = s.CraftCtrlNbr.Value,
                DaysOnCurrentPosition = string.IsNullOrEmpty(s.PositionAssignedDate) ? 0
                    : DateTime.TryParse(s.PositionAssignedDate, out var assignedDate)
                        ? (int)(DateTime.UtcNow - assignedDate.ToUniversalTime()).TotalDays
                        : 0,
            });
        }

        await using var uow = await _uowFactory.CreateAsync(cancellationToken: context.CancellationToken);
        var craftTimeZoneCache = new Dictionary<long, TimeZoneInfo?>();

        foreach (var m in result.Moves)
        {
            var timeZone = await ResolveCraftTimeZoneAsync(
                uow,
                m.CraftCtrlNbr,
                craftTimeZoneCache,
                context.CancellationToken);

            response.Moves.Add(new WorkProfileSeniorityMove
            {
                CtrlNbr                  = m.CtrlNbr.Value,
                CraftCtrlNbr             = m.CraftCtrlNbr.Value,
                TargetPositionCtrlNbr    = m.TargetPositionCtrlNbr.Value,
                DisplacedEmployeeCtrlNbr = m.DisplacedEmployeeCtrlNbr?.Value ?? 0,
                RequestedLocal           = _workAreaClock.FormatLocalIso(m.RequestedUtc, timeZone),
                EffectiveLocal           = m.EffectiveUtc.HasValue
                    ? _workAreaClock.FormatLocalIso(m.EffectiveUtc.Value, timeZone)
                    : string.Empty,
                DaysOnCurrentPosition    = m.DaysOnCurrentPosition,
                MoveType                 = m.MoveType,
                Status                   = m.Status,
                RejectionReason          = m.RejectionReason ?? string.Empty,
                CancellationReason       = m.CancellationReason ?? string.Empty,
                CanCancel                = m.CanCancel,
                TargetPositionName       = m.TargetPositionName,
            });
        }

        foreach (var b in result.Bids)
        {
            response.Bids.Add(new WorkProfileBulletinBid
            {
                CtrlNbr             = b.CtrlNbr.Value,
                BulletinCtrlNbr     = b.BulletinCtrlNbr.Value,
                Priority            = b.Priority,
                SubmittedUtc        = b.SubmittedUtc.ToString("o"),
                Status              = b.Status,
                PositionName        = b.PositionName,
                BidWindowClosesLocal = b.BidWindowClosesLocalIso,
                EffectiveLocal       = b.EffectiveLocalIso,
            });
        }

        return response;
    }

    public override async Task<EmployeeOnDutyRecordsResponse> GetEmployeeOpenOnDutyRecords(
        GetEmployeeOpenOnDutyRecordsRequest request, ServerCallContext context)
    {
        var records = await _employeeAppService.GetOpenOnDutyRecordsAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr), context.CancellationToken);

        return MapOnDutyRecords(records);
    }

    public override async Task<DeleteResponse> CompleteDeferredOnDutyRecord(
        CompleteDeferredOnDutyRecordRequest request, ServerCallContext context)
    {
        try
        {
            await _employeeAppService.CompleteDeferredOnDutyRecordAsync(
                ControlNumber.Create(request.OnDutyRecordCtrlNbr),
                ControlNumber.Create(request.EmployeeCtrlNbr),
                request.OffDutyTimeUtc is null ? null : request.OffDutyTimeUtc.ToDateTime(),
                context.CancellationToken);

            return new DeleteResponse { Success = true, Messages = { "On-duty record completed successfully." } };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
    }

    public override async Task<EmployeeOnDutyRecordsResponse> GetDutyStatusNotStarted(
        GetDutyStatusNotStartedRequest request, ServerCallContext context)
    {
        if (request.RailroadCtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "RailroadCtrlNbr must be greater than zero."));

        var records = await _employeeAppService.GetDutyStatusNotStartedAsync(
            ControlNumber.Create(request.RailroadCtrlNbr),
            context.CancellationToken);

        return MapOnDutyRecords(records);
    }

    public override async Task<EmployeeOnDutyRecordsResponse> GetEmployeeOnDutyHistory(
        GetEmployeeOnDutyHistoryRequest request, ServerCallContext context)
    {
        var railroadCtrlNbr = request.RailroadCtrlNbr > 0
            ? ControlNumber.Create(request.RailroadCtrlNbr) : (ControlNumber?)null;
        var period = (OnDutyHistoryPeriod)request.Period;

        var records = await _employeeAppService.GetOnDutyHistoryAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr), period, railroadCtrlNbr,
            context.CancellationToken);

        return MapOnDutyRecords(records);
    }

    private static EmployeeOnDutyRecordsResponse MapOnDutyRecords(
        IReadOnlyList<EmployeeOnDutyRecordItem> records)
    {
        var response = new EmployeeOnDutyRecordsResponse();
        foreach (var r in records)
        {
            response.Records.Add(new EmployeeOnDutyRecord
            {
                CtrlNbr            = r.CtrlNbr.Value,
                PreviousRestHours  = r.PreviousRestHours.ToString("0.##"),
                AssignmentName     = r.AssignmentName,
                AssignmentCode     = r.AssignmentCode,
                CrewName           = r.CrewName,
                CraftRoleName      = r.CraftRoleName,
                Location           = r.Location,
                OnDutyUtc          = r.OnDutyTimeUtc.ToString("o"),
                OnDutyLocal        = r.OnDutyLocalIso,
                OffDutyUtc         = r.OffDutyTimeUtc?.ToString("o") ?? string.Empty,
                OffDutyLocal       = r.OffDutyLocalIso,
                TotalTimeOnDutyMin = r.TotalTimeOnDutyMinutes ?? 0,
                ConsecutiveDays    = r.ConsecutiveDays,
                IsAssigned         = r.IsAssigned,
                IsLateCall         = r.IsLateCall,
                Status             = r.Status,
                WorkAreaCtrlNbr    = r.WorkAreaCtrlNbr ?? 0,
                WorkAreaName       = r.WorkAreaName,
                CompletionStatus   = r.CompletionStatus,
                IsQuickTieUp       = r.IsQuickTieUp,
                RestedAtUtc        = r.RestedAtUtc?.ToString("o") ?? string.Empty,
                OffDutyTimeConfirmed = r.OffDutyTimeConfirmed,
                OffDutyTimeConfirmedAtUtc = r.OffDutyTimeConfirmedAtUtc?.ToString("o") ?? string.Empty,
                OffDutyTimeConfirmedBy = r.OffDutyTimeConfirmedBy,
                WorkAreaCode = r.WorkAreaCode,
                EmployeeName = r.EmployeeName,
                EmployeeNumber = r.EmployeeNumber,
                EmployeeCtrlNbr = r.EmployeeCtrlNbr,
                CraftCtrlNbr = r.CraftCtrlNbr,
                AssignmentOffDutyLocal = r.AssignmentOffDutyLocalIso,
            });
        }
        return response;
    }

    private async Task<TimeZoneInfo?> ResolveCraftTimeZoneAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber craftCtrlNbr,
        Dictionary<long, TimeZoneInfo?> cache,
        CancellationToken ct)
    {
        if (cache.TryGetValue(craftCtrlNbr.Value, out var cached))
            return cached;

        TimeZoneInfo? tz = null;
        var rosters = await uow.Rosters.GetByCraftCtrlNbrAsync(craftCtrlNbr);
        var roster = rosters.FirstOrDefault();
        if (roster is not null)
        {
            var group = await uow.DynamicGroups.GetByCtrlNbrAsync(roster.WorkAreaGroupCtrlNbr, ct);
            tz = _workAreaClock.ResolveTimeZone(group?.TimeZoneId);
        }

        cache[craftCtrlNbr.Value] = tz;
        return tz;
    }

    #endregion
}