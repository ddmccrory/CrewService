using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.ContactTypes;

public sealed class ContactTypesAppService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    // ── Address Types ────────────────────────────────────────────────────────

    public async Task<List<AddressType>> GetAllAddressTypesAsync(
        ControlNumber clientCtrlNbr, int pageNumber = 0, int pageSize = 0, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return pageSize > 0
            ? await uow.AddressTypes.GetByClientCtrlNbrAsync(clientCtrlNbr, pageNumber, pageSize)
            : await uow.AddressTypes.GetByClientCtrlNbrAsync(clientCtrlNbr);
    }

    public async Task<AddressType> GetAddressTypeAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.AddressTypes.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Address type {ctrlNbr.Value} not found.");
    }

    public async Task<AddressType> CreateAddressTypeAsync(
        ControlNumber clientCtrlNbr, string name, int number, bool emergencyType, CancellationToken ct = default)
    {
        var type = AddressType.Create(clientCtrlNbr, name, number, emergencyType);
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.AddressTypes.Add(type);
        await uow.CommitAsync(ct);
        return type;
    }

    public async Task<AddressType> UpdateAddressTypeAsync(
        ControlNumber ctrlNbr, string name, int number, bool emergencyType, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var type = await uow.AddressTypes.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Address type {ctrlNbr.Value} not found.");
        type.Update(name, number, emergencyType);
        uow.AddressTypes.Update(type);
        await uow.CommitAsync(ct);
        return type;
    }

    public async Task DeleteAddressTypeAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var type = await uow.AddressTypes.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Address type {ctrlNbr.Value} not found.");
        uow.AddressTypes.Remove(type);
        await uow.CommitAsync(ct);
    }

    // ── Phone Number Types ───────────────────────────────────────────────────

    public async Task<List<PhoneNumberType>> GetAllPhoneNumberTypesAsync(
        ControlNumber clientCtrlNbr, int pageNumber = 0, int pageSize = 0, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return pageSize > 0
            ? await uow.PhoneNumberTypes.GetByClientCtrlNbrAsync(clientCtrlNbr, pageNumber, pageSize)
            : await uow.PhoneNumberTypes.GetByClientCtrlNbrAsync(clientCtrlNbr);
    }

    public async Task<PhoneNumberType> GetPhoneNumberTypeAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.PhoneNumberTypes.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Phone number type {ctrlNbr.Value} not found.");
    }

    public async Task<PhoneNumberType> CreatePhoneNumberTypeAsync(
        ControlNumber clientCtrlNbr, string name, int number, bool emergencyType, CancellationToken ct = default)
    {
        var type = PhoneNumberType.Create(clientCtrlNbr, name, number, emergencyType);
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.PhoneNumberTypes.Add(type);
        await uow.CommitAsync(ct);
        return type;
    }

    public async Task<PhoneNumberType> UpdatePhoneNumberTypeAsync(
        ControlNumber ctrlNbr, string name, int number, bool emergencyType, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var type = await uow.PhoneNumberTypes.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Phone number type {ctrlNbr.Value} not found.");
        type.Update(name, number, emergencyType);
        uow.PhoneNumberTypes.Update(type);
        await uow.CommitAsync(ct);
        return type;
    }

    public async Task DeletePhoneNumberTypeAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var type = await uow.PhoneNumberTypes.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Phone number type {ctrlNbr.Value} not found.");
        uow.PhoneNumberTypes.Remove(type);
        await uow.CommitAsync(ct);
    }

    // ── Email Address Types ──────────────────────────────────────────────────

    public async Task<List<EmailAddressType>> GetAllEmailAddressTypesAsync(
        ControlNumber clientCtrlNbr, int pageNumber = 0, int pageSize = 0, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return pageSize > 0
            ? await uow.EmailAddressTypes.GetByClientCtrlNbrAsync(clientCtrlNbr, pageNumber, pageSize)
            : await uow.EmailAddressTypes.GetByClientCtrlNbrAsync(clientCtrlNbr);
    }

    public async Task<EmailAddressType> GetEmailAddressTypeAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.EmailAddressTypes.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Email address type {ctrlNbr.Value} not found.");
    }

    public async Task<EmailAddressType> CreateEmailAddressTypeAsync(
        ControlNumber clientCtrlNbr, string name, int number, bool emergencyType, CancellationToken ct = default)
    {
        var type = EmailAddressType.Create(clientCtrlNbr, name, number, emergencyType);
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.EmailAddressTypes.Add(type);
        await uow.CommitAsync(ct);
        return type;
    }

    public async Task<EmailAddressType> UpdateEmailAddressTypeAsync(
        ControlNumber ctrlNbr, string name, int number, bool emergencyType, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var type = await uow.EmailAddressTypes.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Email address type {ctrlNbr.Value} not found.");
        type.Update(name, number, emergencyType);
        uow.EmailAddressTypes.Update(type);
        await uow.CommitAsync(ct);
        return type;
    }

    public async Task DeleteEmailAddressTypeAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var type = await uow.EmailAddressTypes.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Email address type {ctrlNbr.Value} not found.");
        uow.EmailAddressTypes.Remove(type);
        await uow.CommitAsync(ct);
    }
}
