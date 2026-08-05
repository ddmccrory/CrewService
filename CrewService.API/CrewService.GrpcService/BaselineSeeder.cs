using CrewService.Domain.Models.UserAccess;
using CrewService.Application.FraCompliance;
using CrewService.Domain.Modules.Authorization;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Infrastructure.Models.UserAccount;
using CrewService.Domain.Modules.Workflows;
using CrewService.Domain.ValueObjects;
using CrewService.Application.Workflows;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;
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
        ("work-management/markoff-codes", "Mark-Off Codes", "Work Management", "/work-management/markoff-codes"),

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
        ("admin/workflow-templates", "Workflow Templates", "Administration", "/admin/workflow-templates"),
        ("admin/seniority-move-policies", "Seniority Move Policies", "Administration", "/admin/seniority-move-policies"),
        ("admin/absence-approval-policy", "Absence Approval Policy", "Administration", "/admin/absence-approval-policy"),
        ("admin/call-sheet-rules", "Call Sheet Rules", "Administration", "/admin/call-sheet-rules"),
        ("admin/department-reassignment-rules", "Department Reassignment Rules", "Administration", "/admin/department-reassignment-rules"),
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
        ["work-management/markoff-codes"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],

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
        ["admin/workflow-templates"] = ["SystemAdmin"],
        ["admin/seniority-move-policies"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],
        ["admin/absence-approval-policy"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],
        ["admin/call-sheet-rules"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],
        ["admin/department-reassignment-rules"] = ["SystemAdmin", "ParentAdmin", "RailroadAdmin"],
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
        await SeedSystemGroupTypesAsync(sp);
        await SeedRolesAsync(sp);
        await SeedFeaturesAsync(sp);
        await SeedDefaultPermissionsAsync(sp);
        await SeedRegulatoryQualificationsAsync(sp);
        await SeedNotificationTypeConfigsAsync(sp);
        await SeedSystemAdminAsync(sp);
        await SeedStaticRequiredPositionsStrategyAsync(sp);
        await SeedWorkflowReferenceDataAsync(sp);
        await SeedEmployeeCreatedInviteWorkflowAsync(sp);
        await SeedSeniorityStatusChangedWorkflowAsync(sp);
        await SeedVacancyPlaceOnDutyWorkflowAsync(sp);
    }

    private static async Task SeedWorkflowReferenceDataAsync(IServiceProvider sp)
    {
        var triggerTypeRepo = sp.GetRequiredService<IWorkflowTriggerTypeRepository>();
        var effectTypeRepo = sp.GetRequiredService<IWorkflowEffectTypeRepository>();
        var operatorTypeRepo = sp.GetRequiredService<IWorkflowOperatorTypeRepository>();
        var metadataFieldTypeRepo = sp.GetRequiredService<IWorkflowMetadataFieldTypeRepository>();

        await EnsureTriggerTypeAsync(WorkflowTriggerTypeCodes.EmployeeCreated, TriggerTypes.EmployeeCreated);
        await EnsureTriggerTypeAsync(WorkflowTriggerTypeCodes.SeniorityStatusChanged, TriggerTypes.SeniorityStatusChanged);
        await EnsureTriggerTypeAsync(WorkflowTriggerTypeCodes.VacancyPlaceOnDutyRequested, TriggerTypes.VacancyPlaceOnDutyRequested);

        await EnsureEffectTypeAsync(WorkflowEffectTypeCodes.SendInvitation, WorkflowEffectTypes.SendInvitation);
        await EnsureEffectTypeAsync(WorkflowEffectTypeCodes.DoNothing, WorkflowEffectTypes.DoNothing);
        await EnsureEffectTypeAsync(WorkflowEffectTypeCodes.AddToRosterBoard, WorkflowEffectTypes.AddToRosterBoard);
        await EnsureEffectTypeAsync(WorkflowEffectTypeCodes.VacatePositionAndBulletinPosition, WorkflowEffectTypes.VacatePositionAndBulletinPosition);
        await EnsureEffectTypeAsync(WorkflowEffectTypeCodes.PlaceOnDuty, WorkflowEffectTypes.PlaceOnDuty);

        await EnsureOperatorTypeAsync(WorkflowOperatorTypeCodes.EqualsOperator, "Equals");
        await EnsureOperatorTypeAsync(WorkflowOperatorTypeCodes.NotEquals, "Does Not Equal");

        await EnsureMetadataFieldTypeAsync(WorkflowMetadataFieldTypeCodes.NewSeniorityState, "New Seniority Status");
        await EnsureMetadataFieldTypeAsync(WorkflowMetadataFieldTypeCodes.DepartmentCtrlNbr, "Department CtrlNbr");
        await EnsureMetadataFieldTypeAsync(WorkflowMetadataFieldTypeCodes.DepartmentName, "Department Name");
        await EnsureMetadataFieldTypeAsync(WorkflowMetadataFieldTypeCodes.CraftCtrlNbr, "Craft CtrlNbr");
        await EnsureMetadataFieldTypeAsync(WorkflowMetadataFieldTypeCodes.CraftName, "Craft Name");
        await EnsureMetadataFieldTypeAsync(WorkflowMetadataFieldTypeCodes.SeniorityStateCtrlNbr, "Seniority Status CtrlNbr");
        await EnsureMetadataFieldTypeAsync(WorkflowMetadataFieldTypeCodes.SeniorityStateName, "Seniority Status Name");

        async Task EnsureTriggerTypeAsync(string code, string name)
        {
            var existing = await triggerTypeRepo.GetByCodeAsync(code);
            if (existing is null)
            {
                await triggerTypeRepo.AddAsync(WorkflowTriggerType.Create(code, name));
                return;
            }

            if (!existing.IsActive || !string.Equals(existing.Name, name, StringComparison.Ordinal))
            {
                existing.Update(code, name, isActive: true);
                await triggerTypeRepo.UpdateAsync(existing);
            }
        }

        async Task EnsureEffectTypeAsync(string code, string name)
        {
            var existing = await effectTypeRepo.GetByCodeAsync(code);
            if (existing is null)
            {
                await effectTypeRepo.AddAsync(WorkflowEffectType.Create(code, name));
                return;
            }

            if (!existing.IsActive || !string.Equals(existing.Name, name, StringComparison.Ordinal))
            {
                existing.Update(code, name, isActive: true);
                await effectTypeRepo.UpdateAsync(existing);
            }
        }

        async Task EnsureOperatorTypeAsync(string code, string name)
        {
            var existing = await operatorTypeRepo.GetByCodeAsync(code);
            if (existing is null)
            {
                await operatorTypeRepo.AddAsync(WorkflowOperatorType.Create(code, name));
                return;
            }

            if (!existing.IsActive || !string.Equals(existing.Name, name, StringComparison.Ordinal))
            {
                existing.Update(code, name, isActive: true);
                await operatorTypeRepo.UpdateAsync(existing);
            }
        }

        async Task EnsureMetadataFieldTypeAsync(string code, string name)
        {
            var existing = await metadataFieldTypeRepo.GetByCodeAsync(code);
            if (existing is null)
            {
                await metadataFieldTypeRepo.AddAsync(WorkflowMetadataFieldType.Create(code, name));
                return;
            }

            if (!existing.IsActive || !string.Equals(existing.Name, name, StringComparison.Ordinal))
            {
                existing.Update(code, name, isActive: true);
                await metadataFieldTypeRepo.UpdateAsync(existing);
            }
        }
    }

    private static async Task SeedVacancyPlaceOnDutyWorkflowAsync(IServiceProvider sp)
    {
        var groupRepo = sp.GetRequiredService<IDynamicGroupRepository>();
        var templateRepo = sp.GetRequiredService<IWorkflowTemplateRepository>();
        var versionRepo = sp.GetRequiredService<IWorkflowVersionRepository>();
        var triggerTypeRepo = sp.GetRequiredService<IWorkflowTriggerTypeRepository>();
        var effectTypeRepo = sp.GetRequiredService<IWorkflowEffectTypeRepository>();

        var triggerType = await triggerTypeRepo.GetByCodeAsync(WorkflowTriggerTypeCodes.VacancyPlaceOnDutyRequested);
        var placeOnDutyEffectType = await effectTypeRepo.GetByCodeAsync(WorkflowEffectTypeCodes.PlaceOnDuty);

        if (triggerType is null || placeOnDutyEffectType is null)
            return;

        var railroads = await groupRepo.GetByGroupTypeNameAsync("Railroad");

        foreach (var railroad in railroads)
        {
            var existingTemplate = (await templateRepo.GetByRailroadAndTriggerTypeAsync(railroad.CtrlNbr, triggerType.CtrlNbr))
                .FirstOrDefault(w => string.Equals(w.Name, "Vacancy Place On Duty", StringComparison.OrdinalIgnoreCase));

            if (existingTemplate is null)
            {
                existingTemplate = WorkflowTemplate.Create(
                    railroad.CtrlNbr,
                    name: "Vacancy Place On Duty",
                    triggerTypeCtrlNbr: triggerType.CtrlNbr,
                    isEnabled: true);

                await templateRepo.AddAsync(existingTemplate);
            }

            var existingPublished = await versionRepo.GetLatestPublishedByRailroadAndTriggerAsync(
                railroad.CtrlNbr,
                triggerType.CtrlNbr);

            if (existingPublished is not null && existingPublished.WorkflowTemplateCtrlNbr == existingTemplate.CtrlNbr)
                continue;

            var definition = new WorkflowDefinition(
                TriggerTypeCtrlNbr: triggerType.CtrlNbr,
                TriggerConditionGroupOperator: "ALL",
                TriggerConditions: [],
                Steps:
                [
                    new WorkflowStepDefinition(
                        CtrlNbr: ControlNumber.Create(),
                        Order: 1,
                        Name: "Place On Duty",
                        IsEnabled: true,
                        FailurePolicy: WorkflowFailurePolicies.StopWorkflow,
                        ConditionGroupOperator: "ALL",
                        Conditions: [],
                        Effects:
                        [
                            new WorkflowEffectDefinition(
                                CtrlNbr: ControlNumber.Create(),
                                Order: 1,
                                IsEnabled: true,
                                EffectTypeCtrlNbr: placeOnDutyEffectType.CtrlNbr,
                                Options: new Dictionary<string, string>())
                        ])
                ]);

            var existingVersions = await versionRepo.GetByTemplateAsync(existingTemplate.CtrlNbr);
            var nextVersion = existingVersions.Count == 0 ? 1 : existingVersions.Max(v => v.VersionNumber) + 1;

            var version = WorkflowVersion.Create(
                existingTemplate.CtrlNbr,
                versionNumber: nextVersion,
                definitionJson: JsonSerializer.Serialize(definition, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                notes: "Seeded default Vacancy Place On Duty workflow",
                status: WorkflowVersionStatus.Published);

            await versionRepo.AddAsync(version);
        }
    }

    private static async Task SeedSystemGroupTypesAsync(IServiceProvider sp)
    {
        var parentRepo = sp.GetRequiredService<IParentRepository>();
        var groupTypeRepo = sp.GetRequiredService<IGroupTypeRepository>();

        var parents = await parentRepo.GetAllAsync();
        foreach (var parent in parents)
        {
            if (parent.CtrlNbr is null)
                continue;

            var existingRailroadType = await groupTypeRepo.GetByNameAsync("Railroad", parent.CtrlNbr);
            if (existingRailroadType is null)
            {
                await groupTypeRepo.AddAsync(GroupType.Create(
                    name: "Railroad",
                    description: "Railroad operational boundaries",
                    isWorkArea: true,
                    parentCtrlNbr: parent.CtrlNbr));
                continue;
            }

            var desiredDescription = string.IsNullOrWhiteSpace(existingRailroadType.Description)
                ? "Railroad operational boundaries"
                : existingRailroadType.Description;

            if (existingRailroadType.IsWorkArea && desiredDescription == existingRailroadType.Description)
                continue;

            existingRailroadType.Update(
                name: existingRailroadType.Name,
                description: desiredDescription,
                isWorkArea: true,
                flagsJson: existingRailroadType.FlagsJson,
                parentCtrlNbr: existingRailroadType.ParentCtrlNbr,
                railroadCtrlNbr: existingRailroadType.RailroadCtrlNbr,
                parentGroupTypeCtrlNbr: existingRailroadType.ParentGroupTypeCtrlNbr);

            await groupTypeRepo.UpdateAsync(existingRailroadType);
        }
    }

    private static async Task SeedNotificationTypeConfigsAsync(IServiceProvider sp)
    {
        var groupRepo = sp.GetRequiredService<IDynamicGroupRepository>();
        var railroads = await groupRepo.GetByGroupTypeNameAsync("Railroad");
        if (railroads.Count == 0)
            return;

        var notificationTypeConfigRepo = sp.GetRequiredService<INotificationTypeConfigRepository>();
        await NotificationTypeConfigSeedDefaults.SeedForRailroadsAsync(notificationTypeConfigRepo, railroads);
        await NotificationTypeConfigSeedDefaults.BackfillMessageTemplatesAsync(notificationTypeConfigRepo, railroads);
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

    private static async Task SeedEmployeeCreatedInviteWorkflowAsync(IServiceProvider sp)
    {
        var groupRepo = sp.GetRequiredService<IDynamicGroupRepository>();
        var roleRepo = sp.GetRequiredService<IRoleRepository>();
        var templateRepo = sp.GetRequiredService<IWorkflowTemplateRepository>();
        var versionRepo = sp.GetRequiredService<IWorkflowVersionRepository>();
        var triggerTypeRepo = sp.GetRequiredService<IWorkflowTriggerTypeRepository>();
        var effectTypeRepo = sp.GetRequiredService<IWorkflowEffectTypeRepository>();

        var employeeRole = await roleRepo.GetByNameAsync("Employee");
        if (employeeRole is null)
            return;

        var employeeCreatedTriggerType = await triggerTypeRepo.GetByCodeAsync(WorkflowTriggerTypeCodes.EmployeeCreated);
        var sendInvitationEffectType = await effectTypeRepo.GetByCodeAsync(WorkflowEffectTypeCodes.SendInvitation);
        if (employeeCreatedTriggerType is null || sendInvitationEffectType is null)
            return;

        var railroads = await groupRepo.GetByGroupTypeNameAsync("Railroad");
        foreach (var railroad in railroads)
        {
            var existingTemplate = (await templateRepo.GetByRailroadAndTriggerTypeAsync(railroad.CtrlNbr, employeeCreatedTriggerType.CtrlNbr))
                .FirstOrDefault(w => string.Equals(w.Name, "Invite New Employee", StringComparison.OrdinalIgnoreCase));

            if (existingTemplate is null)
            {
                existingTemplate = WorkflowTemplate.Create(
                    railroad.CtrlNbr,
                    name: "Invite New Employee",
                    triggerTypeCtrlNbr: employeeCreatedTriggerType.CtrlNbr,
                    isEnabled: true);

                await templateRepo.AddAsync(existingTemplate);
            }

            var existingPublished = await versionRepo.GetLatestPublishedByRailroadAndTriggerAsync(railroad.CtrlNbr, employeeCreatedTriggerType.CtrlNbr);
            if (existingPublished is not null && existingPublished.WorkflowTemplateCtrlNbr == existingTemplate.CtrlNbr)
                continue;

            var definition = new WorkflowDefinition(
                TriggerTypeCtrlNbr: employeeCreatedTriggerType.CtrlNbr,
                TriggerConditionGroupOperator: "ALL",
                TriggerConditions: [],
                Steps:
                [
                    new WorkflowStepDefinition(
                        CtrlNbr: ControlNumber.Create(),
                        Order: 1,
                        Name: "Send Invitation",
                        IsEnabled: true,
                        FailurePolicy: "StopWorkflow",
                        ConditionGroupOperator: "ALL",
                        Conditions: [],
                        Effects:
                        [
                            new WorkflowEffectDefinition(
                                CtrlNbr: ControlNumber.Create(),
                                Order: 1,
                                IsEnabled: true,
                                EffectTypeCtrlNbr: sendInvitationEffectType.CtrlNbr,
                                Options: new Dictionary<string, string>
                                {
                                    ["roleCtrlNbr"] = employeeRole.CtrlNbr.Value.ToString(),
                                    ["expirationDays"] = "7",
                                    ["railroadCtrlNbr"] = railroad.CtrlNbr.Value.ToString()
                                })
                        ])
                ]);

            var existingVersions = await versionRepo.GetByTemplateAsync(existingTemplate.CtrlNbr);
            var nextVersion = existingVersions.Count == 0 ? 1 : existingVersions.Max(v => v.VersionNumber) + 1;

            var version = WorkflowVersion.Create(
                existingTemplate.CtrlNbr,
                versionNumber: nextVersion,
                definitionJson: JsonSerializer.Serialize(definition, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                notes: "Seeded default Employee Created invitation workflow",
                status: WorkflowVersionStatus.Published);

            await versionRepo.AddAsync(version);
        }
    }

    private static async Task SeedSeniorityStatusChangedWorkflowAsync(IServiceProvider sp)
    {
        var groupRepo = sp.GetRequiredService<IDynamicGroupRepository>();
        var templateRepo = sp.GetRequiredService<IWorkflowTemplateRepository>();
        var versionRepo = sp.GetRequiredService<IWorkflowVersionRepository>();
        var triggerTypeRepo = sp.GetRequiredService<IWorkflowTriggerTypeRepository>();
        var effectTypeRepo = sp.GetRequiredService<IWorkflowEffectTypeRepository>();
        var operatorTypeRepo = sp.GetRequiredService<IWorkflowOperatorTypeRepository>();
        var metadataFieldTypeRepo = sp.GetRequiredService<IWorkflowMetadataFieldTypeRepository>();

        var seniorityStatusChangedTriggerType = await triggerTypeRepo.GetByCodeAsync(WorkflowTriggerTypeCodes.SeniorityStatusChanged);
        var addToRosterBoardEffectType = await effectTypeRepo.GetByCodeAsync(WorkflowEffectTypeCodes.AddToRosterBoard);
        var vacateAndBulletinEffectType = await effectTypeRepo.GetByCodeAsync(WorkflowEffectTypeCodes.VacatePositionAndBulletinPosition);
        var equalsOperatorType = await operatorTypeRepo.GetByCodeAsync(WorkflowOperatorTypeCodes.EqualsOperator);
        var newSeniorityStatusMetadataFieldType = await metadataFieldTypeRepo.GetByCodeAsync(WorkflowMetadataFieldTypeCodes.NewSeniorityState);
        var legacySeniorityStateTriggerType = await triggerTypeRepo.GetByCodeAsync("Seniority State Changed");

        if (seniorityStatusChangedTriggerType is null
            || addToRosterBoardEffectType is null
            || vacateAndBulletinEffectType is null
            || equalsOperatorType is null
            || newSeniorityStatusMetadataFieldType is null)
        {
            return;
        }

        var railroads = await groupRepo.GetByGroupTypeNameAsync("Railroad");

        if (legacySeniorityStateTriggerType is not null)
        {
            await ConsolidateLegacySeniorityStateWorkflowAsync(
                railroads,
                templateRepo,
                triggerTypeRepo,
                seniorityStatusChangedTriggerType,
                legacySeniorityStateTriggerType);
        }

        foreach (var railroad in railroads)
        {
            var existingTemplate = (await templateRepo.GetByRailroadAndTriggerTypeAsync(railroad.CtrlNbr, seniorityStatusChangedTriggerType.CtrlNbr))
                .FirstOrDefault(w => string.Equals(w.Name, "Seniority Status Change", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(w.Name, "Seniority State Change", StringComparison.OrdinalIgnoreCase));

            if (existingTemplate is null)
            {
                existingTemplate = WorkflowTemplate.Create(
                    railroad.CtrlNbr,
                    name: "Seniority Status Change",
                    triggerTypeCtrlNbr: seniorityStatusChangedTriggerType.CtrlNbr,
                    isEnabled: true);

                await templateRepo.AddAsync(existingTemplate);
            }
            else if (!string.Equals(existingTemplate.Name, "Seniority Status Change", StringComparison.Ordinal))
            {
                existingTemplate.UpdateDefinition("Seniority Status Change", existingTemplate.TriggerTypeCtrlNbr, existingTemplate.IsEnabled);
                await templateRepo.UpdateAsync(existingTemplate);
            }

            var existingPublished = await versionRepo.GetLatestPublishedByRailroadAndTriggerAsync(
                railroad.CtrlNbr,
                seniorityStatusChangedTriggerType.CtrlNbr);
            if (existingPublished is not null && existingPublished.WorkflowTemplateCtrlNbr == existingTemplate.CtrlNbr)
                continue;

            var definition = new WorkflowDefinition(
                TriggerTypeCtrlNbr: seniorityStatusChangedTriggerType.CtrlNbr,
                TriggerConditionGroupOperator: "ALL",
                TriggerConditions: [],
                Steps:
                [
                    new WorkflowStepDefinition(
                        CtrlNbr: ControlNumber.Create(),
                        Order: 1,
                        Name: "Active -> Hangout",
                        IsEnabled: true,
                        FailurePolicy: WorkflowFailurePolicies.StopWorkflow,
                        ConditionGroupOperator: "ALL",
                        Conditions:
                        [
                            new WorkflowConditionDefinition(
                                CtrlNbr: ControlNumber.Create(),
                                FieldTypeCtrlNbr: newSeniorityStatusMetadataFieldType.CtrlNbr,
                                OperatorTypeCtrlNbr: equalsOperatorType.CtrlNbr,
                                Value: "Active")
                        ],
                        Effects:
                        [
                            new WorkflowEffectDefinition(
                                CtrlNbr: ControlNumber.Create(),
                                Order: 1,
                                IsEnabled: true,
                                EffectTypeCtrlNbr: addToRosterBoardEffectType.CtrlNbr,
                                Options: new Dictionary<string, string>
                                {
                                    [WorkflowOptionKeys.EffectOption] = "Hangout"
                                })
                        ]),
                    new WorkflowStepDefinition(
                        CtrlNbr: ControlNumber.Create(),
                        Order: 2,
                        Name: "Any Inactive -> Extended Absence",
                        IsEnabled: true,
                        FailurePolicy: WorkflowFailurePolicies.StopWorkflow,
                        ConditionGroupOperator: "ANY",
                        Conditions:
                        [
                            new WorkflowConditionDefinition(
                                CtrlNbr: ControlNumber.Create(),
                                FieldTypeCtrlNbr: newSeniorityStatusMetadataFieldType.CtrlNbr,
                                OperatorTypeCtrlNbr: equalsOperatorType.CtrlNbr,
                                Value: "Cut Back"),
                            new WorkflowConditionDefinition(
                                CtrlNbr: ControlNumber.Create(),
                                FieldTypeCtrlNbr: newSeniorityStatusMetadataFieldType.CtrlNbr,
                                OperatorTypeCtrlNbr: equalsOperatorType.CtrlNbr,
                                Value: "Inactive"),
                            new WorkflowConditionDefinition(
                                CtrlNbr: ControlNumber.Create(),
                                FieldTypeCtrlNbr: newSeniorityStatusMetadataFieldType.CtrlNbr,
                                OperatorTypeCtrlNbr: equalsOperatorType.CtrlNbr,
                                Value: "Leave of Absence"),
                            new WorkflowConditionDefinition(
                                CtrlNbr: ControlNumber.Create(),
                                FieldTypeCtrlNbr: newSeniorityStatusMetadataFieldType.CtrlNbr,
                                OperatorTypeCtrlNbr: equalsOperatorType.CtrlNbr,
                                Value: "Medical Leave")
                        ],
                        Effects:
                        [
                            new WorkflowEffectDefinition(
                                CtrlNbr: ControlNumber.Create(),
                                Order: 1,
                                IsEnabled: true,
                                EffectTypeCtrlNbr: addToRosterBoardEffectType.CtrlNbr,
                                Options: new Dictionary<string, string>
                                {
                                    [WorkflowOptionKeys.EffectOption] = "Extended Absence"
                                })
                        ]),
                    new WorkflowStepDefinition(
                        CtrlNbr: ControlNumber.Create(),
                        Order: 3,
                        Name: "Any Off Property -> Vacate and Bulletin",
                        IsEnabled: true,
                        FailurePolicy: WorkflowFailurePolicies.StopWorkflow,
                        ConditionGroupOperator: "ANY",
                        Conditions:
                        [
                            new WorkflowConditionDefinition(
                                CtrlNbr: ControlNumber.Create(),
                                FieldTypeCtrlNbr: newSeniorityStatusMetadataFieldType.CtrlNbr,
                                OperatorTypeCtrlNbr: equalsOperatorType.CtrlNbr,
                                Value: "Retired"),
                            new WorkflowConditionDefinition(
                                CtrlNbr: ControlNumber.Create(),
                                FieldTypeCtrlNbr: newSeniorityStatusMetadataFieldType.CtrlNbr,
                                OperatorTypeCtrlNbr: equalsOperatorType.CtrlNbr,
                                Value: "Terminated")
                        ],
                        Effects:
                        [
                            new WorkflowEffectDefinition(
                                CtrlNbr: ControlNumber.Create(),
                                Order: 1,
                                IsEnabled: true,
                                EffectTypeCtrlNbr: vacateAndBulletinEffectType.CtrlNbr,
                                Options: new Dictionary<string, string>())
                        ])
                ]);

            var existingVersions = await versionRepo.GetByTemplateAsync(existingTemplate.CtrlNbr);
            var nextVersion = existingVersions.Count == 0 ? 1 : existingVersions.Max(v => v.VersionNumber) + 1;

            var version = WorkflowVersion.Create(
                existingTemplate.CtrlNbr,
                versionNumber: nextVersion,
                definitionJson: JsonSerializer.Serialize(definition, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                notes: "Seeded default Seniority Status Changed workflow",
                status: WorkflowVersionStatus.Published);

            await versionRepo.AddAsync(version);
        }

        static async Task ConsolidateLegacySeniorityStateWorkflowAsync(
            IReadOnlyList<DynamicGroup> railroads,
            IWorkflowTemplateRepository templateRepo,
            IWorkflowTriggerTypeRepository triggerTypeRepo,
            WorkflowTriggerType seniorityStatusChangedTriggerType,
            WorkflowTriggerType legacySeniorityStateTriggerType)
        {
            foreach (var railroad in railroads)
            {
                var statusTemplates = await templateRepo.GetByRailroadAndTriggerTypeAsync(
                    railroad.CtrlNbr,
                    seniorityStatusChangedTriggerType.CtrlNbr);

                var legacyTemplates = await templateRepo.GetByRailroadAndTriggerTypeAsync(
                    railroad.CtrlNbr,
                    legacySeniorityStateTriggerType.CtrlNbr);

                foreach (var legacyTemplate in legacyTemplates)
                {
                    var desiredName = string.Equals(legacyTemplate.Name, "Seniority State Change", StringComparison.OrdinalIgnoreCase)
                        ? "Seniority Status Change"
                        : legacyTemplate.Name;

                    var hasMatchingStatusTemplate = statusTemplates.Any(t => string.Equals(t.Name, desiredName, StringComparison.OrdinalIgnoreCase));
                    if (hasMatchingStatusTemplate)
                    {
                        await templateRepo.DeleteAsync(legacyTemplate.CtrlNbr);
                        continue;
                    }

                    legacyTemplate.UpdateDefinition(desiredName, seniorityStatusChangedTriggerType.CtrlNbr, legacyTemplate.IsEnabled);
                    await templateRepo.UpdateAsync(legacyTemplate);
                    statusTemplates.Add(legacyTemplate);
                }
            }

            await triggerTypeRepo.DeleteAsync(legacySeniorityStateTriggerType.CtrlNbr);
        }
    }
}
