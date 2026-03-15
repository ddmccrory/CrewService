# Phase 4 — Employee Foundation

**Branch:** `feature/api-employee-foundation`
**Depends on:** Phase 2 (Employee.ClientCtrlNbr → Railroad)

## Why Fourth

Employees are the people who work on crews. Before defining crafts/rosters
(which link to employees via seniority), the reference data for contact types
and employment statuses must exist, and the Employee CRUD must be complete.

## Domain Entities

| Entity | Location | Status |
|--------|----------|--------|
| `Employee` | `Models/Employees/Employee.cs` | ✅ Complete (rich aggregate) |
| `Address` | `Models/Employees/Address.cs` | ✅ Complete |
| `PhoneNumber` | `Models/Employees/PhoneNumber.cs` | ✅ Complete |
| `EmailAddress` | `Models/Employees/EmailAddress.cs` | ✅ Complete |
| `EmployeePriorServiceCredit` | `Models/Employees/EmployeePriorServiceCredit.cs` | ✅ Complete |
| `Gender`, `Race`, `MaritalStatus` | `Models/Employees/` | ✅ Enums |
| `AddressType` | `Models/ContactTypes/AddressType.cs` | ✅ Complete |
| `PhoneNumberType` | `Models/ContactTypes/PhoneNumberType.cs` | ✅ Complete |
| `EmailAddressType` | `Models/ContactTypes/EmailAddressType.cs` | ✅ Complete |
| `EmploymentStatus` | `Models/Employment/EmploymentStatus.cs` | ✅ Complete |
| `EmploymentStatusHistory` | `Models/Employment/EmploymentStatusHistory.cs` | ✅ Complete |

## gRPC Services

| Service | Status |
|---------|--------|
| `EmployeeService` | ✅ Exists — audit CRUD + contact sub-entity RPCs |
| `AddressTypeService` | ✅ Exists — audit |
| `PhoneNumberTypeService` | ✅ Exists — audit |
| `EmailAddressTypeService` | ✅ Exists — audit |
| `EmploymentStatusService` | ✅ Exists — audit |
| `EmploymentStatusHistoryService` | ✅ Exists — audit |
| `PriorServiceCreditService` | ✅ Exists — audit |

## Commits

| # | Commit Message | Work |
|---|---------------|------|
| 1 | `audit: verify contact type services (address/phone/email types)` | CRUD completeness |
| 2 | `audit: verify employment status + history services` | CRUD + GetByEmployee |
| 3 | `audit: verify employee service — CRUD + nested contacts` | Add/Remove address/phone/email |
| 4 | `fix: fill missing RPCs` | Wire stubs |
| 5 | `test: employee aggregate lifecycle` | Create → add contacts → update → status history |

## Railroad Setup Story

> Jane (or a RailroadAdmin she invited) sets up reference data: AddressTypes
> ("Home", "Mailing"), PhoneNumberTypes ("Cell", "Home"), EmploymentStatuses
> ("Active", "Leave", "Terminated"). Then creates Employee records for her
> railroad's workers, adding contact info and employment dates.
