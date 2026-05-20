using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class BulletinsService(IServiceProvider serviceProvider) : BulletinsSrvc.BulletinsSrvcBase
{
    public override async Task<GetVacanciesResponse> GetOpenVacancies(GetOpenVacanciesRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        var vacancies = await svc.GetOpenVacanciesAsync(context.CancellationToken);
        var response = new GetVacanciesResponse { TotalCount = vacancies.Count };
        foreach (var v in vacancies)
        {
            var tz = await GetWorkAreaTimeZoneAsync(v.WorkAreaGroupCtrlNbr.Value, context.CancellationToken);
            var bulletin = await svc.GetBulletinByVacancyAsync(v.CtrlNbr, context.CancellationToken);
            response.Vacancies.Add(MapVacancy(v, tz, bulletin?.CtrlNbr.Value ?? 0));
        }
        return response;
    }

    public override async Task<GetVacanciesResponse> GetVacanciesByWorkArea
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        var vacancies = await svc.GetVacanciesByWorkAreaAsync(ControlNumber.Create(request.WorkAreaCtrlNbr), context.CancellationToken);
        var tz = await GetWorkAreaTimeZoneAsync(request.WorkAreaCtrlNbr, context.CancellationToken);
        var response = new GetVacanciesResponse { TotalCount = vacancies.Count };
        foreach (var v in vacancies)
        {
            var bulletin = await svc.GetBulletinByVacancyAsync(v.CtrlNbr, context.CancellationToken);
            response.Vacancies.Add(MapVacancy(v, tz, bulletin?.CtrlNbr.Value ?? 0));
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
            return MapVacancy(v, tz);
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
            Vacancy = MapVacancy(vacancy, tz),
            Bulletin = bulletin is not null ? MapBulletin(bulletin, vacancy.TargetName, tz) : null
        };
    }

    public override async Task<PositionVacancyResponse> AbolishVacancy(AbolishVacancyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        try
        {
            var v = await svc.AbolishVacancyAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            var tz = await GetWorkAreaTimeZoneAsync(v.WorkAreaGroupCtrlNbr.Value, context.CancellationToken);
            return MapVacancy(v, tz);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<GetBulletinsResponse> GetPostedBulletins(GetPostedBulletinsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        var bulletins = await svc.GetPostedBulletinsAsync(context.CancellationToken);
        var (vacancyIndex, tzIndex) = await BuildVacancyIndexAsync(svc, bulletins, context.CancellationToken);
        var response = new GetBulletinsResponse { TotalCount = bulletins.Count };
        foreach (var b in bulletins)
        {
            var posName = vacancyIndex.TryGetValue(b.PositionVacancyCtrlNbr.Value, out var vi) ? vi.Name : string.Empty;
            response.Bulletins.Add(MapBulletin(b, posName, tzIndex.GetValueOrDefault(b.PositionVacancyCtrlNbr.Value)));
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
            response.Bulletins.Add(MapBulletin(b, posName, tzIndex.GetValueOrDefault(b.PositionVacancyCtrlNbr.Value)));
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
            response.Bulletins.Add(MapBulletin(b, posName, tz));
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
            return MapBulletin(bulletin, vacancy.TargetName, tz);
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
            return MapBulletin(bulletin, vacancy.TargetName, tz);
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
            return MapBulletin(bulletin, vacancy.TargetName, tz);
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
            return MapBulletin(bulletin, vacancy.TargetName, tz);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<BulletinResponse> SetBulletinNoBid(SetBulletinNoBidRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        try
        {
            var bulletin = await svc.SetBulletinNoBidAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            var vacancy = await svc.GetVacancyAsync(bulletin.PositionVacancyCtrlNbr, context.CancellationToken);
            var tz = await GetWorkAreaTimeZoneAsync(vacancy.WorkAreaGroupCtrlNbr.Value, context.CancellationToken);
            return MapBulletin(bulletin, vacancy.TargetName, tz);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<BulletinBidResponse> SubmitBid(SubmitBidRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        var bid = await svc.SubmitBidAsync(
            request.BulletinCtrlNbr, request.EmployeeCtrlNbr, request.Priority, request.SeniorityRank, context.CancellationToken);
        return MapBid(bid);
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
        foreach (var b in bids) response.Bids.Add(MapBid(b));
        return response;
    }

    public override async Task<GetBidsResponse> GetBidsByEmployee(GetBidsByEmployeeRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        var bids = await svc.GetBidsByEmployeeAsync(ControlNumber.Create(request.EmployeeCtrlNbr), context.CancellationToken);
        var response = new GetBidsResponse { TotalCount = bids.Count };
        foreach (var b in bids) response.Bids.Add(MapBid(b));
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
            context.CancellationToken);
        return MapRule(rule);
    }

    private static PositionVacancyResponse MapVacancy(PositionVacancy v, TimeZoneInfo? tz = null, long bulletinCtrlNbr = 0) => new()
    {
        CtrlNbr = v.CtrlNbr.Value,
        WorkAreaCtrlNbr = v.WorkAreaGroupCtrlNbr.Value,
        TargetType = v.TargetType,
        TargetCtrlNbr = v.TargetCtrlNbr.Value,
        CraftCtrlNbr = v.CraftCtrlNbr.Value,
        VacancyReasonCode = v.VacancyReasonCode,
        PreviousIncumbentCtrlNbr = v.PreviousIncumbentCtrlNbr?.Value ?? 0,
        Status = v.Status,
        OpenedUtc = FormatLocalTime(v.OpenedUtc, tz),
        ClosedUtc = v.ClosedUtc.HasValue ? FormatLocalTime(v.ClosedUtc.Value, tz) : string.Empty,
        TargetName = v.TargetName,
        BulletinCtrlNbr = bulletinCtrlNbr
    };

    private static BulletinResponse MapBulletin(Bulletin b, string positionName = "", TimeZoneInfo? tz = null) => new()
    {
        CtrlNbr = b.CtrlNbr.Value,
        PositionVacancyCtrlNbr = b.PositionVacancyCtrlNbr.Value,
        CraftCtrlNbr = b.CraftCtrlNbr.Value,
        BidWindowOpensUtc = FormatLocalTime(b.BidWindowOpensUtc, tz),
        BidWindowClosesUtc = FormatLocalTime(b.BidWindowClosesUtc, tz),
        EffectiveUtc = FormatLocalTime(b.EffectiveUtc, tz),
        Status = b.Status,
        AwardedEmployeeCtrlNbr = b.AwardedEmployeeCtrlNbr?.Value ?? 0,
        AwardType = b.AwardType ?? string.Empty,
        PositionName = positionName
    };

    /// <summary>
    /// Converts a UTC datetime to the work area's local time for display.
    /// Falls back to a UTC ISO-8601 string when no timezone is configured.
    /// </summary>
    private static string FormatLocalTime(DateTime utc, TimeZoneInfo? tz)
    {
        if (tz is null) return utc.ToString("O");
        var local = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utc, DateTimeKind.Utc), tz);
        return local.ToString("O");
    }

    private async Task<(Dictionary<long, (string Name, long WorkAreaCtrlNbr)> Vacancies, Dictionary<long, TimeZoneInfo?> Timezones)> BuildVacancyIndexAsync(
        Application.Bulletins.BulletinsService svc,
        IReadOnlyList<Bulletin> bulletins,
        CancellationToken ct)
    {
        var vacancyIds = bulletins.Select(b => b.PositionVacancyCtrlNbr).Distinct().ToList();
        var vacancyIndex = new Dictionary<long, (string Name, long WorkAreaCtrlNbr)>(vacancyIds.Count);
        foreach (var id in vacancyIds)
        {
            try
            {
                var v = await svc.GetVacancyAsync(id, ct);
                vacancyIndex[id.Value] = (v.TargetName, v.WorkAreaGroupCtrlNbr.Value);
            }
            catch (KeyNotFoundException) { /* vacancy may have been removed */ }
        }

        // Resolve unique work-area timezones
        var tzIndex = new Dictionary<long, TimeZoneInfo?>();
        foreach (var (vacancyCtrlNbr, (_, workAreaCtrlNbr)) in vacancyIndex)
        {
            if (!tzIndex.ContainsKey(vacancyCtrlNbr))
                tzIndex[vacancyCtrlNbr] = await GetWorkAreaTimeZoneAsync(workAreaCtrlNbr, ct);
        }

        return (vacancyIndex, tzIndex);
    }

    private static BulletinBidResponse MapBid(BulletinBid b) => new()
    {
        CtrlNbr = b.CtrlNbr.Value,
        BulletinCtrlNbr = b.BulletinCtrlNbr.Value,
        EmployeeCtrlNbr = b.EmployeeCtrlNbr.Value,
        Priority = b.Priority,
        SubmittedUtc = b.SubmittedUtc.ToString("O"),
        SeniorityRank = b.SeniorityRank,
        Status = b.Status
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
        ForceAssignSelectionMode = r.ForceAssignSelectionMode
    };

    /// <summary>
    /// Looks up the TimeZoneInfo for a work area group. Returns null if no timezone is configured
    /// or the zone id is unrecognised — callers fall back to UTC in that case.
    /// </summary>
    private async Task<TimeZoneInfo?> GetWorkAreaTimeZoneAsync(long workAreaCtrlNbr, CancellationToken ct)
    {
        try
        {
            var tcSvc = serviceProvider.GetRequiredService<Application.TenantConfig.TenantConfigService>();
            var workArea = await tcSvc.GetGroupAsync(ControlNumber.Create(workAreaCtrlNbr), ct);
            if (string.IsNullOrWhiteSpace(workArea?.TimeZoneId)) return null;
            return TimeZoneInfo.FindSystemTimeZoneById(workArea.TimeZoneId);
        }
        catch { return null; }
    }

    /// <summary>
    /// Parses a datetime string that may be in local work-area time and converts it to UTC.
    /// If no timezone is configured, the value is parsed as-is (assumed UTC).
    /// </summary>
    private static DateTime ParseAsUtc(string value, TimeZoneInfo? tz)
    {
        var dt = DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
        if (tz is null || dt.Kind == DateTimeKind.Utc) return dt.ToUniversalTime();
        // Input is local — convert to UTC
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dt, DateTimeKind.Unspecified), tz);
    }
}
