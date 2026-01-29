using CrewService.Domain.Models.Employees;
using CrewService.Domain.Repositories;
using CrewService.Domain.ValueObjects;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace CrewService.Presentation.Services;

public class EmployeeService(IEmployeeRepository employeeRepository) : EmployeeSrvc.EmployeeSrvcBase
{
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;

    #region Employee Operations

    public override async Task<GetAllEmployeesResponse> GetAllEmployeesAsync(GetAllEmployeesRequest request, ServerCallContext context)
    {
        var response = new GetAllEmployeesResponse();

        var employees = request.PageSize > 0
            ? await _employeeRepository.GetAllAsync(request.PageNumber, request.PageSize)
            : await _employeeRepository.GetAllAsync();

        foreach (var employee in employees)
        {
            response.Employees.Add(MapToEmployeeResponse(employee));
        }

        response.TotalCount = employees.Count;

        return response;
    }

    public override async Task<GetEmployeeResponse> GetEmployeeAsync(GetEmployeeRequest request, ServerCallContext context)
    {
        var employee = await _employeeRepository.GetByCtrlNbrAsync(request.CtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with control number {request.CtrlNbr} was not found."));

        return MapToEmployeeResponse(employee);
    }

    public override async Task<GetEmployeeResponse> GetEmployeeByNumberAsync(GetEmployeeByNumberRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.EmployeeNumber))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid employee number."));

        var employee = await _employeeRepository.GetByEmployeeNumberAsync(request.EmployeeNumber)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with number {request.EmployeeNumber} was not found."));

        return MapToEmployeeResponse(employee);
    }

    public override async Task<CreateEmployeeResponse> CreateEmployeeAsync(CreateEmployeeRequest request, ServerCallContext context)
    {
        ValidateCreateRequest(request);

        var employee = Employee.Create(
            request.ClientCtrlNbr,
            request.UserId,
            request.EmployeeNumber,
            request.SocialSecurityNumber,
            request.Gender,
            request.Race,
            request.BirthDate.ToDateTime(),
            request.EmploymentDate.ToDateTime(),
            request.EmploymentStatusCtrlNbr);

        if (!string.IsNullOrEmpty(request.DriversLicenseNumber))
        {
            employee.Update(
                driversLicenseNumber: request.DriversLicenseNumber,
                issuingState: request.IssuingState,
                marriageStatus: request.MarriageStatus);
        }

        _employeeRepository.Add(employee);

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

        var employee = await _employeeRepository.GetByCtrlNbrAsync(request.CtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with control number {request.CtrlNbr} was not found."));

        employee.Update(
            driversLicenseNumber: string.IsNullOrEmpty(request.DriversLicenseNumber) ? null : request.DriversLicenseNumber,
            issuingState: string.IsNullOrEmpty(request.IssuingState) ? null : request.IssuingState,
            marriageStatus: request.MarriageStatus,
            allowFMLAMarkOff: request.AllowFmlaMarkOff,
            callForOvertime: request.CallForOvertime,
            processPayroll: request.ProcessPayroll,
            tieUpOffProperty: request.TieUpOffProperty);

        _employeeRepository.Update(employee);

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

        var employee = await _employeeRepository.GetByCtrlNbrAsync(request.CtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with control number {request.CtrlNbr} was not found."));

        _employeeRepository.Remove(employee);

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
        var employee = await _employeeRepository.GetByCtrlNbrAsync(request.EmployeeCtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with control number {request.EmployeeCtrlNbr} was not found."));

        var address = employee.AddAddress(
            request.Address1,
            request.City,
            request.State,
            request.ZipCode,
            request.AddressTypeCtrlNbr,
            string.IsNullOrEmpty(request.Address2) ? null : request.Address2);

        _employeeRepository.Update(employee);

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
        var employee = await _employeeRepository.GetByCtrlNbrAsync(request.EmployeeCtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with control number {request.EmployeeCtrlNbr} was not found."));

        var address = employee.Addresses.FirstOrDefault(a => a.CtrlNbr == ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Address with control number {request.CtrlNbr} was not found."));

        address.Update(
            address1: string.IsNullOrEmpty(request.Address1) ? null : request.Address1,
            address2: string.IsNullOrEmpty(request.Address2) ? null : request.Address2,
            city: string.IsNullOrEmpty(request.City) ? null : request.City,
            state: string.IsNullOrEmpty(request.State) ? null : request.State,
            zipCode: string.IsNullOrEmpty(request.ZipCode) ? null : request.ZipCode);

        _employeeRepository.Update(employee);

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
        var employee = await _employeeRepository.GetByCtrlNbrAsync(request.EmployeeCtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with control number {request.EmployeeCtrlNbr} was not found."));

        employee.RemoveAddress(ControlNumber.Create(request.CtrlNbr));

        _employeeRepository.Update(employee);

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
        var employee = await _employeeRepository.GetByCtrlNbrAsync(request.EmployeeCtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with control number {request.EmployeeCtrlNbr} was not found."));

        var phone = employee.AddPhoneNumber(
            request.Number,
            request.CallingOrder,
            request.DialOne,
            request.PhoneTypeCtrlNbr);

        _employeeRepository.Update(employee);

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
        var employee = await _employeeRepository.GetByCtrlNbrAsync(request.EmployeeCtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with control number {request.EmployeeCtrlNbr} was not found."));

        var phone = employee.PhoneNumbers.FirstOrDefault(p => p.CtrlNbr == ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Phone number with control number {request.CtrlNbr} was not found."));

        phone.Update(
            number: string.IsNullOrEmpty(request.Number) ? null : request.Number,
            callingOrder: request.CallingOrder > 0 ? request.CallingOrder : null,
            dialOne: request.DialOne);

        _employeeRepository.Update(employee);

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
        var employee = await _employeeRepository.GetByCtrlNbrAsync(request.EmployeeCtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with control number {request.EmployeeCtrlNbr} was not found."));

        employee.RemovePhoneNumber(ControlNumber.Create(request.CtrlNbr));

        _employeeRepository.Update(employee);

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
        var employee = await _employeeRepository.GetByCtrlNbrAsync(request.EmployeeCtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with control number {request.EmployeeCtrlNbr} was not found."));

        var email = employee.AddEmailAddress(request.Email, request.EmailTypeCtrlNbr);

        _employeeRepository.Update(employee);

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
        var employee = await _employeeRepository.GetByCtrlNbrAsync(request.EmployeeCtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with control number {request.EmployeeCtrlNbr} was not found."));

        var email = employee.EmailAddresses.FirstOrDefault(e => e.CtrlNbr == ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Email address with control number {request.CtrlNbr} was not found."));

        email.Update(string.IsNullOrEmpty(request.Email) ? null : request.Email);

        _employeeRepository.Update(employee);

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
        var employee = await _employeeRepository.GetByCtrlNbrAsync(request.EmployeeCtrlNbr)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Employee with control number {request.EmployeeCtrlNbr} was not found."));

        employee.RemoveEmailAddress(ControlNumber.Create(request.CtrlNbr));

        _employeeRepository.Update(employee);

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
            Gender = employee.Gender,
            Race = employee.Race,
            MarriageStatus = employee.MarriageStatus,
            BirthDate = Timestamp.FromDateTime(DateTime.SpecifyKind(employee.BirthDate, DateTimeKind.Utc)),
            EmploymentDate = Timestamp.FromDateTime(DateTime.SpecifyKind(employee.EmploymentDate, DateTimeKind.Utc)),
            EmploymentStatusCtrlNbr = employee.EmploymentStatusCtrlNbr.Value,
            AllowFmlaMarkOff = employee.AllowFMLAMarkOff,
            CallForOvertime = employee.CallForOvertime,
            ProcessPayroll = employee.ProcessPayroll,
            TieUpOffProperty = employee.TieUpOffProperty,
            DriversLicenseNumber = employee.DriversLicenseNumber ?? string.Empty,
            IssuingState = employee.IssuingState ?? string.Empty
        };

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

    private static void ValidateCreateRequest(CreateEmployeeRequest request)
    {
        if (request.ClientCtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid client control number."));

        if (string.IsNullOrWhiteSpace(request.UserId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid user ID."));

        if (string.IsNullOrWhiteSpace(request.EmployeeNumber))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid employee number."));

        if (string.IsNullOrWhiteSpace(request.SocialSecurityNumber))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid social security number."));

        if (string.IsNullOrWhiteSpace(request.Gender))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid gender."));

        if (string.IsNullOrWhiteSpace(request.Race))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid race."));

        if (request.EmploymentStatusCtrlNbr <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Please provide a valid employment status control number."));
    }

    #endregion
}