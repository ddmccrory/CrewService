# CrewService Frontend

Blazor Web App (server + WebAssembly hybrid) for the CrewService platform. Communicates with the backend API exclusively via **gRPC client stubs** generated from shared proto contracts.

## Table of Contents

- [Quickstart](#quickstart)
- [Architecture](#architecture)
- [Projects](#projects)
- [Shared proto contracts](#shared-proto-contracts)
- [Authentication flow](#authentication-flow)
- [gRPC client pattern](#grpc-client-pattern)
- [Pages](#pages)
- [Repository layout](#repository-layout)
- [Configuration](#configuration)
- [Development notes](#development-notes)

---

## Quickstart

**Prerequisites:**

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Backend API running (`CrewService.GrpcService` on `https://localhost:7064`)

**Steps:**

1. Ensure the backend API is running (see `CrewService.API/README.md`)
2. From the repo root: `dotnet run --project CrewService.FrontEnd/CrewService.BlazorUI`
3. Navigate to `https://localhost:7xxx` (see `launchSettings.json` for the assigned port)

## Architecture

- **Render mode:** Interactive Server + Interactive WebAssembly (hybrid)
- **API communication:** gRPC over `GrpcWebHandler` — no REST calls
- **Auth:** Cookie-based session on the frontend; JWT tokens from the backend stored as cookie claims and passed to gRPC calls via interceptor
- **Theming:** Bootswatch themes via `AppThemeService`, persisted per-user through the backend `AccountSrvc`

## Projects

| Project | SDK | Description |
|---|---|---|
| `CrewService.BlazorUI` | `Microsoft.NET.Sdk.Web` | Server-side host — DI composition, gRPC clients, Razor components, account pages |
| `CrewService.BlazorUI.Client` | `Microsoft.NET.Sdk.BlazorWebAssembly` | WASM-side components — auth state provider, redirect-to-login |

## Shared proto contracts

Proto files live at the **repository root** in `Protos/` — shared between backend (`GrpcServices="Server"`) and frontend (`GrpcServices="Client"`). Both solutions reference the same files via relative paths with `ProtoRoot`.

```
CrewService/
??? Protos/                          ? single source of truth
?   ??? auth.proto
?   ??? parent.proto
?   ??? railroad.proto
?   ??? common.proto
?   ??? modules/
?   ?   ??? tenant_config.proto
?   ?   ??? ...
?   ??? google/api/                  ? REST transcoding annotations
??? CrewService.API/                 ? Server stubs
??? CrewService.FrontEnd/            ? Client stubs
```

When adding a new proto, add a `<Protobuf>` entry with `GrpcServices="Client"` and `ProtoRoot="..\..\Protos"` to `CrewService.BlazorUI.csproj`.

## Authentication flow

```
Login.razor
  ? AuthClient.AuthenticateUserAsync() [gRPC, no auth header]
  ? Backend returns JWT + refresh token
  ? JWT stored as "AccessToken" claim in cookie via HttpContext.SignInAsync()
  ? Subsequent gRPC calls:
      BaseGrpcClient reads "AccessToken" from HttpContext.User claims
      ? AuthInterceptor attaches "Authorization: Bearer {token}" header
      ? Backend validates JWT
```

Key files:
- `Clients/BaseGrpcClient.cs` — channel creation, auth header injection
- `Interceptors/AuthInterceptor.cs` — Grpc.Core.Interceptor that adds Bearer token
- `Components/Account/Pages/Login.razor` — login form, cookie creation
- `Components/Account/Pages/Logout.razor` — cookie teardown

## gRPC client pattern

Each backend gRPC service gets a typed client that extends `BaseGrpcClient<T>`:

```csharp
public sealed class ParentsClient(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ILogger<ParentsClient> logger)
    : BaseGrpcClient<ParentSrvc.ParentSrvcClient>(configuration, httpContextAccessor,
        callInvoker => new ParentSrvc.ParentSrvcClient(callInvoker), logger)
{
    public async Task<GetAllParentsResponse> GetAllAsync()
    {
        try
        {
            return await _client.GetAllParentsAsyncAsync(new GetAllParentsRequest());
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
```

Clients are registered as `Scoped` in `Program.cs`. Auth-free clients (e.g., `AuthClient`) pass `addAuthHeader: false` to the base.

### Current clients

| Client | gRPC Service | Auth |
|---|---|---|
| `AuthClient` | `AuthSrvc` | No |
| `AccountClient` | `AccountSrvc` | Yes |
| `ParentsClient` | `ParentSrvc` | Yes |
| `RailroadsClient` | `RailroadSrvc` | Yes |
| `TenantConfigClient` | `TenantConfigSrvc` | Yes |

## Pages

### Account

| Page | Route | Description |
|---|---|---|
| Login | `/Account/Login` | Email/password ? JWT ? cookie session |
| Register | `/Account/Register` | Invitation token + password ? new user |
| Logout | `/Account/Logout` | Clears cookie, resets theme |
| AccessDenied | `/Account/AccessDenied` | 403 landing |
| Manage/Index | `/Account/Manage` | Profile page |
| Manage/Theme | `/Account/Manage/Theme` | Bootswatch theme picker |

### Application

| Page | Route | Description |
|---|---|---|
| Dashboard | `/` | Auth-gated landing page |
| Parents | `/parents` | List all parents, create/delete |
| ParentDetail | `/parents/{id}` | Edit parent, manage railroads (add/delete) |
| GroupTypes | `/config/group-types` | List group types, create/delete |
| GroupTypeDetail | `/config/group-types/{id}` | Edit group type, manage groups of this type |
| GroupDetail | `/config/groups/{id}` | Edit group, place/remove railroads, view child groups |

## Repository layout

```
CrewService.FrontEnd/
??? CrewService.FrontEnd.sln
??? CrewService.BlazorUI/
?   ??? Program.cs                                # DI composition, middleware, render modes
?   ??? Clients/                                  # Typed gRPC clients
?   ?   ??? BaseGrpcClient.cs                     # Abstract base — channel, auth, logging
?   ?   ??? AuthClient.cs
?   ?   ??? AccountClient.cs
?   ?   ??? ParentsClient.cs
?   ?   ??? RailroadsClient.cs
?   ?   ??? TenantConfigClient.cs
?   ??? Interceptors/
?   ?   ??? AuthInterceptor.cs                    # Bearer token injection
?   ??? Services/
?   ?   ??? AppThemeService.cs                    # Bootswatch theme state
?   ??? Models/
?   ?   ??? Account/                              # LoginInputModel, RegisterInputModel
?   ?   ??? Entities/                             # Parent, Railroad, User (local models)
?   ??? Converters/
?   ?   ??? ParentListConverter.cs
?   ??? Components/
?   ?   ??? App.razor                             # Root component
?   ?   ??? Routes.razor                          # Router
?   ?   ??? Layout/
?   ?   ?   ??? MainLayout.razor                  # Shell layout with sidebar
?   ?   ?   ??? NavMenu.razor                     # Navigation (auth-gated)
?   ?   ??? Pages/
?   ?   ?   ??? Dashboard.razor
?   ?   ?   ??? Parents.razor
?   ?   ?   ??? ParentDetail.razor
?   ?   ?   ??? GroupTypes.razor
?   ?   ?   ??? GroupTypeDetail.razor
?   ?   ?   ??? GroupDetail.razor
?   ?   ?   ??? Error.razor
?   ?   ??? Account/
?   ?       ??? Pages/                            # Login, Register, Logout, AccessDenied
?   ?       ??? Pages/Manage/                     # Index, Theme
?   ?       ??? Shared/                           # AccountLayout, ManageLayout, StatusMessage
?   ??? Properties/
?   ?   ??? launchSettings.json
?   ??? wwwroot/                                  # Static assets, Bootswatch CSS
?   ??? appsettings.json                          # CrewServiceApiUrl
?   ??? appsettings.Development.json
??? CrewService.BlazorUI.Client/
?   ??? Program.cs                                # WASM entry point
?   ??? PersistentAuthenticationStateProvider.cs   # Client-side auth state
?   ??? RedirectToLogin.razor                     # Unauthorized redirect
?   ??? UserInfo.cs                               # Serializable user claims
```

## Configuration

| Setting | File | Description |
|---|---|---|
| `CrewServiceApiUrl` | `appsettings.json` | Backend gRPC endpoint (default: `https://localhost:7064`) |
| Cookie auth | `Program.cs` | 30-minute sliding expiration, `/Account/Login` redirect |

## Development notes

- **Both solutions must be running** for the frontend to work — the backend serves gRPC, the frontend calls it
- **Proto changes:** Edit files in `Protos/` at repo root. Build either solution to regenerate stubs
- **Adding a new client:** Create a class extending `BaseGrpcClient<T>`, register as `Scoped` in `Program.cs`, add to this README
- **Adding a new page:** Create `.razor` in `Components/Pages/`, add `@attribute [Authorize]` for auth gating, add nav link in `NavMenu.razor`
- **Theme system:** `AppThemeService` is a singleton; Login sets theme from backend response, Logout resets to default (Spacelab)
