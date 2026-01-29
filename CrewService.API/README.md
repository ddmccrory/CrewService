# CrewService Solution

CrewService is a modular .NET 9 gRPC-based microservice designed for extensible crew and employee management. The solution is architected to support multiple business modules, with the **Employee Management** module as the initial implementation. Additional modules will be added in the future.

## Table of Contents

- [Overview](#overview)
- [Modules](#modules)
  - [Employee Management Module](#employee-management-module)
- [Architecture](#architecture)
- [Authentication & Security](#authentication--security)
- [gRPC & Transcoding](#grpc--transcoding)
- [Swagger & API Documentation](#swagger--api-documentation)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Development](#development)
- [Testing](#testing)
- [Contributing](#contributing)
- [License](#license)

---

## Overview

CrewService exposes a set of gRPC endpoints for managing crew, employee, and related domain entities. It leverages modern .NET features, including:

- .NET 9 and C# 13.0
- gRPC with JSON transcoding for REST compatibility
- JWT-based authentication
- Modular, layered architecture
- Swagger/OpenAPI documentation

## Modules

### Employee Management Module

The **Employee Management** module is the first module implemented in this solution. It provides core models, services, and endpoints for managing employee-related data and operations. This includes:

- **Employee records**: CRUD operations for employee entities
- **Employment status and history**: Track and manage employment status changes
- **Seniority and roster management**: Manage employee seniority, rosters, and related assignments
- **Contact information**: Manage employee addresses, phone numbers, and email addresses

#### Example: Roster Management

The `RosterService` provides gRPC endpoints for managing rosters, including:

- Retrieving all rosters
- Retrieving a specific roster by control number
- Creating, updating, and deleting rosters

The `Roster` model includes properties such as:
- `CraftCtrlNbr`, `RailroadPayrollDepartmentCtrlNbr`
- `RosterName`, `RosterPluralName`
- `RosterNumber`, `Training`, `ExtraBoard`, `OvertimeBoard`

> **Note:** All current models pertain to employee management. The solution is designed to support additional modules (such as Payroll, Scheduling, etc.) in the future.

## Architecture

The solution follows a clean, layered architecture:

- **Presentation**: gRPC service classes, API endpoints, and service registration.
- **Application**: Core business logic, service contracts, and use cases.
- **Infrastructure**: External system integrations (e.g., email, logging).
- **Persistence**: Database context, repositories, and migrations.

## Authentication & Security

- **JWT Bearer Authentication** is enforced for all endpoints (except those explicitly excluded).
- The JWT key, issuer, and audience are configured via user secrets or environment variables.
- Token validation includes signature, lifetime, and (optionally) issuer/audience.

## gRPC & Transcoding

- All services are exposed via gRPC.
- JSON transcoding is enabled, allowing RESTful HTTP/JSON clients to interact with gRPC endpoints.
- gRPC-Web is enabled for browser compatibility.

## Swagger & API Documentation

- Swagger/OpenAPI documentation is available in development mode.
- Security definitions are included for JWT authentication.
- The Swagger UI is accessible at `/swagger` when running locally.

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Visual Studio 2026](https://visualstudio.microsoft.com/)
- (Optional) [Docker](https://www.docker.com/) for containerized deployment

### Setup

1. **Clone the repository:**

   ```bash
   git clone https://github.com/yourusername/crewservice.git
   cd crewservice
   ```

2. **Configure User Secrets (for development):**

   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "Jwt:Key" "your_secret_key"
   dotnet user-secrets set "Jwt:Issuer" "your_issuer"
   dotnet user-secrets set "Jwt:Audience" "your_audience"
   ```

3. **Restore dependencies:**

   ```bash
   dotnet restore
   ```

4. **Run the service:**

   ```bash
   dotnet run
   ```

   The service should now be running on `https://localhost:5001` (or the configured URL).

5. **Access Swagger UI (Development only):**
- Navigate to [https://localhost:5001/swagger](https://localhost:5001/swagger)

## Configuration

Configuration is managed via `appsettings.json`, environment variables, and user secrets. Key settings include:

- `Jwt:Key` (required): Secret key for JWT signing.
- `Jwt:Issuer` (optional): JWT token issuer.
- `Jwt:Audience` (optional): JWT token audience.

## Development

- The solution targets **.NET 9** and **C# 13.0**.
- All gRPC services are registered in `Program.cs` and support gRPC-Web and JSON transcoding.
- Use Visual Studio 2026 for best experience.

### Adding a New gRPC Service

1. Define your service contract in a `.proto` file.
2. Implement the service in the `CrewService.Presentation` project.
3. Register the service in `CrewService.GrpcService/Program.cs` using `app.MapGrpcService<YourService>().EnableGrpcWeb();`

### Adding a New Module

- Create new domain models, services, and endpoints following the existing structure.
- Register new services and update documentation as needed.

## Testing

- Unit and integration tests should be added to a dedicated test project (not included in this repository).
- Use the Swagger UI or gRPC clients (e.g., [grpcurl](https://github.com/fullstorydev/grpcurl)) for manual endpoint testing.

## Contributing

Contributions are welcome! Please fork the repository and submit a pull request.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

---

*This README describes the Employee Management module as the initial focus. Future modules will be documented as they are added.*
