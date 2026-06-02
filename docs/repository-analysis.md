# KarlixQMS.API Repository Analysis

## Project purpose

KarlixQMS.API is the REST API for the Karlix Quality Management System. It is responsible for tenant-aware QMS business operations, authorization enforcement, case and action workflows, dashboard data, lookup data, and field-level workflow editing for complaints and nonconformities.

The API is intended to be the source of truth for business authorization. Clients should request data and actions through this API rather than deciding permissions locally.

## Architecture summary

- ASP.NET Core 8 Web API project using controllers.
- EF Core 8 SQL Server data access through `QmsDbContext`.
- DB-first model style: entity and view classes live under `Models/Tables`, and schema mapping is centralized in `Data/QmsDbContext.cs`.
- Authentication uses OpenIddict validation against a configured identity authority.
- Authorization is policy-based for cases and actions, with additional manual phase/field permission checks in `CaseFieldsController`.
- Tenant filtering is implemented explicitly in controller queries using `ITenantContext.TenantId`.
- Read-heavy screens depend on SQL views such as `vw_QmsIssueList`, `vw_QmsActionOverview`, `vw_QmsIssue_Actions`, and KPI/analysis views.
- Swagger is configured with Bearer token support and is exposed at the application root.

Core folders:

- `Controllers`: API endpoints.
- `Data`: EF Core context and mappings.
- `Infrastructure`: tenant context and permission metadata.
- `Infrastructure/Security`: policies, permission constants, claim helpers, and authorization helpers.
- `Models/Tables`: DB-first table and view models.

## Authentication flow

1. API receives requests with a Bearer access token.
2. `Program.cs` configures the default authentication scheme as `OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme`.
3. OpenIddict validation checks the token issuer using `Authentication:Authority`; if missing, it falls back to `https://localhost:7173`.
4. Controllers require the OpenIddict validation scheme at class level.
5. `TenantContext` reads the current tenant from authenticated claims named `tenant` or `tenant_id`.
6. If no valid tenant GUID is found, `TenantContext.TenantId` becomes `Guid.Empty`, and `IsGlobal` returns true.
7. Authorization policies check roles and `perm` claims:
   - Admin roles: `GlobalAdmin`, `TenantAdmin`.
   - Baseline permissions: `qms.read`, `qms.admin`.
   - Action permissions: `qms.actions.read`, `qms.actions.write.basic`, `qms.actions.verify`, `qms.actions.write.all`.
   - Case phase permissions: `qms.rin.write.RECEIVED`, `qms.rin.write.IN_PROGRESS`, `qms.rin.write.CLOSED`, `qms.un.write.RECEIVED`, `qms.un.write.IN_PROGRESS`, `qms.un.write.CLOSED`.
8. Fine-grained case field writes are checked manually based on entity type and workflow phase.

## API dependencies

- .NET 8 / ASP.NET Core Web API.
- EF Core 8 with SQL Server provider.
- OpenIddict validation for token validation.
- Swashbuckle for Swagger/OpenAPI.
- Microsoft IdentityModel token libraries.
- SQL Server database named/configured as the QMS database.
- External identity authority configured through `Authentication:Authority`:
  - Development config points to local KarlixID.
  - Production config points to Karlix ID domain.
- Configuration includes `DefaultConnection`, logging, allowed hosts, and SMTP settings.

Important note: application settings currently include plaintext database and SMTP credentials. They should be moved to user secrets, environment variables, Key Vault, or another secure configuration provider.

## Key controllers

- `ActionsController`
  - Route: `api/actions`.
  - Endpoints:
    - `GET /api/actions`: action list with open, overdue, type, case number, and awaiting-verification filters.
    - `GET /api/actions/{id}`: action details.
    - `PUT /api/actions/{id}`: full action update.
    - `PATCH /api/actions/{id}/verification`: verification-only update.
  - Uses `Qms.Actions.Read`, `Qms.Actions.WriteBasic`, and `Qms.Actions.Verify` policies.

- `CasesController`
  - Route: `api/cases`.
  - Endpoints:
    - `GET /api/cases`: tenant-filtered case list with type, status, status group, search, and take filters.
    - `GET /api/cases/{number}`: case details with related actions and complaint/nonconformity payload.
    - `GET /api/cases/{number}/history`: workflow history.
    - `POST /api/cases/{number}/transition`: workflow transition.
  - Uses case read/write policies and enforces workflow transition rules.

- `CaseFieldsController`
  - Route: `api/cases`.
  - Endpoint:
    - `PATCH /api/cases/{number}/fields`: field-level patching for complaints and nonconformities.
  - Applies tenant filtering, resolves workflow phase, checks phase-specific permissions, and whitelists editable fields per entity and phase.

- `DashboardController`
  - Route: `api/dashboard`.
  - Endpoint:
    - `GET /api/dashboard`: summary counts, recent cases, monthly trend, and open actions.
  - Requires authentication at class level and applies tenant filtering in queries.

- `LookupsController`
  - Route: `api/lookups`.
  - Endpoints:
    - `GET /api/lookups/action-types`.
    - `GET /api/lookups/effectiveness`.
    - `GET /api/lookups/org-units`.
    - `GET /api/lookups/workflow-statuses`.
  - Requires authentication at class level and tenant-filters active lookup records.

## Key view models

API DTOs:

- `ActionListItemDto`: list projection for actions, including issue number, type, title, responsibility, dates, status, lateness, effectiveness, and delete flag.
- `ActionDetailsDto`: detailed action projection with entity info, responsible org unit, action type, effectiveness, verification, and delete flag.
- `ActionUpdateDto`: writable action fields for full updates.
- `ActionVerificationPatchDto`: verification date, notes, and effectiveness patch payload.
- `CaseTransitionDto`: target workflow status payload for transitions.

Database view models used as read models:

- `vw_QmsIssueList`: main case list/header read model.
- `vw_QmsIssue_Actions`: action list per case.
- `vw_QmsActionOverview`: action overview read model for list/detail/dashboard.
- `vw_QmsIssueKpiMonthly`: monthly dashboard trend.
- `vw_QmsDashboard`: dashboard backing view, currently mapped but not directly used by the controller.
- `vw_QmsCustomerComplaintList` and `vw_QmsNonconformityList`: specific list read models.
- `vw_QmsCustomerComplaint_Analysis`, `vw_QmsNonconformity_Analysis`, `vw_QmsIssue_CombinedAnalysis`, and `vw_QmsIssueActionAnalysis`: analysis read models.
- `vw_QmsActionList`: mapped action list view.

Core table models:

- Cases and workflow: `QmsIssue`, `QmsWorkflowStatus`, `QmsIssueHistory`, `QmsIssueLink`.
- Complaint data: `QmsCustomerComplaint`, `QmsComplaintReason`, `QmsComplaintFindingType`, `QmsProductState`, `QmsUnitOfMeasure`.
- Nonconformity data: `QmsNonconformity`, `QmsNonconformityRelationType`, `QmsStandard`, `QmsStandardRequirement`.
- Actions: `QmsIssueAction`, `QmsAction`, `QmsActionType`, `QmsEffectiveness`.
- Organization and audit support: `QmsOrgUnit`, `QmsAudit`, `QmsAuditAuditor`, `QmsAuditStandard`, `QmsAuditType`.
- Attachments: `QmsAttachment`, `QmsIssueAttachment`.

## Key Razor views

No Razor views are present in this repository. The project is an API-only ASP.NET Core application with controllers and Swagger. There are no `Views`, `Pages`, `Areas`, or `wwwroot` directories.

## Risks

- Plaintext database and SMTP credentials exist in appsettings files.
- Tenant fallback to `Guid.Empty` can behave like a global tenant marker. Endpoints currently filter by the resulting tenant ID, but this should be reviewed carefully for all future code paths.
- `DashboardController` and `LookupsController` require authentication but do not apply explicit QMS read policies.
- Permission constants and helpers are duplicated across `AppPermissionInfo`, `QmsPermissions`, `QmsPerms`, `QmsAuth`, `PermissionAuthorizationHandler`, and controller-local helpers.
- `PermissionRequirement` and `PermissionAuthorizationHandler` exist but are not registered or used in `Program.cs`.
- Case field patching uses manual JSON parsing and string field whitelists, which increases maintenance risk as fields evolve.
- Workflow transition rules are hard-coded in the controller rather than stored/configured in a workflow model.
- `ActionsWriteAll` exists as a permission constant but is not used by the current policies.
- Swagger is enabled in all environments, including production.
- The DB-first model is large and centralized; rescaffolding or manual edits could easily overwrite local changes if not handled carefully.
- Build output and user-specific project files exist in the working tree area; agents should continue avoiding `bin`, `obj`, `.vs`, `*.user`, and related files.

## Recommended next 10 development tasks

1. Move secrets out of appsettings files and rotate exposed database/SMTP credentials.
2. Add explicit QMS read policies to `DashboardController` and `LookupsController`.
3. Consolidate permission constants and helper logic into one source of truth.
4. Register or remove the unused `PermissionRequirement` and `PermissionAuthorizationHandler`.
5. Add integration tests for tenant filtering on every controller endpoint.
6. Add authorization tests for case read/write, action read/write/verify, and phase-specific field patching.
7. Replace raw JSON field patch parsing with typed DTOs or a structured field-update service.
8. Extract workflow transition and phase resolution logic into a dedicated service with tests.
9. Add OpenAPI response schemas and error response conventions for controllers.
10. Review production Swagger exposure and decide whether it should require protection, environment gating, or remain public by policy.
