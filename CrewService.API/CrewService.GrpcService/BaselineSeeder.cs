using CrewService.Domain.Models.UserAccess;
using CrewService.Application.FraCompliance;
using CrewService.Domain.Modules.Authorization;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Infrastructure.Models.UserAccount;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
namespace CrewService.GrpcService;

/// <summary>
/// Seeds baseline data required in all environments (dev, staging, production).
/// Ensures system roles, features, default permissions, and system GroupTypes exist.
/// Idempotent -- safe to call on every startup.
/// </summary>
internal static class BaselineSeeder
{
    // -- System role definitions -----------------------------------------
    private static readonly (string Name, string? Description, int Level, bool IsSystem)[] SystemRoles =
    [
        ("SystemAdmin", "Full system access across all parents and railroads", 100, true),
        ("ParentAdmin", "Parent company administrator", 80, true),
        ("RailroadAdmin", "Railroad-level administrator", 60, true),
        ("CraftManager", "Manages crafts and seniority", 40, false),
        ("CrewManager", "Manages crew staffing and assignments", 40, false),
        ("Dispatcher", "Manages daily operations and vacancy resolution", 40, false),
        ("PayrollClerk", "Manages payroll and pay rates", 40, false),
        ("Employee", "Standard employee access", 20, true)
    ];

    // -- Feature definitions ---------------------------------------------
    private static readonly (string Key, string DisplayName, string Category, string Route)[] SystemFeatures =
    [
        // Daily Operations
                ("daily-operations/call-sheet", "Call Sheet", "Daily Operations", "/daily-operations/call-sheet"),
        ("daily/call-board", "Call Board", "Daily Operations", "/daily/call-board"),
        ("daily/assignments", "Assignments", "Daily Operations", "/daily/assignments"),
        ("daily/mark-offs", "Mark-Offs", "Daily Operations", "/daily/mark-offs"),
        ("daily/duty-status", "On-Duty / Off-Duty", "Daily Operations", "/daily/duty-status"),
        ("daily/vacancy-resolution", "Vacancy Resolution", "Daily Operations", "/daily/vacancy-resolution"),

        // Crew Staffing
        ("staffing/crews", "Crews", "Crew Staffing", "/staffing/crews"),
        ("staffing/extra-boards", "Extra Boards", "Crew Staffing", "/staffing/extra-boards"),
        ("staffing/bulletins", "Bulletins & Bids", "Crew Staffing", "/staffing/bulletins"),
        ("staffing/roster-boards", "Roster Boards", "Crew Staffing", "/staffing/roster-boards"),
        ("staffing/seniority-moves", "Seniority Moves", "Crew Staffing", "/staffing/seniority-moves"),

        // Payroll
        ("payroll/dashboard", "Payroll Dashboard", "Payroll", "/payroll"),
        ("payroll/rates", "Pay Rates", "Payroll", "/payroll/rates"),
        ("payroll/earning-codes", "Earning Codes", "Payroll", "/payroll/earning-codes"),
        ("payroll/holidays", "Holidays", "Payroll", "/payroll/holidays"),
        ("payroll/export", "Export / Import", "Payroll", "/payroll/export"),

        // Compliance
        ("compliance/fra", "FRA Compliance", "Compliance", "/compliance/fra"),
        ("compliance/fra-settings", "FRA Settings", "Compliance", "/compliance/fra-settings"),
        ("compliance/drug-alcohol", "Drug & Alcohol", "Compliance", "/compliance/drug-alcohol"),
        ("compliance/safety", "Safety Observations", "Compliance", "/compliance/safety"),
        ("compliance/absence-codes", "Absence Codes", "Compliance", "/compliance/absence-codes"),
        ("compliance/policies", "Policies", "Compliance", "/compliance/policies"),

        // Work Management
        ("work-management/departments", "Departments", "Work Management", "/work-management/departments"),
        ("work-management/crafts", "Crafts", "Work Management", "/work-management/crafts"),
        ("work-management/assignment-templates", "Assignment Templates", "Work Management", "/work-management/assignment-templates"),
        ("work-management/craft-roles", "Craft Roles", "Work Management", "/work-management/craft-roles"),
        ("work-management/shift-definitions", "Shift Definitions", "Work Management", "/work-management/shift-definitions"),
        ("work-management/rosters", "Rosters", "Work Management", "/work-management/rosters"),
        ("work-management/seniority-states", "Seniority States", "Work Management", "/work-management/seniority-states"),
        ("work-management/group-types", "Group Types", "Work Management", "/work-management/group-types"),
        ("work-management/qualification-types", "Qualification Types", "Work Management", "/work-management/qualification-types"),

        // Employee Management
        ("employees", "Employees", "Employee Management", "/employees"),
        ("employees/seniority", "Seniority Rosters", "Employee Management", "/employees/seniority"),
        ("employees/seniority-states", "Seniority States", "Employee Management", "/employees/seniority-states"),
        ("employees/prior-service", "Prior Service Credits", "Employee Management", "/employees/prior-service"),
        ("employees/qualifications", "Qualifications", "Employee Management", "/employees/qualifications"),
        ("employees/notifications", "Notifications", "Employee Management", "/notifications/review"),
        ("admin/invitations", "Invitations", "Employee Management", "/admin/invitations"),

        // Information
        ("info/railroad", "Railroad Info", "Information", "/info/railroad"),
        ("info/reports", "Reports", "Information", "/info/reports"),

        // Administration
        ("parents", "Parents", "Administration", "/parents"),
        ("admin/users", "User Assignments", "Administration", "/admin/users"),
        ("admin/notifications", "Notification Config", "Administration", "/admin/notifications"),
        ("admin/jobs", "Background Jobs", "Administration", "/admin/jobs"),
        ("admin/roles", "Roles", "Administration", "/admin/roles"),
        ("admin/permissions", "Permissions", "Administration", "/admin/permissions"),
        ("admin/audit-log", "Audit Log", "Administration", "/admin/audit-log"),
        ("admin/required-positions-strategies", "Position Formulas", "Administration", "/admin/required-positions-strategies"),
        ("admin/seniority-vacancy-configs", "Vacancy Actions", "Administration", "/admin/seniority-vacancy-configs"),
        ("admin/seniority-move-policies", "Seniority Move Policies", "Administration", "/admin/seniority-move-policies"),
        ("admin/call-sheet-rules", "Call Sheet Rules", "Administration", "/admin/call-sheet-rules"),
        ("employees/scheduled-state-changes", "Scheduled State Changes", "Employee Management", "/employees/scheduled-state-changes")
    ];

    // -- Default permission mapping --------------------------------------
    // For each feature key, the roles that receive FullAccess.
    // Any role not listed for a feature gets AccessLevel.None.
    private static readonly Dictionary<string, string[]> FeatureFullAccessRoles = new()
    {
        // Daily Operations
                ["daily-operations/call-sheet"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CrewManager", "Dispatcher"],
        ["daily/call-board"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CrewManager", "Dispatcher"],
        ["daily/assignments"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CrewManager", "Dispatcher"],
        ["daily/mark-offs"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CrewManager", "Dispatcher"],
        ["daily/duty-status"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "Dispatcher"],
        ["daily/vacancy-resolution"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "Dispatcher"],

        // Crew Staffing
        ["staffing/crews"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CrewManager"],
        ["staffing/extra-boards"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CrewManager"],
        ["staffing/bulletins"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CrewManager"],
        ["staffing/roster-boards"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CrewManager"],
        ["staffing/seniority-moves"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CrewManager", "CraftManager"],
        // Payroll
        ["payroll/dashboard"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "PayrollClerk"],
        ["payroll/rates"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "PayrollClerk"],
        ["payroll/earning-codes"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "PayrollClerk"],
        ["payroll/holidays"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "PayrollClerk"],
        ["payroll/export"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "PayrollClerk"],

        // Compliance
        ["compliance/fra"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CraftManager"],
        ["compliance/fra-settings"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],
        ["compliance/drug-alcohol"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CraftManager"],
        ["compliance/safety"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CraftManager"],
        ["compliance/absence-codes"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],
        ["compliance/policies"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],

        // Work Management
        ["work-management/departments"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],
        ["work-management/crafts"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CraftManager"],
        ["work-management/assignment-templates"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CrewManager"],
        ["work-management/craft-roles"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CraftManager"],
        ["work-management/shift-definitions"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CraftManager"],
        ["work-management/rosters"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CraftManager"],
        ["work-management/seniority-states"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],
        ["work-management/group-types"] = ["SystemAdmin", "ParentAdmin"],
        ["work-management/qualification-types"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CraftManager"],

        // Employee Management
        ["employees"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CraftManager"],
        ["employees/seniority"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CraftManager"],
        ["employees/seniority-states"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],
        ["employees/prior-service"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],
        ["employees/qualifications"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CraftManager"],
        ["employees/notifications"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CrewManager", "Dispatcher"],
        ["admin/invitations"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],

        // Information -- all roles
        ["info/railroad"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CraftManager", "CrewManager", "Dispatcher", "PayrollClerk", "Employee"],
        ["info/reports"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CraftManager", "CrewManager", "Dispatcher", "PayrollClerk", "Employee"],

        // Administration
        ["parents"] = ["SystemAdmin"],
        ["admin/users"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],
        ["admin/notifications"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],
        ["admin/jobs"] = ["SystemAdmin"],
        ["admin/roles"] = ["SystemAdmin"],
        ["admin/permissions"] = ["SystemAdmin"],
        ["admin/audit-log"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],
        ["admin/required-positions-strategies"] = ["SystemAdmin"],
        ["admin/seniority-vacancy-configs"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],
        ["admin/seniority-move-policies"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],
        ["admin/call-sheet-rules"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],
        ["employees/scheduled-state-changes"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"]
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        // Provide a synthetic SYSTEM user so auditing works outside an HTTP request
        var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
        httpContextAccessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "SYSTEM")], "Seed"))
        };

        await BackfillGroupPathsAsync(sp);
        await SeedRolesAsync(sp);
        await SeedFeaturesAsync(sp);
        await SeedDefaultPermissionsAsync(sp);
        await SeedRegulatoryQualificationsAsync(sp);
        await SeedSystemAdminAsync(sp);
        await SeedStaticRequiredPositionsStrategyAsync(sp);
    }

    private static async Task SeedRegulatoryQualificationsAsync(IServiceProvider sp)
    {
        var repository = sp.GetRequiredService<IRegulatoryQualificationRepository>();

        var seeds = new[]
        {
            new
            {
                Code = "CFR-240-ENGINEER",
                CfrPart = "49 CFR Part 240",
                Description = "Locomotive Engineer Certification"
            },
            new
            {
                Code = "CFR-242-CONDUCTOR",
                CfrPart = "49 CFR Part 242",
                Description = "Conductor Certification"
            },
            new
            {
                Code = "CFR-242-SWITCHMAN",
                CfrPart = "49 CFR Part 242",
                Description = "Switchman Certification"
            }
        };

        foreach (var seed in seeds)
        {
            var existing = await repository.GetByCodeAsync(seed.Code);
            if (existing is not null)
                continue;

            var qualification = RegulatoryQualification.Create(
                code: seed.Code,
                cfrPart: seed.CfrPart,
                description: seed.Description,
                requiresCertification: true,
                recertificationIntervalMonths: 36,
                effectiveDate: DateOnly.FromDateTime(DateTime.UtcNow.Date));

            await repository.AddAsync(qualification);
        }
    }

    private static async Task SeedSystemAdminAsync(IServiceProvider sp)
    {
        var userMgr = sp.GetRequiredService<UserManager<User>>();
        var adminUser = await userMgr.FindByEmailAsync("admin@crewservice.dev");
        if (adminUser is null)
        {
            adminUser = new User
            {
                UserName       = "admin@crewservice.dev",
                Email          = "admin@crewservice.dev",
                EmailConfirmed = true,
                FirstName      = "System",
                LastName       = "Admin",
                FullName       = "System Admin",
                FullNameLNF    = "Admin, System",
                PrimaryRoleId  = Roles.SystemAdmin
            };
            await userMgr.CreateAsync(adminUser, "Admin@123");
        }
    }

    private static async Task BackfillGroupPathsAsync(IServiceProvider sp)
    {
        var dynamicGroupRepo = sp.GetRequiredService<IDynamicGroupRepository>();
        await dynamicGroupRepo.BackfillPathsAsync();
    }

    private static async Task SeedRolesAsync(IServiceProvider sp)
    {
        var roleRepo = sp.GetRequiredService<IRoleRepository>();
        var existingRoles = await roleRepo.GetAllAsync();

        foreach (var (name, description, level, isSystem) in SystemRoles)
        {
            var existing = existingRoles.FirstOrDefault(r => r.Name == name);
            if (existing is null)
            {
                await roleRepo.AddAsync(Role.Create(name, description, isSystem: isSystem, level));
            }
            else if (existing.IsSystem != isSystem)
            {
                existing.SetProtection(isSystem);
                await roleRepo.UpdateAsync(existing);
            }
        }
    }

    private static async Task SeedFeaturesAsync(IServiceProvider sp)
    {
        var featureRepo = sp.GetRequiredService<IFeatureRepository>();
        var existingFeatures = await featureRepo.GetAllAsync();

        foreach (var (key, displayName, category, route) in SystemFeatures)
        {
            if (!existingFeatures.Any(f => f.Key == key))
            {
                await featureRepo.AddAsync(Feature.Create(key, displayName, category, route));
            }
        }
    }

    private static async Task SeedDefaultPermissionsAsync(IServiceProvider sp)
    {
        var roleRepo = sp.GetRequiredService<IRoleRepository>();
        var featureRepo = sp.GetRequiredService<IFeatureRepository>();
        var permissionRepo = sp.GetRequiredService<IPermissionRepository>();

        var allRoles = await roleRepo.GetAllAsync();
        var allFeatures = await featureRepo.GetAllAsync();
        var existingPermissions = await permissionRepo.GetAllAsync();

        foreach (var feature in allFeatures)
        {
            if (!FeatureFullAccessRoles.TryGetValue(feature.Key, out var fullAccessRoleNames))
                continue;

            foreach (var role in allRoles)
            {
                // Only seed global defaults (ParentCtrlNbr = null)
                if (existingPermissions.Any(p =>
                    p.RoleCtrlNbr == role.CtrlNbr &&
                    p.FeatureCtrlNbr == feature.CtrlNbr &&
                    p.ParentCtrlNbr == null))
                    continue;

                var accessLevel = fullAccessRoleNames.Contains(role.Name)
                    ? AccessLevel.FullAccess
                    : AccessLevel.None;

                await permissionRepo.AddAsync(
                    Permission.Create(role.CtrlNbr, feature.CtrlNbr, accessLevel));
            }
        }
    }

    private static async Task SeedStaticRequiredPositionsStrategyAsync(IServiceProvider sp)
    {
        var repo = sp.GetRequiredService<IRequiredPositionsStrategyRepository>();

        var existing = await repo.GetStaticAsync();
        if (existing is not null)
            return;

        await repo.AddAsync(RequiredPositionsStrategy.Create(
            code: "STATIC",
            name: "Static",
            description: "Fixed required-position count set manually per board. Default for all crafts.",
            formulaType: "Static"));
    }
}
