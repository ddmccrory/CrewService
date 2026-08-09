using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.Modules.RailroadInfo;
using CrewService.Domain.Modules.Safety;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.UserAccess;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Application.FraCompliance;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Infrastructure.Models.UserAccount;
using CrewService.Domain.Interfaces;

using CrewService.Application.Employees;
using CrewService.Application.Parents;
using CrewService.Application.Qualifications;
using CrewService.Application.Assignments;
using CrewService.Application.Crews;
using CrewService.Application.Absence;
using CrewService.Application.RosterBoardOps;
using CrewService.Application.SeniorityOps;
using CrewService.Application.WorkManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace CrewService.GrpcService;

/// <summary>
/// Seeds the development database with sample data covering:
///   1. GroupTypes, DynamicGroups (including Railroads as a pre-defined group type), Parents
///   2. Employees with Addresses, Phone Numbers, Email Addresses (via auto-accept invitations)
///   3. SystemAdmin bootstrap user and per-parent role assignments (via auto-accept invitations)
/// Idempotent: each section checks for existing data before seeding.
/// Dev only � uses auto-accept invitation flow to mirror production logic.
/// </summary>
internal static class DevDataSeeder
{
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

        void SetParent(long ctrlNbr) =>
            httpContextAccessor.HttpContext!.Request.Headers["x-parent-ctrl-nbr"] = ctrlNbr.ToString();

        var parentAppService = sp.GetRequiredService<ParentAppService>();
        var groupTypeRepo = sp.GetRequiredService<IGroupTypeRepository>();
        var groupRepo = sp.GetRequiredService<IDynamicGroupRepository>();
        var parentRepo = sp.GetRequiredService<IParentRepository>();

        // Idempotent guard — only seed when DevDataSeeder-specific group types are absent
        // (migration-seeded types like Location, Zone, etc. do not count)
        var existing = await groupTypeRepo.GetAllAsync();
        if (!existing.Any(gt => gt.Name is "Region" or "Subdivision"))
        {

        // ?? Parents (via ParentService — auto-seeds system types + attribute definitions) ??
        var simpleCorpResp = await parentAppService.CreateAsync("Simple Corp");
        var holdingCorpResp = await parentAppService.CreateAsync("CSX Corporation");

        // Look up auto-created system types for subsequent group creation
        var autoCreatedTypes = await groupTypeRepo.GetAllAsync();
        var simpleRailroadType = autoCreatedTypes.First(gt => gt.Name == "Railroad" && gt.ParentCtrlNbr == simpleCorpResp.CtrlNbr);
        var csxRailroadType = autoCreatedTypes.First(gt => gt.Name == "Railroad" && gt.ParentCtrlNbr == holdingCorpResp.CtrlNbr);

        // Railroads (as DynamicGroups)
        SetParent(simpleCorpResp.CtrlNbr.Value);
        var simpleRR = DynamicGroup.Create(simpleRailroadType.CtrlNbr.Value, "Simple Railroad", parentGroupCtrlNbr: null, path: null, isWorkArea: true, code: "SMPL", parentCtrlNbr: simpleCorpResp.CtrlNbr, timeZoneId: "Central Standard Time");
        await groupRepo.AddAsync(simpleRR);
        var ptraRR = simpleRR;
        var ptraRailroadType = simpleRailroadType;

        SetParent(holdingCorpResp.CtrlNbr.Value);
        var csxRR = DynamicGroup.Create(csxRailroadType.CtrlNbr.Value, "CSX Transportation", parentGroupCtrlNbr: null, path: null, isWorkArea: false, code: "CSX", parentCtrlNbr: holdingCorpResp.CtrlNbr);
        await groupRepo.AddAsync(csxRR);

        var csxtRR = DynamicGroup.Create(csxRailroadType.CtrlNbr.Value, "CSX Intermodal", parentGroupCtrlNbr: null, path: null, isWorkArea: false, code: "CSXT", parentCtrlNbr: holdingCorpResp.CtrlNbr);
        await groupRepo.AddAsync(csxtRR);

        // ?? Group Types (CSX Corporation) ?????????????????????????????
        var regionType = GroupType.Create("Region", "Geographic region", isWorkArea: false, parentCtrlNbr: holdingCorpResp.CtrlNbr);
        var subdivType = GroupType.Create("Subdivision", "Track subdivision", isWorkArea: false, parentCtrlNbr: holdingCorpResp.CtrlNbr, parentGroupTypeCtrlNbr: regionType.CtrlNbr.Value);

        await groupTypeRepo.AddAsync(regionType);
        await groupTypeRepo.AddAsync(subdivType);

        // Module reference group types (CSX Corporation)
        await groupTypeRepo.AddAsync(GroupType.Create("Location", "Operational locations used by FRA segments and billing", isWorkArea: false, parentCtrlNbr: holdingCorpResp.CtrlNbr));
        await groupTypeRepo.AddAsync(GroupType.Create("Zone", "Geographic zones for billing and reporting", isWorkArea: false, parentCtrlNbr: holdingCorpResp.CtrlNbr));
        await groupTypeRepo.AddAsync(GroupType.Create("AFE", "Authorization for Expenditure codes", isWorkArea: false, parentCtrlNbr: holdingCorpResp.CtrlNbr));
        await groupTypeRepo.AddAsync(GroupType.Create("WorkCode", "Work/job classification codes", isWorkArea: false, parentCtrlNbr: holdingCorpResp.CtrlNbr));
        await groupTypeRepo.AddAsync(GroupType.Create("Material", "Material and supply codes", isWorkArea: false, parentCtrlNbr: holdingCorpResp.CtrlNbr));
        await groupTypeRepo.AddAsync(GroupType.Create("LocomotiveType", "Locomotive type classification codes", isWorkArea: false, parentCtrlNbr: holdingCorpResp.CtrlNbr));

        // ?? Scenario 1: Simple (no placements) ??????????????????????
        // No placement rows -- backward-compatible scenario

        // ?? Scenario 2: Simple Railroad ??????????????????????
        // Railroad is the work area; hierarchy: Railroad -> Location
        SetParent(simpleCorpResp.CtrlNbr.Value);
        var ptraLocationType = GroupType.Create("Location", "Simple Railroad operational locations", isWorkArea: false, parentCtrlNbr: simpleCorpResp.CtrlNbr, parentGroupTypeCtrlNbr: ptraRailroadType.CtrlNbr.Value);
        await groupTypeRepo.AddAsync(ptraLocationType);

        var northYard = DynamicGroup.Create(ptraLocationType.CtrlNbr.Value, "North Yard", parentGroupCtrlNbr: ptraRR.CtrlNbr.Value, path: null, isWorkArea: false, code: "NOYD", parentCtrlNbr: simpleCorpResp.CtrlNbr, railroadCtrlNbr: ptraRR.CtrlNbr.Value);
        await groupRepo.AddAsync(northYard);

        var mainYard = DynamicGroup.Create(ptraLocationType.CtrlNbr.Value, "Main Yard", parentGroupCtrlNbr: ptraRR.CtrlNbr.Value, path: null, isWorkArea: false, code: "MNYD", parentCtrlNbr: simpleCorpResp.CtrlNbr, railroadCtrlNbr: ptraRR.CtrlNbr.Value);
        await groupRepo.AddAsync(mainYard);

        var southYard = DynamicGroup.Create(ptraLocationType.CtrlNbr.Value, "South Yard", parentGroupCtrlNbr: ptraRR.CtrlNbr.Value, path: null, isWorkArea: false, code: "SOYD", parentCtrlNbr: simpleCorpResp.CtrlNbr, railroadCtrlNbr: ptraRR.CtrlNbr.Value);
        await groupRepo.AddAsync(southYard);

        // ?? Scenario 3: Holding Company (CSX) ????????????????????????
        // Parent -> Region -> Subdivision (user will add work areas)
        SetParent(holdingCorpResp.CtrlNbr.Value);
        var southeast = DynamicGroup.Create(
            regionType.CtrlNbr.Value,
            "Southeast Region",
            parentGroupCtrlNbr: null,
            path: "/southeast",
            isWorkArea: false,
            parentCtrlNbr: holdingCorpResp.CtrlNbr,
            railroadCtrlNbr: csxRR.CtrlNbr.Value);
        await groupRepo.AddAsync(southeast);

        var jaxSub = DynamicGroup.Create(
            subdivType.CtrlNbr.Value,
            "Jacksonville Sub",
            parentGroupCtrlNbr: southeast.CtrlNbr.Value,
            path: "/southeast/jax",
            isWorkArea: true,
            parentCtrlNbr: holdingCorpResp.CtrlNbr,
            railroadCtrlNbr: csxRR.CtrlNbr.Value,
            timeZoneId: "Eastern Standard Time");
        await groupRepo.AddAsync(jaxSub);

        var midwest = DynamicGroup.Create(
            regionType.CtrlNbr.Value,
            "Midwest Region",
            parentGroupCtrlNbr: null,
            path: "/midwest",
            isWorkArea: false,
            parentCtrlNbr: holdingCorpResp.CtrlNbr,
            railroadCtrlNbr: csxRR.CtrlNbr.Value);
        await groupRepo.AddAsync(midwest);
        }

        // Backfill core baseline entities for stale DBs where initial seed was partially applied.

        // Ensure all parents exist first — use ParentService so system types + attributes are auto-seeded
        var allParentsCore = await parentRepo.GetAllAsync();

        if (!allParentsCore.Any(p => p.Name.Value == "Simple Corp"))
            await parentAppService.CreateAsync("Simple Corp");

        if (!allParentsCore.Any(p => p.Name.Value == "CSX Corporation"))
            await parentAppService.CreateAsync("CSX Corporation");

        // Re-read after potential service-based creations
        allParentsCore = await parentRepo.GetAllAsync();
        var simpleCorpCore = allParentsCore.First(p => p.Name.Value == "Simple Corp");
        var ptraParentCore = simpleCorpCore;
        var csxParentCore = allParentsCore.First(p => p.Name.Value == "CSX Corporation");

        // Seed baseline mark-off codes for each railroad context.
        var absenceCodeRepo = sp.GetRequiredService<IAbsenceCodeRepository>();
        var baselineMarkOffCodes = new (string Code, string Description, bool IsExcused, bool IsCompensated, bool RequiresApproval, decimal? DefaultAutoMarkUpHours)[]
        {
            ("CT", "Car Trouble", false, false, true, null),
            ("D", "Discipline", false, false, true, null),
            ("MD", "Medical/Dental", false, false, true, null),
            ("PB", "Personal Business", false, false, true, null),
            ("PD", "Personal Day", true, true, false, 24m),
            ("S", "Sick", false, false, true, null),
            ("SR", "Safety Rest", false, false, false, null),
            ("V", "Vacation", true, true, false, null),
            ("VD", "Vacation Day", true, true, false, 24m),
            ("WR", "Weather Related", false, false, true, null)
        };

        foreach (var parentCore in new[] { simpleCorpCore, csxParentCore })
        {
            SetParent(parentCore.CtrlNbr.Value);

            var railroads = await groupRepo.GetByGroupTypeNameAsync("Railroad", parentCore.CtrlNbr);

            foreach (var railroad in railroads)
            {
                var existingCodes = await absenceCodeRepo.GetByRailroadAsync(railroad.CtrlNbr);

                foreach (var (code, description, isExcused, isCompensated, requiresApproval, defaultAutoMarkUpHours) in baselineMarkOffCodes)
                {
                    var existingCode = existingCodes.FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));
                    if (existingCode is null)
                    {
                        var markOffCode = AbsenceCode.Create(
                            railroad.CtrlNbr.Value,
                            code,
                            description,
                            isExcused,
                            isCompensated,
                            requiresApproval,
                            isSystemOnly: false,
                            isHolidayExempt: false,
                            defaultAutoMarkUpHours,
                            isActive: true);

                        await absenceCodeRepo.AddAsync(markOffCode);
                        continue;
                    }

                    existingCode.Update(
                        description: description,
                        isExcused: isExcused,
                        isCompensated: isCompensated,
                        requiresApproval: requiresApproval,
                        isSystemOnly: false,
                        isHolidayExempt: false,
                        defaultAutoMarkUpHours: defaultAutoMarkUpHours,
                        isActive: true);
                    absenceCodeRepo.Update(existingCode);
                }
            }
        }

        // Backfill per-parent system types for pre-existing parents that may be missing types
        var groupTypesBackfill = await groupTypeRepo.GetAllAsync();

        foreach (var parentCore in new[] { simpleCorpCore, csxParentCore })
        {
            SetParent(parentCore.CtrlNbr.Value);
            var pCtrl = parentCore.CtrlNbr.Value;
            var existingRailroadType = groupTypesBackfill.FirstOrDefault(gt => gt.Name == "Railroad" && gt.ParentCtrlNbr == pCtrl);
            if (existingRailroadType is null)
            {
                await groupTypeRepo.AddAsync(GroupType.Create("Railroad", "Railroad operational boundaries", isWorkArea: true, parentCtrlNbr: pCtrl));
            }
            else if (!existingRailroadType.IsWorkArea)
            {
                existingRailroadType.Update(
                    existingRailroadType.Name,
                    existingRailroadType.Description,
                    isWorkArea: true,
                    existingRailroadType.FlagsJson,
                    existingRailroadType.ParentCtrlNbr,
                    existingRailroadType.RailroadCtrlNbr,
                    existingRailroadType.ParentGroupTypeCtrlNbr);
                await groupTypeRepo.UpdateAsync(existingRailroadType);
            }
        }

        // Backfill seniority states for pre-existing parents that may be missing states
        var seniorityStateRepo = sp.GetRequiredService<ISeniorityStateRepository>();
        var defaultStates = new (string Description, StateType Type)[]
        {
            ("Active", StateType.Active),
            ("Cut Back", StateType.CutBack),
            ("Inactive", StateType.Inactive),
            ("Terminated", StateType.OffProperty),
            ("Dismissed", StateType.Inactive),
            ("Leave of Absence", StateType.Inactive),
            ("Medical Leave", StateType.Inactive),
            ("Retired", StateType.OffProperty)
        };

        foreach (var parentCore in new[] { simpleCorpCore, csxParentCore })
        {
            SetParent(parentCore.CtrlNbr.Value);
            var pCtrl = parentCore.CtrlNbr.Value;
            var existingStates = await seniorityStateRepo.GetByParentCtrlNbrAsync(parentCore.CtrlNbr);
            foreach (var (desc, type) in defaultStates)
            {
                var existingState = existingStates.FirstOrDefault(s => s.StateDescription == desc);
                if (existingState is null)
                    await seniorityStateRepo.AddAsync(SeniorityState.Create(desc, type, pCtrl));
                else if (existingState.StateType != type)
                {
                    existingState.Update(desc, type);
                    await seniorityStateRepo.UpdateAsync(existingState);
                }
            }
        }

        // Backfill SMPL scenario Location group type hierarchy: Location -> Railroad
        SetParent(ptraParentCore.CtrlNbr.Value);
        groupTypesBackfill = await groupTypeRepo.GetAllAsync();
        var ptraRailroadTypeBackfill = groupTypesBackfill.FirstOrDefault(gt => gt.Name == "Railroad" && gt.ParentCtrlNbr == ptraParentCore.CtrlNbr.Value);
        var ptraLocationTypeBackfill = groupTypesBackfill.FirstOrDefault(gt => gt.Name == "Location" && gt.ParentCtrlNbr == ptraParentCore.CtrlNbr.Value);
        if (ptraRailroadTypeBackfill is not null && ptraLocationTypeBackfill is not null && ptraLocationTypeBackfill.ParentGroupTypeCtrlNbr != ptraRailroadTypeBackfill.CtrlNbr)
        {
            ptraLocationTypeBackfill.Update(
                ptraLocationTypeBackfill.Name,
                ptraLocationTypeBackfill.Description,
                ptraLocationTypeBackfill.IsWorkArea,
                ptraLocationTypeBackfill.FlagsJson,
                ptraLocationTypeBackfill.ParentCtrlNbr,
                ptraLocationTypeBackfill.RailroadCtrlNbr,
                ptraRailroadTypeBackfill.CtrlNbr);
            await groupTypeRepo.UpdateAsync(ptraLocationTypeBackfill);
        }

        // Backfill group types scoped to CSX Corporation
        SetParent(csxParentCore.CtrlNbr.Value);
        groupTypesBackfill = await groupTypeRepo.GetAllAsync();
        var csxParentCtrlNbr = csxParentCore.CtrlNbr.Value;

        var regionTypeBackfill = groupTypesBackfill.FirstOrDefault(gt => gt.Name == "Region" && gt.ParentCtrlNbr == csxParentCtrlNbr);
        if (regionTypeBackfill is null)
        {
            regionTypeBackfill = GroupType.Create("Region", "Geographic region", isWorkArea: false, parentCtrlNbr: csxParentCtrlNbr);
            await groupTypeRepo.AddAsync(regionTypeBackfill);
        }

        var subdivTypeBackfill = groupTypesBackfill.FirstOrDefault(gt => gt.Name == "Subdivision" && gt.ParentCtrlNbr == csxParentCtrlNbr);
        if (subdivTypeBackfill is null)
        {
            subdivTypeBackfill = GroupType.Create("Subdivision", "Track subdivision", isWorkArea: false, parentCtrlNbr: csxParentCtrlNbr, parentGroupTypeCtrlNbr: regionTypeBackfill.CtrlNbr.Value);
            await groupTypeRepo.AddAsync(subdivTypeBackfill);
        }

        // Backfill module reference group types (CSX Corporation)
        foreach (var (name, desc) in new[]
        {
            ("Location", "Operational locations used by FRA segments and billing"),
            ("Zone", "Geographic zones for billing and reporting"),
            ("AFE", "Authorization for Expenditure codes"),
            ("WorkCode", "Work/job classification codes"),
            ("Material", "Material and supply codes"),
            ("LocomotiveType", "Locomotive type classification codes")
        })
        {
            if (!groupTypesBackfill.Any(gt => gt.Name == name && gt.ParentCtrlNbr == csxParentCtrlNbr))
                await groupTypeRepo.AddAsync(GroupType.Create(name, desc, isWorkArea: false, parentCtrlNbr: csxParentCtrlNbr));
        }

        // Backfill Railroad DynamicGroups (before other groups so we can reference their CtrlNbrs)
        var allTypesForRR = await groupTypeRepo.GetAllAsync();
        var allGroupsForRR = await groupRepo.GetAllAsync();

        SetParent(simpleCorpCore.CtrlNbr.Value);
        if (allGroupsForRR.All(g => g.Code != "SMPL"))
        {
            var smplRRType = allTypesForRR.First(gt => gt.Name == "Railroad" && gt.ParentCtrlNbr == simpleCorpCore.CtrlNbr.Value);
            await groupRepo.AddAsync(DynamicGroup.Create(smplRRType.CtrlNbr.Value, "Simple Railroad", parentGroupCtrlNbr: null, path: null, isWorkArea: true, code: "SMPL", parentCtrlNbr: simpleCorpCore.CtrlNbr.Value, timeZoneId: "Central Standard Time"));
        }

        SetParent(csxParentCore.CtrlNbr.Value);
        if (allGroupsForRR.All(g => g.Code != "CSX"))
        {
            var csxRRType = allTypesForRR.First(gt => gt.Name == "Railroad" && gt.ParentCtrlNbr == csxParentCore.CtrlNbr.Value);
            await groupRepo.AddAsync(DynamicGroup.Create(csxRRType.CtrlNbr.Value, "CSX Transportation", parentGroupCtrlNbr: null, path: null, isWorkArea: false, code: "CSX", parentCtrlNbr: csxParentCore.CtrlNbr.Value));
        }

        if (allGroupsForRR.All(g => g.Code != "CSXT"))
        {
            var csxtRRType = allTypesForRR.First(gt => gt.Name == "Railroad" && gt.ParentCtrlNbr == csxParentCore.CtrlNbr.Value);
            await groupRepo.AddAsync(DynamicGroup.Create(csxtRRType.CtrlNbr.Value, "CSX Intermodal", parentGroupCtrlNbr: null, path: null, isWorkArea: false, code: "CSXT", parentCtrlNbr: csxParentCore.CtrlNbr.Value));
        }

        // Resolve railroad CtrlNbrs for group scoping
        var allRailroadsBackfill = await groupRepo.GetByGroupTypeNameAsync("Railroad");
        var csxRailroadCore = allRailroadsBackfill.First(g => g.Code == "CSX");

        var notificationTypeConfigRepo = sp.GetRequiredService<INotificationTypeConfigRepository>();
        await NotificationTypeConfigSeedDefaults.SeedForRailroadsAsync(
            notificationTypeConfigRepo,
            allRailroadsBackfill,
            SetParent);

        // Backfill CSX groups
        var allGroupsCore = await groupRepo.GetAllAsync();
        var southeastCore = allGroupsCore.FirstOrDefault(g => g.Name == "Southeast Region");
        if (southeastCore is null)
        {
            southeastCore = DynamicGroup.Create(
                regionTypeBackfill.CtrlNbr.Value,
                "Southeast Region",
                parentGroupCtrlNbr: null,
                path: "/southeast",
                isWorkArea: false,
                parentCtrlNbr: csxParentCore.CtrlNbr.Value,
                railroadCtrlNbr: csxRailroadCore.CtrlNbr.Value);
            await groupRepo.AddAsync(southeastCore);
        }

        var jaxSubCore = allGroupsCore.FirstOrDefault(g => g.Name == "Jacksonville Sub");
        if (jaxSubCore is null)
        {
            jaxSubCore = DynamicGroup.Create(
                subdivTypeBackfill.CtrlNbr.Value,
                "Jacksonville Sub",
                parentGroupCtrlNbr: southeastCore.CtrlNbr.Value,
                path: "/southeast/jax",
                isWorkArea: true,
                parentCtrlNbr: csxParentCore.CtrlNbr.Value,
                railroadCtrlNbr: csxRailroadCore.CtrlNbr.Value,
                timeZoneId: "Eastern Standard Time");
            await groupRepo.AddAsync(jaxSubCore);
        }

        // ?? Employees with Addresses, Phone Numbers, Email Addresses ?????
        var employeeRepo = sp.GetRequiredService<IEmployeeRepository>();
        var employeeAppService = sp.GetRequiredService<EmployeeAppService>();
        var userMgr = sp.GetRequiredService<UserManager<User>>();
        var invitationRepo = sp.GetRequiredService<IInvitationRepository>();
        var assignmentRepo = sp.GetRequiredService<IUserParentAssignmentRepository>();

        var existingEmployees = await employeeRepo.GetAllAsync();
        if (existingEmployees.Count == 0)
        {

        var employmentStatusRepo = sp.GetRequiredService<IEmploymentStatusRepository>();
        var addressTypeRepo = sp.GetRequiredService<IAddressTypeRepository>();
        var phoneNumberTypeRepo = sp.GetRequiredService<IPhoneNumberTypeRepository>();
        var emailAddressTypeRepo = sp.GetRequiredService<IEmailAddressTypeRepository>();

        // Reuse ensured baseline parent.
        var csxParent = csxParentCore;
        SetParent(csxParent.CtrlNbr.Value);

        // Reference data
        var activeStatus = EmploymentStatus.Create(csxParent.CtrlNbr.Value, "A", "Active", 1, "FT");
        await employmentStatusRepo.AddAsync(activeStatus);

        var homeAddressType = AddressType.Create(csxParent.CtrlNbr.Value, "Home", 1, emergencyType: false);
        var mailingAddressType = AddressType.Create(csxParent.CtrlNbr.Value, "Mailing", 2, emergencyType: false);
        var otherAddressType = AddressType.Create(csxParent.CtrlNbr.Value, "Other", 3, emergencyType: false);
        await addressTypeRepo.AddAsync(homeAddressType);
        await addressTypeRepo.AddAsync(mailingAddressType);
        await addressTypeRepo.AddAsync(otherAddressType);

        var mobilePhoneType = PhoneNumberType.Create(csxParent.CtrlNbr.Value, "Mobile", 1, emergencyType: false);
        var homePhoneType = PhoneNumberType.Create(csxParent.CtrlNbr.Value, "Home", 2, emergencyType: false);
        var emergencyPhoneType = PhoneNumberType.Create(csxParent.CtrlNbr.Value, "Emergency", 3, emergencyType: true);
        var otherPhoneType = PhoneNumberType.Create(csxParent.CtrlNbr.Value, "Other", 4, emergencyType: false);
        await phoneNumberTypeRepo.AddAsync(mobilePhoneType);
        await phoneNumberTypeRepo.AddAsync(homePhoneType);
        await phoneNumberTypeRepo.AddAsync(emergencyPhoneType);
        await phoneNumberTypeRepo.AddAsync(otherPhoneType);

        var workEmailType = EmailAddressType.Create(csxParent.CtrlNbr.Value, "Work", 1, emergencyType: false);
        var homeEmailType = EmailAddressType.Create(csxParent.CtrlNbr.Value, "Home", 2, emergencyType: false);
        var otherEmailType = EmailAddressType.Create(csxParent.CtrlNbr.Value, "Other", 3, emergencyType: false);
        await emailAddressTypeRepo.AddAsync(workEmailType);
        await emailAddressTypeRepo.AddAsync(homeEmailType);
        await emailAddressTypeRepo.AddAsync(otherEmailType);

        string[] firstNames = ["James", "Mary", "Robert", "Patricia", "John",
                               "Jennifer", "Michael", "Linda", "David", "Elizabeth",
                               "William", "Barbara", "Richard", "Susan", "Joseph",
                               "Jessica", "Thomas", "Sarah", "Christopher", "Karen"];
        string[] lastNames  = ["Smith", "Johnson", "Williams", "Brown", "Jones",
                               "Garcia", "Miller", "Davis", "Rodriguez", "Martinez",
                               "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson",
                               "Thomas", "Taylor", "Moore", "Jackson", "Martin"];
        string[] streets    = ["Main St", "Oak Ave", "Elm St", "Cedar Ln", "Pine Rd",
                               "Maple Dr", "Walnut St", "Birch Ct", "Ash Blvd", "Spruce Way"];
        string[] cities     = ["Jacksonville", "Atlanta", "Chicago", "Nashville", "Dallas",
                               "Denver", "Baltimore", "Louisville", "Charlotte", "Tampa"];
        string[] states     = ["FL", "GA", "IL", "TN", "TX", "CO", "MD", "KY", "NC", "FL"];
        string[] zips       = ["32099", "30301", "60601", "37201", "75201",
                               "80201", "21201", "40201", "28201", "33601"];
        Gender[] genders    = [Gender.Male, Gender.Female];
        Race[] races        = [Race.White, Race.BlackOrAfricanAmerican, Race.Hispanic, Race.Asian, Race.Other];

        for (int i = 0; i < 100; i++)
        {
            var firstName = firstNames[i % firstNames.Length];
            var lastName  = lastNames[i / firstNames.Length % lastNames.Length];
            var empNumber = $"EMP{i + 1:D4}";
            var email     = $"{firstName.ToLower()}.{lastName.ToLower()}{i + 1}@csx.example.com";

            // Create passwordless user + employee + email address atomically via the app service.
            // sendInvitation: false — seeded accounts get dev passwords via userMgr, invitations are not needed.
            var employee = await employeeAppService.CreateAsync(
                csxParent.CtrlNbr,
                email,
                empNumber,
                socialSecurityNumber: $"{100 + i:D3}-{i % 100:D2}-{1000 + i:D4}",
                gender: genders[i % genders.Length],
                race: races[i % races.Length],
                birthDate: new DateTime(1965, 1, 1).AddDays(i * 73),
                employmentDate: new DateTime(2015, 1, 1).AddDays(i * 12),
                activeStatus.CtrlNbr,
                firstName: firstName,
                lastName: lastName,
                sendInvitation: false);

            // Set a known dev password so seed accounts can log in without accepting the invitation.
            var seededUser = await userMgr.FindByEmailAsync(email);
            if (seededUser is not null)
            {
                seededUser.FirstName      = firstName;
                seededUser.LastName       = lastName;
                seededUser.FullName       = $"{firstName} {lastName}";
                seededUser.FullNameLNF    = $"{lastName}, {firstName}";
                seededUser.EmployeeNumber = empNumber;
                await userMgr.UpdateAsync(seededUser);
                await userMgr.AddPasswordAsync(seededUser, "Seed@123");

                // Create Employee parent assignment — mirrors what AcceptInvitationAsync does.
                var csxEmpAssignment = UserParentAssignment.Create(seededUser.Id, csxParent.CtrlNbr, Roles.Employee);
                await assignmentRepo.AddAsync(csxEmpAssignment);
            }

            // Add address and phone — seeder-specific enrichment beyond the app service.
            var freshEmployee = await employeeRepo.GetByCtrlNbrAsync(employee.CtrlNbr);
            if (freshEmployee is not null)
            {
                freshEmployee.AddAddress(
                    $"{100 + i} {streets[i % streets.Length]}",
                    cities[i % cities.Length],
                    states[i % states.Length],
                    zips[i % zips.Length],
                    homeAddressType.CtrlNbr);

                freshEmployee.AddPhoneNumber(
                    $"555-{100 + i:D3}-{1000 + i:D4}",
                    callingOrder: 1,
                    dialOne: true,
                    mobilePhoneType.CtrlNbr);

                await employeeRepo.UpdateAsync(freshEmployee);
            }
        }

        } // end employee guard

        // ?? SMPL Employees with Addresses, Phone Numbers, Email Addresses ????
        var ptraExistingEmployees = await employeeRepo.GetByClientCtrlNbrAsync(ptraParentCore.CtrlNbr);
        if (ptraExistingEmployees.Count == 0)
        {

        SetParent(ptraParentCore.CtrlNbr.Value);

        var ptraEmploymentStatusRepo = sp.GetRequiredService<IEmploymentStatusRepository>();
        var ptraAddressTypeRepo = sp.GetRequiredService<IAddressTypeRepository>();
        var ptraPhoneNumberTypeRepo = sp.GetRequiredService<IPhoneNumberTypeRepository>();
        var ptraEmailAddressTypeRepo = sp.GetRequiredService<IEmailAddressTypeRepository>();

        var ptraRailroadForEmp = (await groupRepo.GetByGroupTypeNameAsync("Railroad", ptraParentCore.CtrlNbr.Value)).First(g => g.Code == "SMPL");

        var ptraActiveStatus = EmploymentStatus.Create(ptraParentCore.CtrlNbr.Value, "A", "Active", 1, "FT");
        await ptraEmploymentStatusRepo.AddAsync(ptraActiveStatus);

        var ptraHomeAddressType = AddressType.Create(ptraParentCore.CtrlNbr.Value, "Home", 1, emergencyType: false);
        var ptraMailingAddressType = AddressType.Create(ptraParentCore.CtrlNbr.Value, "Mailing", 2, emergencyType: false);
        var ptraOtherAddressType = AddressType.Create(ptraParentCore.CtrlNbr.Value, "Other", 3, emergencyType: false);
        await ptraAddressTypeRepo.AddAsync(ptraHomeAddressType);
        await ptraAddressTypeRepo.AddAsync(ptraMailingAddressType);
        await ptraAddressTypeRepo.AddAsync(ptraOtherAddressType);

        var ptraMobilePhoneType = PhoneNumberType.Create(ptraParentCore.CtrlNbr.Value, "Mobile", 1, emergencyType: false);
        var ptraHomePhoneType = PhoneNumberType.Create(ptraParentCore.CtrlNbr.Value, "Home", 2, emergencyType: false);
        var ptraEmergencyPhoneType = PhoneNumberType.Create(ptraParentCore.CtrlNbr.Value, "Emergency", 3, emergencyType: true);
        var ptraOtherPhoneType = PhoneNumberType.Create(ptraParentCore.CtrlNbr.Value, "Other", 4, emergencyType: false);
        await ptraPhoneNumberTypeRepo.AddAsync(ptraMobilePhoneType);
        await ptraPhoneNumberTypeRepo.AddAsync(ptraHomePhoneType);
        await ptraPhoneNumberTypeRepo.AddAsync(ptraEmergencyPhoneType);
        await ptraPhoneNumberTypeRepo.AddAsync(ptraOtherPhoneType);

        var ptraWorkEmailType = EmailAddressType.Create(ptraParentCore.CtrlNbr.Value, "Work", 1, emergencyType: false);
        var ptraHomeEmailType = EmailAddressType.Create(ptraParentCore.CtrlNbr.Value, "Home", 2, emergencyType: false);
        var ptraOtherEmailType = EmailAddressType.Create(ptraParentCore.CtrlNbr.Value, "Other", 3, emergencyType: false);
        await ptraEmailAddressTypeRepo.AddAsync(ptraWorkEmailType);
        await ptraEmailAddressTypeRepo.AddAsync(ptraHomeEmailType);
        await ptraEmailAddressTypeRepo.AddAsync(ptraOtherEmailType);

        string[] ptraFirstNames = ["Antonio", "Brianna", "Carlos", "Destiny", "Eduardo",
                                   "Felicia", "Giovanni", "Hazel", "Isaiah", "Jasmine"];
        string[] ptraLastNames  =
        [
            "Abernathy",    "Beaumont",     "Castellano",   "Delacroix",    "Espinoza",
            "Fontaine",     "Gallagher",    "Hargrove",     "Ibarra",       "Joubert",
            "Kowalski",     "Langford",     "Montoya",      "Nakamura",     "Oberlin",
            "Petrov",       "Quintero",     "Rafferty",     "Sandoval",     "Thibodeaux",
            "Underwood",    "Volkov",       "Whitaker",     "Ximenez",      "Yamamoto",
            "Zachariadis",  "Aldridge",     "Blackwood",    "Cervantes",    "Donnelly",
            "Eckhardt",     "Fairbanks",    "Gutierrez",    "Holloway",     "Ishikawa",
            "Jablonski",    "Kendrick",     "LaFleur",      "Matsumoto",    "Navarro",
            "Olszewski",    "Pemberton",    "Quinlan",      "Rosenberg",    "Stefanovic",
            "Trujillo",     "Urbaniak",     "Villanueva",   "Wainwright",   "Xu",
            "Yoshida",      "Zamora",       "Ashford",      "Belanger",     "Costello",
            "Driscoll",     "Evangelista",  "Fitzpatrick",  "Grimaldi",     "Henriksen",
            "Ivanenko",     "Jorgensen",    "Kovalenko",    "Lombardi",     "McAllister",
            "Nishimura",    "Ostrowski",    "Palomino",     "Quevedo",      "Rutherford",
            "Strickland",   "Tanaka",       "Uribe",        "Vandenberg",   "Wojciechowski",
            "Xander",       "Yamazaki",     "Zimmerman",    "Archibald",    "Brennan",
            "Calloway",     "DeSilva",      "Eriksson",     "Fernandez",    "Grayson",
            "Hutchinson",   "Inoue",        "Jankovic",     "Kavanaugh",    "Lindqvist",
            "Moreau",       "Nakagawa",     "Ochoa",        "Prescott",     "Rasmussen",
            "Sullivan",     "Takahashi",    "Upton",        "Varga",        "Wellington"
        ];
        string[] ptraStreets    = ["Bayou Bend Dr", "Ship Channel Blvd", "Turning Basin St", "Harrisburg Ave", "Navigation Blvd",
                                   "Wayside Dr", "Lawndale Ave", "Broadway St", "Center St", "Pasadena Blvd"];
        string[] ptraCities     = ["Houston", "Pasadena", "Deer Park", "La Porte", "Baytown",
                                   "Channelview", "Galena Park", "Jacinto City", "Seabrook", "Texas City"];
        string[] ptraZips       = ["77001", "77502", "77536", "77571", "77520",
                                   "77530", "77547", "77029", "77586", "77590"];
        Gender[] ptraGenders    = [Gender.Male, Gender.Female];
        Race[] ptraRaces        = [Race.White, Race.BlackOrAfricanAmerican, Race.Hispanic, Race.Asian, Race.Other];

        for (int i = 0; i < 100; i++)
        {
            var firstName = ptraFirstNames[i % ptraFirstNames.Length];
            var lastName  = ptraLastNames[i];
            var empNumber = $"SMPL{i + 1:D4}";
            var email     = $"{firstName.ToLower()}.{lastName.ToLower()}{i + 1}@smpl.example.com";

            // Create passwordless user + employee + email address atomically via the app service.
            // sendInvitation: false — seeded accounts get dev passwords via userMgr, invitations are not needed.
            var employee = await employeeAppService.CreateAsync(
                ptraParentCore.CtrlNbr,
                email,
                empNumber,
                socialSecurityNumber: $"{200 + i:D3}-{i % 100:D2}-{2000 + i:D4}",
                gender: ptraGenders[i % ptraGenders.Length],
                race: ptraRaces[i % ptraRaces.Length],
                birthDate: new DateTime(1968, 1, 1).AddDays(i * 73),
                employmentDate: new DateTime(2016, 1, 1).AddDays(i * 12),
                ptraActiveStatus.CtrlNbr,
                firstName: firstName,
                lastName: lastName,
                sendInvitation: false);

            // Set a known dev password so seed accounts can log in without accepting the invitation.
            var seededUser = await userMgr.FindByEmailAsync(email);
            if (seededUser is not null)
            {
                seededUser.FirstName      = firstName;
                seededUser.LastName       = lastName;
                seededUser.FullName       = $"{firstName} {lastName}";
                seededUser.FullNameLNF    = $"{lastName}, {firstName}";
                seededUser.EmployeeNumber = empNumber;
                await userMgr.UpdateAsync(seededUser);
                await userMgr.AddPasswordAsync(seededUser, "Seed@123");

                // Create Employee parent assignment — mirrors what AcceptInvitationAsync does.
                var ptraEmpAssignment = UserParentAssignment.Create(seededUser.Id, ptraParentCore.CtrlNbr, Roles.Employee);
                await assignmentRepo.AddAsync(ptraEmpAssignment);
            }

            // Add address and phone — seeder-specific enrichment beyond the app service.
            var freshEmployee = await employeeRepo.GetByCtrlNbrAsync(employee.CtrlNbr);
            if (freshEmployee is not null)
            {
                freshEmployee.AddAddress(
                    $"{200 + i} {ptraStreets[i % ptraStreets.Length]}",
                    ptraCities[i % ptraCities.Length],
                    "TX",
                    ptraZips[i % ptraZips.Length],
                    ptraHomeAddressType.CtrlNbr);

                freshEmployee.AddPhoneNumber(
                    $"713-{200 + i:D3}-{2000 + i:D4}",
                    callingOrder: 1,
                    dialOne: true,
                    ptraMobilePhoneType.CtrlNbr);

                await employeeRepo.UpdateAsync(freshEmployee);
            }
        }

        } // end SMPL employee guard

        // ?? Upgrade specific employee assignments via invitation flow ????
        var csxCorp = csxParentCore;
        var allEmployees = await employeeRepo.GetAllAsync();

        // Idempotent guard -- skip if the first employee was already upgraded to a non-Employee role
        var firstEmpAssignments = allEmployees.Count > 0
            ? await assignmentRepo.GetByUserAndParentAsync(allEmployees[0].UserId, csxCorp.CtrlNbr.Value)
            : [];
        var alreadyUpgraded = firstEmpAssignments.Any(a => a.Role != Roles.Employee);

        if (!alreadyUpgraded)
        {

        // Upgrade first 6 employees to distinct roles (they already have Employee from above)
        string[] rolesToUpgrade = [Roles.ParentAdmin, Roles.RailroadAdmin, "CraftManager", "CrewManager", "Dispatcher", "PayrollClerk"];
        for (int r = 0; r < rolesToUpgrade.Length && r < allEmployees.Count; r++)
        {
            // Create a role-upgrade invitation (auto-accepted)
            var rrCtrlNbr = Roles.RequiresRailroad(rolesToUpgrade[r]) ? csxRailroadCore.CtrlNbr : null;
            var upgradeInvite = Invitation.Create(
                allEmployees[r].EmailAddresses.Count > 0 ? allEmployees[r].EmailAddresses[0].Email : $"emp-{r}@csx.example.com",
                csxCorp.CtrlNbr.Value,
                rolesToUpgrade[r],
                "SYSTEM",
                railroadCtrlNbr: rrCtrlNbr);
            upgradeInvite.Accept();
            await invitationRepo.AddAsync(upgradeInvite);

            // Update or replace existing assignment and supersede original invitations
            var existingAssignments = await assignmentRepo.GetByUserAndParentAsync(allEmployees[r].UserId, csxCorp.CtrlNbr.Value);
            var isParentScoped = !Roles.RequiresRailroad(rolesToUpgrade[r]);
            var upgradeEmail = allEmployees[r].EmailAddresses.Count > 0 ? allEmployees[r].EmailAddresses[0].Email : $"emp-{r}@csx.example.com";

            if (isParentScoped)
            {
                // Parent-scoped upgrade: remove all railroad-scoped assignments, create parent-scoped
                foreach (var old in existingAssignments)
                    await assignmentRepo.DeleteAsync(old.CtrlNbr);

                var newAssignment = UserParentAssignment.Create(allEmployees[r].UserId, csxCorp.CtrlNbr.Value, rolesToUpgrade[r]);
                await assignmentRepo.AddAsync(newAssignment);
            }
            else
            {
                // Railroad-scoped upgrade: update matching assignment
                var matchingAssignment = existingAssignments.FirstOrDefault(a => a.RailroadCtrlNbr == rrCtrlNbr);
                if (matchingAssignment is not null)
                {
                    matchingAssignment.UpdateRole(rolesToUpgrade[r], rrCtrlNbr);
                    await assignmentRepo.UpdateAsync(matchingAssignment);
                }
            }

            // Mark all prior accepted invitations for this parent as superseded
            var oldInvitations = await invitationRepo.GetAcceptedByEmailAndParentAsync(upgradeEmail, csxCorp.CtrlNbr);
            foreach (var oldInv in oldInvitations.Where(i => i.CtrlNbr != upgradeInvite.CtrlNbr))
            {
                oldInv.MarkSuperseded();
                await invitationRepo.UpdateAsync(oldInv);
            }
        }

        } // end upgrade guard

        // ?? Section 3b: Departments ???????????????????????????????????????????
        var departmentRepo = sp.GetRequiredService<IDepartmentRepository>();
        var craftRepo = sp.GetRequiredService<ICraftRepository>();
        var departmentReassignmentRuleRepo = sp.GetRequiredService<IDepartmentReassignmentRuleRepository>();
        var departmentAbsenceRequestWindowPolicyRepo = sp.GetRequiredService<IDepartmentAbsenceRequestWindowPolicyRepository>();
        var craftAbsenceWaitListPolicyRepo = sp.GetRequiredService<ICraftAbsenceWaitListPolicyRepository>();
        var existingDepts = await departmentRepo.GetAllAsync();
        if (existingDepts.Count == 0)
        {
        var csxRR = (await groupRepo.GetByGroupTypeNameAsync("Railroad")).First(g => g.Code == "CSX");
        var ptraRR = (await groupRepo.GetByGroupTypeNameAsync("Railroad", ptraParentCore.CtrlNbr.Value)).First(g => g.Code == "SMPL");
        var csxTransportation = Department.Create(csxParentCtrlNbr, csxRR.CtrlNbr, "Transportation", defaultCallSheetView: "Horizontal");
        var csxClerical = Department.Create(csxParentCtrlNbr, csxRR.CtrlNbr, "Clerical", defaultCallSheetView: "Vertical");
        var ptraTransportation = Department.Create(ptraParentCore.CtrlNbr.Value, ptraRR.CtrlNbr, "Transportation", defaultCallSheetView: "Horizontal");
        SetParent(csxParentCtrlNbr);
        await departmentRepo.AddAsync(csxTransportation);
        await departmentRepo.AddAsync(csxClerical);
        var ptraClerical = Department.Create(ptraParentCore.CtrlNbr.Value, ptraRR.CtrlNbr, "Clerical", defaultCallSheetView: "Vertical");
        SetParent(ptraParentCore.CtrlNbr.Value);
        await departmentRepo.AddAsync(ptraTransportation);
        await departmentRepo.AddAsync(ptraClerical);
        } // end departments guard

        // Backfill department default views to align with seeded UX expectations.
        var railroadGroupsForDeptBackfill = await groupRepo.GetByGroupTypeNameAsync("Railroad");
        var ptraRailroadForDeptBackfill = (await groupRepo.GetByGroupTypeNameAsync("Railroad", ptraParentCore.CtrlNbr.Value)).FirstOrDefault(g => g.Code == "SMPL");
        var csxRailroadForDeptBackfill = railroadGroupsForDeptBackfill.FirstOrDefault(g => g.Code == "CSX");
        var deptViewByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Transportation"] = "Horizontal",
            ["Clerical"] = "Vertical"
        };

        var allDepartmentsForDeptBackfill = await departmentRepo.GetAllAsync();
        foreach (var dept in allDepartmentsForDeptBackfill.Where(d => deptViewByName.ContainsKey(d.Name)))
        {
            if (dept.DynamicGroupCtrlNbr is null || dept.ParentCtrlNbr is null)
                continue;

            var isPtraRailroadDept = ptraRailroadForDeptBackfill is not null && dept.DynamicGroupCtrlNbr == ptraRailroadForDeptBackfill.CtrlNbr;
            var isCsxRailroadDept = csxRailroadForDeptBackfill is not null && dept.DynamicGroupCtrlNbr == csxRailroadForDeptBackfill.CtrlNbr;
            if (!isPtraRailroadDept && !isCsxRailroadDept)
                continue;

            var expectedView = deptViewByName[dept.Name];
            if (!string.Equals(dept.DefaultCallSheetView, expectedView, StringComparison.OrdinalIgnoreCase))
            {
                SetParent(dept.ParentCtrlNbr.Value);
                dept.Update(dept.Name, expectedView);
                await departmentRepo.UpdateAsync(dept);
            }
        }

        // Seed call sheet rules for Transportation and Clerical using the simplified first-calling-start model.
        var callSheetRuleRepo = sp.GetRequiredService<ICallSheetRuleRepository>();
        var allDepartments = await departmentRepo.GetAllAsync();
        var ruleDepartments = allDepartments
            .Where(d => d.Name.Equals("Transportation", StringComparison.OrdinalIgnoreCase)
                     || d.Name.Equals("Clerical", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var existingRules = await callSheetRuleRepo.GetByDepartmentsAsync(ruleDepartments.Select(d => d.CtrlNbr));
        var existingRuleByDept = existingRules.ToDictionary(r => r.DepartmentCtrlNbr);

        foreach (var dept in ruleDepartments)
        {
            if (dept.ParentCtrlNbr is null)
                continue;

            SetParent(dept.ParentCtrlNbr.Value);

            var callLeadMinutes = 90;
            var callDurationMinutes = 30;
            var buildLeadHours = 14;
            var globalPreCreateOffsetMinutes = -(buildLeadHours * 60);

            if (!existingRuleByDept.TryGetValue(dept.CtrlNbr, out var existingRule))
            {
                await callSheetRuleRepo.AddAsync(CallSheetRule.Create(
                    dept.CtrlNbr,
                    callLeadMinutes,
                    callDurationMinutes,
                    CallSheetHolidayAdjustmentType.None,
                    holidayCustomOffsetMinutes: null,
                    globalPreCreateOffsetMinutes: globalPreCreateOffsetMinutes,
                    isEnabled: true));
                continue;
            }

            existingRule.Update(
                callLeadMinutes,
                callDurationMinutes,
                CallSheetHolidayAdjustmentType.None,
                holidayCustomOffsetMinutes: null,
                globalPreCreateOffsetMinutes: globalPreCreateOffsetMinutes,
                isEnabled: true);
            await callSheetRuleRepo.UpdateAsync(existingRule);
        }

        // Seed department request-window caps to match configured defaults.
        // Transportation = 45 days, Clerical = no cap entry.
        var allDepartmentRequestWindowPolicies = await departmentAbsenceRequestWindowPolicyRepo.GetAllAsync();
        var departmentRequestWindowPolicyByDepartment = allDepartmentRequestWindowPolicies
            .ToDictionary(p => p.DepartmentCtrlNbr);

        foreach (var transportationDepartment in allDepartments.Where(d => d.Name.Equals("Transportation", StringComparison.OrdinalIgnoreCase)))
        {
            if (transportationDepartment.ParentCtrlNbr is null)
                continue;

            SetParent(transportationDepartment.ParentCtrlNbr.Value);

            if (!departmentRequestWindowPolicyByDepartment.TryGetValue(transportationDepartment.CtrlNbr, out var requestWindowPolicy))
            {
                await departmentAbsenceRequestWindowPolicyRepo.AddAsync(
                    DepartmentAbsenceRequestWindowPolicy.Create(transportationDepartment.CtrlNbr, requestWindowCapDays: 45));
                continue;
            }

            if (requestWindowPolicy.RequestWindowCapDays != 45)
            {
                requestWindowPolicy.Update(45);
                await departmentAbsenceRequestWindowPolicyRepo.UpdateAsync(requestWindowPolicy);
            }
        }

        foreach (var clericalDepartment in allDepartments.Where(d => d.Name.Equals("Clerical", StringComparison.OrdinalIgnoreCase)))
        {
            if (departmentRequestWindowPolicyByDepartment.TryGetValue(clericalDepartment.CtrlNbr, out var clericalPolicy))
                await departmentAbsenceRequestWindowPolicyRepo.DeleteAsync(clericalPolicy.CtrlNbr);
        }

        // Seed craft waitlist defaults to match configured defaults.
        // Clerical, Engineer, Trainman => Comp Day Max 3, Vacation Week Max 3, Enabled.
        var allCraftWaitListPolicies = await craftAbsenceWaitListPolicyRepo.GetAllAsync();
        var craftWaitListPolicyByCraft = allCraftWaitListPolicies
            .ToDictionary(p => p.CraftCtrlNbr);

        var allCrafts = await craftRepo.GetAllAsync();
        var waitListCraftNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Clerical",
            "Engineer",
            "Trainman"
        };

        foreach (var craft in allCrafts.Where(c => waitListCraftNames.Contains(c.CraftName)))
        {
            if (craft.ParentCtrlNbr is null)
                continue;

            SetParent(craft.ParentCtrlNbr.Value);

            if (!craftWaitListPolicyByCraft.TryGetValue(craft.CtrlNbr, out var waitListPolicy))
            {
                await craftAbsenceWaitListPolicyRepo.AddAsync(
                    CraftAbsenceWaitListPolicy.Create(
                        craft.CtrlNbr,
                        compensableDayMaxAssignments: 3,
                        vacationWeekMaxAssignments: 3,
                        isEnabled: true));
                continue;
            }

            if (waitListPolicy.CompensableDayMaxAssignments != 3
                || waitListPolicy.VacationWeekMaxAssignments != 3
                || !waitListPolicy.IsEnabled)
            {
                waitListPolicy.Update(3, 3, isEnabled: true);
                await craftAbsenceWaitListPolicyRepo.UpdateAsync(waitListPolicy);
            }
        }

        // ?? Section 4: Seniority � Crafts, Rosters, Rankings ?????????????
        var rosterRepo = sp.GetRequiredService<IRosterRepository>();
        var seniorityRepo = sp.GetRequiredService<ISeniorityRepository>();
        var uowFactory = sp.GetRequiredService<IOrchestrationUnitOfWorkFactory>();
        var newHireSvc = sp.GetRequiredService<NewHireService>();
        var regQualRepoForNewHires = sp.GetRequiredService<IRegulatoryQualificationRepository>();
        var craftAppSvc = sp.GetRequiredService<CraftAppService>();

        var existingCrafts = await craftRepo.GetAllAsync();
        if (existingCrafts.Count == 0)
        {
        // Crafts at railroad level with parent ownership
        var csxRailroadForCraft = (await groupRepo.GetByGroupTypeNameAsync("Railroad")).First(g => g.Code == "CSX");
        var ptraRailroadForCraft = (await groupRepo.GetByGroupTypeNameAsync("Railroad", ptraParentCore.CtrlNbr.Value)).First(g => g.Code == "SMPL");
        var allDepts = await departmentRepo.GetAllAsync();
        var csxTransDept = allDepts.First(d => d.Name == "Transportation" && d.ParentCtrlNbr == csxParentCtrlNbr);
        var csxClericalDept = allDepts.First(d => d.Name == "Clerical" && d.ParentCtrlNbr == csxParentCtrlNbr);
        var ptraTransDept = allDepts.First(d => d.Name == "Transportation" && d.ParentCtrlNbr == ptraParentCore.CtrlNbr);
        var ptraClericalDept = allDepts.First(d => d.Name == "Clerical" && d.ParentCtrlNbr == ptraParentCore.CtrlNbr);
        var csxWorkArea = (await groupRepo.GetAllAsync()).First(g => g.Name == "Jacksonville Sub");
        var ptraWorkArea = ptraRailroadForCraft; // PTRA railroad is itself a work area

        // CSX Crafts (owned by CSX railroad under CSX Corporation parent)
        SetParent(csxParentCtrlNbr);
        var (csxEngineer, csxEngRosterNullable, _) = await craftAppSvc.CreateCraftAsync(
            csxParentCtrlNbr, csxRailroadForCraft.CtrlNbr, "Engineer", "Engineers", 1,
            autoMarkUp: false, approveAllMarkOffs: false, markOffHours: 10, markUpHours: 10,
            requiredRestHours: 10, maximumVacationDayTime: 480, unpaidMealPeriodMinutes: 0,
            hoursofService: true, processPayroll: true, showNotifications: true, vacationAssignmentType: 1,
            departmentCtrlNbr: csxTransDept.CtrlNbr, workAreaCtrlNbr: csxWorkArea.CtrlNbr);
        var csxEngRoster = csxEngRosterNullable ?? throw new InvalidOperationException("Roster not created for CSX Engineer craft.");

        var (csxConductor, csxCondRosterNullable, _) = await craftAppSvc.CreateCraftAsync(
            csxParentCtrlNbr, csxRailroadForCraft.CtrlNbr, "Trainman", "Trainmen", 2,
            autoMarkUp: false, approveAllMarkOffs: false, markOffHours: 10, markUpHours: 10,
            requiredRestHours: 10, maximumVacationDayTime: 480, unpaidMealPeriodMinutes: 0,
            hoursofService: true, processPayroll: true, showNotifications: true, vacationAssignmentType: 1,
            departmentCtrlNbr: csxTransDept.CtrlNbr, workAreaCtrlNbr: csxWorkArea.CtrlNbr);
        var csxCondRoster = csxCondRosterNullable ?? throw new InvalidOperationException("Roster not created for CSX Trainman craft.");

        var (csxClerical, csxClericalRosterNullable, _) = await craftAppSvc.CreateCraftAsync(
            csxParentCtrlNbr, csxRailroadForCraft.CtrlNbr, "Clerical", "Clerical", 3,
            autoMarkUp: true, approveAllMarkOffs: true, markOffHours: 0, markUpHours: 0,
            requiredRestHours: 0, maximumVacationDayTime: 480, unpaidMealPeriodMinutes: 30,
            hoursofService: false, processPayroll: true, showNotifications: true, vacationAssignmentType: 0,
            departmentCtrlNbr: csxClericalDept.CtrlNbr, workAreaCtrlNbr: csxWorkArea.CtrlNbr);
        var csxClericalRoster = csxClericalRosterNullable ?? throw new InvalidOperationException("Roster not created for CSX Clerical craft.");

        // PTRA Crafts
        SetParent(ptraParentCore.CtrlNbr.Value);
        var (ptraEngineer, ptraEngRosterNullable, _) = await craftAppSvc.CreateCraftAsync(
            ptraParentCore.CtrlNbr, ptraRailroadForCraft.CtrlNbr, "Engineer", "Engineers", 1,
            autoMarkUp: false, approveAllMarkOffs: false, markOffHours: 10, markUpHours: 10,
            requiredRestHours: 10, maximumVacationDayTime: 480, unpaidMealPeriodMinutes: 0,
            hoursofService: true, processPayroll: true, showNotifications: true, vacationAssignmentType: 1,
            departmentCtrlNbr: ptraTransDept.CtrlNbr, workAreaCtrlNbr: ptraWorkArea.CtrlNbr);
        var ptraEngRoster = ptraEngRosterNullable ?? throw new InvalidOperationException("Roster not created for PTRA Engineer craft.");

        var (ptraConductor, ptraCondRosterNullable, _) = await craftAppSvc.CreateCraftAsync(
            ptraParentCore.CtrlNbr, ptraRailroadForCraft.CtrlNbr, "Trainman", "Trainmen", 2,
            autoMarkUp: false, approveAllMarkOffs: false, markOffHours: 10, markUpHours: 10,
            requiredRestHours: 10, maximumVacationDayTime: 480, unpaidMealPeriodMinutes: 0,
            hoursofService: true, processPayroll: true, showNotifications: true, vacationAssignmentType: 1,
            departmentCtrlNbr: ptraTransDept.CtrlNbr, workAreaCtrlNbr: ptraWorkArea.CtrlNbr);
        var ptraCondRoster = ptraCondRosterNullable ?? throw new InvalidOperationException("Roster not created for PTRA Trainman craft.");
        var ptraCondTrainingRoster = await rosterRepo.GetTrainingRosterByCraftAsync(ptraConductor.CtrlNbr)
            ?? throw new InvalidOperationException("Training roster not created for PTRA Conductor craft.");

        var (ptraClerical, ptraClericalRosterNullable, _) = await craftAppSvc.CreateCraftAsync(
            ptraParentCore.CtrlNbr, ptraRailroadForCraft.CtrlNbr, "Clerical", "Clerical", 3,
            autoMarkUp: true, approveAllMarkOffs: true, markOffHours: 0, markUpHours: 0,
            requiredRestHours: 0, maximumVacationDayTime: 480, unpaidMealPeriodMinutes: 30,
            hoursofService: false, processPayroll: true, showNotifications: true, vacationAssignmentType: 0,
            departmentCtrlNbr: ptraClericalDept.CtrlNbr, workAreaCtrlNbr: ptraWorkArea.CtrlNbr);
        var ptraClericalRoster = ptraClericalRosterNullable ?? throw new InvalidOperationException("Roster not created for PTRA Clerical craft.");
        // Assign Annualized Average strategy to PTRA Transportation crafts — moved outside guard, see below

        SetParent(csxParentCtrlNbr);
        // Seniority entries: 40 Engineer, 50 Trainman, 10 Clerical
        // Employees are hired in groups sharing the same seniority date.
        // Rank is a tiebreaker within each hire date (1 if sole hire, 1..N for group hires).
        var empList = await employeeRepo.GetAllAsync();

        var csxStates = await seniorityStateRepo.GetByParentCtrlNbrAsync(csxParentCore.CtrlNbr);
        var activeState = csxStates.First(s => s.StateDescription == "Active");

        // Hire group sizes per craft (each number = employees sharing one seniority date)
        int[] engGroups = [5, 3, 1, 4, 5, 3, 4, 1, 5, 4, 3, 2]; // 40 Engineers
        int[] trnGroups = [4, 5, 1, 3, 5, 4, 1, 5, 3, 4, 5, 3, 4, 3]; // 50 Trainmen
        int[] clrGroups = [3, 1, 2, 1, 3]; // 10 Clerical

        int empIdx = 0;

        // Engineer roster: hire dates starting 2015-01-01, ~45 days apart
        var engDate = new DateTime(2015, 1, 1);
        foreach (var groupSize in engGroups)
        {
            for (int r = 0; r < groupSize; r++)
            {
                await seniorityRepo.AddAsync(Seniority.Create(
                    csxEngRoster.CtrlNbr, empList[empIdx].CtrlNbr,
                    lastActiveRoster: true, rosterDate: engDate,
                    rank: r + 1, seniorityStateCtrlNbr: activeState.CtrlNbr,
                    canTrain: empIdx % 5 == 0));
                empIdx++;
            }
            engDate = engDate.AddDays(45);
        }

        // Trainman roster: hire dates starting 2015-02-01, ~35 days apart
        var trnDate = new DateTime(2015, 2, 1);
        foreach (var groupSize in trnGroups)
        {
            for (int r = 0; r < groupSize; r++)
            {
                await seniorityRepo.AddAsync(Seniority.Create(
                    csxCondRoster.CtrlNbr, empList[empIdx].CtrlNbr,
                    lastActiveRoster: true, rosterDate: trnDate,
                    rank: r + 1, seniorityStateCtrlNbr: activeState.CtrlNbr,
                    canTrain: empIdx % 5 == 0));
                empIdx++;
            }
            trnDate = trnDate.AddDays(35);
        }

        // Clerical roster: hire dates starting 2015-06-01, ~60 days apart
        var clrDate = new DateTime(2015, 6, 1);
        foreach (var groupSize in clrGroups)
        {
            for (int r = 0; r < groupSize; r++)
            {
                await seniorityRepo.AddAsync(Seniority.Create(
                    csxClericalRoster.CtrlNbr, empList[empIdx].CtrlNbr,
                    lastActiveRoster: true, rosterDate: clrDate,
                    rank: r + 1, seniorityStateCtrlNbr: activeState.CtrlNbr,
                    canTrain: empIdx % 5 == 0));
                empIdx++;
            }
            clrDate = clrDate.AddDays(60);
        }

        // PTRA Seniority: 30 Engineer, 60 Trainman, 10 Clerical
        SetParent(ptraParentCore.CtrlNbr.Value);
        var ptraEmpList = await employeeRepo.GetByClientCtrlNbrAsync(ptraParentCore.CtrlNbr);
        var ptraCfr242swQ = await regQualRepoForNewHires.GetByCodeAsync("CFR-242-SWITCHMAN");

        if (ptraEmpList.Count > 0)
        {
        var ptraSenStates = await seniorityStateRepo.GetByParentCtrlNbrAsync(ptraParentCore.CtrlNbr);
        var ptraActiveSenState = ptraSenStates.First(s => s.StateDescription == "Active");

        // Hire group sizes per craft (each number = employees sharing one seniority date)
        int[] ptraEngGroups = [4, 3, 2, 5, 3, 4, 2, 3, 2, 2]; // 30 Engineers
        int[] ptraClrGroups = [3, 2, 1, 2, 2]; // 10 Clerical

        int ptraEmpIdx = 0;

        // Engineer roster: hire dates starting 2016-01-01, ~50 days apart
        var ptraEngDate = new DateTime(2016, 1, 1);
        foreach (var groupSize in ptraEngGroups)
        {
            for (int r = 0; r < groupSize; r++)
            {
                await seniorityRepo.AddAsync(Seniority.Create(
                    ptraEngRoster.CtrlNbr, ptraEmpList[ptraEmpIdx].CtrlNbr,
                    lastActiveRoster: true, rosterDate: ptraEngDate,
                    rank: r + 1, seniorityStateCtrlNbr: ptraActiveSenState.CtrlNbr,
                    canTrain: ptraEmpIdx % 5 == 0));
                ptraEmpIdx++;
            }
            ptraEngDate = ptraEngDate.AddDays(50);
        }

        // Trainman roster: first 11 groups are veteran hires distributed dynamically across wide
        // seniority ranges. Last 3 groups are recent hires under 90 days so Helper Only logic can
        // be validated without reseeding static dates.
        var ptraTrnToday = DateTime.UtcNow.Date;
        int[] ptraTrnVeteranGroups = [5, 4, 3, 6, 4, 5, 3, 4, 5, 3, 6]; // 48 veterans
        int[] ptraTrnVeteranDaysAgo = [3200, 2600, 2100, 1700, 1300, 950, 700, 500, 365, 240, 120];
        int[] ptraTrnNewHireGroups = [4, 3, 5];                           // 12 new hires — Helper Only
        for (int g = 0; g < ptraTrnVeteranGroups.Length; g++)
        {
            var groupSize = ptraTrnVeteranGroups[g];
            var ptraTrnDate = ptraTrnToday.AddDays(-ptraTrnVeteranDaysAgo[g]);
            for (int r = 0; r < groupSize; r++)
            {
                await seniorityRepo.AddAsync(Seniority.Create(
                    ptraCondRoster.CtrlNbr, ptraEmpList[ptraEmpIdx].CtrlNbr,
                    lastActiveRoster: true, rosterDate: ptraTrnDate,
                    rank: r + 1, seniorityStateCtrlNbr: ptraActiveSenState.CtrlNbr,
                    canTrain: ptraEmpIdx % 5 == 0));
                ptraEmpIdx++;
            }
        }
        // New hires: seeded via NewHireService for atomic onboarding (seniority + pending cert + board placement)
        // Seniority dates 75, 45, and 20 days ago — all under the 90-day Helper Only threshold
        int[] ptraTrnNewHireDaysAgo = [75, 45, 20];
        for (int g = 0; g < ptraTrnNewHireGroups.Length; g++)
        {
            var newHireDate = ptraTrnToday.AddDays(-ptraTrnNewHireDaysAgo[g]);
            for (int r = 0; r < ptraTrnNewHireGroups[g]; r++)
            {
                var assignedRank = r + 1;
                SetParent(ptraParentCore.CtrlNbr.Value);
                // The older new-hire cohorts are promoted onto the active Trainman roster so they
                // appear on the Trainman roster + extra board flows. Only the most recent cohort
                // remains on the Training roster/New Hire board.
                if (g < ptraTrnNewHireGroups.Length - 1)
                {
                    await seniorityRepo.AddAsync(Seniority.Create(
                        ptraCondRoster.CtrlNbr,
                        ptraEmpList[ptraEmpIdx].CtrlNbr,
                        lastActiveRoster: true,
                        rosterDate: newHireDate,
                        rank: assignedRank,
                        seniorityStateCtrlNbr: ptraActiveSenState.CtrlNbr,
                        canTrain: false));
                }
                else
                {
                    await newHireSvc.OnboardAsync(
                        employeeCtrlNbr: ptraEmpList[ptraEmpIdx].CtrlNbr,
                        craftCtrlNbr: ptraConductor.CtrlNbr,
                        trainingRosterCtrlNbr: ptraCondTrainingRoster.CtrlNbr,
                        seniorityStateCtrlNbr: ptraActiveSenState.CtrlNbr,
                        hireDate: newHireDate,
                        regulatoryQualificationCtrlNbr: ptraCfr242swQ?.CtrlNbr,
                        rank: assignedRank);
                }

                ptraEmpIdx++;
            }
        }

        // Clerical roster: hire dates starting 2016-06-01, ~60 days apart
        var ptraClrDate = new DateTime(2016, 6, 1);
        foreach (var groupSize in ptraClrGroups)
        {
            for (int r = 0; r < groupSize; r++)
            {
                await seniorityRepo.AddAsync(Seniority.Create(
                    ptraClericalRoster.CtrlNbr, ptraEmpList[ptraEmpIdx].CtrlNbr,
                    lastActiveRoster: true, rosterDate: ptraClrDate,
                    rank: r + 1, seniorityStateCtrlNbr: ptraActiveSenState.CtrlNbr,
                    canTrain: ptraEmpIdx % 5 == 0));
                ptraEmpIdx++;
            }
            ptraClrDate = ptraClrDate.AddDays(60);
        }

        }

        } // end seniority guard

        // ?? Section 5: Work Management
        var craftRoleRepo = sp.GetRequiredService<ICraftRoleRepository>();
        var workInstanceRepo = sp.GetRequiredService<IWorkInstanceRepository>();
        var positionSlotRepo = sp.GetRequiredService<IPositionSlotRepository>();

        var existingRoles = await craftRoleRepo.GetAllAsync();
        if (existingRoles.Count == 0)
        {
        SetParent(csxParentCtrlNbr);
        // Re-lookup crafts and work areas for FK references
        var crafts = await craftRepo.GetAllAsync();
        var csxRailroadWM = (await groupRepo.GetByGroupTypeNameAsync("Railroad")).First(g => g.Code == "CSX");
        var jaxSubWM = (await groupRepo.GetAllAsync()).First(g => g.Name == "Jacksonville Sub");
        var engCraft = crafts.First(c => c.CraftName == "Engineer" && c.DynamicGroupCtrlNbr == csxRailroadWM.CtrlNbr);
        var condCraft = crafts.First(c => c.CraftName == "Trainman" && c.DynamicGroupCtrlNbr == csxRailroadWM.CtrlNbr);
        var clerCraft = crafts.First(c => c.CraftName == "Clerical" && c.DynamicGroupCtrlNbr == csxRailroadWM.CtrlNbr);

        // Craft Roles � Trainman craft (hierarchy: Student Trainman < Trainman < Conductor)
        var studentTrainman = CraftRole.Create(condCraft.CtrlNbr, "STRN", "Student Trainman", hierarchyLevel: 0);
        var trainman = CraftRole.Create(condCraft.CtrlNbr, "TRMN", "Trainman", hierarchyLevel: 1);
        var conductor = CraftRole.Create(condCraft.CtrlNbr, "COND", "Conductor", hierarchyLevel: 2);
        await craftRoleRepo.AddAsync(studentTrainman);
        await craftRoleRepo.AddAsync(trainman);
        await craftRoleRepo.AddAsync(conductor);

        // Craft Roles � Engineer craft (hierarchy: Student Engineer < Engineer)
        var studentEngineer = CraftRole.Create(engCraft.CtrlNbr, "SENG", "Student Engineer", hierarchyLevel: 0);
        var engineer = CraftRole.Create(engCraft.CtrlNbr, "ENGR", "Engineer", hierarchyLevel: 1);
        await craftRoleRepo.AddAsync(studentEngineer);
        await craftRoleRepo.AddAsync(engineer);

        // Craft Roles � Clerical craft
        var crewDispatcher = CraftRole.Create(clerCraft.CtrlNbr, "DISP", "Crew Dispatcher");
        await craftRoleRepo.AddAsync(crewDispatcher);

        // Craft Roles - PTRA Engineer craft
        SetParent(ptraParentCore.CtrlNbr.Value);
        var ptraRailroadWM = (await groupRepo.GetByGroupTypeNameAsync("Railroad", ptraParentCore.CtrlNbr.Value)).First(g => g.Code == "SMPL");
        var ptraEngCraft = crafts.First(c => c.CraftName == "Engineer" && c.DynamicGroupCtrlNbr == ptraRailroadWM.CtrlNbr);
        var ptraCondCraft = crafts.First(c => c.CraftName == "Trainman" && c.DynamicGroupCtrlNbr == ptraRailroadWM.CtrlNbr);
        var ptraBoardsForRoles = await sp.GetRequiredService<IRosterBoardRepository>().GetAllAsync();
        var ptraEngineerExtraBoard = ptraBoardsForRoles
            .FirstOrDefault(b => b.CraftCtrlNbr == ptraEngCraft.CtrlNbr && b.BoardType == BoardType.ExtraBoard && b.Name == "Engineer Extra Board")
            ?? ptraBoardsForRoles.First(b => b.CraftCtrlNbr == ptraEngCraft.CtrlNbr && b.BoardType == BoardType.ExtraBoard);
        var ptraEngineerNewHireBoard = ptraBoardsForRoles
            .FirstOrDefault(b => b.CraftCtrlNbr == ptraEngCraft.CtrlNbr && b.BoardType == BoardType.NewHire && b.Name == "Engineer New Hires")
            ?? ptraBoardsForRoles.First(b => b.CraftCtrlNbr == ptraEngCraft.CtrlNbr && b.BoardType == BoardType.NewHire);
        var ptraTrainmanExtraBoard = ptraBoardsForRoles
            .FirstOrDefault(b => b.CraftCtrlNbr == ptraCondCraft.CtrlNbr && b.BoardType == BoardType.ExtraBoard && b.Name == "Trainman Extra Board")
            ?? ptraBoardsForRoles.First(b => b.CraftCtrlNbr == ptraCondCraft.CtrlNbr && b.BoardType == BoardType.ExtraBoard);
        var ptraTrainmanNewHireBoard = ptraBoardsForRoles
            .FirstOrDefault(b => b.CraftCtrlNbr == ptraCondCraft.CtrlNbr && b.BoardType == BoardType.NewHire && b.Name == "Trainman New Hires")
            ?? ptraBoardsForRoles.First(b => b.CraftCtrlNbr == ptraCondCraft.CtrlNbr && b.BoardType == BoardType.NewHire);

        var ptraEngineerRole = CraftRole.Create(
            ptraEngCraft.CtrlNbr,
            "E",
            "Engineer",
            defaultRosterBoardCtrlNbr: ptraEngineerExtraBoard.CtrlNbr);
        var ptraEngineerTraineeRole = CraftRole.Create(
            ptraEngCraft.CtrlNbr,
            "ET",
            "Engineer Trainee",
            hierarchyLevel: 0,
            defaultRosterBoardCtrlNbr: ptraEngineerNewHireBoard.CtrlNbr);
        await craftRoleRepo.AddAsync(ptraEngineerRole);
        await craftRoleRepo.AddAsync(ptraEngineerTraineeRole);

        // Craft Roles - PTRA Trainman craft (hierarchy: Trainman Trainee < Helper < Foreman)
        var ptraForeman = CraftRole.Create(
            ptraCondCraft.CtrlNbr,
            "F",
            "Foreman",
            hierarchyLevel: 2,
            defaultRosterBoardCtrlNbr: ptraTrainmanExtraBoard.CtrlNbr);
        var ptraHelper = CraftRole.Create(
            ptraCondCraft.CtrlNbr,
            "H",
            "Helper",
            hierarchyLevel: 1,
            defaultRosterBoardCtrlNbr: ptraTrainmanExtraBoard.CtrlNbr);
        var ptraTrainmanTrainee = CraftRole.Create(
            ptraCondCraft.CtrlNbr,
            "TT",
            "Trainman Trainee",
            hierarchyLevel: 0,
            defaultRosterBoardCtrlNbr: ptraTrainmanNewHireBoard.CtrlNbr);
        await craftRoleRepo.AddAsync(ptraForeman);
        await craftRoleRepo.AddAsync(ptraHelper);
        await craftRoleRepo.AddAsync(ptraTrainmanTrainee);


        SetParent(csxParentCtrlNbr);
        // Work Instances
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var wi101Today = WorkInstance.Create(null, jaxSubWM.CtrlNbr, today.AddHours(6), today.AddHours(18), today.AddHours(5));
        var wi101Tomorrow = WorkInstance.Create(null, jaxSubWM.CtrlNbr, tomorrow.AddHours(6), tomorrow.AddHours(18), tomorrow.AddHours(5));
        var wi202Today = WorkInstance.Create(null, jaxSubWM.CtrlNbr, today.AddHours(7), today.AddHours(15), today.AddHours(6));
        var wi202Tomorrow = WorkInstance.Create(null, jaxSubWM.CtrlNbr, tomorrow.AddHours(7), tomorrow.AddHours(15), tomorrow.AddHours(6));
        var wi303Today = WorkInstance.Create(null, jaxSubWM.CtrlNbr, today.AddHours(22), today.AddDays(1).AddHours(10), today.AddHours(21));
        var wi303Tomorrow = WorkInstance.Create(null, jaxSubWM.CtrlNbr, tomorrow.AddHours(22), tomorrow.AddDays(1).AddHours(10), tomorrow.AddHours(21));
        await workInstanceRepo.AddAsync(wi101Today);
        await workInstanceRepo.AddAsync(wi101Tomorrow);
        await workInstanceRepo.AddAsync(wi202Today);
        await workInstanceRepo.AddAsync(wi202Tomorrow);
        await workInstanceRepo.AddAsync(wi303Today);
        await workInstanceRepo.AddAsync(wi303Tomorrow);

        // Position Slots � each work instance gets a Trainman + Engineer slot
        var slots = new List<PositionSlot>();
        foreach (var wi in new[] { wi101Today, wi101Tomorrow, wi202Today, wi202Tomorrow, wi303Today, wi303Tomorrow })
        {
            var condSlot = PositionSlot.Create(wi.CtrlNbr, conductor.CtrlNbr);
            var engSlot = PositionSlot.Create(wi.CtrlNbr, engineer.CtrlNbr);
            await positionSlotRepo.AddAsync(condSlot);
            await positionSlotRepo.AddAsync(engSlot);
            slots.Add(condSlot);
            slots.Add(engSlot);
        }

        // Bind a few today slots to employees
        var empList = await employeeRepo.GetAllAsync();
        slots[0].Bind(empList[40].CtrlNbr, "DISPATCH");  // Job 101 today � Trainman
        slots[1].Bind(empList[0].CtrlNbr, "DISPATCH");    // Job 101 today � Engineer
        slots[4].Bind(empList[41].CtrlNbr, "DISPATCH");   // Job 202 today � Trainman
        slots[5].Bind(empList[1].CtrlNbr, "DISPATCH");    // Job 202 today � Engineer
        await positionSlotRepo.UpdateAsync(slots[0]);
        await positionSlotRepo.UpdateAsync(slots[1]);
        await positionSlotRepo.UpdateAsync(slots[4]);
        await positionSlotRepo.UpdateAsync(slots[5]);

        } // end work management guard

        async Task EnsurePtraCraftRolesAsync()
        {
            SetParent(ptraParentCore.CtrlNbr.Value);

            var ptraRailroad = (await groupRepo.GetByGroupTypeNameAsync("Railroad", ptraParentCore.CtrlNbr.Value))
                .FirstOrDefault(g => g.Code == "SMPL");
            if (ptraRailroad is null)
                return;

            var crafts = await craftRepo.GetAllAsync();
            var ptraEngineerCraft = crafts.FirstOrDefault(c => c.CraftName == "Engineer" && c.DynamicGroupCtrlNbr == ptraRailroad.CtrlNbr);
            var ptraTrainmanCraft = crafts.FirstOrDefault(c => c.CraftName == "Trainman" && c.DynamicGroupCtrlNbr == ptraRailroad.CtrlNbr);
            if (ptraEngineerCraft is null || ptraTrainmanCraft is null)
                return;

            var boards = await sp.GetRequiredService<IRosterBoardRepository>().GetAllAsync();

            var ptraEngineerExtraBoard = boards
                .FirstOrDefault(b => b.CraftCtrlNbr == ptraEngineerCraft.CtrlNbr && b.BoardType == BoardType.ExtraBoard && b.Name == "Engineer Extra Board")
                ?? boards.FirstOrDefault(b => b.CraftCtrlNbr == ptraEngineerCraft.CtrlNbr && b.BoardType == BoardType.ExtraBoard);
            var ptraEngineerNewHireBoard = boards
                .FirstOrDefault(b => b.CraftCtrlNbr == ptraEngineerCraft.CtrlNbr && b.BoardType == BoardType.NewHire && b.Name == "Engineer New Hires")
                ?? boards.FirstOrDefault(b => b.CraftCtrlNbr == ptraEngineerCraft.CtrlNbr && b.BoardType == BoardType.NewHire);
            var ptraTrainmanExtraBoard = boards
                .FirstOrDefault(b => b.CraftCtrlNbr == ptraTrainmanCraft.CtrlNbr && b.BoardType == BoardType.ExtraBoard && b.Name == "Trainman Extra Board")
                ?? boards.FirstOrDefault(b => b.CraftCtrlNbr == ptraTrainmanCraft.CtrlNbr && b.BoardType == BoardType.ExtraBoard);
            var ptraTrainmanNewHireBoard = boards
                .FirstOrDefault(b => b.CraftCtrlNbr == ptraTrainmanCraft.CtrlNbr && b.BoardType == BoardType.NewHire && b.Name == "Trainman New Hires")
                ?? boards.FirstOrDefault(b => b.CraftCtrlNbr == ptraTrainmanCraft.CtrlNbr && b.BoardType == BoardType.NewHire);

            if (ptraEngineerExtraBoard is null || ptraEngineerNewHireBoard is null || ptraTrainmanExtraBoard is null || ptraTrainmanNewHireBoard is null)
                return;

            var ptraRoles = await craftRoleRepo.GetByRailroadAsync(ptraRailroad.CtrlNbr);

            async Task UpsertPtraRoleAsync(ControlNumber craftCtrlNbr, string code, string name, int hierarchyLevel, ControlNumber defaultBoardCtrlNbr)
            {
                var existingRole = ptraRoles.FirstOrDefault(r => r.Code == code && r.CraftCtrlNbr == craftCtrlNbr);
                if (existingRole is null)
                {
                    await craftRoleRepo.AddAsync(CraftRole.Create(
                        craftCtrlNbr,
                        code,
                        name,
                        hierarchyLevel: hierarchyLevel,
                        defaultRosterBoardCtrlNbr: defaultBoardCtrlNbr));
                    return;
                }

                existingRole.Update(
                    code,
                    name,
                    alternateName: null,
                    defaultRosterBoardCtrlNbr: defaultBoardCtrlNbr,
                    hierarchyLevel: hierarchyLevel);
                await craftRoleRepo.UpdateAsync(existingRole);
            }

            await UpsertPtraRoleAsync(ptraEngineerCraft.CtrlNbr, "E", "Engineer", 1, ptraEngineerExtraBoard.CtrlNbr);
            await UpsertPtraRoleAsync(ptraEngineerCraft.CtrlNbr, "ET", "Engineer Trainee", 0, ptraEngineerNewHireBoard.CtrlNbr);
            await UpsertPtraRoleAsync(ptraTrainmanCraft.CtrlNbr, "F", "Foreman", 2, ptraTrainmanExtraBoard.CtrlNbr);
            await UpsertPtraRoleAsync(ptraTrainmanCraft.CtrlNbr, "H", "Helper", 1, ptraTrainmanExtraBoard.CtrlNbr);
            await UpsertPtraRoleAsync(ptraTrainmanCraft.CtrlNbr, "TT", "Trainman Trainee", 0, ptraTrainmanNewHireBoard.CtrlNbr);
        }

        await EnsurePtraCraftRolesAsync();

        // ?? Section 6: Crews � Crews, Positions, Incumbencies, Attachments ???
        var crewRepo = sp.GetRequiredService<ICrewRepository>();
        var crewPositionRepo = sp.GetRequiredService<ICrewPositionRepository>();
        var incumbencyRepo = sp.GetRequiredService<ICrewIncumbencyRepository>();
        var crewAssignmentRepo = sp.GetRequiredService<ICrewAssignmentRepository>();
        var assignmentRepo2 = sp.GetRequiredService<IAssignmentRepository>();
        var assignmentScheduleRepo = sp.GetRequiredService<IAssignmentScheduleRepository>();

        var existingCrews = await crewRepo.GetAllAsync();
        if (existingCrews.Count == 0)
        {
        SetParent(csxParentCtrlNbr);
        var allGroups2 = await groupRepo.GetAllAsync();
        var jaxSub2 = allGroups2.First(g => g.Name == "Jacksonville Sub");
        var allRoles = await craftRoleRepo.GetAllAsync();
        var condRole = allRoles.First(r => r.Code == "COND");
        var engRole = allRoles.First(r => r.Code == "ENGR");
        var empList2 = await employeeRepo.GetAllAsync();
        var crewDepts = await departmentRepo.GetAllAsync();
        var csxRailroadForCrews = (await groupRepo.GetByGroupTypeNameAsync("Railroad")).First(g => g.Code == "CSX");
        var crewTransDept = crewDepts.FirstOrDefault(d => d.Name == "Transportation" && d.DynamicGroupCtrlNbr == csxRailroadForCrews.CtrlNbr);
        // Regular crews
        var crewEffective = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var crewsAppSvcCsx = sp.GetRequiredService<CrewsAppService>();
        var crewA = await crewsAppSvcCsx.CreateCrewAsync("REGULAR", jaxSub2.CtrlNbr.Value, "Jax Turn Crew A", true, crewTransDept?.CtrlNbr, crewEffective, null);
        var crewB = await crewsAppSvcCsx.CreateCrewAsync("REGULAR", jaxSub2.CtrlNbr.Value, "Jax Turn Crew B", true, crewTransDept?.CtrlNbr, crewEffective, null);
        var extraCrew = await crewsAppSvcCsx.CreateCrewAsync("EXTRA", jaxSub2.CtrlNbr.Value, "Jax Extra Board Crew", true, crewTransDept?.CtrlNbr, crewEffective, null);

        // Crew Positions — 2 per crew (Trainman + Engineer); app service creates StaffablePosition internally
        var crewAPos1 = await crewsAppSvcCsx.CreateCrewPositionAsync(crewA.CtrlNbr.Value, condRole.CtrlNbr.Value, 1);
        var crewAPos2 = await crewsAppSvcCsx.CreateCrewPositionAsync(crewA.CtrlNbr.Value, engRole.CtrlNbr.Value, 2);
        var crewBPos1 = await crewsAppSvcCsx.CreateCrewPositionAsync(crewB.CtrlNbr.Value, condRole.CtrlNbr.Value, 1);
        var crewBPos2 = await crewsAppSvcCsx.CreateCrewPositionAsync(crewB.CtrlNbr.Value, engRole.CtrlNbr.Value, 2);
        var extraPos1 = await crewsAppSvcCsx.CreateCrewPositionAsync(extraCrew.CtrlNbr.Value, condRole.CtrlNbr.Value, 1);
        var extraPos2 = await crewsAppSvcCsx.CreateCrewPositionAsync(extraCrew.CtrlNbr.Value, engRole.CtrlNbr.Value, 2);

        // Incumbencies � assign employees to crew positions
        // Incumbencies — assign employees to crew positions
        var now = DateTime.UtcNow;
        await crewsAppSvcCsx.CreateCrewIncumbencyAsync(crewAPos1.CtrlNbr.Value, empList2[40].CtrlNbr.Value, now, null);
        await crewsAppSvcCsx.CreateCrewIncumbencyAsync(crewAPos2.CtrlNbr.Value, empList2[0].CtrlNbr.Value, now, null);
        await crewsAppSvcCsx.CreateCrewIncumbencyAsync(crewBPos1.CtrlNbr.Value, empList2[41].CtrlNbr.Value, now, null);
        await crewsAppSvcCsx.CreateCrewIncumbencyAsync(crewBPos2.CtrlNbr.Value, empList2[1].CtrlNbr.Value, now, null);
        await crewsAppSvcCsx.CreateCrewIncumbencyAsync(extraPos1.CtrlNbr.Value, empList2[42].CtrlNbr.Value, now, null);
        await crewsAppSvcCsx.CreateCrewIncumbencyAsync(extraPos2.CtrlNbr.Value, empList2[2].CtrlNbr.Value, now, null);

        // Shift Definitions for Jacksonville Sub
        var workMgmtSvcCsx = sp.GetRequiredService<WorkManagementService>();
        var shiftDefRepo = sp.GetRequiredService<IShiftDefinitionRepository>();
        var existingShifts = await shiftDefRepo.GetByWorkAreaAsync(jaxSub2.CtrlNbr);
        var csxShiftByCode = existingShifts
            .GroupBy(s => s.ShiftCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        ShiftDefinition shiftFirst;
        if (!csxShiftByCode.TryGetValue("1ST", out shiftFirst!))
            shiftFirst = await workMgmtSvcCsx.CreateShiftDefinitionAsync(jaxSub2.CtrlNbr, "1ST", "First Shift", 1, true);

        ShiftDefinition shiftSecond;
        if (!csxShiftByCode.TryGetValue("2ND", out shiftSecond!))
            shiftSecond = await workMgmtSvcCsx.CreateShiftDefinitionAsync(jaxSub2.CtrlNbr, "2ND", "Second Shift", 2, true);

        ShiftDefinition shiftThird;
        if (!csxShiftByCode.TryGetValue("3RD", out shiftThird!))
            shiftThird = await workMgmtSvcCsx.CreateShiftDefinitionAsync(jaxSub2.CtrlNbr, "3RD", "Third Shift", 3, true);

        // Shift Definitions for PTRA
        SetParent(ptraParentCore.CtrlNbr.Value);
        var workMgmtSvcPtra = sp.GetRequiredService<WorkManagementService>();
        var ptraRRForShifts = (await groupRepo.GetByGroupTypeNameAsync("Railroad", ptraParentCore.CtrlNbr.Value)).First(g => g.Code == "SMPL");
        var existingPtraShifts = await shiftDefRepo.GetByWorkAreaAsync(ptraRRForShifts.CtrlNbr);
        var ptraShiftByCode = existingPtraShifts
            .GroupBy(s => s.ShiftCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        ShiftDefinition ptraShift1;
        if (!ptraShiftByCode.TryGetValue("1", out ptraShift1!))
            ptraShift1 = await workMgmtSvcPtra.CreateShiftDefinitionAsync(ptraRRForShifts.CtrlNbr, "1", "First Shift", 1, true);

        ShiftDefinition ptraShift2;
        if (!ptraShiftByCode.TryGetValue("2", out ptraShift2!))
            ptraShift2 = await workMgmtSvcPtra.CreateShiftDefinitionAsync(ptraRRForShifts.CtrlNbr, "2", "Second Shift", 2, true);

        ShiftDefinition ptraShift3;
        if (!ptraShiftByCode.TryGetValue("3", out ptraShift3!))
            ptraShift3 = await workMgmtSvcPtra.CreateShiftDefinitionAsync(ptraRRForShifts.CtrlNbr, "3", "Third Shift", 3, true);

        // Assignments
        SetParent(csxParentCtrlNbr);
        var assignmentsSvcCsx = sp.GetRequiredService<AssignmentsService>();
        var (asgn1, _, _) = await assignmentsSvcCsx.CreateAssignmentAsync(jaxSub2.CtrlNbr, "JAX-101", "Jax Turn 101", false, true, crewTransDept?.CtrlNbr);
        var (asgn2, _, _) = await assignmentsSvcCsx.CreateAssignmentAsync(jaxSub2.CtrlNbr, "JAX-102", "Jax Turn 102", false, true, crewTransDept?.CtrlNbr);
        var (asgnExtra, _, _) = await assignmentsSvcCsx.CreateAssignmentAsync(jaxSub2.CtrlNbr, "JAX-XB", "Jax Extra Board", true, true, crewTransDept?.CtrlNbr);

        // Assignment Schedules — weekday bitmask: Mon-Fri = 0b0111110 = 62
        const int weekdays = 0b0111110;
        await assignmentsSvcCsx.CreateAssignmentScheduleAsync(asgn1.CtrlNbr, shiftFirst.CtrlNbr, weekdays, new TimeOnly(6, 0), new TimeOnly(14, 0));
        await assignmentsSvcCsx.CreateAssignmentScheduleAsync(asgn2.CtrlNbr, shiftSecond.CtrlNbr, weekdays, new TimeOnly(14, 0), new TimeOnly(22, 0));
        await assignmentsSvcCsx.CreateAssignmentScheduleAsync(asgnExtra.CtrlNbr, shiftFirst.CtrlNbr, weekdays, new TimeOnly(6, 0), new TimeOnly(14, 0));
        await crewsAppSvcCsx.CreateCrewAssignmentAsync(crewA.CtrlNbr.Value, asgn1.CtrlNbr.Value, weekdays, now, null);
        await crewsAppSvcCsx.CreateCrewAssignmentAsync(crewB.CtrlNbr.Value, asgn2.CtrlNbr.Value, weekdays, now, null);
        await crewsAppSvcCsx.CreateCrewAssignmentAsync(extraCrew.CtrlNbr.Value, asgnExtra.CtrlNbr.Value, weekdays, now, null);

        // ── PTRA Assignments ────────────────────────────────────────────
        SetParent(ptraParentCore.CtrlNbr.Value);
        var assignmentsSvcPtra = sp.GetRequiredService<AssignmentsService>();
        var ptraLocNOYD = allGroups2.First(g => g.Code == "NOYD");
        var ptraLocMNYD = allGroups2.First(g => g.Code == "MNYD");
        var ptraLocSOYD = allGroups2.First(g => g.Code == "SOYD");
        var ptraEngRole = allRoles.First(r => r.Code == "E");
        var ptraFmnRole = allRoles.First(r => r.Code == "F");
        var ptraHlpRole = allRoles.First(r => r.Code == "H");
        var ptraTransDeptCrew = crewDepts.FirstOrDefault(d => d.Name == "Transportation" && d.DynamicGroupCtrlNbr == ptraRRForShifts.CtrlNbr);

        if (ptraTransDeptCrew is not null)
        {
            var existingDeptRule = await departmentReassignmentRuleRepo.GetByDepartmentAsync(ptraTransDeptCrew.CtrlNbr);
            if (existingDeptRule is null)
            {
                var ptraDeptRule = DepartmentReassignmentRule.Create(
                    ptraTransDeptCrew.CtrlNbr,
                    BoardType.Hangout,
                    isRequired: true);
                await departmentReassignmentRuleRepo.AddAsync(ptraDeptRule);
            }
        }

        // 9 assignments — 3 per shift, one per location (SOYD, MNYD, NOYD)
        var (ptraAsgn130, _, _) = await assignmentsSvcPtra.CreateAssignmentAsync(ptraLocSOYD.CtrlNbr, "130", "130", false, true, ptraTransDeptCrew?.CtrlNbr);
        var (ptraAsgn140, _, _) = await assignmentsSvcPtra.CreateAssignmentAsync(ptraLocMNYD.CtrlNbr, "140", "140", false, true, ptraTransDeptCrew?.CtrlNbr);
        var (ptraAsgn150, _, _) = await assignmentsSvcPtra.CreateAssignmentAsync(ptraLocNOYD.CtrlNbr, "150", "150", false, true, ptraTransDeptCrew?.CtrlNbr);
        var (ptraAsgn230, _, _) = await assignmentsSvcPtra.CreateAssignmentAsync(ptraLocSOYD.CtrlNbr, "230", "230", false, true, ptraTransDeptCrew?.CtrlNbr);
        var (ptraAsgn240, _, _) = await assignmentsSvcPtra.CreateAssignmentAsync(ptraLocMNYD.CtrlNbr, "240", "240", false, true, ptraTransDeptCrew?.CtrlNbr);
        var (ptraAsgn250, _, _) = await assignmentsSvcPtra.CreateAssignmentAsync(ptraLocNOYD.CtrlNbr, "250", "250", false, true, ptraTransDeptCrew?.CtrlNbr);
        var (ptraAsgn330, _, _) = await assignmentsSvcPtra.CreateAssignmentAsync(ptraLocSOYD.CtrlNbr, "330", "330", false, true, ptraTransDeptCrew?.CtrlNbr);
        var (ptraAsgn340, _, _) = await assignmentsSvcPtra.CreateAssignmentAsync(ptraLocMNYD.CtrlNbr, "340", "340", false, true, ptraTransDeptCrew?.CtrlNbr);
        var (ptraAsgn350, _, _) = await assignmentsSvcPtra.CreateAssignmentAsync(ptraLocNOYD.CtrlNbr, "350", "350", false, true, ptraTransDeptCrew?.CtrlNbr);

        // ── PTRA Crews — 9 regular + 3 relief ───────────────────────────
        var ptraCrewEffective = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var crewsAppSvcPtra = sp.GetRequiredService<CrewsAppService>();
        var ptraCrew130 = await crewsAppSvcPtra.CreateCrewAsync("REGULAR", ptraRRForShifts.CtrlNbr.Value, "130", true, ptraTransDeptCrew?.CtrlNbr, ptraCrewEffective, null);
        var ptraCrew140 = await crewsAppSvcPtra.CreateCrewAsync("REGULAR", ptraRRForShifts.CtrlNbr.Value, "140", true, ptraTransDeptCrew?.CtrlNbr, ptraCrewEffective, null);
        var ptraCrew150 = await crewsAppSvcPtra.CreateCrewAsync("REGULAR", ptraRRForShifts.CtrlNbr.Value, "150", true, ptraTransDeptCrew?.CtrlNbr, ptraCrewEffective, null);
        var ptraCrew230 = await crewsAppSvcPtra.CreateCrewAsync("REGULAR", ptraRRForShifts.CtrlNbr.Value, "230", true, ptraTransDeptCrew?.CtrlNbr, ptraCrewEffective, null);
        var ptraCrew240 = await crewsAppSvcPtra.CreateCrewAsync("REGULAR", ptraRRForShifts.CtrlNbr.Value, "240", true, ptraTransDeptCrew?.CtrlNbr, ptraCrewEffective, null);
        var ptraCrew250 = await crewsAppSvcPtra.CreateCrewAsync("REGULAR", ptraRRForShifts.CtrlNbr.Value, "250", true, ptraTransDeptCrew?.CtrlNbr, ptraCrewEffective, null);
        var ptraCrew330 = await crewsAppSvcPtra.CreateCrewAsync("REGULAR", ptraRRForShifts.CtrlNbr.Value, "330", true, ptraTransDeptCrew?.CtrlNbr, ptraCrewEffective, null);
        var ptraCrew340 = await crewsAppSvcPtra.CreateCrewAsync("REGULAR", ptraRRForShifts.CtrlNbr.Value, "340", true, ptraTransDeptCrew?.CtrlNbr, ptraCrewEffective, null);
        var ptraCrew350 = await crewsAppSvcPtra.CreateCrewAsync("REGULAR", ptraRRForShifts.CtrlNbr.Value, "350", true, ptraTransDeptCrew?.CtrlNbr, ptraCrewEffective, null);
        var ptraCrewRlfA = await crewsAppSvcPtra.CreateCrewAsync("RELIEF", ptraRRForShifts.CtrlNbr.Value, "RLF-A", true, ptraTransDeptCrew?.CtrlNbr, ptraCrewEffective, null);
        var ptraCrewRlfB = await crewsAppSvcPtra.CreateCrewAsync("RELIEF", ptraRRForShifts.CtrlNbr.Value, "RLF-B", true, ptraTransDeptCrew?.CtrlNbr, ptraCrewEffective, null);
        var ptraCrewRlfC = await crewsAppSvcPtra.CreateCrewAsync("RELIEF", ptraRRForShifts.CtrlNbr.Value, "RLF-C", true, ptraTransDeptCrew?.CtrlNbr, ptraCrewEffective, null);

        // ── PTRA StaffablePositions ─────────────────────────────────────
        // 3-position crews (E, F, H): 130, 150, 230, 250, 330, 350
        // 2-position crews (E, F): 140, 240, 340, RLF-A, RLF-B, RLF-C
        var ptra3PosCrews = new[] { ptraCrew130, ptraCrew150, ptraCrew230, ptraCrew250, ptraCrew330, ptraCrew350 };
        var ptra2PosCrews = new[] { ptraCrew140, ptraCrew240, ptraCrew340, ptraCrewRlfA, ptraCrewRlfB, ptraCrewRlfC };

        // ── PTRA CrewPositions ───────────────────────────────────────────
        foreach (var crew in ptra3PosCrews)
        {
            await crewsAppSvcPtra.CreateCrewPositionAsync(crew.CtrlNbr.Value, ptraEngRole.CtrlNbr.Value, 1);
            await crewsAppSvcPtra.CreateCrewPositionAsync(crew.CtrlNbr.Value, ptraFmnRole.CtrlNbr.Value, 2);
            await crewsAppSvcPtra.CreateCrewPositionAsync(crew.CtrlNbr.Value, ptraHlpRole.CtrlNbr.Value, 3);
        }
        foreach (var crew in ptra2PosCrews)
        {
            await crewsAppSvcPtra.CreateCrewPositionAsync(crew.CtrlNbr.Value, ptraEngRole.CtrlNbr.Value, 1);
            await crewsAppSvcPtra.CreateCrewPositionAsync(crew.CtrlNbr.Value, ptraFmnRole.CtrlNbr.Value, 2);
        }

        // ── PTRA AssignmentSchedules + CrewAssignments ───────────────────
        var ptraStart = new DateTime(2026, 1, 1);

        // Schedules: mask 62 = weekdays (Mon–Fri), 63 = 6-day (Sun–Fri), 127 = every day
        await assignmentsSvcPtra.CreateAssignmentScheduleAsync(ptraAsgn130.CtrlNbr, ptraShift1.CtrlNbr, 63, new TimeOnly(7, 0), new TimeOnly(15, 0));
        await assignmentsSvcPtra.CreateAssignmentScheduleAsync(ptraAsgn140.CtrlNbr, ptraShift1.CtrlNbr, 127, new TimeOnly(7, 30), new TimeOnly(15, 30));
        await assignmentsSvcPtra.CreateAssignmentScheduleAsync(ptraAsgn150.CtrlNbr, ptraShift1.CtrlNbr, 127, new TimeOnly(7, 59), new TimeOnly(15, 59));
        await assignmentsSvcPtra.CreateAssignmentScheduleAsync(ptraAsgn230.CtrlNbr, ptraShift2.CtrlNbr, 63, new TimeOnly(15, 0), new TimeOnly(23, 0));
        await assignmentsSvcPtra.CreateAssignmentScheduleAsync(ptraAsgn240.CtrlNbr, ptraShift2.CtrlNbr, 127, new TimeOnly(15, 30), new TimeOnly(23, 30));
        await assignmentsSvcPtra.CreateAssignmentScheduleAsync(ptraAsgn250.CtrlNbr, ptraShift2.CtrlNbr, 127, new TimeOnly(15, 59), new TimeOnly(23, 59));
        await assignmentsSvcPtra.CreateAssignmentScheduleAsync(ptraAsgn330.CtrlNbr, ptraShift3.CtrlNbr, 63, new TimeOnly(23, 0), new TimeOnly(7, 0));
        await assignmentsSvcPtra.CreateAssignmentScheduleAsync(ptraAsgn340.CtrlNbr, ptraShift3.CtrlNbr, 127, new TimeOnly(23, 30), new TimeOnly(7, 30));
        await assignmentsSvcPtra.CreateAssignmentScheduleAsync(ptraAsgn350.CtrlNbr, ptraShift3.CtrlNbr, 127, new TimeOnly(23, 59), new TimeOnly(7, 59));

        // Regular crew → assignment links
        await crewsAppSvcPtra.CreateCrewAssignmentAsync(ptraCrew130.CtrlNbr.Value, ptraAsgn130.CtrlNbr.Value, 62, ptraStart, null);
        await crewsAppSvcPtra.CreateCrewAssignmentAsync(ptraCrew140.CtrlNbr.Value, ptraAsgn140.CtrlNbr.Value, 121, ptraStart, null);
        await crewsAppSvcPtra.CreateCrewAssignmentAsync(ptraCrew150.CtrlNbr.Value, ptraAsgn150.CtrlNbr.Value, 103, ptraStart, null);
        await crewsAppSvcPtra.CreateCrewAssignmentAsync(ptraCrew230.CtrlNbr.Value, ptraAsgn230.CtrlNbr.Value, 62, ptraStart, null);
        await crewsAppSvcPtra.CreateCrewAssignmentAsync(ptraCrew240.CtrlNbr.Value, ptraAsgn240.CtrlNbr.Value, 121, ptraStart, null);
        await crewsAppSvcPtra.CreateCrewAssignmentAsync(ptraCrew250.CtrlNbr.Value, ptraAsgn250.CtrlNbr.Value, 103, ptraStart, null);
        await crewsAppSvcPtra.CreateCrewAssignmentAsync(ptraCrew330.CtrlNbr.Value, ptraAsgn330.CtrlNbr.Value, 62, ptraStart, null);
        await crewsAppSvcPtra.CreateCrewAssignmentAsync(ptraCrew340.CtrlNbr.Value, ptraAsgn340.CtrlNbr.Value, 121, ptraStart, null);
        await crewsAppSvcPtra.CreateCrewAssignmentAsync(ptraCrew350.CtrlNbr.Value, ptraAsgn350.CtrlNbr.Value, 103, ptraStart, null);

        // Relief crew → assignment links (cover remaining days per shift)
        await crewsAppSvcPtra.CreateCrewAssignmentAsync(ptraCrewRlfA.CtrlNbr.Value, ptraAsgn130.CtrlNbr.Value, 1, ptraStart, null);
        await crewsAppSvcPtra.CreateCrewAssignmentAsync(ptraCrewRlfA.CtrlNbr.Value, ptraAsgn140.CtrlNbr.Value, 6, ptraStart, null);
        await crewsAppSvcPtra.CreateCrewAssignmentAsync(ptraCrewRlfA.CtrlNbr.Value, ptraAsgn150.CtrlNbr.Value, 24, ptraStart, null);
        await crewsAppSvcPtra.CreateCrewAssignmentAsync(ptraCrewRlfB.CtrlNbr.Value, ptraAsgn230.CtrlNbr.Value, 1, ptraStart, null);
        await crewsAppSvcPtra.CreateCrewAssignmentAsync(ptraCrewRlfB.CtrlNbr.Value, ptraAsgn240.CtrlNbr.Value, 6, ptraStart, null);
        await crewsAppSvcPtra.CreateCrewAssignmentAsync(ptraCrewRlfB.CtrlNbr.Value, ptraAsgn250.CtrlNbr.Value, 24, ptraStart, null);
        await crewsAppSvcPtra.CreateCrewAssignmentAsync(ptraCrewRlfC.CtrlNbr.Value, ptraAsgn330.CtrlNbr.Value, 1, ptraStart, null);
        await crewsAppSvcPtra.CreateCrewAssignmentAsync(ptraCrewRlfC.CtrlNbr.Value, ptraAsgn340.CtrlNbr.Value, 6, ptraStart, null);
        await crewsAppSvcPtra.CreateCrewAssignmentAsync(ptraCrewRlfC.CtrlNbr.Value, ptraAsgn350.CtrlNbr.Value, 24, ptraStart, null);

        } // end crews guard

        // Section 7: Boards - Roster Boards, Positions, Cascade Policies
        SetParent(csxParentCtrlNbr);
        var rosterBoardRepo = sp.GetRequiredService<IRosterBoardRepository>();
        var cascadeRepo = sp.GetRequiredService<IBoardCascadePolicyRepository>();
        var staffPosRepo = sp.GetRequiredService<IStaffablePositionRepository>();

        var existingBoards = await rosterBoardRepo.GetAllAsync();
        var boardsNeedPositions = existingBoards.Count > 0 && existingBoards.All(b => b.Positions.Count == 0);
        if (boardsNeedPositions)
        {
        var crafts2 = await craftRepo.GetAllAsync();
        var groups2 = await groupRepo.GetAllAsync();
        var jaxSub3 = groups2.First(g => g.Name == "Jacksonville Sub");
        var csxRailroad3 = (await groupRepo.GetByGroupTypeNameAsync("Railroad")).First(g => g.Code == "CSX");
        var engCraft2 = crafts2.First(c => c.CraftName == "Engineer" && c.DynamicGroupCtrlNbr == csxRailroad3.CtrlNbr);
        var condCraft2 = crafts2.First(c => c.CraftName == "Trainman" && c.DynamicGroupCtrlNbr == csxRailroad3.CtrlNbr);
        var empList3 = await employeeRepo.GetAllAsync();

        // Find ExtraBoard boards and add positions
        var engBoard = existingBoards.First(b => b.CraftCtrlNbr == engCraft2.CtrlNbr && b.BoardType == BoardType.ExtraBoard);
        var condBoard = existingBoards.First(b => b.CraftCtrlNbr == condCraft2.CtrlNbr && b.BoardType == BoardType.ExtraBoard);

        // Board Positions - 5 engineers, 5 trainmen
        for (int i = 0; i < 5; i++)
        {
            var engPos = StaffablePosition.Create(StaffablePositionType.Board);
            await staffPosRepo.AddAsync(engPos);
            engBoard.AddPosition(empList3[3 + i].CtrlNbr, i + 1, engPos.CtrlNbr);

            var condPos = StaffablePosition.Create(StaffablePositionType.Board);
            await staffPosRepo.AddAsync(condPos);
            condBoard.AddPosition(empList3[43 + i].CtrlNbr, i + 1, condPos.CtrlNbr);
        }
        await rosterBoardRepo.UpdateAsync(engBoard);
        await rosterBoardRepo.UpdateAsync(condBoard);

        // Cascade Policies
        await cascadeRepo.AddAsync(BoardCascadePolicy.Create(jaxSub3.CtrlNbr, engCraft2.CtrlNbr,
            "UP_HIERARCHY", 2, true, 1, "SENIORITY"));
        await cascadeRepo.AddAsync(BoardCascadePolicy.Create(jaxSub3.CtrlNbr, condCraft2.CtrlNbr,
            "UP_HIERARCHY", 2, true, 1, "SENIORITY"));

        } // end boards guard

        // ?? Section 8: Bulletins & Vacancy ???????????????????????????????????
        var vacancyRepo = sp.GetRequiredService<IPositionVacancyRepository>();
        var bulletinRepo = sp.GetRequiredService<IBulletinRepository>();
        var bidRepo = sp.GetRequiredService<IBulletinBidRepository>();

        // ?? Section 9: Dispatching � Projections, Bookings ???????????????????
        var projectionRepo = sp.GetRequiredService<IDispatchProjectionRepository>();
        var bookingRepo = sp.GetRequiredService<IEmployeeBookingRepository>();

        var existingProjections = await projectionRepo.GetAllAsync();
        if (existingProjections.Count == 0)
        {
        var allSlots2 = await positionSlotRepo.GetAllAsync();
        var empList5 = await employeeRepo.GetAllAsync();
        var now4 = DateTime.UtcNow;

        // Projections for open slots
        var openSlots = allSlots2.Where(s => s.Status == "Open").Take(4).ToList();
        for (int i = 0; i < openSlots.Count; i++)
        {
            ControlNumber? projected = i < empList5.Count ? empList5[i + 5].CtrlNbr : null;
            await projectionRepo.AddAsync(DispatchProjection.Create(openSlots[i].CtrlNbr, now4, projected, null));
        }

        // Bookings for bound slots
        var boundSlots = allSlots2.Where(s => s.Status == "Bound" && s.BoundEmployeeCtrlNbr is not null).Take(4).ToList();
        foreach (var slot in boundSlots)
        {
            var today2 = DateTime.UtcNow.Date;
            await bookingRepo.AddAsync(EmployeeBooking.Create(
                slot.BoundEmployeeCtrlNbr!, today2.AddHours(6), today2.AddHours(18), slot.CtrlNbr));
        }

        } // end dispatching guard

        // ?? Section 10: Policies � Displacement, Bulletin, Seniority Move ????
        var displacementPolicyRepo = sp.GetRequiredService<ICraftDisplacementPolicyRepository>();
        var bulletinPolicyRepo = sp.GetRequiredService<IBulletinPolicyRepository>();
        var senMovePolicyRepo = sp.GetRequiredService<ISeniorityMovePolicyRepository>();
        var craftOpsPolicyRepo = sp.GetRequiredService<ICraftOperationsPolicyRepository>();
        var craftCallSheetRuleRepo = sp.GetRequiredService<ICraftCallSheetRuleRepository>();
        var noAccessPolicyRepo = sp.GetRequiredService<INoAccessPolicyRepository>();

        var existingPolicies = await displacementPolicyRepo.GetAllAsync();
        if (existingPolicies.Count == 0)
        {
        var crafts4 = await craftRepo.GetAllAsync();
        var csxRailroad5 = (await groupRepo.GetByGroupTypeNameAsync("Railroad")).First(g => g.Code == "CSX");
        var engCraft4 = crafts4.First(c => c.CraftName == "Engineer" && c.DynamicGroupCtrlNbr == csxRailroad5.CtrlNbr);
        var condCraft4 = crafts4.First(c => c.CraftName == "Trainman" && c.DynamicGroupCtrlNbr == csxRailroad5.CtrlNbr);

        await displacementPolicyRepo.AddAsync(CraftDisplacementPolicy.Create(engCraft4.CtrlNbr, 72, "ROSTER_DATE", "EXTRA_BOARD"));
        await displacementPolicyRepo.AddAsync(CraftDisplacementPolicy.Create(condCraft4.CtrlNbr, 72, "ROSTER_DATE", "EXTRA_BOARD"));

        await bulletinPolicyRepo.AddAsync(BulletinPolicy.Create(engCraft4.CtrlNbr, 120));
        await bulletinPolicyRepo.AddAsync(BulletinPolicy.Create(condCraft4.CtrlNbr, 120));

        } // end policies guard

        async Task SeedSeniorityMoveAndHangoutPoliciesAsync(ControlNumber parentCtrlNbr, string railroadCode)
        {
            SetParent(parentCtrlNbr.Value);

            var railroad = (await groupRepo.GetByGroupTypeNameAsync("Railroad", parentCtrlNbr.Value))
                .FirstOrDefault(g => g.Code == railroadCode);
            if (railroad is null)
                return;

            var crafts = await craftRepo.GetByParentAndRailroadAsync(parentCtrlNbr, railroad.CtrlNbr);
            var targetCrafts = crafts.Where(c => c.CraftName is "Engineer" or "Trainman").ToList();
            if (targetCrafts.Count == 0)
                return;

            foreach (var craft in targetCrafts)
            {
                var existingPolicy = await senMovePolicyRepo.GetByRailroadAndCraftAsync(railroad.CtrlNbr, craft.CtrlNbr);
                if (existingPolicy is null)
                {
                    await senMovePolicyRepo.AddAsync(SeniorityMovePolicy.Create(
                        railroad.CtrlNbr,
                        craft.CtrlNbr,
                        requestHours: 72,
                        cancelHours: 0,
                        autoApprove: true,
                        crewToCrewStrategy: SeniorityMoveEffectiveDateStrategy.FirstOffDay,
                        crewToBoardStrategy: SeniorityMoveEffectiveDateStrategy.FirstOffDay,
                        extraBoardToCrewStrategy: SeniorityMoveEffectiveDateStrategy.FirstOffDay,
                        hangoutToCrewStrategy: SeniorityMoveEffectiveDateStrategy.Immediate,
                        extendedAbsenceToCrewStrategy: string.Empty,
                        trainingToCrewStrategy: string.Empty,
                        newHireToCrewStrategy: string.Empty,
                        willWorkEnabled: true,
                        crewToCrewEligibilityDays: 30,
                        crewToBoardEligibilityDays: 30,
                        extraBoardToCrewEligibilityDays: 30,
                        hangoutToCrewEligibilityDays: 0,
                        extendedAbsenceToCrewEligibilityDays: 0,
                        trainingToCrewEligibilityDays: 0,
                        newHireToCrewEligibilityDays: 0));
                }
                else
                {
                    existingPolicy.Update(
                        requestHours: 72,
                        cancelHours: 0,
                        autoApprove: true,
                        crewToCrewStrategy: SeniorityMoveEffectiveDateStrategy.FirstOffDay,
                        crewToBoardStrategy: SeniorityMoveEffectiveDateStrategy.FirstOffDay,
                        extraBoardToCrewStrategy: SeniorityMoveEffectiveDateStrategy.FirstOffDay,
                        hangoutToCrewStrategy: SeniorityMoveEffectiveDateStrategy.Immediate,
                        extendedAbsenceToCrewStrategy: string.Empty,
                        trainingToCrewStrategy: string.Empty,
                        newHireToCrewStrategy: string.Empty,
                        willWorkEnabled: true,
                        crewToCrewEligibilityDays: 30,
                        crewToBoardEligibilityDays: 30,
                        extraBoardToCrewEligibilityDays: 30,
                        hangoutToCrewEligibilityDays: 0,
                        extendedAbsenceToCrewEligibilityDays: 0,
                        trainingToCrewEligibilityDays: 0,
                        newHireToCrewEligibilityDays: 0);
                    await senMovePolicyRepo.UpdateAsync(existingPolicy);
                }

                var existingCraftOpsPolicy = await craftOpsPolicyRepo.GetByCraftAsync(craft.CtrlNbr);
                if (existingCraftOpsPolicy is null)
                {
                    await craftOpsPolicyRepo.AddAsync(CraftOperationsPolicy.Create(
                        craft.CtrlNbr,
                        hangoutAutoMoveEnabled: true,
                        hangoutAutoMoveTargetBoardType: BoardType.ExtraBoard.ToString(),
                        hangoutAutoMoveDelayHours: 48));
                }
                else
                {
                    existingCraftOpsPolicy.Update(
                        hangoutAutoMoveEnabled: true,
                        hangoutAutoMoveTargetBoardType: BoardType.ExtraBoard.ToString(),
                        hangoutAutoMoveDelayHours: 48);
                    await craftOpsPolicyRepo.UpdateAsync(existingCraftOpsPolicy);
                }

                var existingNoAccessPolicy = await noAccessPolicyRepo.GetByRailroadAndCraftAsync(railroad.CtrlNbr, craft.CtrlNbr);
                if (existingNoAccessPolicy is null)
                {
                    await noAccessPolicyRepo.AddAsync(NoAccessPolicy.CreateLegacyDefaults(railroad.CtrlNbr, craft.CtrlNbr));
                }
                else
                {
                    existingNoAccessPolicy.Update(
                        isEnabled: true,
                        allowEmployeeSelfRequest: true,
                        requireBulletinAccessAudit: true,
                        blockIfOnExtendedAbsence: true,
                        requirePositionCurrentlyAssigned: true,
                        applyExtraBoardSpecialCase: true,
                        requireBoardAvailableForMoveOff: true,
                        autoApproveNoAccess: true,
                        allowAdminOverride: true,
                        blockIfEmployeeMarkedOff: true,
                        blockIfLastVacatedIncumbent: true,
                        defaultEffectiveMode: NoAccessEffectiveDateMode.NextDay0001);
                    await noAccessPolicyRepo.UpdateAsync(existingNoAccessPolicy);
                }

                var existingCraftCallSheetRule = await craftCallSheetRuleRepo.GetByCraftAsync(craft.CtrlNbr);
                if (existingCraftCallSheetRule is null)
                {
                    await craftCallSheetRuleRepo.AddAsync(CraftCallSheetRule.Create(
                        craft.CtrlNbr,
                        isEnabled: true,
                        preOnDutyChangeCutoffMinutes: 180));
                }
                else
                {
                    existingCraftCallSheetRule.Update(
                        isEnabled: true,
                        preOnDutyChangeCutoffMinutes: 180);
                    await craftCallSheetRuleRepo.UpdateAsync(existingCraftCallSheetRule);
                }
            }
        }

        await SeedSeniorityMoveAndHangoutPoliciesAsync(csxParentCore.CtrlNbr, "CSX");
        await SeedSeniorityMoveAndHangoutPoliciesAsync(ptraParentCore.CtrlNbr, "SMPL");

        // Ensure PTRA Trainman trainees are placed on the New Hire board.
        async Task SeedPtraTrainmanNewHireBoardPlacementsAsync()
        {
            SetParent(ptraParentCore.CtrlNbr.Value);

            var ptraRailroad = (await groupRepo.GetByGroupTypeNameAsync("Railroad", ptraParentCore.CtrlNbr.Value))
                .FirstOrDefault(g => g.Code == "SMPL");
            if (ptraRailroad is null)
                return;

            var ptraCrafts = await craftRepo.GetByParentAndRailroadAsync(ptraParentCore.CtrlNbr, ptraRailroad.CtrlNbr);
            var ptraTrainmanCraft = ptraCrafts.FirstOrDefault(c => c.CraftName == "Trainman");
            if (ptraTrainmanCraft is null)
                return;

            var trainingRoster = await rosterRepo.GetTrainingRosterByCraftAsync(ptraTrainmanCraft.CtrlNbr);
            if (trainingRoster is null)
                return;

            var allBoards = await rosterBoardRepo.GetAllAsync();
            var newHireBoard = allBoards.FirstOrDefault(b => b.CraftCtrlNbr == ptraTrainmanCraft.CtrlNbr && b.BoardType == BoardType.NewHire);
            if (newHireBoard is null)
                return;

            var trainees = (await seniorityRepo.GetByRosterCtrlNbrAsync(trainingRoster.CtrlNbr))
                .OrderBy(s => s.RosterDate)
                .ThenBy(s => s.Rank)
                .ToList();

            var alreadyOnBoard = newHireBoard.Positions.Select(p => p.EmployeeCtrlNbr).ToHashSet();
            var rosterBoardAppSvc = sp.GetRequiredService<RosterBoardAppService>();
            var nextOrder = newHireBoard.Positions.Count + 1;

            foreach (var trainee in trainees)
            {
                if (alreadyOnBoard.Contains(trainee.EmployeeCtrlNbr))
                    continue;

                try
                {
                    await rosterBoardAppSvc.AddRosterBoardPositionAsync(newHireBoard.CtrlNbr, trainee.EmployeeCtrlNbr, nextOrder);
                    alreadyOnBoard.Add(trainee.EmployeeCtrlNbr);
                    nextOrder++;
                }
                catch (InvalidOperationException)
                {
                    // Employee already assigned elsewhere; leave unchanged.
                }
            }

            // Backfill missing position assignments for existing New Hire board positions
            // so Seniority list "Current Position" resolves for trainees.
            var positionAssignmentRepo = sp.GetRequiredService<IPositionAssignmentRepository>();
            var staffablePositionRepo = sp.GetRequiredService<IStaffablePositionRepository>();
            var existingBoardAssignments = await positionAssignmentRepo.GetAssignedEmployeeCtrlNbrsByTypeAsync(PositionAssignmentType.Board);

            foreach (var boardPos in newHireBoard.Positions)
            {
                if (existingBoardAssignments.Contains(boardPos.EmployeeCtrlNbr.Value))
                    continue;

                var staffablePosition = await staffablePositionRepo.GetByCtrlNbrAsync(boardPos.StaffablePositionCtrlNbr);
                if (staffablePosition is null)
                    continue;

                await positionAssignmentRepo.AddAsync(PositionAssignment.Create(
                    boardPos.StaffablePositionCtrlNbr,
                    boardPos.EmployeeCtrlNbr,
                    PositionAssignmentType.Board,
                    assignmentSourceCtrlNbr: boardPos.CtrlNbr));
                existingBoardAssignments.Add(boardPos.EmployeeCtrlNbr.Value);
            }
        }

        await SeedPtraTrainmanNewHireBoardPlacementsAsync();

        // ?? Section 11: Payroll � Tiers, Time Entries, Payroll Run ???????????
        var payrollTierRepo = sp.GetRequiredService<IPayrollTierRepository>();
        var timeEntryRepo = sp.GetRequiredService<ITimeEntryRepository>();
        var payrollRunRepo = sp.GetRequiredService<IPayrollRunRepository>();

        var existingTiers = await payrollTierRepo.GetAllAsync();
        if (existingTiers.Count == 0)
        {
        var groups5 = await groupRepo.GetAllAsync();
        var jaxSub6 = groups5.First(g => g.Name == "Jacksonville Sub");
        var empList6 = await employeeRepo.GetAllAsync();
        var today3 = DateTime.UtcNow.Date;

        // Payroll Tiers
        await payrollTierRepo.AddAsync(PayrollTier.Create(jaxSub6.CtrlNbr, 7, 1, 100));
        await payrollTierRepo.AddAsync(PayrollTier.Create(jaxSub6.CtrlNbr, 14, 2, 150));

        // Time Entries for first 10 employees
        for (int i = 0; i < 10 && i < empList6.Count; i++)
        {
            await timeEntryRepo.AddAsync(TimeEntry.Create(empList6[i].CtrlNbr, today3, "REGULAR", 8.0m));
        }

        // Payroll Run � current pay period
        var payPeriod = $"{today3:yyyy}-W{System.Globalization.ISOWeek.GetWeekOfYear(today3):D2}";
        await payrollRunRepo.AddAsync(PayrollRun.Create(payPeriod));

        } // end payroll guard

        // ?? Section 12: Safety � Categories, Observations ????????????????????
        var safetyCatRepo = sp.GetRequiredService<ISafetyCategoryRepository>();
        var safetyObsRepo = sp.GetRequiredService<ISafetyObservationRepository>();

        var existingCategories = await safetyCatRepo.GetAllAsync();
        if (existingCategories.Count == 0)
        {
        var groups6 = await groupRepo.GetAllAsync();
        var jaxSub7 = groups6.First(g => g.Name == "Jacksonville Sub");
        var empList7 = await employeeRepo.GetAllAsync();

        await safetyCatRepo.AddAsync(SafetyCategory.Create(jaxSub7.CtrlNbr, "TRACK", "Track Safety"));
        await safetyCatRepo.AddAsync(SafetyCategory.Create(jaxSub7.CtrlNbr, "EQUIP", "Equipment Safety"));
        await safetyCatRepo.AddAsync(SafetyCategory.Create(jaxSub7.CtrlNbr, "HAZMAT", "Hazardous Materials"));

        // Open observation
        var obs1 = SafetyObservation.Create(jaxSub7.CtrlNbr, empList7[10].CtrlNbr,
            "TRACK", "Jacksonville Sub", "Broken rail joint near switch 12", "Jacksonville Sub");
        obs1.AddAction(empList7[0].CtrlNbr, "Flagged area and notified maintenance.");
        await safetyObsRepo.AddAsync(obs1);

        // Observation with no actions yet
        var obs2 = SafetyObservation.Create(jaxSub7.CtrlNbr, empList7[42].CtrlNbr,
            "EQUIP", "Jacksonville Sub", "Locomotive headlight flickering on unit 4521");
        await safetyObsRepo.AddAsync(obs2);

        } // end safety guard

        // ?? Section 13: Railroad Information ?????????????????????????????????
        var rrInfoRepo = sp.GetRequiredService<IRailroadInformationRepository>();

        var existingInfo = await rrInfoRepo.GetAllAsync();
        if (existingInfo.Count == 0)
        {
        var groups7 = await groupRepo.GetAllAsync();
        var jaxSub8 = groups7.First(g => g.Name == "Jacksonville Sub");

        var info1 = RailroadInformation.Create(jaxSub8.CtrlNbr, "GENERAL",
            "Track Speed Restriction � MP 42.5", "Speed restricted to 25 MPH through MP 42.5 due to maintenance.");
        info1.Publish();
        await rrInfoRepo.AddAsync(info1);

        var info2 = RailroadInformation.Create(jaxSub8.CtrlNbr, "SAFETY",
            "Draft: New PPE Requirements", "All yard employees required to wear high-visibility vests effective next month.");
        await rrInfoRepo.AddAsync(info2);

        } // end railroad info guard

        // ?? Section 14: FRA Compliance � Duty Tours ??????????????????????????
        var regStdRepo = sp.GetRequiredService<IRegulatoryStandardRepository>();
        var fraDutyTourRepo = sp.GetRequiredService<IFraDutyTourRepository>();
        var empList8 = await employeeRepo.GetAllAsync();

        var existingTour = await fraDutyTourRepo.GetActiveTourForEmployeeAsync(empList8[0].CtrlNbr);
        var searchResult = await fraDutyTourRepo.SearchAsync(new FraRecordSearchCriteria { EmployeeCtrlNbr = empList8[0].CtrlNbr });
        if (searchResult.Count == 0)
        {
        // Seed a default FRA regulatory standard (49 CFR Part 228)
        var existingStandards = await regStdRepo.GetAllAsync();
        if (existingStandards.Count == 0)
        {
            await regStdRepo.AddAsync(Domain.Modules.FraCompliance.RegulatoryStandard.Create(
                "49CFR228", "Federal Hours of Service - 49 CFR Part 228",
                maxOnDutyMinutes: 720, minRestMinutes: 600,
                min8hRestInPreceding24h: true,
                consecutiveDayLimit6: 6, consecutiveDayLimit7: 7,
                restAfter6DaysMinutes: 2880, restAfter7DaysMinutes: 2880,
                monthlyCapMinutes: 16800,
                deadheadAfter12hMonthlyCapMinutes: 17520,
                wreckReliefExtraMinutes: 240,
                effectiveDate: new DateOnly(2009, 10, 16)));
        }
        var regStd = (await regStdRepo.GetAllAsync())[0];

        var tour = Domain.Modules.FraCompliance.FraDutyTour.Create(
            empList8[0].CtrlNbr, regStd.CtrlNbr,
            DateTime.UtcNow.AddDays(-1).Date.AddHours(6),
            priorTimeOffMinutes: 720, consecutiveDays: 3);
        tour.Close(DateTime.UtcNow.AddDays(-1).Date.AddHours(16),
            totalTimeOnDutyMinutes: 600, excessMinutes: null, excessServiceReason: null, isQuickTieUp: false);
        await fraDutyTourRepo.AddAsync(tour);
        }

        // ?? Section 15: FRA Compliance � Employee Certifications ????????????????
        var regQualRepo = sp.GetRequiredService<IRegulatoryQualificationRepository>();
        var empCertRepo = sp.GetRequiredService<IEmployeeCertificationRepository>();
        // Seed FraCertificationConfig + FraCertificationCheckConfig per parent (idempotent)
        {
            var certConfigRepo = sp.GetRequiredService<IFraCertificationConfigRepository>();
            var checkConfigRepo = sp.GetRequiredService<IFraCertificationCheckConfigRepository>();
            var allParents = await parentRepo.GetAllAsync();
            foreach (var p in allParents)
            {
                SetParent(p.CtrlNbr.Value);
                var parentCn = p.CtrlNbr;
                var existingCertCfg = await certConfigRepo.GetByParentAsync(parentCn);
                if (existingCertCfg is null)
                {
                    var fraConfig = FraCertificationConfig.Create(parentCn, railroadCtrlNbr: null,
                        certCycleMonths: 36, recertWindowDays: 180, renewWindowDays: 60);
                    await certConfigRepo.AddAsync(fraConfig);
                }
                var existingCheckCfgs = await checkConfigRepo.GetByParentAsync(parentCn);
                if (existingCheckCfgs.Count == 0)
                {
                    foreach (var (checkType, _, stalenessLimitDays, isEnforced, isEnforcementLocked)
                        in CertificationCheckDefaults.Checks)
                    {
                        var cc = FraCertificationCheckConfig.Create(parentCn, railroadCtrlNbr: null,
                            checkType, stalenessLimitDays, isEnforced, isEnforcementLocked);
                        await checkConfigRepo.AddAsync(cc);
                    }
                }
            }
        }

        var existingCerts = await empCertRepo.GetAllAsync();
        if (existingCerts.All(c => c.Status == CertificationStatuses.Pending))
        {
            var cfr240 = await regQualRepo.GetByCodeAsync("CFR-240-ENGINEER");
            var cfr242sw = await regQualRepo.GetByCodeAsync("CFR-242-SWITCHMAN");

            if (cfr240 != null && cfr242sw != null)
            {
                string[] checkTypes =
                [
                    "Performance", "Knowledge", "MotorVehicle",
                    "SafetyConduct", "SubstanceAbuse", "Vision", "Hearing",
                    "OperationalMonitoring", "ComplianceTest"
                ];
                string[] evaluators = ["Stevenson", "Williams", "Johnson"];
                var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

                async Task SeedCertsForRosterAsync(
                    Domain.Models.Seniority.Roster roster,
                    Domain.Modules.FraCompliance.RegulatoryQualification qual)
                {
                    var seniority = await seniorityRepo.GetByRosterCtrlNbrAsync(roster.CtrlNbr);
                    // Skip employees already processed by NewHireService (they have a pending cert from onboarding)
                    var existingCertEmpIds = (await empCertRepo.GetAllAsync())
                        .Where(c => c.RegulatoryQualificationCtrlNbr == qual.CtrlNbr)
                        .Select(c => c.EmployeeCtrlNbr)
                        .ToHashSet();
                    seniority = [.. seniority.Where(s => !existingCertEmpIds.Contains(s.EmployeeCtrlNbr))];
                    int total = seniority.Count;
                    if (total == 0) return;

                    // Status is derived automatically by RecomputeStatus inside AddEligibilityCheck:
                    //   i < 2            -> Expired  (certDate > 36 months ago, ExpirationDate is in the past)
                    //   i >= total - 2   -> Pending  (only 3 of 9 checks seeded)
                    //   first 2 active   -> Renew    (certDate ~31-32 months ago, expiration within ~180 days)
                    //   remaining active -> Active   (all checks within staleness window)
                    for (int i = 0; i < total; i++)
                    {
                        int monthsAgo;
                        int checksToSeed;
                        if (i < 2)
                        {
                            monthsAgo = 40 + i;          // ExpirationDate is in the past -> Expired
                            checksToSeed = checkTypes.Length;
                        }
                        else if (i >= total - 2)
                        {
                            monthsAgo = 1;
                            checksToSeed = 3;             // Missing 4 of 7 check types -> Pending
                        }
                        else
                        {
                            int activeIndex = i - 2;
                            monthsAgo = activeIndex < 2 ? 31 + activeIndex : 6 + ((i * 2) % 22);
                            checksToSeed = checkTypes.Length;
                        }

                        var certDate = today.AddMonths(-monthsAgo).AddDays((i * 7) % 28);
                        var cert = Domain.Modules.FraCompliance.EmployeeCertification.Create(
                            seniority[i].EmployeeCtrlNbr,
                            qual.CtrlNbr,
                            "Yard",
                            certDate,
                            recertificationIntervalMonths: 36,
                            certificationNumber: $"{qual.Code}-{i + 1:D4}");

                        for (int c = 0; c < checksToSeed; c++)
                        {
                            var evalDate = certDate.AddMonths(c * 3);
                            if (evalDate > today) evalDate = today;
                            cert.AddEligibilityCheck(
                                checkTypes[c],
                                evalDate,
                                stalenessLimitDays: 365,
                                result: "Pass",
                                evaluatorName: evaluators[(i + c) % evaluators.Length]);
                        }

                        await empCertRepo.AddAsync(cert);
                    }
                }

                // CSX
                SetParent(csxParentCtrlNbr);
                var csxRR15 = (await groupRepo.GetByGroupTypeNameAsync("Railroad"))
                    .First(g => g.Code == "CSX");
                var csxCrafts15 = await craftRepo.GetByParentAndRailroadAsync(csxParentCtrlNbr, csxRR15.CtrlNbr);
                var csxEngCraft = csxCrafts15.First(c => c.CraftName == "Engineer");
                var csxTrnCraft = csxCrafts15.First(c => c.CraftName == "Trainman");
                foreach (var r in await rosterRepo.GetByCraftCtrlNbrAsync(csxEngCraft.CtrlNbr))
                    await SeedCertsForRosterAsync(r, cfr240);
                foreach (var r in await rosterRepo.GetByCraftCtrlNbrAsync(csxTrnCraft.CtrlNbr))
                    await SeedCertsForRosterAsync(r, cfr242sw);

                // PTRA
                SetParent(ptraParentCore.CtrlNbr.Value);
                var ptraRR15 = (await groupRepo.GetByGroupTypeNameAsync("Railroad", ptraParentCore.CtrlNbr.Value))
                    .First(g => g.Code == "SMPL");
                var ptraCrafts15 = await craftRepo.GetByParentAndRailroadAsync(ptraParentCore.CtrlNbr, ptraRR15.CtrlNbr);
                var ptraEngCraft = ptraCrafts15.First(c => c.CraftName == "Engineer");
                var ptraTrnCraft = ptraCrafts15.First(c => c.CraftName == "Trainman");
                foreach (var r in await rosterRepo.GetByCraftCtrlNbrAsync(ptraEngCraft.CtrlNbr))
                    await SeedCertsForRosterAsync(r, cfr240);
                foreach (var r in await rosterRepo.GetByCraftCtrlNbrAsync(ptraTrnCraft.CtrlNbr))
                    await SeedCertsForRosterAsync(r, cfr242sw);
            }
        }

        // ?? Section 15b: Extended Absence Board Assignments (expired FRA certs) ??????????????
        // Employees seeded with expired certifications are placed on their craft's Extended Absence
        // board. Uses the app service (same path as the UI) to enforce all business rules and checks.
        var allCerts15b = await empCertRepo.GetAllAsync();
        var employeesWithExpiredCerts = allCerts15b
            .Where(c => c.Status == CertificationStatuses.Expired)
            .Select(c => c.EmployeeCtrlNbr)
            .Distinct()
            .ToList();

        if (employeesWithExpiredCerts.Count > 0)
        {
            var rosterBoardAppSvc = sp.GetRequiredService<RosterBoardAppService>();

            var allBoards15b = await rosterBoardRepo.GetAllAsync();
            var extAbsBoards = allBoards15b.Where(b => b.BoardType == BoardType.ExtendedAbsence).ToList();

            // Build a CtrlNbr → CraftCtrlNbr lookup for all rosters
            var allRosters15b = await rosterRepo.GetByCraftCtrlNbrsAsync(
                (await craftRepo.GetAllAsync()).Select(c => c.CtrlNbr));
            var rosterCraftMap = allRosters15b.ToDictionary(r => r.CtrlNbr, r => r.CraftCtrlNbr);

            foreach (var empCtrlNbr in employeesWithExpiredCerts)
            {
                // Find this employee's active seniority entry to identify their craft
                var empSeniority = await seniorityRepo.GetByEmployeeCtrlNbrAsync(empCtrlNbr);
                var activeSen = empSeniority.FirstOrDefault(s => s.LastActiveRoster);
                if (activeSen is null) continue;

                if (!rosterCraftMap.TryGetValue(activeSen.RosterCtrlNbr, out var craftCtrlNbr)) continue;

                // Find the Extended Absence board for this craft
                var extBoard = extAbsBoards.FirstOrDefault(b => b.CraftCtrlNbr == craftCtrlNbr);
                if (extBoard is null) continue;

                var nextOrder = extBoard.Positions.Count + 1;
                try
                {
                    await rosterBoardAppSvc.AddRosterBoardPositionAsync(extBoard.CtrlNbr, empCtrlNbr, nextOrder);
                }
                catch (InvalidOperationException)
                {
                    // Employee already has a position on this board — skip
                    continue;
                }

                // Refresh local snapshot so the next employee gets the correct nextOrder
                extBoard = (await rosterBoardRepo.GetAllAsync()).First(b => b.CtrlNbr == extBoard.CtrlNbr);
                var idx = extAbsBoards.FindIndex(b => b.CtrlNbr == extBoard.CtrlNbr);
                if (idx >= 0) extAbsBoards[idx] = extBoard;
            }
        }

        // ?? Section 16: Qualifications (Transportation) ??????????????????????????
        // Rule: All transportation department employees must hold the CFR certification
        // to be "qualified" in their craft. Foreman is earned 90 days after certification.
        var qualTypeRepo = sp.GetRequiredService<IQualificationTypeRepository>();
        var empQualRepo = sp.GetRequiredService<IEmployeeQualificationRepository>();
        var allQualTypes = await qualTypeRepo.GetAllAsync();
        if (allQualTypes.Count == 0)
        {
            var cfr240Q = await regQualRepo.GetByCodeAsync("CFR-240-ENGINEER");
            var cfr242swQ = await regQualRepo.GetByCodeAsync("CFR-242-SWITCHMAN");

            async Task SeedTenantQualificationsAsync(
                Domain.ValueObjects.ControlNumber parentCtrlNbr,
                Domain.Modules.TenantConfig.DynamicGroup railroad,
                Domain.Models.Seniority.Craft engCraft,
                Domain.Models.Seniority.Craft trnCraft)
            {
                SetParent(parentCtrlNbr.Value);

                // ---- Engineer Qualified ----
                var engQT = QualificationType.Create(
                    parentCtrlNbr,
                    code: "ENGINEER-QUALIFIED",
                    name: "Qualified Engineer",
                    evaluationStrategy: EvaluationStrategies.QualificationHeld,
                    scopeGroupCtrlNbr: railroad.CtrlNbr,
                    craftCtrlNbr: engCraft.CtrlNbr,
                    regulatoryQualificationCtrlNbr: cfr240Q?.CtrlNbr,
                    description: "Engineer who holds an active CFR-240 certification.",
                    isBlocking: true);
                engQT.AddRequirement(
                    requirementKind: RequirementKinds.FraCertificationHeld,
                    threshold: 1,
                    thresholdUnit: ThresholdUnits.Count,
                    description: "Must hold an Active CFR-240 Engineer certification.",
                    requiredRegulatoryQualCtrlNbr: cfr240Q?.CtrlNbr);
                await qualTypeRepo.AddAsync(engQT);

                // ---- Trainman Qualified ----
                var trnQT = QualificationType.Create(
                    parentCtrlNbr,
                    code: "TRAINMAN-QUALIFIED",
                    name: "Qualified Trainman",
                    evaluationStrategy: EvaluationStrategies.QualificationHeld,
                    scopeGroupCtrlNbr: railroad.CtrlNbr,
                    craftCtrlNbr: trnCraft.CtrlNbr,
                    regulatoryQualificationCtrlNbr: cfr242swQ?.CtrlNbr,
                    description: "Trainman who holds an active CFR-242 Switchman certification.",
                    isBlocking: true);
                trnQT.AddRequirement(
                    requirementKind: RequirementKinds.FraCertificationHeld,
                    threshold: 1,
                    thresholdUnit: ThresholdUnits.Count,
                    description: "Must hold an Active CFR-242 Switchman certification.",
                    requiredRegulatoryQualCtrlNbr: cfr242swQ?.CtrlNbr);
                await qualTypeRepo.AddAsync(trnQT);

                // ---- Yard Foreman (90 days post-certification) ----
                var foremanQT = QualificationType.Create(
                    parentCtrlNbr,
                    code: "YARD-FOREMAN",
                    name: "Yard Foreman",
                    evaluationStrategy: EvaluationStrategies.TimeFromEvent,
                    scopeGroupCtrlNbr: railroad.CtrlNbr,
                    craftCtrlNbr: trnCraft.CtrlNbr,
                    description: "Trainman eligible to work Foreman position 90 days after CFR-242 certification.",
                    restrictionLabel: "Helper Only");
                foremanQT.AddRequirement(
                    requirementKind: RequirementKinds.TimeFromEvent,
                    threshold: 90,
                    thresholdUnit: ThresholdUnits.Days,
                    description: "At least 90 days since seniority date.",
                    eventSource: EventSources.SeniorityDate);
                await qualTypeRepo.AddAsync(foremanQT);

                // ---- Auto-assign required qualifications for every employee on the roster ----
                var qualReactiveSvc = sp.GetRequiredService<QualificationReactiveService>();
                foreach (var craft in new[] { engCraft, trnCraft })
                {
                    var rosters = await rosterRepo.GetByCraftCtrlNbrAsync(craft.CtrlNbr);
                    foreach (var roster in rosters.Where(r => r.RosterType == RosterType.Active))
                    {
                        var seniorities = await seniorityRepo.GetByRosterCtrlNbrAsync(roster.CtrlNbr);
                        foreach (var sen in seniorities)
                            await qualReactiveSvc.HandleAddedToRosterAsync(sen.EmployeeCtrlNbr, craft.CtrlNbr);
                    }
                }
            }

            // CSX
            SetParent(csxParentCtrlNbr);
            var csxRR16 = (await groupRepo.GetByGroupTypeNameAsync("Railroad"))
                .First(g => g.Code == "CSX");
            var csxCrafts16 = await craftRepo.GetByParentAndRailroadAsync(csxParentCtrlNbr, csxRR16.CtrlNbr);
            await SeedTenantQualificationsAsync(
                csxParentCtrlNbr,
                csxRR16,
                csxCrafts16.First(c => c.CraftName == "Engineer"),
                csxCrafts16.First(c => c.CraftName == "Trainman"));

            // PTRA
            SetParent(ptraParentCore.CtrlNbr.Value);
            var ptraRR16 = (await groupRepo.GetByGroupTypeNameAsync("Railroad", ptraParentCore.CtrlNbr.Value))
                .First(g => g.Code == "SMPL");
            var ptraCrafts16 = await craftRepo.GetByParentAndRailroadAsync(ptraParentCore.CtrlNbr, ptraRR16.CtrlNbr);
            await SeedTenantQualificationsAsync(
                ptraParentCore.CtrlNbr.Value,
                ptraRR16,
                ptraCrafts16.First(c => c.CraftName == "Engineer"),
                ptraCrafts16.First(c => c.CraftName == "Trainman"));
        }

        // ?? Section: Craft Role Qualifications ??
        var craftRoleQualRepo = sp.GetRequiredService<ICraftRoleQualificationRepository>();
        var existingRoleQuals = await craftRoleQualRepo.GetAllAsync();
        if (existingRoleQuals.Count == 0)
        {
            async Task SeedRoleQualAsync(CraftRole role, QualificationType qt)
            {
                var rq = role.AddRequiredQualification(qt.CtrlNbr);
                await craftRoleQualRepo.AddAsync(rq);
            }

            // CSX
            SetParent(csxParentCtrlNbr);
            var csxRailroad = (await groupRepo.GetByGroupTypeNameAsync("Railroad")).First(g => g.Code == "CSX");
            var csxRoles = await craftRoleRepo.GetByRailroadAsync(csxRailroad.CtrlNbr);
            var csxQT = await qualTypeRepo.GetByParentCtrlNbrAsync(csxParentCtrlNbr);
            var csxEngRole   = csxRoles.First(r => r.Code == "ENGR");
            var csxCondRole  = csxRoles.First(r => r.Code == "COND");
            var csxTrmnRole  = csxRoles.First(r => r.Code == "TRMN");
            var csxEngQT     = csxQT.First(q => q.Code == "ENGINEER-QUALIFIED");
            var csxTrnQT     = csxQT.First(q => q.Code == "TRAINMAN-QUALIFIED");
            var csxForemanQT = csxQT.First(q => q.Code == "YARD-FOREMAN");
            await SeedRoleQualAsync(csxEngRole,  csxEngQT);
            await SeedRoleQualAsync(csxCondRole, csxTrnQT);
            await SeedRoleQualAsync(csxCondRole, csxForemanQT);
            await SeedRoleQualAsync(csxTrmnRole, csxTrnQT);

            // PTRA
            SetParent(ptraParentCore.CtrlNbr.Value);
            var ptraRailroad = (await groupRepo.GetByGroupTypeNameAsync("Railroad", ptraParentCore.CtrlNbr.Value)).First(g => g.Code == "SMPL");
            var ptraRoles = await craftRoleRepo.GetByRailroadAsync(ptraRailroad.CtrlNbr);
            var ptraQT = await qualTypeRepo.GetByParentCtrlNbrAsync(ptraParentCore.CtrlNbr);
            var ptraEngRole     = ptraRoles.First(r => r.Code == "E");
            var ptraForemanRole = ptraRoles.First(r => r.Code == "F");
            var ptraHelperRole  = ptraRoles.First(r => r.Code == "H");
            var ptraEngQT       = ptraQT.First(q => q.Code == "ENGINEER-QUALIFIED");
            var ptraTrnQT       = ptraQT.First(q => q.Code == "TRAINMAN-QUALIFIED");
            var ptraForemanQT   = ptraQT.First(q => q.Code == "YARD-FOREMAN");
            await SeedRoleQualAsync(ptraEngRole,     ptraEngQT);
            await SeedRoleQualAsync(ptraForemanRole, ptraTrnQT);
            await SeedRoleQualAsync(ptraForemanRole, ptraForemanQT);
            await SeedRoleQualAsync(ptraHelperRole,  ptraTrnQT);
        }

        // ???? Section 17: PTRA Crew Incumbencies + Extra Board Placements ??????????
        // Guard: skip if any PositionAssignments already exist for PTRA
        SetParent(ptraParentCore.CtrlNbr.Value);
        var positionAssignmentRepo = sp.GetRequiredService<IPositionAssignmentRepository>();
        var ptraEmpListFinal = await employeeRepo.GetByClientCtrlNbrAsync(ptraParentCore.CtrlNbr);

        if (ptraEmpListFinal.Count > 0)
        {
            // Check if any PTRA employee already has a Crew position assignment (excludes Board assignments)
            var assignedSet = await positionAssignmentRepo.GetAssignedEmployeeCtrlNbrsByTypeAsync(PositionAssignmentType.Direct);
            bool anyPtraAssigned = ptraEmpListFinal.Any(e => assignedSet.Contains(e.CtrlNbr.Value));

            if (!anyPtraAssigned)
            {
                // Resolve PTRA railroad + crafts
                var ptraRailroadF = (await groupRepo.GetByGroupTypeNameAsync("Railroad", ptraParentCore.CtrlNbr.Value))
                    .First(g => g.Code == "SMPL");
                var allCraftsF = await craftRepo.GetByParentAndRailroadAsync(ptraParentCore.CtrlNbr, ptraRailroadF.CtrlNbr);
                var ptraEngCraftF = allCraftsF.First(c => c.CraftName == "Engineer");
                var ptraTrnCraftF = allCraftsF.First(c => c.CraftName == "Trainman");

                // Resolve roles
                var allRolesF = await craftRoleRepo.GetAllAsync();
                var ptraEngRoleF = allRolesF.First(r => r.Code == "E");
                var ptraFmnRoleF = allRolesF.First(r => r.Code == "F");
                var ptraHlpRoleF = allRolesF.First(r => r.Code == "H");

                // Resolve PTRA crews by railroad
                var allPtraCrews = await crewRepo.GetByRailroadAsync(ptraRailroadF.CtrlNbr);
                var ptra3PosCrewNames = new HashSet<string> { "130", "150", "230", "250", "330", "350" };
                var ptra2PosCrewNames = new HashSet<string> { "140", "240", "340", "RLF-A", "RLF-B", "RLF-C" };

                // Collect all crew positions ordered by crew name then display order
                var ptra3PosCrewsF = allPtraCrews.Where(c => ptra3PosCrewNames.Contains(c.Name)).OrderBy(c => c.Name).ToList();
                var ptra2PosCrewsF = allPtraCrews.Where(c => ptra2PosCrewNames.Contains(c.Name)).OrderBy(c => c.Name).ToList();

                var allCrewCtrlNbrs = ptra3PosCrewsF.Concat(ptra2PosCrewsF).Select(c => c.CtrlNbr);
                var allPositions = await crewPositionRepo.GetByCrewsAsync(allCrewCtrlNbrs);

                // Separate by role
                var engPositions = allPositions.Where(p => p.CraftRoleCtrlNbr == ptraEngRoleF.CtrlNbr)
                    .OrderBy(p => p.CrewCtrlNbr.Value).ThenBy(p => p.DisplayOrder).ToList();
                var fmnPositions = allPositions.Where(p => p.CraftRoleCtrlNbr == ptraFmnRoleF.CtrlNbr)
                    .OrderBy(p => p.CrewCtrlNbr.Value).ThenBy(p => p.DisplayOrder).ToList();
                var hlpPositions = allPositions.Where(p => p.CraftRoleCtrlNbr == ptraHlpRoleF.CtrlNbr)
                    .OrderBy(p => p.CrewCtrlNbr.Value).ThenBy(p => p.DisplayOrder).ToList();

                // Exclude any employee already on a board (e.g. Extended Absence from Section 15b).
                // An employee can only hold one staffable position at a time.
                var alreadyBoardAssigned = await positionAssignmentRepo.GetAssignedEmployeeCtrlNbrsByTypeAsync(PositionAssignmentType.Board);

                // Derive board candidates from the active seniority roster — not hardcoded counts.
                // Anyone on the active seniority roster who does not yet hold a staffable position is eligible.
                var ptraAllRosters = await rosterRepo.GetByCraftCtrlNbrAsync(ptraEngCraftF.CtrlNbr);
                ptraAllRosters.AddRange(await rosterRepo.GetByCraftCtrlNbrAsync(ptraTrnCraftF.CtrlNbr));
                var ptraEngActiveRoster = ptraAllRosters.First(r => r.CraftCtrlNbr == ptraEngCraftF.CtrlNbr && r.RosterType == RosterType.Active);
                var ptraTrnActiveRoster = ptraAllRosters.First(r => r.CraftCtrlNbr == ptraTrnCraftF.CtrlNbr && r.RosterType == RosterType.Active);

                var ptraEngSeniorities = await seniorityRepo.GetByRosterCtrlNbrAsync(ptraEngActiveRoster.CtrlNbr);
                var ptraTrnSeniorities = await seniorityRepo.GetByRosterCtrlNbrAsync(ptraTrnActiveRoster.CtrlNbr);

                var empLookup = ptraEmpListFinal.ToDictionary(e => e.CtrlNbr);
                var alreadyCrewAssigned = assignedSet; // "Crew" assignments resolved above

                var ptraEngEmps = ptraEngSeniorities
                    .Select(s => empLookup.GetValueOrDefault(s.EmployeeCtrlNbr))
                    .Where(e => e is not null && !alreadyBoardAssigned.Contains(e!.CtrlNbr.Value) && !alreadyCrewAssigned.Contains(e.CtrlNbr.Value))
                    .Select(e => e!)
                    .ToList();

                var ptraTrnEmps = ptraTrnSeniorities
                    .Select(s => empLookup.GetValueOrDefault(s.EmployeeCtrlNbr))
                    .Where(e => e is not null && !alreadyBoardAssigned.Contains(e!.CtrlNbr.Value) && !alreadyCrewAssigned.Contains(e.CtrlNbr.Value))
                    .Select(e => e!)
                    .ToList();

                var ptraTrnSeniorityDateByEmployee = ptraTrnSeniorities
                    .GroupBy(s => s.EmployeeCtrlNbr)
                    .ToDictionary(g => g.Key, g => g.Min(s => s.RosterDate.Date));

                var incumbencyBase = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var today = DateTime.UtcNow.Date;
                var rng = new Random(42);

                // Position dates: randomly 60–365 days in the past so all crew employees
                // meet the typical 30-day eligibility requirement during testing.
                DateTime RandomIncumbencyDate()
                {
                    var daysAgo = rng.Next(60, 366);
                    return today.AddDays(-daysAgo);
                }

                // ── Crew Incumbencies ───────────────────────────────────────────
                var crewsAppSvc = sp.GetRequiredService<CrewsAppService>();

                // Engineer crew slots — only as many as we have eligible employees
                int engCrewAssigned = 0;
                for (int i = 0; i < engPositions.Count && i < ptraEngEmps.Count; i++)
                {
                    var pos = engPositions[i];
                    var emp = ptraEngEmps[i];
                    await crewsAppSvc.CreateCrewIncumbencyAsync(pos.CtrlNbr.Value, emp.CtrlNbr.Value, RandomIncumbencyDate(), null);
                    engCrewAssigned++;
                }

                var foremanEligibilityCutoff = today.AddDays(-90);
                var foremanEligibleTrainmen = ptraTrnEmps
                    .Where(e => ptraTrnSeniorityDateByEmployee.TryGetValue(e.CtrlNbr, out var seniorityDate)
                                && seniorityDate <= foremanEligibilityCutoff)
                    .ToList();
                var helperOnlyTrainmen = ptraTrnEmps
                    .Where(e => !foremanEligibleTrainmen.Any(fe => fe.CtrlNbr == e.CtrlNbr))
                    .ToList();

                // Foreman crew slots (only trainmen who are not Helper Only)
                int fmnCrewAssigned = 0;
                for (int i = 0; i < fmnPositions.Count && i < foremanEligibleTrainmen.Count; i++)
                {
                    var pos = fmnPositions[i];
                    var emp = foremanEligibleTrainmen[i];
                    await crewsAppSvc.CreateCrewIncumbencyAsync(pos.CtrlNbr.Value, emp.CtrlNbr.Value, RandomIncumbencyDate(), null);
                    fmnCrewAssigned++;
                }

                // Helper-only trainmen stay on the board; helper crew slots are filled only from
                // fully eligible trainmen (90+ days).
                var helperCandidateTrainmen = foremanEligibleTrainmen
                    .Skip(fmnCrewAssigned)
                    .ToList();

                // Helper crew slots (remaining fully eligible trainmen only)
                int hlpCrewAssigned = 0;
                for (int i = 0; i < hlpPositions.Count && i < helperCandidateTrainmen.Count; i++)
                {
                    var pos = hlpPositions[i];
                    var emp = helperCandidateTrainmen[i];
                    await crewsAppSvc.CreateCrewIncumbencyAsync(pos.CtrlNbr.Value, emp.CtrlNbr.Value, RandomIncumbencyDate(), null);
                    hlpCrewAssigned++;
                }

                // ── Extra Board Placements ──────────────────────────────────────
                // Resolve extra boards
                var allBoardsF = await rosterBoardRepo.GetAllAsync();
                var ptraEngBoardCtrlNbr = allBoardsF.First(b => b.CraftCtrlNbr == ptraEngCraftF.CtrlNbr && b.BoardType == BoardType.ExtraBoard).CtrlNbr;
                var ptraTrnBoardCtrlNbr = allBoardsF.First(b => b.CraftCtrlNbr == ptraTrnCraftF.CtrlNbr && b.BoardType == BoardType.ExtraBoard).CtrlNbr;
                var rosterBoardAppSvcF = sp.GetRequiredService<RosterBoardAppService>();

                // Remaining eligible engineers go to extra board (those not placed in crew slots)
                var boardEngEmps = ptraEngEmps.Skip(engCrewAssigned).ToList();
                for (int i = 0; i < boardEngEmps.Count; i++)
                {
                    await rosterBoardAppSvcF.AddRosterBoardPositionAsync(ptraEngBoardCtrlNbr, boardEngEmps[i].CtrlNbr, i + 1, assignedDateUtc: RandomIncumbencyDate());
                }

                // Remaining eligible trainmen go to extra board (those not placed in crew slots)
                var boardTrnEmps = helperOnlyTrainmen
                    .Concat(helperCandidateTrainmen.Skip(hlpCrewAssigned))
                    .ToList();
                for (int i = 0; i < boardTrnEmps.Count; i++)
                {
                    await rosterBoardAppSvcF.AddRosterBoardPositionAsync(ptraTrnBoardCtrlNbr, boardTrnEmps[i].CtrlNbr, i + 1, assignedDateUtc: RandomIncumbencyDate());
                }
            }
        }

        // Keep seniority state aligned with board placement: employees on Extended Absence boards
        // should be in the Inactive seniority state.
        async Task SyncExtendedAbsenceBoardStatesAsync(ControlNumber parentCtrlNbr)
        {
            SetParent(parentCtrlNbr.Value);

            var inactiveState = (await seniorityStateRepo.GetByParentCtrlNbrAsync(parentCtrlNbr))
                .FirstOrDefault(s => s.StateDescription == "Inactive");
            if (inactiveState is null)
                return;

            var allBoards = await rosterBoardRepo.GetAllAsync();
            var extendedAbsenceBoards = allBoards
                .Where(b => b.BoardType == BoardType.ExtendedAbsence)
                .ToList();
            if (extendedAbsenceBoards.Count == 0)
                return;

            foreach (var board in extendedAbsenceBoards)
            {
                foreach (var boardPos in board.Positions)
                {
                    var employeeSeniority = await seniorityRepo.GetByEmployeeCtrlNbrAsync(boardPos.EmployeeCtrlNbr);

                    var matchingSeniority = employeeSeniority
                        .FirstOrDefault(s => s.RosterCtrlNbr == board.RosterCtrlNbr)
                        ?? employeeSeniority.FirstOrDefault(s => s.LastActiveRoster);

                    if (matchingSeniority is null || matchingSeniority.SeniorityStateCtrlNbr == inactiveState.CtrlNbr)
                        continue;

                    matchingSeniority.Update(seniorityStateCtrlNbr: inactiveState.CtrlNbr);
                    await seniorityRepo.UpdateAsync(matchingSeniority);
                }
            }
        }

        await SyncExtendedAbsenceBoardStatesAsync(simpleCorpCore.CtrlNbr);
        await SyncExtendedAbsenceBoardStatesAsync(csxParentCore.CtrlNbr);

        async Task SeedDefaultBoardPositionOrderAsync(ControlNumber parentCtrlNbr)
        {
            SetParent(parentCtrlNbr.Value);

            var rosterBoardAppSvc = sp.GetRequiredService<RosterBoardAppService>();
            var boards = await rosterBoardRepo.GetAllAsync();

            foreach (var board in boards.Where(b => b.Positions.Count > 0))
            {
                var orderedPositions = board.Positions
                    .OrderBy(p => p.PositionOrder <= 0 ? int.MaxValue : p.PositionOrder)
                    .ThenBy(p => p.CtrlNbr.Value)
                    .ToList();

                var reorder = orderedPositions
                    .Select((position, index) => (position.CtrlNbr, PositionOrder: index + 1))
                    .ToList();

                if (!orderedPositions
                    .Select((position, index) => position.PositionOrder == index + 1)
                    .All(isAlreadyOrdered => isAlreadyOrdered))
                {
                    await rosterBoardAppSvc.ReorderRosterBoardPositionsAsync(board.CtrlNbr, reorder);
                }
            }
        }

        await SeedDefaultBoardPositionOrderAsync(simpleCorpCore.CtrlNbr);
        await SeedDefaultBoardPositionOrderAsync(csxParentCore.CtrlNbr);

        await SeedSimpleCorpAnnualizedStrategyAsync(sp);
    }

    private static async Task SeedSimpleCorpAnnualizedStrategyAsync(IServiceProvider sp)
    {
        var parentRepo        = sp.GetRequiredService<IParentRepository>();
        var groupRepo         = sp.GetRequiredService<IDynamicGroupRepository>();
        var strategyRepo      = sp.GetRequiredService<IRequiredPositionsStrategyRepository>();
        var craftStrategyRepo = sp.GetRequiredService<ICraftRequiredPositionsStrategyRepository>();
        var craftRepo         = sp.GetRequiredService<ICraftRepository>();

        var ptraParent = (await parentRepo.GetAllAsync())
            .FirstOrDefault(p => p.Name.Value == "Simple Corp");
        if (ptraParent is null) return;

        var ptraRailroad = (await groupRepo.GetByGroupTypeNameAsync("Railroad", ptraParent.CtrlNbr))
            .FirstOrDefault(g => g.Code == "SMPL");
        if (ptraRailroad is null) return;

        // Seed ANNUALIZED_AVG as a system-level strategy
        // Formula: ceiling((avgDailyVacancies * daysPerYear) / payPeriodsPerYear / daysPerPayPeriod)
        const string annualizedName = "Annualized Average";
        const string annualizedDesc = "Calculates required board positions using: ceiling((avgDailyVacancies * daysPerYear) / payPeriodsPerYear / daysPerPayPeriod).";
        const string annualizedParams = """{"daysPerYear":365,"payPeriodsPerYear":24,"daysPerPayPeriod":12}""";
        var annualized = await strategyRepo.GetByCodeAsync("ANNUALIZED_AVG");
        if (annualized is null)
        {
            annualized = RequiredPositionsStrategy.Create(
                code: "ANNUALIZED_AVG",
                name: annualizedName,
                description: annualizedDesc,
                formulaType: "AnnualizedAverage",
                parametersJson: annualizedParams);
            await strategyRepo.AddAsync(annualized);
        }
        else
        {
            annualized.Update(annualizedName, annualizedDesc, annualized.FormulaType, annualized.ParametersJson);
            await strategyRepo.UpdateAsync(annualized);
        }

        // Assign PTRA Engineer and Trainman crafts to the annualized strategy
        var ptraCrafts = await craftRepo.GetByParentAndRailroadAsync(ptraParent.CtrlNbr, ptraRailroad.CtrlNbr);
        foreach (var craft in ptraCrafts.Where(c => c.CraftName is "Engineer" or "Trainman"))
        {
            var existing = await craftStrategyRepo.GetByCraftAsync(craft.CtrlNbr!);
            if (existing is null)
                await craftStrategyRepo.AddAsync(
                    CraftRequiredPositionsStrategy.Create(craft.CtrlNbr!, annualized.CtrlNbr!));
        }

        // Assign PTRA Clerical craft to STATIC strategy
        var staticStrategy = await strategyRepo.GetStaticAsync();
        if (staticStrategy is not null)
        {
            foreach (var craft in ptraCrafts.Where(c => c.CraftName == "Clerical"))
            {
                var existing = await craftStrategyRepo.GetByCraftAsync(craft.CtrlNbr!);
                if (existing is null)
                    await craftStrategyRepo.AddAsync(
                        CraftRequiredPositionsStrategy.Create(craft.CtrlNbr!, staticStrategy.CtrlNbr!));
            }
        }

        // Seed PTRA bulletin rules for all crafts — Engineer: 72h bid window; all other crafts
        // (Trainman, Clerical, …): 24h bid window. All times match legacy RosterBulletinRule
        // defaults (04:00 start/close/effective/cutoff).
        var bulletinRuleRepo = sp.GetRequiredService<IBulletinRuleRepository>();
        var legacyStartTime    = new TimeSpan(04, 00, 00);
        var legacyCloseTime    = new TimeSpan(04, 00, 00);
        var legacyEffectiveTime = new TimeSpan(04, 00, 00);
        // Bulletins created after 04:00 local roll to 04:00 the next day (legacy
        // RailroadPosition.CreateRailroadPositionBulletin: next-day when now > BulletinStartTime).
        var legacyCutOffTime = new TimeSpan(04, 00, 00);

        var (engineerBidHours, defaultBidHours) = (72, 24);

        foreach (var craft in ptraCrafts)
        {
            var existingRule = await bulletinRuleRepo.GetByCraftAsync(craft.CtrlNbr!);
            int bidHours = craft.CraftName == "Engineer" ? engineerBidHours : defaultBidHours;

            // Trainman uses JuniorHelperOrExtraBoard: foreman vacancies are filled by the youngest
            // helper regardless of whether they are on the extra board or in an assigned helper position.
            // Engineer uses the default JuniorExtraBoard.
            var selectionMode = craft.CraftName == "Trainman"
                ? ForceAssignSelectionMode.JuniorHelperOrExtraBoard
                : ForceAssignSelectionMode.JuniorExtraBoard;

            // Engineer bulletins are assigned at 04:00 unless the effective date is an off day, in
            // which case they are assigned 3 hours before the first work day's on-duty time
            // (legacy RailroadPositionBulletin.AssignDateTime "Engineer" branch). Trainman keeps the
            // fixed effective time.
            var (effectiveTimeMode, forceAssignHours) = craft.CraftName == "Engineer"
                ? (BulletinEffectiveTimeMode.EffectiveTimeUnlessOffDay, 3)
                : (BulletinEffectiveTimeMode.FixedEffectiveTime, 0);

            if (existingRule is null)
                await bulletinRuleRepo.AddAsync(BulletinRule.Create(
                    craft.CtrlNbr!,
                    bidWindowHours: bidHours,
                    bidWindowStartTime: legacyStartTime,
                    bidWindowCloseTime: legacyCloseTime,
                    effectiveOffsetDays: 0,
                    effectiveTime: legacyEffectiveTime,
                    forceAssignHours: forceAssignHours,
                    forceAssignSelectionMode: selectionMode,
                    bulletinCutOffTime: legacyCutOffTime,
                    effectiveTimeMode: effectiveTimeMode));
            else
            {
                existingRule.Update(
                    bidWindowHours: bidHours,
                    bidWindowStartTime: legacyStartTime,
                    bidWindowCloseTime: legacyCloseTime,
                    effectiveOffsetDays: 0,
                    effectiveTime: legacyEffectiveTime,
                    forceAssignHours: forceAssignHours,
                    forceAssignSelectionMode: selectionMode,
                    bulletinCutOffTime: legacyCutOffTime,
                    effectiveTimeMode: effectiveTimeMode);
                await bulletinRuleRepo.UpdateAsync(existingRule);
            }
        }

    }
}
