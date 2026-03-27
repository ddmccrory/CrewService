using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.Modules.Authorization;
using CrewService.Domain.Modules.Employees;
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

        // Payroll
        ("payroll/dashboard", "Payroll Dashboard", "Payroll", "/payroll"),
        ("payroll/rates", "Pay Rates", "Payroll", "/payroll/rates"),
        ("payroll/earning-codes", "Earning Codes", "Payroll", "/payroll/earning-codes"),
        ("payroll/holidays", "Holidays", "Payroll", "/payroll/holidays"),
        ("payroll/export", "Export / Import", "Payroll", "/payroll/export"),

        // Compliance
        ("compliance/fra", "FRA Compliance", "Compliance", "/compliance/fra"),
        ("compliance/safety", "Safety Observations", "Compliance", "/compliance/safety"),
        ("compliance/absence-codes", "Absence Codes", "Compliance", "/compliance/absence-codes"),
        ("compliance/policies", "Policies", "Compliance", "/compliance/policies"),

        // Work Management
        ("work-management/departments", "Departments", "Work Management", "/work-management/departments"),
        ("work-management/crafts", "Crafts", "Work Management", "/work-management/crafts"),
        ("work-management/assignment-templates", "Assignment Templates", "Work Management", "/work-management/assignment-templates"),
        ("work-management/position-roles", "Position Roles", "Work Management", "/work-management/position-roles"),

        // Employee Management
        ("employees", "Employees", "Employee Management", "/employees"),
        ("employees/seniority", "Seniority Rosters", "Employee Management", "/employees/seniority"),
        ("employees/prior-service", "Prior Service Credits", "Employee Management", "/employees/prior-service"),
        ("admin/invitations", "Invitations", "Employee Management", "/admin/invitations"),

        // Information
        ("info/railroad", "Railroad Info", "Information", "/info/railroad"),
        ("info/reports", "Reports", "Information", "/info/reports"),

        // Administration
        ("parents", "Parents", "Administration", "/parents"),
        ("config/group-types", "Group Types", "Administration", "/config/group-types"),
        ("admin/users", "User Assignments", "Administration", "/admin/users"),
        ("admin/notifications", "Notification Config", "Administration", "/admin/notifications"),
        ("admin/jobs", "Background Jobs", "Administration", "/admin/jobs"),
        ("admin/roles", "Roles", "Administration", "/admin/roles"),
        ("admin/permissions", "Permissions", "Administration", "/admin/permissions")
    ];

    // -- Default permission mapping --------------------------------------
    // For each feature key, the roles that receive FullAccess.
    // Any role not listed for a feature gets AccessLevel.None.
    private static readonly Dictionary<string, string[]> FeatureFullAccessRoles = new()
    {
        // Daily Operations
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

        // Payroll
        ["payroll/dashboard"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "PayrollClerk"],
        ["payroll/rates"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "PayrollClerk"],
        ["payroll/earning-codes"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "PayrollClerk"],
        ["payroll/holidays"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "PayrollClerk"],
        ["payroll/export"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "PayrollClerk"],

        // Compliance
        ["compliance/fra"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CraftManager"],
        ["compliance/safety"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CraftManager"],
        ["compliance/absence-codes"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],
        ["compliance/policies"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],

        // Work Management
        ["work-management/departments"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],
        ["work-management/crafts"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CraftManager"],
        ["work-management/assignment-templates"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CrewManager"],
        ["work-management/position-roles"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CraftManager"],

        // Employee Management
        ["employees"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CraftManager"],
        ["employees/seniority"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CraftManager"],
        ["employees/prior-service"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],
        ["admin/invitations"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],

        // Information -- all roles
        ["info/railroad"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CraftManager", "CrewManager", "Dispatcher", "PayrollClerk", "Employee"],
        ["info/reports"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin", "CraftManager", "CrewManager", "Dispatcher", "PayrollClerk", "Employee"],

        // Administration
        ["parents"] = ["SystemAdmin"],
        ["config/group-types"] = ["SystemAdmin", "ParentAdmin"],
        ["admin/users"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],
        ["admin/notifications"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],
        ["admin/jobs"] = ["SystemAdmin"],
        ["admin/roles"] = ["SystemAdmin"],
        ["admin/permissions"] = ["SystemAdmin"]
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

        await SeedGroupTypesAsync(sp);
        await BackfillGroupPathsAsync(sp);
        await SeedRolesAsync(sp);
        await SeedFeaturesAsync(sp);
        await SeedDefaultPermissionsAsync(sp);
        await SeedSystemAdminAsync(sp);
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

    private static async Task SeedGroupTypesAsync(IServiceProvider sp)
    {
        var groupTypeRepo = sp.GetRequiredService<IGroupTypeRepository>();
        var parentRepo = sp.GetRequiredService<IParentRepository>();

        var allParents = await parentRepo.GetAllAsync();
        var allGroupTypes = await groupTypeRepo.GetAllAsync();

        foreach (var parent in allParents)
        {
            foreach (var systemTypeName in GroupType.SystemTypeNames)
            {
                if (!allGroupTypes.Any(gt => gt.Name == systemTypeName && gt.ParentCtrlNbr == parent.CtrlNbr.Value))
                {
                    var isWorkArea = string.Equals(systemTypeName, "WorkArea", StringComparison.OrdinalIgnoreCase);
                    await groupTypeRepo.AddAsync(
                        GroupType.Create(systemTypeName, $"{systemTypeName} (auto-created)", isWorkArea: isWorkArea, parentCtrlNbr: parent.CtrlNbr.Value));
                }
            }
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
}
