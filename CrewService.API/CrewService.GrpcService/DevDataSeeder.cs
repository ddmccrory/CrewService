using CrewService.Domain.Models.ContactTypes;
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
using CrewService.Domain.Modules.RailroadInfo;
using CrewService.Domain.Modules.Safety;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.UserAccess;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Application.FraCompliance;
using CrewService.Infrastructure.Models.UserAccount;
using CrewService.Domain.Interfaces;
using CrewService.Presentation;
using CrewService.Presentation.Services;
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

        var parentService = sp.GetRequiredService<ParentService>();
        var groupTypeRepo = sp.GetRequiredService<IGroupTypeRepository>();
        var groupRepo = sp.GetRequiredService<IDynamicGroupRepository>();
        var parentRepo = sp.GetRequiredService<IParentRepository>();

        // Idempotent guard — only seed when DevDataSeeder-specific group types are absent
        // (migration-seeded types like Location, Zone, etc. do not count)
        var existing = await groupTypeRepo.GetAllAsync();
        if (!existing.Any(gt => gt.Name is "Region" or "Subdivision"))
        {

        // ?? Parents (via ParentService — auto-seeds system types + attribute definitions) ??
        var simpleCorpResp = await parentService.CreateParentAsync(new CreateParentRequest { Name = "Simple Corp" }, null!);
        var ptraCorpResp = await parentService.CreateParentAsync(new CreateParentRequest { Name = "Port Terminal Railroad Association" }, null!);
        var holdingCorpResp = await parentService.CreateParentAsync(new CreateParentRequest { Name = "CSX Corporation" }, null!);

        // Look up auto-created system types for subsequent group creation
        var autoCreatedTypes = await groupTypeRepo.GetAllAsync();
        var simpleRailroadType = autoCreatedTypes.First(gt => gt.Name == "Railroad" && gt.ParentCtrlNbr == simpleCorpResp.CtrlNbr);
        var ptraRailroadType = autoCreatedTypes.First(gt => gt.Name == "Railroad" && gt.ParentCtrlNbr == ptraCorpResp.CtrlNbr);
        var csxRailroadType = autoCreatedTypes.First(gt => gt.Name == "Railroad" && gt.ParentCtrlNbr == holdingCorpResp.CtrlNbr);

        // Railroads (as DynamicGroups)
        var simpleRR = DynamicGroup.Create(simpleRailroadType.CtrlNbr.Value, "Simple Railroad", parentGroupCtrlNbr: null, path: null, isWorkArea: false, code: "SMPL", parentCtrlNbr: simpleCorpResp.CtrlNbr);
        await groupRepo.AddAsync(simpleRR);

        var ptraRR = DynamicGroup.Create(ptraRailroadType.CtrlNbr.Value, "Port Terminal Railroad Association", parentGroupCtrlNbr: null, path: null, isWorkArea: true, code: "PTRA", parentCtrlNbr: ptraCorpResp.CtrlNbr);
        await groupRepo.AddAsync(ptraRR);

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

        // ?? Scenario 2: PTRA (Legacy Railroad) ??????????????????????
        // Railroad is the work area; hierarchy: Railroad -> Location
        var ptraLocationType = GroupType.Create("Location", "On duty locations", isWorkArea: false, parentCtrlNbr: ptraCorpResp.CtrlNbr, parentGroupTypeCtrlNbr: ptraRailroadType.CtrlNbr.Value);
        await groupTypeRepo.AddAsync(ptraLocationType);

        var northYard = DynamicGroup.Create(ptraLocationType.CtrlNbr.Value, "North Yard", parentGroupCtrlNbr: ptraRR.CtrlNbr.Value, path: null, isWorkArea: false, code: "NOYD", parentCtrlNbr: ptraCorpResp.CtrlNbr, railroadCtrlNbr: ptraRR.CtrlNbr.Value);
        await groupRepo.AddAsync(northYard);

        var manchesterYard = DynamicGroup.Create(ptraLocationType.CtrlNbr.Value, "Manchester Yard", parentGroupCtrlNbr: ptraRR.CtrlNbr.Value, path: null, isWorkArea: false, code: "MCYD", parentCtrlNbr: ptraCorpResp.CtrlNbr, railroadCtrlNbr: ptraRR.CtrlNbr.Value);
        await groupRepo.AddAsync(manchesterYard);

        var pasadenaYard = DynamicGroup.Create(ptraLocationType.CtrlNbr.Value, "Pasadena Yard", parentGroupCtrlNbr: ptraRR.CtrlNbr.Value, path: null, isWorkArea: false, code: "PSYD", parentCtrlNbr: ptraCorpResp.CtrlNbr, railroadCtrlNbr: ptraRR.CtrlNbr.Value);
        await groupRepo.AddAsync(pasadenaYard);

        // ?? Scenario 3: Holding Company (CSX) ????????????????????????
        // Parent -> Region -> Subdivision (user will add work areas)
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
            isWorkArea: false,
            parentCtrlNbr: holdingCorpResp.CtrlNbr,
            railroadCtrlNbr: csxRR.CtrlNbr.Value);
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
            await parentService.CreateParentAsync(new CreateParentRequest { Name = "Simple Corp" }, null!);

        if (!allParentsCore.Any(p => p.Name.Value == "Port Terminal Railroad Association"))
            await parentService.CreateParentAsync(new CreateParentRequest { Name = "Port Terminal Railroad Association" }, null!);

        if (!allParentsCore.Any(p => p.Name.Value == "CSX Corporation"))
            await parentService.CreateParentAsync(new CreateParentRequest { Name = "CSX Corporation" }, null!);

        // Re-read after potential service-based creations
        allParentsCore = await parentRepo.GetAllAsync();
        var simpleCorpCore = allParentsCore.First(p => p.Name.Value == "Simple Corp");
        var ptraParentCore = allParentsCore.First(p => p.Name.Value == "Port Terminal Railroad Association");
        var csxParentCore = allParentsCore.First(p => p.Name.Value == "CSX Corporation");

        // Backfill per-parent system types for pre-existing parents that may be missing types
        var groupTypesBackfill = await groupTypeRepo.GetAllAsync();

        foreach (var parentCore in new[] { simpleCorpCore, ptraParentCore, csxParentCore })
        {
            var pCtrl = parentCore.CtrlNbr.Value;
            if (!groupTypesBackfill.Any(gt => gt.Name == "Railroad" && gt.ParentCtrlNbr == pCtrl))
                await groupTypeRepo.AddAsync(GroupType.Create("Railroad", "Railroad operational boundaries", isWorkArea: false, parentCtrlNbr: pCtrl));
        }

        // Backfill group types scoped to CSX Corporation
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

        if (allGroupsForRR.All(g => g.Code != "SMPL"))
        {
            var smplRRType = allTypesForRR.First(gt => gt.Name == "Railroad" && gt.ParentCtrlNbr == simpleCorpCore.CtrlNbr.Value);
            await groupRepo.AddAsync(DynamicGroup.Create(smplRRType.CtrlNbr.Value, "Simple Railroad", parentGroupCtrlNbr: null, path: null, isWorkArea: false, code: "SMPL", parentCtrlNbr: simpleCorpCore.CtrlNbr.Value));
        }

        if (allGroupsForRR.All(g => g.Code != "PTRA"))
        {
            var ptraRRType = allTypesForRR.First(gt => gt.Name == "Railroad" && gt.ParentCtrlNbr == ptraParentCore.CtrlNbr.Value);
            await groupRepo.AddAsync(DynamicGroup.Create(ptraRRType.CtrlNbr.Value, "Port Terminal Railroad Association", parentGroupCtrlNbr: null, path: null, isWorkArea: true, code: "PTRA", parentCtrlNbr: ptraParentCore.CtrlNbr.Value));
        }

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
                isWorkArea: false,
                parentCtrlNbr: csxParentCore.CtrlNbr.Value,
                railroadCtrlNbr: csxRailroadCore.CtrlNbr.Value);
            await groupRepo.AddAsync(jaxSubCore);
        }

        // ?? Employees with Addresses, Phone Numbers, Email Addresses ?????
        var employeeRepo = sp.GetRequiredService<IEmployeeRepository>();
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

        // Reference data
        var activeStatus = EmploymentStatus.Create(csxParent.CtrlNbr.Value, "A", "Active", 1, "FT");
        await employmentStatusRepo.AddAsync(activeStatus);

        var homeAddressType = AddressType.Create(csxParent.CtrlNbr.Value, "Home", 1, emergencyType: false);
        await addressTypeRepo.AddAsync(homeAddressType);

        var cellPhoneType = PhoneNumberType.Create(csxParent.CtrlNbr.Value, "Cell", 1, emergencyType: false);
        await phoneNumberTypeRepo.AddAsync(cellPhoneType);

        var workEmailType = EmailAddressType.Create(csxParent.CtrlNbr.Value, "Work", 1, emergencyType: false);
        await emailAddressTypeRepo.AddAsync(workEmailType);

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

            // Auto-accept invitation flow: create invitation, accept it, create user + assignment
            var invitation = Invitation.Create(
                email,
                csxParent.CtrlNbr.Value,
                Roles.Employee,
                "SYSTEM",
                railroadCtrlNbr: csxRailroadCore.CtrlNbr);
            invitation.Accept();
            await invitationRepo.AddAsync(invitation);

            var user = new User
            {
                UserName       = email,
                Email          = email,
                EmailConfirmed = true,
                FirstName      = firstName,
                LastName       = lastName,
                FullName       = $"{firstName} {lastName}",
                FullNameLNF    = $"{lastName}, {firstName}",
                EmployeeNumber = empNumber
            };
            await userMgr.CreateAsync(user, "Seed@123");

            var assignment = UserParentAssignment.Create(user.Id, csxParent.CtrlNbr.Value, invitation.Role, csxRailroadCore.CtrlNbr);
            await assignmentRepo.AddAsync(assignment);

            var employee = Employee.Create(
                csxParent.CtrlNbr.Value,
                userId: user.Id,
                employeeNumber: empNumber,
                ssn: $"{100 + i:D3}-{50 + i % 100:D2}-{1000 + i:D4}",
                gender: genders[i % genders.Length],
                race: races[i % races.Length],
                birthDate: new DateTime(1965, 1, 1).AddDays(i * 73),
                employmentDate: new DateTime(2015, 1, 1).AddDays(i * 12),
                activeStatus.CtrlNbr.Value);

            employee.AddAddress(
                $"{100 + i} {streets[i % streets.Length]}",
                cities[i % cities.Length],
                states[i % states.Length],
                zips[i % zips.Length],
                homeAddressType.CtrlNbr.Value);

            employee.AddPhoneNumber(
                $"555-{100 + i:D3}-{1000 + i:D4}",
                callingOrder: 1,
                dialOne: true,
                cellPhoneType.CtrlNbr.Value);

            employee.AddEmailAddress(
                email,
                workEmailType.CtrlNbr.Value);

            await employeeRepo.AddAsync(employee);
        }

        } // end employee guard

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
        var existingDepts = await departmentRepo.GetAllAsync();
        if (existingDepts.Count == 0)
        {
        var csxRR = (await groupRepo.GetByGroupTypeNameAsync("Railroad")).First(g => g.Code == "CSX");
        var ptraRR = (await groupRepo.GetByGroupTypeNameAsync("Railroad", ptraParentCore.CtrlNbr.Value)).First(g => g.Code == "PTRA");
        var csxTransportation = Department.Create(csxParentCtrlNbr, csxRR.CtrlNbr, "Transportation");
        var csxClerical = Department.Create(csxParentCtrlNbr, csxRR.CtrlNbr, "Clerical");
        var ptraTransportation = Department.Create(ptraParentCore.CtrlNbr.Value, ptraRR.CtrlNbr, "Transportation");
        await departmentRepo.AddAsync(csxTransportation);
        await departmentRepo.AddAsync(csxClerical);
        var ptraClerical = Department.Create(ptraParentCore.CtrlNbr.Value, ptraRR.CtrlNbr, "Clerical");
        await departmentRepo.AddAsync(ptraTransportation);
        await departmentRepo.AddAsync(ptraClerical);
        } // end departments guard

        // ?? Section 4: Seniority � Crafts, Rosters, Rankings ?????????????
        var craftRepo = sp.GetRequiredService<ICraftRepository>();
        var rosterRepo = sp.GetRequiredService<IRosterRepository>();
        var seniorityRepo = sp.GetRequiredService<ISeniorityRepository>();

        var existingCrafts = await craftRepo.GetAllAsync();
        if (existingCrafts.Count == 0)
        {
        // Crafts at railroad level with parent ownership
        var csxRailroadForCraft = (await groupRepo.GetByGroupTypeNameAsync("Railroad")).First(g => g.Code == "CSX");
        var ptraRailroadForCraft = (await groupRepo.GetByGroupTypeNameAsync("Railroad", ptraParentCore.CtrlNbr.Value)).First(g => g.Code == "PTRA");
        var allDepts = await departmentRepo.GetAllAsync();
        var csxTransDept = allDepts.First(d => d.Name == "Transportation" && d.ParentCtrlNbr == csxParentCtrlNbr);
        var csxClericalDept = allDepts.First(d => d.Name == "Clerical" && d.ParentCtrlNbr == csxParentCtrlNbr);
        var ptraTransDept = allDepts.First(d => d.Name == "Transportation" && d.ParentCtrlNbr == ptraParentCore.CtrlNbr);
        var ptraClericalDept = allDepts.First(d => d.Name == "Clerical" && d.ParentCtrlNbr == ptraParentCore.CtrlNbr);

        // CSX Crafts (owned by CSX railroad under CSX Corporation parent)
        var csxEngineer = Craft.Create(csxParentCtrlNbr, csxRailroadForCraft.CtrlNbr, "Engineer", "Engineers", 1,
            autoMarkUp: false, approveAllMarkOffs: false, markOffHours: 10, markUpHours: 10,
            requiredRestHours: 10, maximumVacationDayTime: 480, unpaidMealPeriodMinutes: 0,
            hoursofService: true, processPayroll: true, showNotifications: true, vacationAssignmentType: 1, departmentCtrlNbr: csxTransDept.CtrlNbr);
        await craftRepo.AddAsync(csxEngineer);

        var csxConductor = Craft.Create(csxParentCtrlNbr, csxRailroadForCraft.CtrlNbr, "Conductor", "Conductors", 2,
            autoMarkUp: false, approveAllMarkOffs: false, markOffHours: 10, markUpHours: 10,
            requiredRestHours: 10, maximumVacationDayTime: 480, unpaidMealPeriodMinutes: 0,
            hoursofService: true, processPayroll: true, showNotifications: true, vacationAssignmentType: 1, departmentCtrlNbr: csxTransDept.CtrlNbr);
        await craftRepo.AddAsync(csxConductor);

        var csxClerical = Craft.Create(csxParentCtrlNbr, csxRailroadForCraft.CtrlNbr, "Clerical", "Clerical", 3,
            autoMarkUp: true, approveAllMarkOffs: true, markOffHours: 0, markUpHours: 0,
            requiredRestHours: 0, maximumVacationDayTime: 480, unpaidMealPeriodMinutes: 30,
            hoursofService: false, processPayroll: true, showNotifications: true, vacationAssignmentType: 0, departmentCtrlNbr: csxClericalDept.CtrlNbr);
        await craftRepo.AddAsync(csxClerical);

        // PTRA Crafts (owned by PTRA railroad under PTRA parent)
        var ptraEngineer = Craft.Create(ptraParentCore.CtrlNbr.Value, ptraRailroadForCraft.CtrlNbr, "Engineer", "Engineers", 1,
            autoMarkUp: false, approveAllMarkOffs: false, markOffHours: 10, markUpHours: 10,
            requiredRestHours: 10, maximumVacationDayTime: 480, unpaidMealPeriodMinutes: 0,
            hoursofService: true, processPayroll: true, showNotifications: true, vacationAssignmentType: 1, departmentCtrlNbr: ptraTransDept.CtrlNbr);
        await craftRepo.AddAsync(ptraEngineer);

        var ptraConductor = Craft.Create(ptraParentCore.CtrlNbr.Value, ptraRailroadForCraft.CtrlNbr, "Conductor", "Conductors", 2,
            autoMarkUp: false, approveAllMarkOffs: false, markOffHours: 10, markUpHours: 10,
            requiredRestHours: 10, maximumVacationDayTime: 480, unpaidMealPeriodMinutes: 0,
            hoursofService: true, processPayroll: true, showNotifications: true, vacationAssignmentType: 1, departmentCtrlNbr: ptraTransDept.CtrlNbr);
        await craftRepo.AddAsync(ptraConductor);

        var ptraClerical = Craft.Create(ptraParentCore.CtrlNbr.Value, ptraRailroadForCraft.CtrlNbr, "Clerical", "Clerical", 3,
            autoMarkUp: true, approveAllMarkOffs: true, markOffHours: 0, markUpHours: 0,
            requiredRestHours: 0, maximumVacationDayTime: 480, unpaidMealPeriodMinutes: 30,
            hoursofService: false, processPayroll: true, showNotifications: true, vacationAssignmentType: 0, departmentCtrlNbr: ptraClericalDept.CtrlNbr);
        await craftRepo.AddAsync(ptraClerical);

        // Rosters -- one per craft per railroad
        var csxRailroadsForRoster = await groupRepo.GetByGroupTypeNameAsync("Railroad");
        var csxRailroad = csxRailroadsForRoster.First(rr => rr.Code == "CSX");

        var csxEngRoster = Roster.Create(csxEngineer.CtrlNbr, csxRailroad.CtrlNbr, "Engineer Roster", "Engineer Rosters", 1,
            training: false, extraBoard: false, overtimeBoard: false);
        await rosterRepo.AddAsync(csxEngRoster);

        var csxCondRoster = Roster.Create(csxConductor.CtrlNbr, csxRailroad.CtrlNbr, "Conductor Roster", "Conductor Rosters", 2,
            training: false, extraBoard: false, overtimeBoard: false);
        await rosterRepo.AddAsync(csxCondRoster);

        var csxClericalRoster = Roster.Create(csxClerical.CtrlNbr, csxRailroad.CtrlNbr, "Clerical Roster", "Clerical Rosters", 3,
            training: false, extraBoard: false, overtimeBoard: false);
        await rosterRepo.AddAsync(csxClericalRoster);

        // Seniority entries � split 100 employees: 40 Engineer, 50 Conductor, 10 Clerical
        var empList = await employeeRepo.GetAllAsync();
        var rosterDate = new DateTime(2015, 1, 1);

        for (int i = 0; i < empList.Count; i++)
        {
            Roster targetRoster;
            if (i < 40) targetRoster = csxEngRoster;
            else if (i < 90) targetRoster = csxCondRoster;
            else targetRoster = csxClericalRoster;

            var seniority = Seniority.Create(
                targetRoster.CtrlNbr, empList[i].CtrlNbr,
                lastActiveRoster: true,
                rosterDate: rosterDate.AddDays(i * 12),
                rank: i + 1,
                stateID: 1,
                canTrain: i % 5 == 0);
            await seniorityRepo.AddAsync(seniority);
        }

        } // end seniority guard

        // ?? Section 5: Work Management � Roles, Templates, Instances, Slots ??
        var craftRoleRepo = sp.GetRequiredService<ICraftRoleRepository>();
        var workInstanceRepo = sp.GetRequiredService<IWorkInstanceRepository>();
        var positionSlotRepo = sp.GetRequiredService<IPositionSlotRepository>();

        var existingRoles = await craftRoleRepo.GetAllAsync();
        if (existingRoles.Count == 0)
        {
        // Re-lookup crafts and work areas for FK references
        var crafts = await craftRepo.GetAllAsync();
        var csxRailroadWM = (await groupRepo.GetByGroupTypeNameAsync("Railroad")).First(g => g.Code == "CSX");
        var jaxSubWM = (await groupRepo.GetAllAsync()).First(g => g.Name == "Jacksonville Sub");
        var engCraft = crafts.First(c => c.CraftName == "Engineer" && c.DynamicGroupCtrlNbr == csxRailroadWM.CtrlNbr);
        var condCraft = crafts.First(c => c.CraftName == "Conductor" && c.DynamicGroupCtrlNbr == csxRailroadWM.CtrlNbr);
        var clerCraft = crafts.First(c => c.CraftName == "Clerical" && c.DynamicGroupCtrlNbr == csxRailroadWM.CtrlNbr);

        // Craft Roles � Conductor craft
        var studentTrainman = CraftRole.Create(condCraft.CtrlNbr, "STRN", "Student Trainman");
        var trainman = CraftRole.Create(condCraft.CtrlNbr, "TRMN", "Trainman");
        var conductor = CraftRole.Create(condCraft.CtrlNbr, "COND", "Conductor");
        await craftRoleRepo.AddAsync(studentTrainman);
        await craftRoleRepo.AddAsync(trainman);
        await craftRoleRepo.AddAsync(conductor);

        // Craft Roles � Engineer craft
        var studentEngineer = CraftRole.Create(engCraft.CtrlNbr, "SENG", "Student Engineer");
        var engineer = CraftRole.Create(engCraft.CtrlNbr, "ENGR", "Engineer");
        await craftRoleRepo.AddAsync(studentEngineer);
        await craftRoleRepo.AddAsync(engineer);

        // Craft Roles � Clerical craft
        var crewDispatcher = CraftRole.Create(clerCraft.CtrlNbr, "DISP", "Crew Dispatcher");
        await craftRoleRepo.AddAsync(crewDispatcher);

        // Craft Roles - PTRA Engineer craft
        var ptraRailroadWM = (await groupRepo.GetByGroupTypeNameAsync("Railroad", ptraParentCore.CtrlNbr.Value)).First(g => g.Code == "PTRA");
        var ptraEngCraft = crafts.First(c => c.CraftName == "Engineer" && c.DynamicGroupCtrlNbr == ptraRailroadWM.CtrlNbr);
        var ptraCondCraft = crafts.First(c => c.CraftName == "Conductor" && c.DynamicGroupCtrlNbr == ptraRailroadWM.CtrlNbr);
        var ptraEngineerRole = CraftRole.Create(ptraEngCraft.CtrlNbr, "E", "Engineer");
        await craftRoleRepo.AddAsync(ptraEngineerRole);

        // Craft Roles - PTRA Conductor craft
        var ptraForeman = CraftRole.Create(ptraCondCraft.CtrlNbr, "F", "Foreman");
        var ptraHelper = CraftRole.Create(ptraCondCraft.CtrlNbr, "H", "Helper");
        await craftRoleRepo.AddAsync(ptraForeman);
        await craftRoleRepo.AddAsync(ptraHelper);


        // Work Instances � 2 per template (today + tomorrow)
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

        // Position Slots � each work instance gets a Conductor + Engineer slot
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
        slots[0].Bind(empList[40].CtrlNbr, "DISPATCH");  // Job 101 today � Conductor
        slots[1].Bind(empList[0].CtrlNbr, "DISPATCH");    // Job 101 today � Engineer
        slots[4].Bind(empList[41].CtrlNbr, "DISPATCH");   // Job 202 today � Conductor
        slots[5].Bind(empList[1].CtrlNbr, "DISPATCH");    // Job 202 today � Engineer
        await positionSlotRepo.UpdateAsync(slots[0]);
        await positionSlotRepo.UpdateAsync(slots[1]);
        await positionSlotRepo.UpdateAsync(slots[4]);
        await positionSlotRepo.UpdateAsync(slots[5]);

        } // end work management guard

        // ?? Section 6: Crews � Crews, Positions, Incumbencies, Attachments ???
        var crewRepo = sp.GetRequiredService<ICrewRepository>();
        var crewPositionRepo = sp.GetRequiredService<ICrewPositionRepository>();
        var incumbencyRepo = sp.GetRequiredService<ICrewIncumbencyRepository>();
        var crewAssignmentRepo = sp.GetRequiredService<ICrewAssignmentRepository>();
        var assignmentRepo2 = sp.GetRequiredService<IAssignmentRepository>();
        var assignmentScheduleRepo = sp.GetRequiredService<IAssignmentScheduleRepository>();
        var uowFactory = sp.GetRequiredService<IOrchestrationUnitOfWorkFactory>();

        var existingCrews = await crewRepo.GetAllAsync();
        if (existingCrews.Count == 0)
        {
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
        var crewA = Crew.Create("REGULAR", jaxSub2.CtrlNbr, "Jax Turn Crew A", departmentCtrlNbr: crewTransDept?.CtrlNbr);
        var crewB = Crew.Create("REGULAR", jaxSub2.CtrlNbr, "Jax Turn Crew B", departmentCtrlNbr: crewTransDept?.CtrlNbr);
        var extraCrew = Crew.Create("EXTRA", jaxSub2.CtrlNbr, "Jax Extra Board Crew", departmentCtrlNbr: crewTransDept?.CtrlNbr);

        await using (var uow = await uowFactory.CreateAsync())
        {
            uow.Crews.Add(crewA);
            uow.Crews.Add(crewB);
            uow.Crews.Add(extraCrew);
            await uow.CommitAsync();
        }

        // Staffable Positions — one per crew slot (PositionType = "Crew")
        var sp1 = StaffablePosition.Create("Crew");
        var sp2 = StaffablePosition.Create("Crew");
        var sp3 = StaffablePosition.Create("Crew");
        var sp4 = StaffablePosition.Create("Crew");
        var sp5 = StaffablePosition.Create("Crew");
        var sp6 = StaffablePosition.Create("Crew");

        await using (var uow = await uowFactory.CreateAsync())
        {
            uow.StaffablePositions.Add(sp1);
            uow.StaffablePositions.Add(sp2);
            uow.StaffablePositions.Add(sp3);
            uow.StaffablePositions.Add(sp4);
            uow.StaffablePositions.Add(sp5);
            uow.StaffablePositions.Add(sp6);
            await uow.CommitAsync();
        }

        // Crew Positions — 2 per crew (Conductor + Engineer)
        var crewAPos1 = CrewPosition.Create(crewA.CtrlNbr, condRole.CtrlNbr, 1, sp1.CtrlNbr);
        var crewAPos2 = CrewPosition.Create(crewA.CtrlNbr, engRole.CtrlNbr, 2, sp2.CtrlNbr);
        var crewBPos1 = CrewPosition.Create(crewB.CtrlNbr, condRole.CtrlNbr, 1, sp3.CtrlNbr);
        var crewBPos2 = CrewPosition.Create(crewB.CtrlNbr, engRole.CtrlNbr, 2, sp4.CtrlNbr);
        var extraPos1 = CrewPosition.Create(extraCrew.CtrlNbr, condRole.CtrlNbr, 1, sp5.CtrlNbr);
        var extraPos2 = CrewPosition.Create(extraCrew.CtrlNbr, engRole.CtrlNbr, 2, sp6.CtrlNbr);

        await using (var uow = await uowFactory.CreateAsync())
        {
            uow.CrewPositions.Add(crewAPos1);
            uow.CrewPositions.Add(crewAPos2);
            uow.CrewPositions.Add(crewBPos1);
            uow.CrewPositions.Add(crewBPos2);
            uow.CrewPositions.Add(extraPos1);
            uow.CrewPositions.Add(extraPos2);
            await uow.CommitAsync();
        }

        // Incumbencies � assign employees to crew positions
        var now = DateTime.UtcNow;
        await using (var uow = await uowFactory.CreateAsync())
        {
            uow.CrewIncumbencies.Add(CrewIncumbency.Create(crewAPos1.CtrlNbr, empList2[40].CtrlNbr, now));
            uow.CrewIncumbencies.Add(CrewIncumbency.Create(crewAPos2.CtrlNbr, empList2[0].CtrlNbr, now));
            uow.CrewIncumbencies.Add(CrewIncumbency.Create(crewBPos1.CtrlNbr, empList2[41].CtrlNbr, now));
            uow.CrewIncumbencies.Add(CrewIncumbency.Create(crewBPos2.CtrlNbr, empList2[1].CtrlNbr, now));
            uow.CrewIncumbencies.Add(CrewIncumbency.Create(extraPos1.CtrlNbr, empList2[42].CtrlNbr, now));
            uow.CrewIncumbencies.Add(CrewIncumbency.Create(extraPos2.CtrlNbr, empList2[2].CtrlNbr, now));
            await uow.CommitAsync();
        }

        // Shift Definitions for Jacksonville Sub
        var shiftDefRepo = sp.GetRequiredService<IShiftDefinitionRepository>();
        var existingShifts = await shiftDefRepo.GetByWorkAreaAsync(jaxSub2.CtrlNbr);
        ShiftDefinition shiftFirst, shiftSecond, shiftThird;
        if (existingShifts.Count == 0)
        {
            shiftFirst = ShiftDefinition.Create(jaxSub2.CtrlNbr, "1ST", "First Shift", 1, true);
            shiftSecond = ShiftDefinition.Create(jaxSub2.CtrlNbr, "2ND", "Second Shift", 2, true);
            shiftThird = ShiftDefinition.Create(jaxSub2.CtrlNbr, "3RD", "Third Shift", 3, true);
            await using (var uow = await uowFactory.CreateAsync())
            {
                uow.ShiftDefinitions.Add(shiftFirst);
                uow.ShiftDefinitions.Add(shiftSecond);
                uow.ShiftDefinitions.Add(shiftThird);
                await uow.CommitAsync();
            }
        }
        else
        {
            shiftFirst = existingShifts.First(s => s.ShiftCode == "1ST");
            shiftSecond = existingShifts.First(s => s.ShiftCode == "2ND");
            shiftThird = existingShifts.First(s => s.ShiftCode == "3RD");
        }

        // Shift Definitions for PTRA
        var ptraRRForShifts = (await groupRepo.GetByGroupTypeNameAsync("Railroad", ptraParentCore.CtrlNbr.Value)).First(g => g.Code == "PTRA");
        var existingPtraShifts = await shiftDefRepo.GetByWorkAreaAsync(ptraRRForShifts.CtrlNbr);
        ShiftDefinition ptraShift1, ptraShift2, ptraShift3;
        if (existingPtraShifts.Count == 0)
        {
            ptraShift1 = ShiftDefinition.Create(ptraRRForShifts.CtrlNbr, "1", "First Shift", 1, true);
            ptraShift2 = ShiftDefinition.Create(ptraRRForShifts.CtrlNbr, "2", "Second Shift", 2, true);
            ptraShift3 = ShiftDefinition.Create(ptraRRForShifts.CtrlNbr, "3", "Third Shift", 3, true);
            await using (var uow = await uowFactory.CreateAsync())
            {
                uow.ShiftDefinitions.Add(ptraShift1);
                uow.ShiftDefinitions.Add(ptraShift2);
                uow.ShiftDefinitions.Add(ptraShift3);
                await uow.CommitAsync();
            }
        }
        else
        {
            ptraShift1 = existingPtraShifts.First(s => s.ShiftCode == "1");
            ptraShift2 = existingPtraShifts.First(s => s.ShiftCode == "2");
            ptraShift3 = existingPtraShifts.First(s => s.ShiftCode == "3");
        }

        // Assignments
        var asgn1 = Assignment.Create(jaxSub2.CtrlNbr, "JAX-101", "Jax Turn 101", departmentCtrlNbr: crewTransDept?.CtrlNbr);
        var asgn2 = Assignment.Create(jaxSub2.CtrlNbr, "JAX-102", "Jax Turn 102", departmentCtrlNbr: crewTransDept?.CtrlNbr);
        var asgnExtra = Assignment.Create(jaxSub2.CtrlNbr, "JAX-XB", "Jax Extra Board", isExtra: true, departmentCtrlNbr: crewTransDept?.CtrlNbr);

        await using (var uow = await uowFactory.CreateAsync())
        {
            uow.Assignments.Add(asgn1);
            uow.Assignments.Add(asgn2);
            uow.Assignments.Add(asgnExtra);
            await uow.CommitAsync();
        }

        // Assignment Schedules — weekday bitmask: Mon-Fri = 0b0111110 = 62
        const int weekdays = 0b0111110;
        await using (var uow = await uowFactory.CreateAsync())
        {
            uow.AssignmentSchedules.Add(AssignmentSchedule.Create(asgn1.CtrlNbr, shiftFirst.CtrlNbr, weekdays, new TimeOnly(6, 0), new TimeOnly(14, 0)));
            uow.AssignmentSchedules.Add(AssignmentSchedule.Create(asgn2.CtrlNbr, shiftSecond.CtrlNbr, weekdays, new TimeOnly(14, 0), new TimeOnly(22, 0)));
            uow.AssignmentSchedules.Add(AssignmentSchedule.Create(asgnExtra.CtrlNbr, shiftFirst.CtrlNbr, weekdays, new TimeOnly(6, 0), new TimeOnly(14, 0)));
            uow.CrewAssignments.Add(CrewAssignment.Create(crewA.CtrlNbr, asgn1.CtrlNbr, weekdays, now));
            uow.CrewAssignments.Add(CrewAssignment.Create(crewB.CtrlNbr, asgn2.CtrlNbr, weekdays, now));
            uow.CrewAssignments.Add(CrewAssignment.Create(extraCrew.CtrlNbr, asgnExtra.CtrlNbr, weekdays, now));
            await uow.CommitAsync();
        }

        // ── PTRA Assignments ────────────────────────────────────────────
        var ptraLocNOYD = allGroups2.First(g => g.Code == "NOYD");
        var ptraLocMCYD = allGroups2.First(g => g.Code == "MCYD");
        var ptraLocPSYD = allGroups2.First(g => g.Code == "PSYD");
        var ptraEngRole = allRoles.First(r => r.Code == "E");
        var ptraFmnRole = allRoles.First(r => r.Code == "F");
        var ptraHlpRole = allRoles.First(r => r.Code == "H");
        var ptraTransDeptCrew = crewDepts.FirstOrDefault(d => d.Name == "Transportation" && d.DynamicGroupCtrlNbr == ptraRRForShifts.CtrlNbr);

        // 9 assignments — 3 per shift, one per location (PSYD, MCYD, NOYD)
        var ptraAsgn130 = Assignment.Create(ptraLocPSYD.CtrlNbr, "130", "Assignment 130", departmentCtrlNbr: ptraTransDeptCrew?.CtrlNbr);
        var ptraAsgn140 = Assignment.Create(ptraLocMCYD.CtrlNbr, "140", "Assignment 140", departmentCtrlNbr: ptraTransDeptCrew?.CtrlNbr);
        var ptraAsgn150 = Assignment.Create(ptraLocNOYD.CtrlNbr, "150", "Assignment 150", departmentCtrlNbr: ptraTransDeptCrew?.CtrlNbr);
        var ptraAsgn230 = Assignment.Create(ptraLocPSYD.CtrlNbr, "230", "Assignment 230", departmentCtrlNbr: ptraTransDeptCrew?.CtrlNbr);
        var ptraAsgn240 = Assignment.Create(ptraLocMCYD.CtrlNbr, "240", "Assignment 240", departmentCtrlNbr: ptraTransDeptCrew?.CtrlNbr);
        var ptraAsgn250 = Assignment.Create(ptraLocNOYD.CtrlNbr, "250", "Assignment 250", departmentCtrlNbr: ptraTransDeptCrew?.CtrlNbr);
        var ptraAsgn330 = Assignment.Create(ptraLocPSYD.CtrlNbr, "330", "Assignment 330", departmentCtrlNbr: ptraTransDeptCrew?.CtrlNbr);
        var ptraAsgn340 = Assignment.Create(ptraLocMCYD.CtrlNbr, "340", "Assignment 340", departmentCtrlNbr: ptraTransDeptCrew?.CtrlNbr);
        var ptraAsgn350 = Assignment.Create(ptraLocNOYD.CtrlNbr, "350", "Assignment 350", departmentCtrlNbr: ptraTransDeptCrew?.CtrlNbr);

        await using (var uow = await uowFactory.CreateAsync())
        {
            uow.Assignments.Add(ptraAsgn130);
            uow.Assignments.Add(ptraAsgn140);
            uow.Assignments.Add(ptraAsgn150);
            uow.Assignments.Add(ptraAsgn230);
            uow.Assignments.Add(ptraAsgn240);
            uow.Assignments.Add(ptraAsgn250);
            uow.Assignments.Add(ptraAsgn330);
            uow.Assignments.Add(ptraAsgn340);
            uow.Assignments.Add(ptraAsgn350);
            await uow.CommitAsync();
        }

        // ── PTRA Crews — 9 regular + 3 relief ───────────────────────────
        var ptraCrew130 = Crew.Create("REGULAR", ptraRRForShifts.CtrlNbr, "130", departmentCtrlNbr: ptraTransDeptCrew?.CtrlNbr);
        var ptraCrew140 = Crew.Create("REGULAR", ptraRRForShifts.CtrlNbr, "140", departmentCtrlNbr: ptraTransDeptCrew?.CtrlNbr);
        var ptraCrew150 = Crew.Create("REGULAR", ptraRRForShifts.CtrlNbr, "150", departmentCtrlNbr: ptraTransDeptCrew?.CtrlNbr);
        var ptraCrew230 = Crew.Create("REGULAR", ptraRRForShifts.CtrlNbr, "230", departmentCtrlNbr: ptraTransDeptCrew?.CtrlNbr);
        var ptraCrew240 = Crew.Create("REGULAR", ptraRRForShifts.CtrlNbr, "240", departmentCtrlNbr: ptraTransDeptCrew?.CtrlNbr);
        var ptraCrew250 = Crew.Create("REGULAR", ptraRRForShifts.CtrlNbr, "250", departmentCtrlNbr: ptraTransDeptCrew?.CtrlNbr);
        var ptraCrew330 = Crew.Create("REGULAR", ptraRRForShifts.CtrlNbr, "330", departmentCtrlNbr: ptraTransDeptCrew?.CtrlNbr);
        var ptraCrew340 = Crew.Create("REGULAR", ptraRRForShifts.CtrlNbr, "340", departmentCtrlNbr: ptraTransDeptCrew?.CtrlNbr);
        var ptraCrew350 = Crew.Create("REGULAR", ptraRRForShifts.CtrlNbr, "350", departmentCtrlNbr: ptraTransDeptCrew?.CtrlNbr);
        var ptraCrewRlfA = Crew.Create("RELIEF", ptraRRForShifts.CtrlNbr, "RLF-A", departmentCtrlNbr: ptraTransDeptCrew?.CtrlNbr);
        var ptraCrewRlfB = Crew.Create("RELIEF", ptraRRForShifts.CtrlNbr, "RLF-B", departmentCtrlNbr: ptraTransDeptCrew?.CtrlNbr);
        var ptraCrewRlfC = Crew.Create("RELIEF", ptraRRForShifts.CtrlNbr, "RLF-C", departmentCtrlNbr: ptraTransDeptCrew?.CtrlNbr);

        await using (var uow = await uowFactory.CreateAsync())
        {
            uow.Crews.Add(ptraCrew130);
            uow.Crews.Add(ptraCrew140);
            uow.Crews.Add(ptraCrew150);
            uow.Crews.Add(ptraCrew230);
            uow.Crews.Add(ptraCrew240);
            uow.Crews.Add(ptraCrew250);
            uow.Crews.Add(ptraCrew330);
            uow.Crews.Add(ptraCrew340);
            uow.Crews.Add(ptraCrew350);
            uow.Crews.Add(ptraCrewRlfA);
            uow.Crews.Add(ptraCrewRlfB);
            uow.Crews.Add(ptraCrewRlfC);
            await uow.CommitAsync();
        }

        // ── PTRA StaffablePositions ─────────────────────────────────────
        // 3-position crews (E, F, H): 130, 150, 230, 250, 330, 350
        // 2-position crews (E, F): 140, 240, 340, RLF-A, RLF-B, RLF-C
        var ptra3PosCrews = new[] { ptraCrew130, ptraCrew150, ptraCrew230, ptraCrew250, ptraCrew330, ptraCrew350 };
        var ptra2PosCrews = new[] { ptraCrew140, ptraCrew240, ptraCrew340, ptraCrewRlfA, ptraCrewRlfB, ptraCrewRlfC };
        var ptraSPs = new List<StaffablePosition>();
        for (int i = 0; i < (ptra3PosCrews.Length * 3) + (ptra2PosCrews.Length * 2); i++)
            ptraSPs.Add(StaffablePosition.Create("Crew"));

        await using (var uow = await uowFactory.CreateAsync())
        {
            foreach (var ptraSP in ptraSPs)
                uow.StaffablePositions.Add(ptraSP);
            await uow.CommitAsync();
        }

        // ── PTRA CrewPositions ───────────────────────────────────────────
        int ptraSpIdx = 0;
        await using (var uow = await uowFactory.CreateAsync())
        {
            foreach (var crew in ptra3PosCrews)
            {
                uow.CrewPositions.Add(CrewPosition.Create(crew.CtrlNbr, ptraEngRole.CtrlNbr, 1, ptraSPs[ptraSpIdx++].CtrlNbr));
                uow.CrewPositions.Add(CrewPosition.Create(crew.CtrlNbr, ptraFmnRole.CtrlNbr, 2, ptraSPs[ptraSpIdx++].CtrlNbr));
                uow.CrewPositions.Add(CrewPosition.Create(crew.CtrlNbr, ptraHlpRole.CtrlNbr, 3, ptraSPs[ptraSpIdx++].CtrlNbr));
            }
            foreach (var crew in ptra2PosCrews)
            {
                uow.CrewPositions.Add(CrewPosition.Create(crew.CtrlNbr, ptraEngRole.CtrlNbr, 1, ptraSPs[ptraSpIdx++].CtrlNbr));
                uow.CrewPositions.Add(CrewPosition.Create(crew.CtrlNbr, ptraFmnRole.CtrlNbr, 2, ptraSPs[ptraSpIdx++].CtrlNbr));
            }
            await uow.CommitAsync();
        }

        // ── PTRA AssignmentSchedules + CrewAssignments ───────────────────
        var ptraStart = new DateTime(2026, 1, 1);
        await using (var uow = await uowFactory.CreateAsync())
        {
            // Schedules: mask 62 = weekdays (Mon–Fri), 63 = 6-day (Sun–Fri), 127 = every day
            uow.AssignmentSchedules.Add(AssignmentSchedule.Create(ptraAsgn130.CtrlNbr, ptraShift1.CtrlNbr, 63, new TimeOnly(7, 0), new TimeOnly(15, 0)));
            uow.AssignmentSchedules.Add(AssignmentSchedule.Create(ptraAsgn140.CtrlNbr, ptraShift1.CtrlNbr, 127, new TimeOnly(7, 0), new TimeOnly(15, 0)));
            uow.AssignmentSchedules.Add(AssignmentSchedule.Create(ptraAsgn150.CtrlNbr, ptraShift1.CtrlNbr, 127, new TimeOnly(7, 0), new TimeOnly(15, 0)));
            uow.AssignmentSchedules.Add(AssignmentSchedule.Create(ptraAsgn230.CtrlNbr, ptraShift2.CtrlNbr, 63, new TimeOnly(15, 0), new TimeOnly(23, 0)));
            uow.AssignmentSchedules.Add(AssignmentSchedule.Create(ptraAsgn240.CtrlNbr, ptraShift2.CtrlNbr, 127, new TimeOnly(15, 0), new TimeOnly(23, 0)));
            uow.AssignmentSchedules.Add(AssignmentSchedule.Create(ptraAsgn250.CtrlNbr, ptraShift2.CtrlNbr, 127, new TimeOnly(15, 0), new TimeOnly(23, 0)));
            uow.AssignmentSchedules.Add(AssignmentSchedule.Create(ptraAsgn330.CtrlNbr, ptraShift3.CtrlNbr, 63, new TimeOnly(23, 0), new TimeOnly(7, 0)));
            uow.AssignmentSchedules.Add(AssignmentSchedule.Create(ptraAsgn340.CtrlNbr, ptraShift3.CtrlNbr, 127, new TimeOnly(23, 0), new TimeOnly(7, 0)));
            uow.AssignmentSchedules.Add(AssignmentSchedule.Create(ptraAsgn350.CtrlNbr, ptraShift3.CtrlNbr, 127, new TimeOnly(23, 0), new TimeOnly(7, 0)));

            // Regular crew → assignment links
            uow.CrewAssignments.Add(CrewAssignment.Create(ptraCrew130.CtrlNbr, ptraAsgn130.CtrlNbr, 62, ptraStart));
            uow.CrewAssignments.Add(CrewAssignment.Create(ptraCrew140.CtrlNbr, ptraAsgn140.CtrlNbr, 121, ptraStart));
            uow.CrewAssignments.Add(CrewAssignment.Create(ptraCrew150.CtrlNbr, ptraAsgn150.CtrlNbr, 103, ptraStart));
            uow.CrewAssignments.Add(CrewAssignment.Create(ptraCrew230.CtrlNbr, ptraAsgn230.CtrlNbr, 62, ptraStart));
            uow.CrewAssignments.Add(CrewAssignment.Create(ptraCrew240.CtrlNbr, ptraAsgn240.CtrlNbr, 121, ptraStart));
            uow.CrewAssignments.Add(CrewAssignment.Create(ptraCrew250.CtrlNbr, ptraAsgn250.CtrlNbr, 103, ptraStart));
            uow.CrewAssignments.Add(CrewAssignment.Create(ptraCrew330.CtrlNbr, ptraAsgn330.CtrlNbr, 62, ptraStart));
            uow.CrewAssignments.Add(CrewAssignment.Create(ptraCrew340.CtrlNbr, ptraAsgn340.CtrlNbr, 121, ptraStart));
            uow.CrewAssignments.Add(CrewAssignment.Create(ptraCrew350.CtrlNbr, ptraAsgn350.CtrlNbr, 103, ptraStart));

            // Relief crew → assignment links (cover remaining days per shift)
            uow.CrewAssignments.Add(CrewAssignment.Create(ptraCrewRlfA.CtrlNbr, ptraAsgn130.CtrlNbr, 1, ptraStart));
            uow.CrewAssignments.Add(CrewAssignment.Create(ptraCrewRlfA.CtrlNbr, ptraAsgn140.CtrlNbr, 6, ptraStart));
            uow.CrewAssignments.Add(CrewAssignment.Create(ptraCrewRlfA.CtrlNbr, ptraAsgn150.CtrlNbr, 24, ptraStart));
            uow.CrewAssignments.Add(CrewAssignment.Create(ptraCrewRlfB.CtrlNbr, ptraAsgn230.CtrlNbr, 1, ptraStart));
            uow.CrewAssignments.Add(CrewAssignment.Create(ptraCrewRlfB.CtrlNbr, ptraAsgn240.CtrlNbr, 6, ptraStart));
            uow.CrewAssignments.Add(CrewAssignment.Create(ptraCrewRlfB.CtrlNbr, ptraAsgn250.CtrlNbr, 24, ptraStart));
            uow.CrewAssignments.Add(CrewAssignment.Create(ptraCrewRlfC.CtrlNbr, ptraAsgn330.CtrlNbr, 1, ptraStart));
            uow.CrewAssignments.Add(CrewAssignment.Create(ptraCrewRlfC.CtrlNbr, ptraAsgn340.CtrlNbr, 6, ptraStart));
            uow.CrewAssignments.Add(CrewAssignment.Create(ptraCrewRlfC.CtrlNbr, ptraAsgn350.CtrlNbr, 24, ptraStart));

            await uow.CommitAsync();
        }

        } // end crews guard

        // ?? Section 7: Boards � Extra Boards, Members, Cascade Policies ??????
        var boardRepo = sp.GetRequiredService<IExtraBoardRepository>();
        var boardMemberRepo = sp.GetRequiredService<IBoardMemberRepository>();
        var cascadeRepo = sp.GetRequiredService<IBoardCascadePolicyRepository>();

        var existingBoards = await boardRepo.GetAllAsync();
        if (existingBoards.Count == 0)
        {
        var crafts2 = await craftRepo.GetAllAsync();
        var groups2 = await groupRepo.GetAllAsync();
        var jaxSub3 = groups2.First(g => g.Name == "Jacksonville Sub");
        var csxRailroad3 = (await groupRepo.GetByGroupTypeNameAsync("Railroad")).First(g => g.Code == "CSX");
        var engCraft2 = crafts2.First(c => c.CraftName == "Engineer" && c.DynamicGroupCtrlNbr == csxRailroad3.CtrlNbr);
        var condCraft2 = crafts2.First(c => c.CraftName == "Conductor" && c.DynamicGroupCtrlNbr == csxRailroad3.CtrlNbr);
        var empList3 = await employeeRepo.GetAllAsync();
        var now2 = DateTime.UtcNow;

        var engBoard = ExtraBoard.Create(engCraft2.CtrlNbr, jaxSub3.CtrlNbr, "PRIMARY", "Jax Engineer Extra Board");
        var condBoard = ExtraBoard.Create(condCraft2.CtrlNbr, jaxSub3.CtrlNbr, "PRIMARY", "Jax Conductor Extra Board");
        await boardRepo.AddAsync(engBoard);
        await boardRepo.AddAsync(condBoard);

        // Board Members � 5 engineers, 5 conductors
        for (int i = 0; i < 5; i++)
        {
            await boardMemberRepo.AddAsync(BoardMember.Create(engBoard.CtrlNbr, empList3[3 + i].CtrlNbr, i + 1, now2));
            await boardMemberRepo.AddAsync(BoardMember.Create(condBoard.CtrlNbr, empList3[43 + i].CtrlNbr, i + 1, now2));
        }

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

        var existingVacancies = await vacancyRepo.GetAllAsync();
        if (existingVacancies.Count == 0)
        {
        var crafts3 = await craftRepo.GetAllAsync();
        var csxRailroad4 = (await groupRepo.GetByGroupTypeNameAsync("Railroad")).First(g => g.Code == "CSX");
        var engCraft3 = crafts3.First(c => c.CraftName == "Engineer" && c.DynamicGroupCtrlNbr == csxRailroad4.CtrlNbr);
        var condCraft3 = crafts3.First(c => c.CraftName == "Conductor" && c.DynamicGroupCtrlNbr == csxRailroad4.CtrlNbr);
        var empList4 = await employeeRepo.GetAllAsync();
        var allSlots = await positionSlotRepo.GetAllAsync();
        var now3 = DateTime.UtcNow;

        // Create vacancies targeting position slots (use an unbound tomorrow slot)
        var unboundCondSlot = allSlots.FirstOrDefault(s => s.Status == "Open");
        var unboundEngSlot = allSlots.LastOrDefault(s => s.Status == "Open");

        if (unboundCondSlot is not null)
        {
            var condVacancy = PositionVacancy.Create("PositionSlot", unboundCondSlot.CtrlNbr, condCraft3.CtrlNbr, "RESIGNATION");
            condVacancy.MarkBulletined();
            await vacancyRepo.AddAsync(condVacancy);

            var condBulletin = Bulletin.Create(condVacancy.CtrlNbr, condCraft3.CtrlNbr,
                now3, now3.AddDays(5));
            await bulletinRepo.AddAsync(condBulletin);

            await bidRepo.AddAsync(BulletinBid.Create(condBulletin.CtrlNbr, empList4[50].CtrlNbr, 1, 50));
            await bidRepo.AddAsync(BulletinBid.Create(condBulletin.CtrlNbr, empList4[51].CtrlNbr, 1, 51));
        }

        if (unboundEngSlot is not null && unboundEngSlot != unboundCondSlot)
        {
            var engVacancy = PositionVacancy.Create("PositionSlot", unboundEngSlot.CtrlNbr, engCraft3.CtrlNbr, "PROMOTION");
            engVacancy.MarkBulletined();
            await vacancyRepo.AddAsync(engVacancy);

            var engBulletin = Bulletin.Create(engVacancy.CtrlNbr, engCraft3.CtrlNbr,
                now3, now3.AddDays(7));
            await bulletinRepo.AddAsync(engBulletin);

            await bidRepo.AddAsync(BulletinBid.Create(engBulletin.CtrlNbr, empList4[10].CtrlNbr, 1, 10));
            await bidRepo.AddAsync(BulletinBid.Create(engBulletin.CtrlNbr, empList4[11].CtrlNbr, 1, 11));
        }

        } // end bulletins guard

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
            var projected = i < empList5.Count ? empList5[i + 5].CtrlNbr : (Domain.ValueObjects.ControlNumber?)null;
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

        var existingPolicies = await displacementPolicyRepo.GetAllAsync();
        if (existingPolicies.Count == 0)
        {
        var crafts4 = await craftRepo.GetAllAsync();
        var csxRailroad5 = (await groupRepo.GetByGroupTypeNameAsync("Railroad")).First(g => g.Code == "CSX");
        var engCraft4 = crafts4.First(c => c.CraftName == "Engineer" && c.DynamicGroupCtrlNbr == csxRailroad5.CtrlNbr);
        var condCraft4 = crafts4.First(c => c.CraftName == "Conductor" && c.DynamicGroupCtrlNbr == csxRailroad5.CtrlNbr);

        await displacementPolicyRepo.AddAsync(CraftDisplacementPolicy.Create(engCraft4.CtrlNbr, 72, "ROSTER_DATE", "EXTRA_BOARD"));
        await displacementPolicyRepo.AddAsync(CraftDisplacementPolicy.Create(condCraft4.CtrlNbr, 72, "ROSTER_DATE", "EXTRA_BOARD"));

        await bulletinPolicyRepo.AddAsync(BulletinPolicy.Create(engCraft4.CtrlNbr, 120));
        await bulletinPolicyRepo.AddAsync(BulletinPolicy.Create(condCraft4.CtrlNbr, 120));

        await senMovePolicyRepo.AddAsync(SeniorityMovePolicy.Create(engCraft4.CtrlNbr, 90, "ROSTER_DATE"));
        await senMovePolicyRepo.AddAsync(SeniorityMovePolicy.Create(condCraft4.CtrlNbr, 90, "ROSTER_DATE"));

        } // end policies guard

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
        var fraDutyTourRepo = sp.GetRequiredService<IFraDutyTourRepository>();
        var empList8 = await employeeRepo.GetAllAsync();

        var existingTour = await fraDutyTourRepo.GetActiveTourForEmployeeAsync(empList8[0].CtrlNbr);
        var searchResult = await fraDutyTourRepo.SearchAsync(new FraRecordSearchCriteria { EmployeeCtrlNbr = empList8[0].CtrlNbr });
        if (searchResult.Count == 0)
        {
        // Completed duty tour for an engineer � create a dummy regulatory standard CtrlNbr
        var tour = Domain.Modules.FraCompliance.FraDutyTour.Create(
            empList8[0].CtrlNbr, Domain.ValueObjects.ControlNumber.Create(1),
            DateTime.UtcNow.AddDays(-1).Date.AddHours(6),
            priorTimeOffMinutes: 720, consecutiveDays: 3);
        tour.Close(DateTime.UtcNow.AddDays(-1).Date.AddHours(16),
            totalTimeOnDutyMinutes: 600, excessMinutes: null, excessServiceReason: null, isQuickTieUp: false);
        await fraDutyTourRepo.AddAsync(tour);
        }
    }
}
