using CrewService.Application.Time;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;
using CrewService.Presentation.Services;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class BulletinsService(IServiceProvider serviceProvider) : BulletinsSrvc.BulletinsSrvcBase
{
    private IWorkAreaClock? _clock;
    private IWorkAreaClock Clock => _clock ??= serviceProvider.GetRequiredService<IWorkAreaClock>();
    public override async Task<GetVacanciesResponse> GetOpenVacancies(GetOpenVacanciesRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        var railroadCtrlNbr = request.RailroadCtrlNbr > 0 ? ControlNumber.Create(request.RailroadCtrlNbr) : null;
        var vacancies = await svc.GetOpenVacanciesAsync(railroadCtrlNbr, context.CancellationToken);
        var response = new GetVacanciesResponse { TotalCount = vacancies.Count };
        foreach (var v in vacancies)
        {
            var tz = await GetWorkAreaTimeZoneAsync(v.WorkAreaGroupCtrlNbr.Value, context.CancellationToken);
            var bulletin = await svc.GetBulletinByVacancyAsync(v.CtrlNbr, context.CancellationToken);
            var crewCtrlNbr = await ResolveCrewCtrlNbrAsync(v, context.CancellationToken);
            response.Vacancies.Add(MapVacancy(v, tz, bulletin, crewCtrlNbr));
        }
        return response;
    }

    public override async Task<GetVacanciesResponse> GetVacanciesByWorkArea(GetVacanciesByWorkAreaRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        var vacancies = await svc.GetVacanciesByWorkAreaAsync(ControlNumber.Create(request.WorkAreaCtrlNbr), context.CancellationToken);
        var tz = await GetWorkAreaTimeZoneAsync(request.WorkAreaCtrlNbr, context.CancellationToken);
        var response = new GetVacanciesResponse { TotalCount = vacancies.Count };
        foreach (var v in vacancies)
        {
            var bulletin = await svc.GetBulletinByVacancyAsync(v.CtrlNbr, context.CancellationToken);
            var crewCtrlNbr = await ResolveCrewCtrlNbrAsync(v, context.CancellationToken);
            response.Vacancies.Add(MapVacancy(v, tz, bulletin, crewCtrlNbr));
        }
        return response;
    }

    public override async Task<PositionVacancyResponse> GetVacancy(GetVacancyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        try
        {
            var v = await svc.GetVacancyAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            var tz = await GetWorkAreaTimeZoneAsync(v.WorkAreaGroupCtrlNbr.Value, context.CancellationToken);
            var crewCtrlNbr = await ResolveCrewCtrlNbrAsync(v, context.CancellationToken);
            return MapVacancy(v, tz, crewCtrlNbr: crewCtrlNbr);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<OpenVacancyResponse> OpenVacancy(OpenVacancyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        var (vacancy, bulletin) = await svc.OpenVacancyAsync(
            ControlNumber.Create(request.WorkAreaCtrlNbr),
            request.TargetType,
            ControlNumber.Create(request.TargetCtrlNbr),
            ControlNumber.Create(request.CraftCtrlNbr),
            request.VacancyReasonCode,
            request.PreviousIncumbentCtrlNbr > 0 ? ControlNumber.Create(request.PreviousIncumbentCtrlNbr) : null,
            request.TargetName,
            context.CancellationToken);
        var tz = await GetWorkAreaTimeZoneAsync(request.WorkAreaCtrlNbr, context.CancellationToken);
        return new OpenVacancyResponse
        {
            Vacancy = MapVacancy(vacancy, tz, crewCtrlNbr: await ResolveCrewCtrlNbrAsync(vacancy, context.CancellationToken)),
            Bulletin = bulletin is not null ? MapBulletin(bulletin, vacancy.TargetName, tz, craftRoleCtrlNbr: await ResolveCraftRoleCtrlNbrAsync(vacancy, context.CancellationToken)) : null
        };
    }

    public override async Task<PositionVacancyResponse> AbolishVacancy(AbolishVacancyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        try
        {
            var v = await svc.AbolishVacancyAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            var tz = await GetWorkAreaTimeZoneAsync(v.WorkAreaGroupCtrlNbr.Value, context.CancellationToken);
            var crewCtrlNbr = await ResolveCrewCtrlNbrAsync(v, context.CancellationToken);
            return MapVacancy(v, tz, crewCtrlNbr: crewCtrlNbr);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<GetBulletinsResponse> GetActiveBulletins(GetActiveBulletinsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        var railroadCtrlNbr = request.RailroadCtrlNbr > 0 ? ControlNumber.Create(request.RailroadCtrlNbr) : null;
        var bulletins = await svc.GetActiveBulletinsAsync(railroadCtrlNbr, request.EmployeeCtrlNbr > 0, context.CancellationToken);
        var (vacancyIndex, tzIndex) = await BuildVacancyIndexAsync(svc, bulletins, context.CancellationToken);
        var response = new GetBulletinsResponse { TotalCount = bulletins.Count };
        foreach (var b in bulletins)
        {
            var posName = vacancyIndex.TryGetValue(b.PositionVacancyCtrlNbr.Value, out var vi) ? vi.Name : string.Empty;
            var craftRoleCtrlNbr = vacancyIndex.TryGetValue(b.PositionVacancyCtrlNbr.Value, out var vi2) ? vi2.CraftRoleCtrlNbr : 0;
            var vacInfoTz = tzIndex.GetValueOrDefault(b.PositionVacancyCtrlNbr.Value);
            var bidCount = await GetBidCountAsync(svc, b.CtrlNbr, context.CancellationToken);
            var vacCtrlNbr = b.PositionVacancyCtrlNbr;
            long prevIncumbent = 0;
            string targetType = string.Empty;
            long crewCtrlNbr = 0;
            try { var vac = await svc.GetVacancyAsync(vacCtrlNbr, context.CancellationToken); prevIncumbent = vac.PreviousIncumbentCtrlNbr?.Value ?? 0; } catch { }
            try { var vac = await svc.GetVacancyAsync(vacCtrlNbr, context.CancellationToken); targetType = vac.TargetType; crewCtrlNbr = await ResolveCrewCtrlNbrAsync(vac, context.CancellationToken); } catch { }
            var vacatedByName = await ResolveEmployeeNameAsync(prevIncumbent, context.CancellationToken);
            var awardedName = await ResolveEmployeeNameAsync(b.AwardedEmployeeCtrlNbr?.Value ?? 0, context.CancellationToken);
            response.Bulletins.Add(MapBulletin(b, posName, vacInfoTz, bidCount, vacatedByName, awardedName, craftRoleCtrlNbr, targetType, crewCtrlNbr));
        }
        return response;
    }

    public override async Task<GetBulletinsResponse> GetBulletinsInDateRange(GetBulletinsInDateRangeRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        var railroadCtrlNbr = request.RailroadCtrlNbr > 0 ? ControlNumber.Create(request.RailroadCtrlNbr) : null;
        var fromUtc = DateTime.TryParse(request.FromUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
            ? dt
            : DateTime.UtcNow.AddDays(-7);
        var bulletins = await svc.GetBulletinsInDateRangeAsync(fromUtc, railroadCtrlNbr, request.EmployeeCtrlNbr > 0, context.CancellationToken);
        var (vacancyIndex, tzIndex) = await BuildVacancyIndexAsync(svc, bulletins, context.CancellationToken);
        var response = new GetBulletinsResponse { TotalCount = bulletins.Count };
        foreach (var b in bulletins)
        {
            var posName = vacancyIndex.TryGetValue(b.PositionVacancyCtrlNbr.Value, out var vi) ? vi.Name : string.Empty;
            var craftRoleCtrlNbr = vacancyIndex.TryGetValue(b.PositionVacancyCtrlNbr.Value, out var vi2) ? vi2.CraftRoleCtrlNbr : 0;
            var vacInfoTz = tzIndex.GetValueOrDefault(b.PositionVacancyCtrlNbr.Value);
            var bidCount = await GetBidCountAsync(svc, b.CtrlNbr, context.CancellationToken);
            long prevIncumbent = 0;
            string targetType = string.Empty;
            long crewCtrlNbr = 0;
            try { var vac = await svc.GetVacancyAsync(b.PositionVacancyCtrlNbr, context.CancellationToken); prevIncumbent = vac.PreviousIncumbentCtrlNbr?.Value ?? 0; } catch { }
            try { var vac = await svc.GetVacancyAsync(b.PositionVacancyCtrlNbr, context.CancellationToken); targetType = vac.TargetType; crewCtrlNbr = await ResolveCrewCtrlNbrAsync(vac, context.CancellationToken); } catch { }
            var vacatedByName = await ResolveEmployeeNameAsync(prevIncumbent, context.CancellationToken);
            var awardedName = await ResolveEmployeeNameAsync(b.AwardedEmployeeCtrlNbr?.Value ?? 0, context.CancellationToken);
            response.Bulletins.Add(MapBulletin(b, posName, vacInfoTz, bidCount, vacatedByName, awardedName, craftRoleCtrlNbr, targetType, crewCtrlNbr));
        }
        return response;
    }
    public override async Task<GetBulletinsResponse> GetPostedBulletins(GetPostedBulletinsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        var railroadCtrlNbr = request.RailroadCtrlNbr > 0 ? ControlNumber.Create(request.RailroadCtrlNbr) : null;
        var bulletins = await svc.GetPostedBulletinsAsync(railroadCtrlNbr, context.CancellationToken);
        var (vacancyIndex, tzIndex) = await BuildVacancyIndexAsync(svc, bulletins, context.CancellationToken);
        var response = new GetBulletinsResponse { TotalCount = bulletins.Count };
        foreach (var b in bulletins)
        {
            var posName = vacancyIndex.TryGetValue(b.PositionVacancyCtrlNbr.Value, out var vi) ? vi.Name : string.Empty;
            var craftRoleCtrlNbr = vacancyIndex.TryGetValue(b.PositionVacancyCtrlNbr.Value, out var vi2) ? vi2.CraftRoleCtrlNbr : 0;
            var vacInfoTz = tzIndex.GetValueOrDefault(b.PositionVacancyCtrlNbr.Value);
            var bidCount = await GetBidCountAsync(svc, b.CtrlNbr, context.CancellationToken);
            var vacCtrlNbr = b.PositionVacancyCtrlNbr;
            long prevIncumbent = 0;
            string targetType = string.Empty;
            long crewCtrlNbr = 0;
            try { var vac = await svc.GetVacancyAsync(vacCtrlNbr, context.CancellationToken); prevIncumbent = vac.PreviousIncumbentCtrlNbr?.Value ?? 0; } catch { }
            try { var vac = await svc.GetVacancyAsync(vacCtrlNbr, context.CancellationToken); targetType = vac.TargetType; crewCtrlNbr = await ResolveCrewCtrlNbrAsync(vac, context.CancellationToken); } catch { }
            var vacatedByName = await ResolveEmployeeNameAsync(prevIncumbent, context.CancellationToken);
            var awardedName = await ResolveEmployeeNameAsync(b.AwardedEmployeeCtrlNbr?.Value ?? 0, context.CancellationToken);
            response.Bulletins.Add(MapBulletin(b, posName, vacInfoTz, bidCount, vacatedByName, awardedName, craftRoleCtrlNbr, targetType, crewCtrlNbr));
        }
        return response;
    }

    public override async Task<GetBulletinsResponse> GetPostedBulletinsByCraft(GetPostedBulletinsByCraftRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        var bulletins = await svc.GetPostedBulletinsByCraftAsync(ControlNumber.Create(request.CraftCtrlNbr), context.CancellationToken);
        var (vacancyIndex, tzIndex) = await BuildVacancyIndexAsync(svc, bulletins, context.CancellationToken);
        var response = new GetBulletinsResponse { TotalCount = bulletins.Count };
        foreach (var b in bulletins)
        {
            var posName = vacancyIndex.TryGetValue(b.PositionVacancyCtrlNbr.Value, out var vi) ? vi.Name : string.Empty;
            var craftRoleCtrlNbrBc = vacancyIndex.TryGetValue(b.PositionVacancyCtrlNbr.Value, out var vi3) ? vi3.CraftRoleCtrlNbr : 0;
            var vacInfoTz = tzIndex.GetValueOrDefault(b.PositionVacancyCtrlNbr.Value);
            var bidCount = await GetBidCountAsync(svc, b.CtrlNbr, context.CancellationToken);
            var vacCtrlNbr = b.PositionVacancyCtrlNbr;
            long prevIncumbent = 0;
            string targetType = string.Empty;
            long crewCtrlNbr = 0;
            try { var vac = await svc.GetVacancyAsync(vacCtrlNbr, context.CancellationToken); prevIncumbent = vac.PreviousIncumbentCtrlNbr?.Value ?? 0; } catch { }
            try { var vac = await svc.GetVacancyAsync(vacCtrlNbr, context.CancellationToken); targetType = vac.TargetType; crewCtrlNbr = await ResolveCrewCtrlNbrAsync(vac, context.CancellationToken); } catch { }
            var vacatedByName = await ResolveEmployeeNameAsync(prevIncumbent, context.CancellationToken);
            var awardedName = await ResolveEmployeeNameAsync(b.AwardedEmployeeCtrlNbr?.Value ?? 0, context.CancellationToken);
            response.Bulletins.Add(MapBulletin(b, posName, vacInfoTz, bidCount, vacatedByName, awardedName, craftRoleCtrlNbrBc, targetType, crewCtrlNbr));
        }
        return response;
    }

    public override async Task<GetBulletinsResponse> GetBulletinsByWorkArea(GetBulletinsByWorkAreaRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        var bulletins = await svc.GetBulletinsByWorkAreaAsync(ControlNumber.Create(request.WorkAreaCtrlNbr), context.CancellationToken);
        var tz = await GetWorkAreaTimeZoneAsync(request.WorkAreaCtrlNbr, context.CancellationToken);
        var (vacancyIndex, _) = await BuildVacancyIndexAsync(svc, bulletins, context.CancellationToken);
        var response = new GetBulletinsResponse { TotalCount = bulletins.Count };
        foreach (var b in bulletins)
        {
            var posName = vacancyIndex.TryGetValue(b.PositionVacancyCtrlNbr.Value, out var vi) ? vi.Name : string.Empty;
            var craftRoleCtrlNbrWa = vacancyIndex.TryGetValue(b.PositionVacancyCtrlNbr.Value, out var vi4) ? vi4.CraftRoleCtrlNbr : 0;
            var bidCount = await GetBidCountAsync(svc, b.CtrlNbr, context.CancellationToken);
            long prevIncumbent = 0;
            string targetType = string.Empty;
            long crewCtrlNbr = 0;
            try { var vac = await svc.GetVacancyAsync(b.PositionVacancyCtrlNbr, context.CancellationToken); prevIncumbent = vac.PreviousIncumbentCtrlNbr?.Value ?? 0; } catch { }
            try { var vac = await svc.GetVacancyAsync(b.PositionVacancyCtrlNbr, context.CancellationToken); targetType = vac.TargetType; crewCtrlNbr = await ResolveCrewCtrlNbrAsync(vac, context.CancellationToken); } catch { }
            var vacatedByName = await ResolveEmployeeNameAsync(prevIncumbent, context.CancellationToken);
            var awardedName = await ResolveEmployeeNameAsync(b.AwardedEmployeeCtrlNbr?.Value ?? 0, context.CancellationToken);
            response.Bulletins.Add(MapBulletin(b, posName, tz, bidCount, vacatedByName, awardedName, craftRoleCtrlNbrWa, targetType, crewCtrlNbr));
        }
        return response;
    }

    public override async Task<BulletinResponse> GetBulletin(GetBulletinRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        try
        {
            var bulletin = await svc.GetBulletinAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            var vacancy = await svc.GetVacancyAsync(bulletin.PositionVacancyCtrlNbr, context.CancellationToken);
            var tz = await GetWorkAreaTimeZoneAsync(vacancy.WorkAreaGroupCtrlNbr.Value, context.CancellationToken);
            var bidCount = await GetBidCountAsync(svc, bulletin.CtrlNbr, context.CancellationToken);
            var vacatedByName = await ResolveEmployeeNameAsync(vacancy.PreviousIncumbentCtrlNbr?.Value ?? 0, context.CancellationToken);
            var awardedName = await ResolveEmployeeNameAsync(bulletin.AwardedEmployeeCtrlNbr?.Value ?? 0, context.CancellationToken);
            var craftRoleCtrlNbrGb = await ResolveCraftRoleCtrlNbrAsync(vacancy, context.CancellationToken);
            var crewCtrlNbrGb = await ResolveCrewCtrlNbrAsync(vacancy, context.CancellationToken);
            return MapBulletin(bulletin, vacancy.TargetName, tz, bidCount, vacatedByName, awardedName, craftRoleCtrlNbrGb, vacancy.TargetType, crewCtrlNbrGb);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<BulletinResponse> PostBulletinForVacancy(PostBulletinForVacancyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        try
        {
            var vacancy = await svc.GetVacancyAsync(ControlNumber.Create(request.VacancyCtrlNbr), context.CancellationToken);
            var tz = await GetWorkAreaTimeZoneAsync(vacancy.WorkAreaGroupCtrlNbr.Value, context.CancellationToken);

            // Input times from the UI are in local work-area time; convert to UTC for storage.
            var opens = ParseAsUtc(request.BidWindowOpensUtc, tz);
            var closes = ParseAsUtc(request.BidWindowClosesUtc, tz);
            var effective = ParseAsUtc(request.EffectiveUtc, tz);

            var bulletin = await svc.PostBulletinForVacancyAsync(
                ControlNumber.Create(request.VacancyCtrlNbr), opens, closes, effective,
                context.CancellationToken);
            var vacatedByNamePbfv = await ResolveEmployeeNameAsync(vacancy.PreviousIncumbentCtrlNbr?.Value ?? 0, context.CancellationToken);
            var craftRoleCtrlNbrPbfv = await ResolveCraftRoleCtrlNbrAsync(vacancy, context.CancellationToken);
            var crewCtrlNbrPbfv = await ResolveCrewCtrlNbrAsync(vacancy, context.CancellationToken);
            return MapBulletin(bulletin, vacancy.TargetName, tz, 0, vacatedByNamePbfv, craftRoleCtrlNbr: craftRoleCtrlNbrPbfv, targetType: vacancy.TargetType, crewCtrlNbr: crewCtrlNbrPbfv);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }

    public override async Task<BulletinResponse> AwardBulletin(AwardBulletinRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        try
        {
            var bulletin = await svc.AwardBulletinAsync(ControlNumber.Create(request.CtrlNbr), ControlNumber.Create(request.EmployeeCtrlNbr), context.CancellationToken);
            var vacancy = await svc.GetVacancyAsync(bulletin.PositionVacancyCtrlNbr, context.CancellationToken);
            var tz = await GetWorkAreaTimeZoneAsync(vacancy.WorkAreaGroupCtrlNbr.Value, context.CancellationToken);
            var bidCountAw = await GetBidCountAsync(svc, bulletin.CtrlNbr, context.CancellationToken);
            var vacatedByNameAw = await ResolveEmployeeNameAsync(vacancy.PreviousIncumbentCtrlNbr?.Value ?? 0, context.CancellationToken);
            var awardedNameAw = await ResolveEmployeeNameAsync(bulletin.AwardedEmployeeCtrlNbr?.Value ?? 0, context.CancellationToken);
            var craftRoleCtrlNbrAw = await ResolveCraftRoleCtrlNbrAsync(vacancy, context.CancellationToken);
            var crewCtrlNbrAw = await ResolveCrewCtrlNbrAsync(vacancy, context.CancellationToken);
            return MapBulletin(bulletin, vacancy.TargetName, tz, bidCountAw, vacatedByNameAw, awardedNameAw, craftRoleCtrlNbrAw, vacancy.TargetType, crewCtrlNbrAw);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<BulletinResponse> ForceAssignBulletin(ForceAssignBulletinRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        try
        {
            var bulletin = await svc.ForceAssignBulletinAsync(ControlNumber.Create(request.CtrlNbr), ControlNumber.Create(request.EmployeeCtrlNbr), context.CancellationToken);
            var vacancy = await svc.GetVacancyAsync(bulletin.PositionVacancyCtrlNbr, context.CancellationToken);
            var tz = await GetWorkAreaTimeZoneAsync(vacancy.WorkAreaGroupCtrlNbr.Value, context.CancellationToken);
            var bidCountFa = await GetBidCountAsync(svc, bulletin.CtrlNbr, context.CancellationToken);
            var vacatedByNameFa = await ResolveEmployeeNameAsync(vacancy.PreviousIncumbentCtrlNbr?.Value ?? 0, context.CancellationToken);
            var awardedNameFa = await ResolveEmployeeNameAsync(bulletin.AwardedEmployeeCtrlNbr?.Value ?? 0, context.CancellationToken);
            var craftRoleCtrlNbrFa = await ResolveCraftRoleCtrlNbrAsync(vacancy, context.CancellationToken);
            var crewCtrlNbrFa = await ResolveCrewCtrlNbrAsync(vacancy, context.CancellationToken);
            return MapBulletin(bulletin, vacancy.TargetName, tz, bidCountFa, vacatedByNameFa, awardedNameFa, craftRoleCtrlNbrFa, vacancy.TargetType, crewCtrlNbrFa);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<BulletinResponse> AutoForceAssignBulletin(AutoForceAssignBulletinRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        try
        {
            var bulletin = await svc.ForceAssignBulletinAsync(ControlNumber.Create(request.CtrlNbr), null, context.CancellationToken);
            var vacancy = await svc.GetVacancyAsync(bulletin.PositionVacancyCtrlNbr, context.CancellationToken);
            var tz = await GetWorkAreaTimeZoneAsync(vacancy.WorkAreaGroupCtrlNbr.Value, context.CancellationToken);
            var bidCount = await GetBidCountAsync(svc, bulletin.CtrlNbr, context.CancellationToken);
            var vacatedByName = await ResolveEmployeeNameAsync(vacancy.PreviousIncumbentCtrlNbr?.Value ?? 0, context.CancellationToken);
            var awardedName = await ResolveEmployeeNameAsync(bulletin.AwardedEmployeeCtrlNbr?.Value ?? 0, context.CancellationToken);
            var craftRoleCtrlNbr = await ResolveCraftRoleCtrlNbrAsync(vacancy, context.CancellationToken);
            var crewCtrlNbr = await ResolveCrewCtrlNbrAsync(vacancy, context.CancellationToken);
            return MapBulletin(bulletin, vacancy.TargetName, tz, bidCount, vacatedByName, awardedName, craftRoleCtrlNbr, vacancy.TargetType, crewCtrlNbr);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }
    public override async Task<BulletinResponse> SetBulletinNoBid(SetBulletinNoBidRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        try
        {
            var bulletin = await svc.SetBulletinNoBidAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            var vacancy = await svc.GetVacancyAsync(bulletin.PositionVacancyCtrlNbr, context.CancellationToken);
            var tz = await GetWorkAreaTimeZoneAsync(vacancy.WorkAreaGroupCtrlNbr.Value, context.CancellationToken);
            var vacatedByNameNb = await ResolveEmployeeNameAsync(vacancy.PreviousIncumbentCtrlNbr?.Value ?? 0, context.CancellationToken);
            // SetBulletinNoBid now automatically chains the force-assign process, so the bulletin may
            // come back Forced with an awarded employee — resolve the name so the response is complete.
            var awardedByNameNb = await ResolveEmployeeNameAsync(bulletin.AwardedEmployeeCtrlNbr?.Value ?? 0, context.CancellationToken);
            var craftRoleCtrlNbrNb = await ResolveCraftRoleCtrlNbrAsync(vacancy, context.CancellationToken);
            return MapBulletin(bulletin, vacancy.TargetName, tz, 0, vacatedByNameNb, awardedByNameNb, craftRoleCtrlNbrNb);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<BulletinResponse> CancelBulletin(CancelBulletinRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        try
        {
            var bulletin = await svc.CancelBulletinAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            var vacancy = await svc.GetVacancyAsync(bulletin.PositionVacancyCtrlNbr, context.CancellationToken);
            var tz = await GetWorkAreaTimeZoneAsync(vacancy.WorkAreaGroupCtrlNbr.Value, context.CancellationToken);
            var vacatedByName = await ResolveEmployeeNameAsync(vacancy.PreviousIncumbentCtrlNbr?.Value ?? 0, context.CancellationToken);
            var craftRoleCtrlNbr = await ResolveCraftRoleCtrlNbrAsync(vacancy, context.CancellationToken);
            return MapBulletin(bulletin, vacancy.TargetName, tz, 0, vacatedByName, craftRoleCtrlNbr: craftRoleCtrlNbr);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }

    public override async Task<BulletinBidResponse> SubmitBid(SubmitBidRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        try
        {
            var bid = await svc.SubmitBidAsync(
                request.BulletinCtrlNbr, request.EmployeeCtrlNbr, request.Priority, context.CancellationToken);
            return MapBid(bid);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }

    public override async Task<GetBulletinWinnerResponse> GetBulletinWinner(GetBulletinWinnerRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        try
        {
            var winner = await svc.GetBulletinWinnerAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            if (winner is null) return new GetBulletinWinnerResponse { HasWinner = false };

            var bulletin = await svc.GetBulletinAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            var empName = await ResolveEmployeeNameAsync(winner.EmployeeCtrlNbr.Value, context.CancellationToken);
            var senDate = await ResolveSeniorityDateAsync(winner.EmployeeCtrlNbr.Value, bulletin.CraftCtrlNbr.Value, context.CancellationToken);
            return new GetBulletinWinnerResponse
            {
                HasWinner = true,
                EmployeeCtrlNbr = winner.EmployeeCtrlNbr.Value,
                EmployeeName = empName,
                SeniorityDate = senDate,
                SeniorityRank = winner.SeniorityRank,
                Priority = winner.Priority
            };
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<BulletinBidResponse> WithdrawBid(WithdrawBidRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        try { return MapBid(await svc.WithdrawBidAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken)); }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<GetBidsResponse> GetBidsByBulletin(GetBidsByBulletinRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        var bids = await svc.GetBidsByBulletinAsync(ControlNumber.Create(request.BulletinCtrlNbr), context.CancellationToken);
        var response = new GetBidsResponse { TotalCount = bids.Count };
        // Resolve the craft ctrl nbr once from the bulletin for seniority date lookups
        long craftCtrlNbr = 0;
        if (bids.Count > 0)
        {
            try { var bulletin = await svc.GetBulletinAsync(ControlNumber.Create(request.BulletinCtrlNbr), context.CancellationToken); craftCtrlNbr = bulletin.CraftCtrlNbr.Value; }
            catch { /* ignore */ }
        }
        foreach (var b in bids)
        {
            var empName = await ResolveEmployeeNameAsync(b.EmployeeCtrlNbr.Value, context.CancellationToken);
            var senDate = await ResolveSeniorityDateAsync(b.EmployeeCtrlNbr.Value, craftCtrlNbr, context.CancellationToken);
            response.Bids.Add(MapBid(b, empName, senDate));
        }
        return response;
    }

    public override async Task<GetBidsResponse> GetBidsByEmployee(GetBidsByEmployeeRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        var bids = await svc.GetBidsByEmployeeAsync(ControlNumber.Create(request.EmployeeCtrlNbr), context.CancellationToken);
        var empName = await ResolveEmployeeNameAsync(request.EmployeeCtrlNbr, context.CancellationToken);
        var response = new GetBidsResponse { TotalCount = bids.Count };
        foreach (var b in bids)
        {
            long craftCtrlNbr = 0;
            try { var bulletin = await svc.GetBulletinAsync(b.BulletinCtrlNbr, context.CancellationToken); craftCtrlNbr = bulletin.CraftCtrlNbr.Value; }
            catch { /* ignore */ }
            var senDate = await ResolveSeniorityDateAsync(b.EmployeeCtrlNbr.Value, craftCtrlNbr, context.CancellationToken);
            response.Bids.Add(MapBid(b, empName, senDate));
        }
        return response;
    }

    public override async Task<BulletinRuleResponse> GetBulletinRule(GetBulletinRuleRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        try { return MapRule(await svc.GetBulletinRuleAsync(ControlNumber.Create(request.CraftCtrlNbr), context.CancellationToken)); }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<BulletinRuleResponse> SaveBulletinRule(SaveBulletinRuleRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        TimeSpan? cutOff = string.IsNullOrWhiteSpace(request.BulletinCutoffTime)
            ? null
            : TimeSpan.TryParse(request.BulletinCutoffTime, out var ts) ? ts : null;
        var rule = await svc.SaveBulletinRuleAsync(
            ControlNumber.Create(request.CraftCtrlNbr),
            request.BidWindowHours,
            TimeSpan.Parse(request.BidWindowStartTime),
            TimeSpan.Parse(request.BidWindowCloseTime),
            request.EffectiveOffsetDays,
            TimeSpan.Parse(request.EffectiveTime),
            request.ForceAssignHours,
            string.IsNullOrWhiteSpace(request.ForceAssignSelectionMode)
                ? Domain.Modules.Bulletins.ForceAssignSelectionMode.JuniorExtraBoard
                : request.ForceAssignSelectionMode,
            cutOff,
            string.IsNullOrWhiteSpace(request.EffectiveTimeMode)
                ? Domain.Modules.Bulletins.BulletinEffectiveTimeMode.FixedEffectiveTime
                : request.EffectiveTimeMode,
            context.CancellationToken);
        return MapRule(rule);
    }

    private PositionVacancyResponse MapVacancy(PositionVacancy v, TimeZoneInfo? tz = null, Bulletin? bulletin = null, long crewCtrlNbr = 0) => new()
    {
        CtrlNbr = v.CtrlNbr.Value,
        WorkAreaCtrlNbr = v.WorkAreaGroupCtrlNbr.Value,
        TargetType = v.TargetType,
        TargetCtrlNbr = v.TargetCtrlNbr.Value,
        CrewCtrlNbr = crewCtrlNbr,
        CraftCtrlNbr = v.CraftCtrlNbr.Value,
        VacancyReasonCode = v.VacancyReasonCode,
        PreviousIncumbentCtrlNbr = v.PreviousIncumbentCtrlNbr?.Value ?? 0,
        Status = v.Status,
        OpenedUtc = Clock.FormatLocalIso(v.OpenedUtc, tz),
        ClosedUtc = v.ClosedUtc.HasValue ? Clock.FormatLocalIso(v.ClosedUtc.Value, tz) : string.Empty,
        TargetName = v.TargetName,
        BulletinCtrlNbr = bulletin?.CtrlNbr.Value ?? 0,
        StatusBadge = Domain.Modules.Bulletins.BulletinStatusBadge.ForVacancy(v.Status),
        BulletinOpenWindowUtc = bulletin is null ? string.Empty : Clock.FormatLocalIso(bulletin.BidWindowOpensUtc, tz)
    };

    private async Task<long> ResolveCrewCtrlNbrAsync(PositionVacancy vacancy, CancellationToken ct)
    {
        try
        {
            var uowFactory = serviceProvider.GetRequiredService<Domain.Interfaces.IOrchestrationUnitOfWorkFactory>();
            await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
            // Crew vacancies are keyed to a staffable position ctrl nbr in most creation paths.
            // Resolve via staffable position first, then fall back to direct crew-position lookup.
            var crewPosition = await uow.CrewPositions.GetByStaffablePositionAsync(vacancy.TargetCtrlNbr)
                ?? await uow.CrewPositions.GetByCtrlNbrAsync(vacancy.TargetCtrlNbr, ct);
            return crewPosition?.CrewCtrlNbr.Value ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private BulletinResponse MapBulletin(Bulletin b, string positionName = "", TimeZoneInfo? tz = null, int bidCount = 0, string vacatedByName = "", string awardedEmployeeName = "", long craftRoleCtrlNbr = 0, string targetType = "", long crewCtrlNbr = 0) => new()
    {
        CtrlNbr = b.CtrlNbr.Value,
        PositionVacancyCtrlNbr = b.PositionVacancyCtrlNbr.Value,
        CraftCtrlNbr = b.CraftCtrlNbr.Value,
        BidWindowOpensUtc = Clock.FormatLocalIso(b.BidWindowOpensUtc, tz),
        BidWindowClosesUtc = Clock.FormatLocalIso(b.BidWindowClosesUtc, tz),
        EffectiveUtc = Clock.FormatLocalIso(b.EffectiveUtc, tz),
        Status = b.Status,
        AwardedEmployeeCtrlNbr = b.AwardedEmployeeCtrlNbr?.Value ?? 0,
        AwardType = b.AwardType ?? string.Empty,
        PositionName = positionName,
        BidCount = bidCount,
        VacatedByName = vacatedByName,
        AwardedEmployeeName = awardedEmployeeName,
        CraftRoleCtrlNbr = craftRoleCtrlNbr,
        ForceAssignDeadlineUtc = b.ForceAssignDeadlineUtc.HasValue ? Clock.FormatLocalIso(b.ForceAssignDeadlineUtc.Value, tz) : string.Empty,
        StatusBadge = Domain.Modules.Bulletins.BulletinStatusBadge.ForBulletin(b.Status),
        IsBidWindowOpen = b.IsBidWindowOpen(DateTime.UtcNow),
        TargetType = targetType,
        CrewCtrlNbr = crewCtrlNbr
    };

    /// <summary>
    /// Converts a UTC datetime to the work area's local time for display.
    /// Falls back to a UTC ISO-8601 string when no timezone is configured.
    /// </summary>
    private string FormatLocalTime(DateTime utc, TimeZoneInfo? tz) => Clock.FormatLocalIso(utc, tz);

    private async Task<(Dictionary<long, (string Name, long WorkAreaCtrlNbr, long CraftRoleCtrlNbr)> Vacancies, Dictionary<long, TimeZoneInfo?> Timezones)> BuildVacancyIndexAsync(
        Application.Bulletins.BulletinsService svc,
        IReadOnlyList<Bulletin> bulletins,
        CancellationToken ct)
    {
        var vacancyIds = bulletins.Select(b => b.PositionVacancyCtrlNbr).Distinct().ToList();
        var vacancyIndex = new Dictionary<long, (string Name, long WorkAreaCtrlNbr, long CraftRoleCtrlNbr)>(vacancyIds.Count);
        foreach (var id in vacancyIds)
        {
            try
            {
                var v = await svc.GetVacancyAsync(id, ct);
                var craftRoleCtrlNbr = await ResolveCraftRoleCtrlNbrAsync(v, ct);
                vacancyIndex[id.Value] = (v.TargetName, v.WorkAreaGroupCtrlNbr.Value, craftRoleCtrlNbr);
            }
            catch (KeyNotFoundException) { /* vacancy may have been removed */ }
        }

        // Resolve unique work-area timezones
        var tzIndex = new Dictionary<long, TimeZoneInfo?>();
        foreach (var (vacancyCtrlNbr, (_, workAreaCtrlNbr, _)) in vacancyIndex)
        {
            if (!tzIndex.ContainsKey(vacancyCtrlNbr))
                tzIndex[vacancyCtrlNbr] = await GetWorkAreaTimeZoneAsync(workAreaCtrlNbr, ct);
        }

        return (vacancyIndex, tzIndex);
    }

    private static BulletinBidResponse MapBid(BulletinBid b, string employeeName = "", string seniorityDate = "") => new()
    {
        CtrlNbr = b.CtrlNbr.Value,
        BulletinCtrlNbr = b.BulletinCtrlNbr.Value,
        EmployeeCtrlNbr = b.EmployeeCtrlNbr.Value,
        Priority = b.Priority,
        SubmittedUtc = b.SubmittedUtc.ToString("O"),
        SeniorityRank = b.SeniorityRank,
        Status = b.Status,
        EmployeeName = employeeName,
        SeniorityDate = !string.IsNullOrEmpty(seniorityDate) ? seniorityDate
                        : b.SeniorityDate != default ? b.SeniorityDate.ToString("MM/dd/yyyy") : string.Empty,
        StatusBadge = Domain.Modules.Bulletins.BulletinStatusBadge.ForBid(b.Status)
    };

    private static BulletinRuleResponse MapRule(BulletinRule r) => new()
    {
        CtrlNbr = r.CtrlNbr.Value,
        CraftCtrlNbr = r.CraftCtrlNbr.Value,
        BidWindowHours = r.BidWindowHours,
        BidWindowStartTime = r.BidWindowStartTime.ToString(),
        BidWindowCloseTime = r.BidWindowCloseTime.ToString(),
        EffectiveOffsetDays = r.EffectiveOffsetDays,
        EffectiveTime = r.EffectiveTime.ToString(),
        ForceAssignHours = r.ForceAssignHours,
        ForceAssignSelectionMode = r.ForceAssignSelectionMode,
        BulletinCutoffTime = r.BulletinCutOffTime.HasValue ? r.BulletinCutOffTime.Value.ToString() : string.Empty,
        EffectiveTimeMode = r.EffectiveTimeMode
    };

    /// <summary>
    /// Looks up the TimeZoneInfo for a work area group. Returns null if no timezone is configured
    /// or the zone id is unrecognised — callers fall back to UTC in that case.
    /// </summary>

    private async Task<long> ResolveCraftRoleCtrlNbrAsync(Domain.Modules.Bulletins.PositionVacancy vacancy, CancellationToken ct)
    {
        try
        {
            var uowFactory = serviceProvider.GetRequiredService<Domain.Interfaces.IOrchestrationUnitOfWorkFactory>();
            await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
            var roles = await uow.CraftRoles.GetByCraftAsync(vacancy.CraftCtrlNbr);
            return roles.FirstOrDefault()?.CtrlNbr.Value ?? 0;
        }
        catch { return 0; }
    }

    private async Task<string> ResolveEmployeeNameAsync(long ctrlNbr, CancellationToken ct)
    {
        _ = ct;
        if (ctrlNbr <= 0) return string.Empty;
        try
        {
            var nameSvc = serviceProvider.GetRequiredService<EmployeeNameService>();
            return await nameSvc.GetFullNameLnfAsync(Domain.ValueObjects.ControlNumber.Create(ctrlNbr));
        }
        catch { return string.Empty; }
    }

    private static async Task<int> GetBidCountAsync(Application.Bulletins.BulletinsService svc, Domain.ValueObjects.ControlNumber bulletinCtrlNbr, CancellationToken ct)
    {
        try { return (await svc.GetBidsByBulletinAsync(bulletinCtrlNbr, ct)).Count; }
        catch { return 0; }
    }

    private async Task<string> ResolveSeniorityDateAsync(long employeeCtrlNbr, long craftCtrlNbr, CancellationToken ct)
    {
        if (employeeCtrlNbr <= 0) return string.Empty;
        try
        {
            var uowFactory = serviceProvider.GetRequiredService<Domain.Interfaces.IOrchestrationUnitOfWorkFactory>();
            await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
            var entries = await uow.Seniority.GetByEmployeeCtrlNbrAsync(Domain.ValueObjects.ControlNumber.Create(employeeCtrlNbr));
            if (craftCtrlNbr > 0)
            {
                var rosters = await uow.Rosters.GetByCraftCtrlNbrAsync(Domain.ValueObjects.ControlNumber.Create(craftCtrlNbr));
                var rosterCtrlNbrs = rosters.Select(r => r.CtrlNbr).ToHashSet();
                var match = entries.FirstOrDefault(s => rosterCtrlNbrs.Contains(s.RosterCtrlNbr) && s.LastActiveRoster)
                            ?? entries.FirstOrDefault(s => rosterCtrlNbrs.Contains(s.RosterCtrlNbr));
                if (match is not null) return match.RosterDate.ToString("MM/dd/yyyy");
            }
            var active = entries.FirstOrDefault(s => s.LastActiveRoster);
            return active is not null ? active.RosterDate.ToString("MM/dd/yyyy") : string.Empty;
        }
        catch { return string.Empty; }
    }
    private Task<TimeZoneInfo?> GetWorkAreaTimeZoneAsync(long workAreaCtrlNbr, CancellationToken ct) =>
        Clock.GetWorkAreaTimeZoneAsync(ControlNumber.Create(workAreaCtrlNbr), ct);

    /// <summary>
    /// Parses a datetime string that may be in local work-area time and converts it to UTC.
    /// If no timezone is configured, the value is parsed as-is (assumed UTC).
    /// </summary>
    private DateTime ParseAsUtc(string value, TimeZoneInfo? tz) => Clock.ParseToUtc(value, tz);

    public override async Task<GetForceAssignCandidateResponse> GetForceAssignCandidate(GetForceAssignCandidateRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        try
        {
            var candidate = await svc.GetForceAssignCandidateAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            var name = candidate is not null ? await ResolveEmployeeNameAsync(candidate.Value, context.CancellationToken) : string.Empty;
            return new GetForceAssignCandidateResponse
            {
                EmployeeCtrlNbr = candidate?.Value ?? 0,
                EmployeeName = name
            };
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<GetNextBulletinEventResponse> GetNextBulletinEvent(GetNextBulletinEventRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        var (nextUtc, workAreaCtrlNbr) = await svc.GetNextBulletinEventAsync(context.CancellationToken);
        if (!nextUtc.HasValue)
            return new GetNextBulletinEventResponse { NextEventUtc = string.Empty };

        // Resolve timezone from the bulletin's own work area, exactly as MapBulletin does
        TimeZoneInfo? tz = workAreaCtrlNbr.HasValue
            ? await GetWorkAreaTimeZoneAsync(workAreaCtrlNbr.Value, context.CancellationToken)
            : null;

        return new GetNextBulletinEventResponse { NextEventUtc = FormatLocalTime(nextUtc.Value, tz) };
    }
}
