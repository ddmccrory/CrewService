using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.TenantConfig;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CrewService.GrpcService;

/// <summary>
/// Seeds the development database with sample data for the RailroadGroupPlacement
/// feature, covering all three supported scenarios:
///   1. Simple – railroad under a parent with no placement rows
///   2. Simple + WorkArea – railroad placed into a single work-area group
///   3. Holding company – multi-level tree (Region ? Subdivision ? WorkArea)
/// Idempotent: skips seeding when GroupTypes already exist.
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
        if (existing.Count > 0)
            return;

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

        // ?? Employees with Addresses, Phone Numbers, Email Addresses ?????
        var employeeRepo = sp.GetRequiredService<IEmployeeRepository>();
        var employmentStatusRepo = sp.GetRequiredService<IEmploymentStatusRepository>();
        var addressTypeRepo = sp.GetRequiredService<IAddressTypeRepository>();
        var phoneNumberTypeRepo = sp.GetRequiredService<IPhoneNumberTypeRepository>();
        var emailAddressTypeRepo = sp.GetRequiredService<IEmailAddressTypeRepository>();

        // Reference data
        var activeStatus = EmploymentStatus.Create(holdingCorp.CtrlNbr.Value, "A", "Active", 1, "FT");
        await employmentStatusRepo.AddAsync(activeStatus);

        var homeAddressType = AddressType.Create(holdingCorp.CtrlNbr.Value, "Home", 1, emergencyType: false);
        await addressTypeRepo.AddAsync(homeAddressType);

        var cellPhoneType = PhoneNumberType.Create(holdingCorp.CtrlNbr.Value, "Cell", 1, emergencyType: false);
        await phoneNumberTypeRepo.AddAsync(cellPhoneType);

        var workEmailType = EmailAddressType.Create(holdingCorp.CtrlNbr.Value, "Work", 1, emergencyType: false);
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
        string[] genders    = ["M", "F"];
        string[] races      = ["W", "B", "H", "A", "O"];

        for (int i = 0; i < 100; i++)
        {
            var firstName = firstNames[i % firstNames.Length];
            var lastName  = lastNames[i / firstNames.Length % lastNames.Length];

            var employee = Employee.Create(
                holdingCorp.CtrlNbr.Value,
                userId: $"seed-user-{i + 1:D4}",
                employeeNumber: $"EMP{i + 1:D4}",
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
                $"{firstName.ToLower()}.{lastName.ToLower()}{i + 1}@csx.example.com",
                workEmailType.CtrlNbr.Value);

            await employeeRepo.AddAsync(employee);
        }
    }
}
