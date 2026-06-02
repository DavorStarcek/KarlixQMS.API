# KarlixQMS.API - Agent Instructions

This repository contains the Karlix QMS REST API.

## Responsibility

- QMS business API
- Tenant-aware data access
- Authorization source of truth
- Cases, actions, dashboard, lookups and workflow endpoints

## Rules

- API is the only source of truth for business authorization.
- Web clients must not decide permissions.
- Always enforce tenant filtering.
- TenantId comes from authenticated context/claims.
- Use existing policies and permission patterns.
- Preserve GUID identifiers.
- Preserve EF Core DB-first approach.
- Do not change database schema unless explicitly requested.
- Do not change authentication/OpenIddict validation unless explicitly requested.
- Do not connect to production databases.
- Do not modify secrets unless explicitly requested.
- Do not edit sibling repositories unless explicitly requested.
- Do not modify bin, obj, .vs, *.user, *.suo files.

## Build

Default build command:

```bash
dotnet build .\KarlixQMS.API.csproj
Workflow

Before coding:

Inspect existing implementation.
Identify affected controllers, DTOs, services and EF models.
Propose a short plan.
Keep scope narrow.

After coding:

Run build if allowed.
Summarize changed files.
Report build result.
Mention risks and next steps.