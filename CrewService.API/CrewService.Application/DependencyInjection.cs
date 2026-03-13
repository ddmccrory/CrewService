using CrewService.Application.DailyOperations;
using CrewService.Application.ElectronicCalling;
using CrewService.Application.ElectronicCalling.Providers;
using CrewService.Application.FraCompliance;
using CrewService.Application.HolidayManagement;
using CrewService.Application.MarkOff;
using CrewService.Application.Payroll;
using CrewService.Application.ReportingExports;
using CrewService.Application.ReportingExports.Formatters;
using CrewService.Application.RosterBoardOps;
using CrewService.Application.VacancyAssignment;
using CrewService.Application.VacancyAssignment.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // B01 – FRA Compliance
        services.AddScoped<FraRecordSearchService>();
        services.AddScoped<FraDutyTourCalculator>();
        services.AddScoped<FraRestValidator>();
        services.AddScoped<FraConsecutiveDayTracker>();
        services.AddScoped<FraExcessServiceDetector>();
        services.AddScoped<FraMonthlyCapTracker>();

        // B02 – Daily Operations
        services.AddScoped<OnDutyPlacementService>();
        services.AddScoped<TieUpService>();

        // B03 – Mark-Off
        services.AddScoped<AutoMarkUpService>();
        services.AddScoped<CompensationBalanceService>();

        // B04 – Vacancy Assignment
        services.AddScoped<VacancyResolutionEngine>();
        services.AddScoped<IAssignmentStrategy, StandardAssignmentStrategy>();
        services.AddScoped<ISkipRule, WorkedCapRule>();
        services.AddScoped<ISkipRule, AlreadyOnDutyRule>();
        services.AddScoped<ISkipRule, AvailabilityRule>();
        services.AddScoped<ISkipRule, RestRule>();
        services.AddScoped<ISkipRule, MarkOffRule>();
        services.AddScoped<ISkipRule, QualificationRule>();
        services.AddScoped<ISkipRule, WeeklyHoursCapRule>();

        // B05 – Payroll Engine
        services.AddScoped<EarningCodeResolver>();
        services.AddScoped<PayrollPeriodService>();

        // B06 – Electronic Calling
        services.AddScoped<CrewCallingService>();
        services.AddScoped<ICrewNotificationProvider, MockNotificationProvider>();
        services.AddScoped<CallSheetGenerationService>();

        // B08 – Roster Board Ops
        services.AddScoped<HangoutProcessingService>();
        services.AddScoped<DailyStatusSnapshotService>();

        // B09 – Holiday services
        services.AddScoped<HolidayAutoGenerationService>();
        services.AddScoped<HolidayQualificationService>();
        services.AddScoped<HolidayPayrollGenerationService>();

        // B10 – Reporting & Exports
        services.AddScoped<IPayrollExportFormatter, AdpExportFormatter>();
        services.AddScoped<IPayrollExportFormatter, UkgExportFormatter>();
        services.AddScoped<PayrollExportService>();

        return services;
    }
}
