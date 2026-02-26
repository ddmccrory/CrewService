using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.UserAccess;
using CrewService.Infrastructure.Models.UserAccount;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace CrewService.GrpcService;

/// <summary>
/// Seeds the development database with sample data covering:
///   1. GroupTypes, DynamicGroups, Parents, Railroads, RailroadGroupPlacements
///   2. Employees with Addresses, Phone Numbers, Email Addresses (via auto-accept invitations)
///   3. SystemAdmin bootstrap user and per-parent role assignments (via auto-accept invitations)
/// Idempotent: each section checks for existing data before seeding.
/// Dev only — uses auto-accept invitation flow to mirror production logic.
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

        var groupTypeRepo = sp.GetRequiredService<IGroupTypeRepository>();
        var groupRepo = sp.GetRequiredService<IDynamicGroupRepository>();
        var parentRepo = sp.GetRequiredService<IParentRepository>();
        var railroadRepo = sp.GetRequiredService<IRailroadRepository>();
        var placementRepo = sp.GetRequiredService<IRailroadGroupPlacementRepository>();

        // Idempotent guard – if group types already exist, skip seeding
        var existing = await groupTypeRepo.GetAllAsync();
        if (existing.Count == 0)
        {

        // ?? Group Types ??????????????????????????????????????????????
        var regionType = GroupType.Create("Region", "Geographic region", isWorkArea: false);
        var subdivType = GroupType.Create("Subdivision", "Track subdivision", isWorkArea: false);
        var workAreaType = GroupType.Create("WorkArea", "Operational work area", isWorkArea: true);

        await groupTypeRepo.AddAsync(regionType);
        await groupTypeRepo.AddAsync(subdivType);
        await groupTypeRepo.AddAsync(workAreaType);

        // ?? Scenario 1: Simple (no placements) ??????????????????????
        var simpleCorp = Parent.Create("Simple Corp");
        await parentRepo.AddAsync(simpleCorp);

        var simpleRR = Railroad.Create(simpleCorp.CtrlNbr.Value, "SMPL", "Simple Railroad");
        await railroadRepo.AddAsync(simpleRR);
        // No placement rows – backward-compatible scenario

        // ?? Scenario 2: Simple + WorkArea ????????????????????????????
        var waCorp = Parent.Create("WorkArea Corp");
        await parentRepo.AddAsync(waCorp);

        var waRR = Railroad.Create(waCorp.CtrlNbr.Value, "WARK", "WorkArea Railroad");
        await railroadRepo.AddAsync(waRR);

        var waGroup = DynamicGroup.Create(
            workAreaType.CtrlNbr.Value,
            "Main Yard",
            parentGroupCtrlNbr: null,
            path: "/main-yard",
            isWorkArea: true);
        await groupRepo.AddAsync(waGroup);

        var waPlacement = RailroadGroupPlacement.Create(waRR.CtrlNbr.Value, waGroup.CtrlNbr.Value);
        await placementRepo.AddAsync(waPlacement);

        // ?? Scenario 3: Holding Company ??????????????????????????????
        // Parent ? Region ? Subdivision ? WorkArea
        var holdingCorp = Parent.Create("CSX Corporation");
        await parentRepo.AddAsync(holdingCorp);

        var csxRR = Railroad.Create(holdingCorp.CtrlNbr.Value, "CSX", "CSX Transportation");
        await railroadRepo.AddAsync(csxRR);

        var csxtRR = Railroad.Create(holdingCorp.CtrlNbr.Value, "CSXT", "CSX Intermodal");
        await railroadRepo.AddAsync(csxtRR);

        // Group tree
        var southeast = DynamicGroup.Create(
            regionType.CtrlNbr.Value,
            "Southeast Region",
            parentGroupCtrlNbr: null,
            path: "/southeast",
            isWorkArea: false);
        await groupRepo.AddAsync(southeast);

        var jaxSub = DynamicGroup.Create(
            subdivType.CtrlNbr.Value,
            "Jacksonville Sub",
            parentGroupCtrlNbr: southeast.CtrlNbr.Value,
            path: "/southeast/jax",
            isWorkArea: false);
        await groupRepo.AddAsync(jaxSub);

        var jaxYard = DynamicGroup.Create(
            workAreaType.CtrlNbr.Value,
            "Jax Yard",
            parentGroupCtrlNbr: jaxSub.CtrlNbr.Value,
            path: "/southeast/jax/yard",
            isWorkArea: true);
        await groupRepo.AddAsync(jaxYard);

        var midwest = DynamicGroup.Create(
            regionType.CtrlNbr.Value,
            "Midwest Region",
            parentGroupCtrlNbr: null,
            path: "/midwest",
            isWorkArea: false);
        await groupRepo.AddAsync(midwest);

        // Place CSX at Region level, CSXT at the WorkArea level
        var csxPlacement = RailroadGroupPlacement.Create(csxRR.CtrlNbr.Value, southeast.CtrlNbr.Value);
        await placementRepo.AddAsync(csxPlacement);

        var csxtPlacement = RailroadGroupPlacement.Create(csxtRR.CtrlNbr.Value, jaxYard.CtrlNbr.Value);
        await placementRepo.AddAsync(csxtPlacement);
        }

        // ?? Employees with Addresses, Phone Numbers, Email Addresses ?????
        var employeeRepo = sp.GetRequiredService<IEmployeeRepository>();
        var existingEmployees = await employeeRepo.GetAllAsync();
        if (existingEmployees.Count > 0)
            return;

        var employmentStatusRepo = sp.GetRequiredService<IEmploymentStatusRepository>();
        var addressTypeRepo = sp.GetRequiredService<IAddressTypeRepository>();
        var phoneNumberTypeRepo = sp.GetRequiredService<IPhoneNumberTypeRepository>();
        var emailAddressTypeRepo = sp.GetRequiredService<IEmailAddressTypeRepository>();

        // Look up the CSX Corporation parent (created above or in a prior run)
        var parents = await parentRepo.GetAllAsync();
        var csxParent = parents.First(p => p.Name.Value == "CSX Corporation");

        // Reference data
        var activeStatus = EmploymentStatus.Create(csxParent.CtrlNbr.Value, "A", "Active", 1, "FT");
        await employmentStatusRepo.AddAsync(activeStatus);

        var homeAddressType = AddressType.Create(csxParent.CtrlNbr.Value, "Home", 1, emergencyType: false);
        await addressTypeRepo.AddAsync(homeAddressType);

        var cellPhoneType = PhoneNumberType.Create(csxParent.CtrlNbr.Value, "Cell", 1, emergencyType: false);
        await phoneNumberTypeRepo.AddAsync(cellPhoneType);

        var workEmailType = EmailAddressType.Create(csxParent.CtrlNbr.Value, "Work", 1, emergencyType: false);
        await emailAddressTypeRepo.AddAsync(workEmailType);

        var userMgr = sp.GetRequiredService<UserManager<User>>();
        var invitationRepo = sp.GetRequiredService<IInvitationRepository>();
        var assignmentRepo = sp.GetRequiredService<IUserParentAssignmentRepository>();

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
        string[] genders    = ["M", "F"];
        string[] races      = ["W", "B", "H", "A", "O"];

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
                Roles.ReadOnly,
                "SYSTEM");
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

            var assignment = UserParentAssignment.Create(user.Id, csxParent.CtrlNbr.Value, invitation.Role);
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

        // ?? SystemAdmin bootstrap user ???????????????????????????????????
        var existingInvitations = await invitationRepo.GetAllAsync();
        if (existingInvitations.Count > 100)
            return;

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

        // ?? Upgrade specific employee assignments via invitation flow ????
        var allParents = await parentRepo.GetAllAsync();
        var csxCorp = allParents.First(p => p.Name.Value == "CSX Corporation");
        var allEmployees = await employeeRepo.GetAllAsync();

        // Upgrade first 6 employees to distinct roles (they already have ReadOnly from above)
        string[] rolesToUpgrade = [Roles.ParentAdmin, Roles.RailroadAdmin, Roles.CraftManager, Roles.CrewManager, Roles.Dispatcher, Roles.PayrollClerk];
        for (int r = 0; r < rolesToUpgrade.Length && r < allEmployees.Count; r++)
        {
            // Create a role-upgrade invitation (auto-accepted)
            var upgradeInvite = Invitation.Create(
                allEmployees[r].EmailAddresses.FirstOrDefault()?.Email ?? $"emp-{r}@csx.example.com",
                csxCorp.CtrlNbr.Value,
                rolesToUpgrade[r],
                "SYSTEM");
            upgradeInvite.Accept();
            await invitationRepo.AddAsync(upgradeInvite);

            // Update existing assignment role
            var existingAssignment = await assignmentRepo.GetByUserAndParentAsync(allEmployees[r].UserId, csxCorp.CtrlNbr.Value);
            if (existingAssignment is not null)
            {
                existingAssignment.UpdateRole(rolesToUpgrade[r]);
                await assignmentRepo.UpdateAsync(existingAssignment);
            }
        }
    }
}
