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

        // ?? Scenario 2: PTRA (Legacy Railroad) ?????????????????????????
        var ptraCorp = Parent.Create("Port Terminal Railroad Association");
        await parentRepo.AddAsync(ptraCorp);

        var ptraRR = Railroad.Create(ptraCorp.CtrlNbr.Value, "PTRA", "Port Terminal Railroad Association");
        await railroadRepo.AddAsync(ptraRR);

        var ptraYard = DynamicGroup.Create(
            workAreaType.CtrlNbr.Value,
            "PTRA Terminal",
            parentGroupCtrlNbr: null,
            path: "/ptra-terminal",
            isWorkArea: true);
        await groupRepo.AddAsync(ptraYard);

        var ptraPlacement = RailroadGroupPlacement.Create(ptraRR.CtrlNbr.Value, ptraYard.CtrlNbr.Value);
        await placementRepo.AddAsync(ptraPlacement);

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

        } // end employee guard

        // ?? SystemAdmin bootstrap user ???????????????????????????????????
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

        // ?? Section 4: Seniority — Crafts, Rosters, Rankings ?????????????
        var craftRepo = sp.GetRequiredService<ICraftRepository>();
        var rosterRepo = sp.GetRequiredService<IRosterRepository>();
        var seniorityRepo = sp.GetRequiredService<ISeniorityRepository>();

        var existingCrafts = await craftRepo.GetAllAsync();
        if (existingCrafts.Count == 0)
        {
        // Look up Jax Yard and PTRA Terminal work areas
        var allGroups = await groupRepo.GetAllAsync();
        var jaxYardGroup = allGroups.First(g => g.Name == "Jax Yard");
        var ptraGroup = allGroups.First(g => g.Name == "PTRA Terminal");

        // CSX Crafts at Jax Yard
        var csxEngineer = Craft.Create(jaxYardGroup.CtrlNbr, "Engineer", "Engineers", 1,
            autoMarkUp: false, approveAllMarkOffs: false, markOffHours: 10, markUpHours: 10,
            requiredRestHours: 10, maximumVacationDayTime: 480, unpaidMealPeriodMinutes: 0,
            hoursofService: true, processPayroll: true, showNotifications: true, vacationAssignmentType: 1);
        await craftRepo.AddAsync(csxEngineer);

        var csxConductor = Craft.Create(jaxYardGroup.CtrlNbr, "Conductor", "Conductors", 2,
            autoMarkUp: false, approveAllMarkOffs: false, markOffHours: 10, markUpHours: 10,
            requiredRestHours: 10, maximumVacationDayTime: 480, unpaidMealPeriodMinutes: 0,
            hoursofService: true, processPayroll: true, showNotifications: true, vacationAssignmentType: 1);
        await craftRepo.AddAsync(csxConductor);

        var csxClerical = Craft.Create(jaxYardGroup.CtrlNbr, "Clerical", "Clericals", 3,
            autoMarkUp: true, approveAllMarkOffs: true, markOffHours: 0, markUpHours: 0,
            requiredRestHours: 0, maximumVacationDayTime: 480, unpaidMealPeriodMinutes: 30,
            hoursofService: false, processPayroll: true, showNotifications: true, vacationAssignmentType: 0);
        await craftRepo.AddAsync(csxClerical);

        // PTRA Crafts at PTRA Terminal
        var ptraEngineer = Craft.Create(ptraGroup.CtrlNbr, "Engineer", "Engineers", 1,
            autoMarkUp: false, approveAllMarkOffs: false, markOffHours: 10, markUpHours: 10,
            requiredRestHours: 10, maximumVacationDayTime: 480, unpaidMealPeriodMinutes: 0,
            hoursofService: true, processPayroll: true, showNotifications: true, vacationAssignmentType: 1);
        await craftRepo.AddAsync(ptraEngineer);

        var ptraConductor = Craft.Create(ptraGroup.CtrlNbr, "Conductor", "Conductors", 2,
            autoMarkUp: false, approveAllMarkOffs: false, markOffHours: 10, markUpHours: 10,
            requiredRestHours: 10, maximumVacationDayTime: 480, unpaidMealPeriodMinutes: 0,
            hoursofService: true, processPayroll: true, showNotifications: true, vacationAssignmentType: 1);
        await craftRepo.AddAsync(ptraConductor);

        var ptraClerical = Craft.Create(ptraGroup.CtrlNbr, "Clerical", "Clericals", 3,
            autoMarkUp: true, approveAllMarkOffs: true, markOffHours: 0, markUpHours: 0,
            requiredRestHours: 0, maximumVacationDayTime: 480, unpaidMealPeriodMinutes: 30,
            hoursofService: false, processPayroll: true, showNotifications: true, vacationAssignmentType: 0);
        await craftRepo.AddAsync(ptraClerical);

        // Rosters — one per craft per railroad
        var allRailroads = await railroadRepo.GetAllAsync();
        var csxRailroad = allRailroads.First(rr => rr.RailroadMark == "CSX");

        var csxEngRoster = Roster.Create(csxEngineer.CtrlNbr, csxRailroad.CtrlNbr, "Engineer Roster", "Engineer Rosters", 1,
            training: false, extraBoard: false, overtimeBoard: false);
        await rosterRepo.AddAsync(csxEngRoster);

        var csxCondRoster = Roster.Create(csxConductor.CtrlNbr, csxRailroad.CtrlNbr, "Conductor Roster", "Conductor Rosters", 2,
            training: false, extraBoard: false, overtimeBoard: false);
        await rosterRepo.AddAsync(csxCondRoster);

        var csxClericalRoster = Roster.Create(csxClerical.CtrlNbr, csxRailroad.CtrlNbr, "Clerical Roster", "Clerical Rosters", 3,
            training: false, extraBoard: false, overtimeBoard: false);
        await rosterRepo.AddAsync(csxClericalRoster);

        // Seniority entries — split 100 employees: 40 Engineer, 50 Conductor, 10 Clerical
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

        // ?? Section 5: Work Management — Roles, Templates, Instances, Slots ??
        var positionRoleRepo = sp.GetRequiredService<IPositionRoleRepository>();
        var templateRepo = sp.GetRequiredService<IAssignmentTemplateRepository>();
        var workInstanceRepo = sp.GetRequiredService<IWorkInstanceRepository>();
        var positionSlotRepo = sp.GetRequiredService<IPositionSlotRepository>();

        var existingRoles = await positionRoleRepo.GetAllAsync();
        if (existingRoles.Count == 0)
        {
        // Re-lookup crafts for FK references
        var crafts = await craftRepo.GetAllAsync();
        var jaxYardGroup = (await groupRepo.GetAllAsync()).First(g => g.Name == "Jax Yard");
        var engCraft = crafts.First(c => c.CraftName == "Engineer" && c.DynamicGroupCtrlNbr == jaxYardGroup.CtrlNbr);
        var condCraft = crafts.First(c => c.CraftName == "Conductor" && c.DynamicGroupCtrlNbr == jaxYardGroup.CtrlNbr);
        var clerCraft = crafts.First(c => c.CraftName == "Clerical" && c.DynamicGroupCtrlNbr == jaxYardGroup.CtrlNbr);

        // Position Roles — Conductor craft
        var studentTrainman = PositionRole.Create(condCraft.CtrlNbr, "STRN", "Student Trainman");
        var trainman = PositionRole.Create(condCraft.CtrlNbr, "TRMN", "Trainman");
        var conductor = PositionRole.Create(condCraft.CtrlNbr, "COND", "Conductor");
        await positionRoleRepo.AddAsync(studentTrainman);
        await positionRoleRepo.AddAsync(trainman);
        await positionRoleRepo.AddAsync(conductor);

        // Position Roles — Engineer craft
        var studentEngineer = PositionRole.Create(engCraft.CtrlNbr, "SENG", "Student Engineer");
        var engineer = PositionRole.Create(engCraft.CtrlNbr, "ENGR", "Engineer");
        await positionRoleRepo.AddAsync(studentEngineer);
        await positionRoleRepo.AddAsync(engineer);

        // Position Roles — Clerical craft
        var crewCaller = PositionRole.Create(clerCraft.CtrlNbr, "CALL", "Crew Caller");
        var crewDispatcher = PositionRole.Create(clerCraft.CtrlNbr, "DISP", "Crew Dispatcher");
        await positionRoleRepo.AddAsync(crewCaller);
        await positionRoleRepo.AddAsync(crewDispatcher);

        // Assignment Templates at Jax Yard
        var job101 = AssignmentTemplate.Create(jaxYardGroup.CtrlNbr, "JOB101", "Job 101 - Daily Turn", null);
        var job202 = AssignmentTemplate.Create(jaxYardGroup.CtrlNbr, "JOB202", "Job 202 - Local Switch", null);
        var job303 = AssignmentTemplate.Create(jaxYardGroup.CtrlNbr, "JOB303", "Job 303 - Road Train", null);
        await templateRepo.AddAsync(job101);
        await templateRepo.AddAsync(job202);
        await templateRepo.AddAsync(job303);

        // Work Instances — 2 per template (today + tomorrow)
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var wi101Today = WorkInstance.Create(job101.CtrlNbr, jaxYardGroup.CtrlNbr, today.AddHours(6), today.AddHours(18), today.AddHours(5));
        var wi101Tomorrow = WorkInstance.Create(job101.CtrlNbr, jaxYardGroup.CtrlNbr, tomorrow.AddHours(6), tomorrow.AddHours(18), tomorrow.AddHours(5));
        var wi202Today = WorkInstance.Create(job202.CtrlNbr, jaxYardGroup.CtrlNbr, today.AddHours(7), today.AddHours(15), today.AddHours(6));
        var wi202Tomorrow = WorkInstance.Create(job202.CtrlNbr, jaxYardGroup.CtrlNbr, tomorrow.AddHours(7), tomorrow.AddHours(15), tomorrow.AddHours(6));
        var wi303Today = WorkInstance.Create(job303.CtrlNbr, jaxYardGroup.CtrlNbr, today.AddHours(22), today.AddDays(1).AddHours(10), today.AddHours(21));
        var wi303Tomorrow = WorkInstance.Create(job303.CtrlNbr, jaxYardGroup.CtrlNbr, tomorrow.AddHours(22), tomorrow.AddDays(1).AddHours(10), tomorrow.AddHours(21));
        await workInstanceRepo.AddAsync(wi101Today);
        await workInstanceRepo.AddAsync(wi101Tomorrow);
        await workInstanceRepo.AddAsync(wi202Today);
        await workInstanceRepo.AddAsync(wi202Tomorrow);
        await workInstanceRepo.AddAsync(wi303Today);
        await workInstanceRepo.AddAsync(wi303Tomorrow);

        // Position Slots — each work instance gets a Conductor + Engineer slot
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
        slots[0].Bind(empList[40].CtrlNbr, "DISPATCH");  // Job 101 today — Conductor
        slots[1].Bind(empList[0].CtrlNbr, "DISPATCH");    // Job 101 today — Engineer
        slots[4].Bind(empList[41].CtrlNbr, "DISPATCH");   // Job 202 today — Conductor
        slots[5].Bind(empList[1].CtrlNbr, "DISPATCH");    // Job 202 today — Engineer
        await positionSlotRepo.UpdateAsync(slots[0]);
        await positionSlotRepo.UpdateAsync(slots[1]);
        await positionSlotRepo.UpdateAsync(slots[4]);
        await positionSlotRepo.UpdateAsync(slots[5]);

        } // end work management guard

        // ?? Section 6: Crews — Crews, Positions, Incumbencies, Attachments ???
        var crewRepo = sp.GetRequiredService<ICrewRepository>();
        var crewPositionRepo = sp.GetRequiredService<ICrewPositionRepository>();
        var incumbencyRepo = sp.GetRequiredService<ICrewIncumbencyRepository>();
        var attachmentTemplateRepo = sp.GetRequiredService<ICrewAttachmentTemplateRepository>();
        var reliefRuleRepo = sp.GetRequiredService<IReliefCoverageRuleRepository>();

        var existingCrews = await crewRepo.GetAllAsync();
        if (existingCrews.Count == 0)
        {
        var allGroups2 = await groupRepo.GetAllAsync();
        var jaxYard2 = allGroups2.First(g => g.Name == "Jax Yard");
        var allRoles = await positionRoleRepo.GetAllAsync();
        var condRole = allRoles.First(r => r.Code == "COND");
        var engRole = allRoles.First(r => r.Code == "ENGR");
        var empList2 = await employeeRepo.GetAllAsync();
        var allTemplates = await templateRepo.GetAllAsync();
        var job101Tmpl = allTemplates.First(t => t.Code == "JOB101");

        // Regular crews
        var crewA = Crew.Create("REGULAR", jaxYard2.CtrlNbr, "Jax Turn Crew A");
        var crewB = Crew.Create("REGULAR", jaxYard2.CtrlNbr, "Jax Turn Crew B");
        var extraCrew = Crew.Create("EXTRA", jaxYard2.CtrlNbr, "Jax Extra Board Crew");
        await crewRepo.AddAsync(crewA);
        await crewRepo.AddAsync(crewB);
        await crewRepo.AddAsync(extraCrew);

        // Crew Positions — 2 per crew (Conductor + Engineer)
        var crewAPos1 = CrewPosition.Create(crewA.CtrlNbr, condRole.CtrlNbr, 1);
        var crewAPos2 = CrewPosition.Create(crewA.CtrlNbr, engRole.CtrlNbr, 2);
        var crewBPos1 = CrewPosition.Create(crewB.CtrlNbr, condRole.CtrlNbr, 1);
        var crewBPos2 = CrewPosition.Create(crewB.CtrlNbr, engRole.CtrlNbr, 2);
        var extraPos1 = CrewPosition.Create(extraCrew.CtrlNbr, condRole.CtrlNbr, 1);
        var extraPos2 = CrewPosition.Create(extraCrew.CtrlNbr, engRole.CtrlNbr, 2);
        await crewPositionRepo.AddAsync(crewAPos1);
        await crewPositionRepo.AddAsync(crewAPos2);
        await crewPositionRepo.AddAsync(crewBPos1);
        await crewPositionRepo.AddAsync(crewBPos2);
        await crewPositionRepo.AddAsync(extraPos1);
        await crewPositionRepo.AddAsync(extraPos2);

        // Incumbencies — assign employees to crew positions
        var now = DateTime.UtcNow;
        await incumbencyRepo.AddAsync(CrewIncumbency.Create(crewAPos1.CtrlNbr, empList2[40].CtrlNbr, now));
        await incumbencyRepo.AddAsync(CrewIncumbency.Create(crewAPos2.CtrlNbr, empList2[0].CtrlNbr, now));
        await incumbencyRepo.AddAsync(CrewIncumbency.Create(crewBPos1.CtrlNbr, empList2[41].CtrlNbr, now));
        await incumbencyRepo.AddAsync(CrewIncumbency.Create(crewBPos2.CtrlNbr, empList2[1].CtrlNbr, now));
        await incumbencyRepo.AddAsync(CrewIncumbency.Create(extraPos1.CtrlNbr, empList2[42].CtrlNbr, now));
        await incumbencyRepo.AddAsync(CrewIncumbency.Create(extraPos2.CtrlNbr, empList2[2].CtrlNbr, now));

        // Attachment — link Crew A to Job 101 template
        await attachmentTemplateRepo.AddAsync(CrewAttachmentTemplate.Create(job101Tmpl.CtrlNbr, crewA.CtrlNbr, now));

        // Relief rule — extra crew covers Job 101 on weekdays (Mon–Fri = 0b0111110)
        await reliefRuleRepo.AddAsync(ReliefCoverageRule.Create(extraCrew.CtrlNbr, job101Tmpl.CtrlNbr, 0b0111110, now));

        } // end crews guard

        // ?? Section 7: Boards — Extra Boards, Members, Cascade Policies ??????
        var boardRepo = sp.GetRequiredService<IExtraBoardRepository>();
        var boardMemberRepo = sp.GetRequiredService<IBoardMemberRepository>();
        var cascadeRepo = sp.GetRequiredService<IBoardCascadePolicyRepository>();

        var existingBoards = await boardRepo.GetAllAsync();
        if (existingBoards.Count == 0)
        {
        var crafts2 = await craftRepo.GetAllAsync();
        var groups2 = await groupRepo.GetAllAsync();
        var jaxYard3 = groups2.First(g => g.Name == "Jax Yard");
        var engCraft2 = crafts2.First(c => c.CraftName == "Engineer" && c.DynamicGroupCtrlNbr == jaxYard3.CtrlNbr);
        var condCraft2 = crafts2.First(c => c.CraftName == "Conductor" && c.DynamicGroupCtrlNbr == jaxYard3.CtrlNbr);
        var empList3 = await employeeRepo.GetAllAsync();
        var now2 = DateTime.UtcNow;

        var engBoard = ExtraBoard.Create(engCraft2.CtrlNbr, jaxYard3.CtrlNbr, "PRIMARY", "Jax Engineer Extra Board");
        var condBoard = ExtraBoard.Create(condCraft2.CtrlNbr, jaxYard3.CtrlNbr, "PRIMARY", "Jax Conductor Extra Board");
        await boardRepo.AddAsync(engBoard);
        await boardRepo.AddAsync(condBoard);

        // Board Members — 5 engineers, 5 conductors
        for (int i = 0; i < 5; i++)
        {
            await boardMemberRepo.AddAsync(BoardMember.Create(engBoard.CtrlNbr, empList3[3 + i].CtrlNbr, i + 1, now2));
            await boardMemberRepo.AddAsync(BoardMember.Create(condBoard.CtrlNbr, empList3[43 + i].CtrlNbr, i + 1, now2));
        }

        // Cascade Policies
        await cascadeRepo.AddAsync(BoardCascadePolicy.Create(jaxYard3.CtrlNbr, engCraft2.CtrlNbr,
            "UP_HIERARCHY", 2, true, 1, "SENIORITY"));
        await cascadeRepo.AddAsync(BoardCascadePolicy.Create(jaxYard3.CtrlNbr, condCraft2.CtrlNbr,
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
        var groups3 = await groupRepo.GetAllAsync();
        var jaxYard4 = groups3.First(g => g.Name == "Jax Yard");
        var engCraft3 = crafts3.First(c => c.CraftName == "Engineer" && c.DynamicGroupCtrlNbr == jaxYard4.CtrlNbr);
        var condCraft3 = crafts3.First(c => c.CraftName == "Conductor" && c.DynamicGroupCtrlNbr == jaxYard4.CtrlNbr);
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

        // ?? Section 9: Dispatching — Projections, Bookings ???????????????????
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

        // ?? Section 10: Policies — Displacement, Bulletin, Seniority Move ????
        var displacementPolicyRepo = sp.GetRequiredService<ICraftDisplacementPolicyRepository>();
        var bulletinPolicyRepo = sp.GetRequiredService<IBulletinPolicyRepository>();
        var senMovePolicyRepo = sp.GetRequiredService<ISeniorityMovePolicyRepository>();

        var existingPolicies = await displacementPolicyRepo.GetAllAsync();
        if (existingPolicies.Count == 0)
        {
        var crafts4 = await craftRepo.GetAllAsync();
        var groups4 = await groupRepo.GetAllAsync();
        var jaxYard5 = groups4.First(g => g.Name == "Jax Yard");
        var engCraft4 = crafts4.First(c => c.CraftName == "Engineer" && c.DynamicGroupCtrlNbr == jaxYard5.CtrlNbr);
        var condCraft4 = crafts4.First(c => c.CraftName == "Conductor" && c.DynamicGroupCtrlNbr == jaxYard5.CtrlNbr);

        await displacementPolicyRepo.AddAsync(CraftDisplacementPolicy.Create(engCraft4.CtrlNbr, 72, "ROSTER_DATE", "EXTRA_BOARD"));
        await displacementPolicyRepo.AddAsync(CraftDisplacementPolicy.Create(condCraft4.CtrlNbr, 72, "ROSTER_DATE", "EXTRA_BOARD"));

        await bulletinPolicyRepo.AddAsync(BulletinPolicy.Create(engCraft4.CtrlNbr, 120));
        await bulletinPolicyRepo.AddAsync(BulletinPolicy.Create(condCraft4.CtrlNbr, 120));

        await senMovePolicyRepo.AddAsync(SeniorityMovePolicy.Create(engCraft4.CtrlNbr, 90, "ROSTER_DATE"));
        await senMovePolicyRepo.AddAsync(SeniorityMovePolicy.Create(condCraft4.CtrlNbr, 90, "ROSTER_DATE"));

        } // end policies guard

        // ?? Section 11: Payroll — Tiers, Time Entries, Payroll Run ???????????
        var payrollTierRepo = sp.GetRequiredService<IPayrollTierRepository>();
        var timeEntryRepo = sp.GetRequiredService<ITimeEntryRepository>();
        var payrollRunRepo = sp.GetRequiredService<IPayrollRunRepository>();

        var existingTiers = await payrollTierRepo.GetAllAsync();
        if (existingTiers.Count == 0)
        {
        var groups5 = await groupRepo.GetAllAsync();
        var jaxYard6 = groups5.First(g => g.Name == "Jax Yard");
        var empList6 = await employeeRepo.GetAllAsync();
        var today3 = DateTime.UtcNow.Date;

        // Payroll Tiers
        await payrollTierRepo.AddAsync(PayrollTier.Create(jaxYard6.CtrlNbr, 7, 1, 100));
        await payrollTierRepo.AddAsync(PayrollTier.Create(jaxYard6.CtrlNbr, 14, 2, 150));

        // Time Entries for first 10 employees
        for (int i = 0; i < 10 && i < empList6.Count; i++)
        {
            await timeEntryRepo.AddAsync(TimeEntry.Create(empList6[i].CtrlNbr, today3, "REGULAR", 8.0m));
        }

        // Payroll Run — current pay period
        var payPeriod = $"{today3:yyyy}-W{System.Globalization.ISOWeek.GetWeekOfYear(today3):D2}";
        await payrollRunRepo.AddAsync(PayrollRun.Create(payPeriod));

        } // end payroll guard

        // ?? Section 12: Safety — Categories, Observations ????????????????????
        var safetyCatRepo = sp.GetRequiredService<ISafetyCategoryRepository>();
        var safetyObsRepo = sp.GetRequiredService<ISafetyObservationRepository>();

        var existingCategories = await safetyCatRepo.GetAllAsync();
        if (existingCategories.Count == 0)
        {
        var groups6 = await groupRepo.GetAllAsync();
        var jaxYard7 = groups6.First(g => g.Name == "Jax Yard");
        var empList7 = await employeeRepo.GetAllAsync();

        await safetyCatRepo.AddAsync(SafetyCategory.Create(jaxYard7.CtrlNbr, "TRACK", "Track Safety"));
        await safetyCatRepo.AddAsync(SafetyCategory.Create(jaxYard7.CtrlNbr, "EQUIP", "Equipment Safety"));
        await safetyCatRepo.AddAsync(SafetyCategory.Create(jaxYard7.CtrlNbr, "HAZMAT", "Hazardous Materials"));

        // Open observation
        var obs1 = SafetyObservation.Create(jaxYard7.CtrlNbr, empList7[10].CtrlNbr,
            "TRACK", "Jax Yard", "Broken rail joint near switch 12", "Jacksonville Sub");
        obs1.AddAction(empList7[0].CtrlNbr, "Flagged area and notified maintenance.");
        await safetyObsRepo.AddAsync(obs1);

        // Observation with no actions yet
        var obs2 = SafetyObservation.Create(jaxYard7.CtrlNbr, empList7[42].CtrlNbr,
            "EQUIP", "Jax Yard", "Locomotive headlight flickering on unit 4521");
        await safetyObsRepo.AddAsync(obs2);

        } // end safety guard

        // ?? Section 13: Railroad Information ?????????????????????????????????
        var rrInfoRepo = sp.GetRequiredService<IRailroadInformationRepository>();

        var existingInfo = await rrInfoRepo.GetAllAsync();
        if (existingInfo.Count == 0)
        {
        var groups7 = await groupRepo.GetAllAsync();
        var jaxYard8 = groups7.First(g => g.Name == "Jax Yard");

        var info1 = RailroadInformation.Create(jaxYard8.CtrlNbr, "GENERAL",
            "Track Speed Restriction — MP 42.5", "Speed restricted to 25 MPH through MP 42.5 due to maintenance.");
        info1.Publish();
        await rrInfoRepo.AddAsync(info1);

        var info2 = RailroadInformation.Create(jaxYard8.CtrlNbr, "SAFETY",
            "Draft: New PPE Requirements", "All yard employees required to wear high-visibility vests effective next month.");
        await rrInfoRepo.AddAsync(info2);

        } // end railroad info guard

        // ?? Section 14: FRA Compliance — Duty Tours ??????????????????????????
        var fraDutyTourRepo = sp.GetRequiredService<IFraDutyTourRepository>();
        var empList8 = await employeeRepo.GetAllAsync();

        var existingTour = await fraDutyTourRepo.GetActiveTourForEmployeeAsync(empList8[0].CtrlNbr);
        var searchResult = await fraDutyTourRepo.SearchAsync(new FraRecordSearchCriteria { EmployeeCtrlNbr = empList8[0].CtrlNbr });
        if (searchResult.Count == 0)
        {
        // Completed duty tour for an engineer — create a dummy regulatory standard CtrlNbr
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
