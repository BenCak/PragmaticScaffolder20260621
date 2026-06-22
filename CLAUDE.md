# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.
Create a developer tool that scaffolds a Pragmatic Vertical Slice ASP.NET Core application from an existing SQL Server database.

The tool should allow me to point to a database connection string, select tables, and automatically generate a working codebase.

Goal:
Build a scaffold generator similar in spirit to NetTiers, but modern, simpler, and easier for Rails and WebForms developers to understand.

Target architecture:

```text
Feature → Page/Endpoint → Service → EF Core DbContext
```

Avoid over-engineered Clean Architecture:

```text
Controller → Command → Handler → Validator → Repository → UnitOfWork → Mapper → Response
```

Technology stack:

- ASP.NET Core
- Minimal APIs
- Telerik UI for Blazor
- EF Core database-first
- SQL Server
- Feature folders
- Simple services
- DTOs where useful
- No MediatR
- No CQRS by default
- No unnecessary interfaces
- No separate Domain/Application/Infrastructure projects unless optional later

The scaffold tool should generate this structure:

````text
GeneratedApp
│
├── src
│   ├── GeneratedApp.Blazor      // Telerik UI for Blazor UI only
│   ├── GeneratedApp.Api         // Minimal API only
│   ├── GeneratedApp.Data        // EF Core DbContext + scaffolded DB entities
│   └── GeneratedApp.Shared      // DTOs/contracts/validation
│
├── tests
│   ├── GeneratedApp.Api.Tests   // endpoint/service tests
│   └── GeneratedApp.Blazor.Tests// bUnit UI tests
│
├── docker
│   ├── Dockerfile.Api
│   ├── Dockerfile.Blazor
│   └── docker-compose.yml
│
└── GeneratedApp.sln

Required dependency rules:

GeneratedApp.Blazor → GeneratedApp.Shared
GeneratedApp.Api    → GeneratedApp.Shared + GeneratedApp.Data
GeneratedApp.Data   → SQL Server / EF Core
GeneratedApp.Shared → no project references
Tests               → only the project being tested plus needed test packages


Scaffolding tool requirements:

1. User enters SQL Server connection string.
2. Tool connects to database and reads schema.
3. Tool displays tables in a tree/list.
4. User can select one or more tables to scaffold.
5. Tool reads:
   - table names
   - column names
   - data types
   - nullable columns
   - primary keys
   - foreign keys
   - identity columns

6. Tool generates EF Core entities and DbContext.
7. Tool generates one feature folder per selected table.
8. For each selected table, generate:
   - Razor page using Telerik UI for Blazor Grid
   - Minimal API endpoints
   - Service class
   - DTO class
   - basic create/edit/delete/list methods

9. Generated UI should support:
   - list records
   - search/filter
   - add record
   - edit record
   - delete record
   - basic validation based on nullable columns and string lengths

10. Generated endpoints should support:

- GET all
- GET by ID
- POST create
- PUT update
- DELETE

11. Use async EF Core methods.
12. Use dependency injection.
13. Keep generated code simple and readable.
14. Add comments explaining the architecture.
15. Generate all required NuGet package references.
16. Generate appsettings.json with connection string placeholder.
17. Generate Program.cs with Telerik UI for Blazor, EF Core, and endpoint registration.
18. Generate a README with:

- how to run
- how to change connection string
- how to scaffold selected tables
- how to add a new feature manually

19. The scaffolder itself should run on Ubuntu and VS Code.
20. Prefer a web-based scaffolder UI using ASP.NET Core + Telerik UI for Blazor.

The scaffolder project should be separate from the generated app:

```text
PragmaticScaffolder
  src
    PragmaticScaffolder.Web
      Pages
      Services
        DatabaseSchemaReader.cs
        CodeGeneratorService.cs
        ProjectGeneratorService.cs
      Models
        DatabaseTable.cs
        DatabaseColumn.cs
        DatabaseRelationship.cs
      Templates
        FeaturePageTemplate.scriban
        EndpointTemplate.scriban
        ServiceTemplate.scriban
        DtoTemplate.scriban
        ProgramTemplate.scriban
        DbContextTemplate.scriban
````

Use Scriban or another simple template engine for code generation.

Important:
Generate real working code, not pseudocode.

Start by creating:

1. Full solution structure
2. NuGet commands
3. Schema reader code
4. Code generation service
5. Table selection UI
6. Templates
7. Sample generated output using Northwind Customers, Products, Orders
8. Instructions to run the scaffolder
9. Instructions to run the generated app

Keep the design practical and simple. The main purpose is developer adoption, not architectural purity.

## Blazor scaffolding rules

- For Landing Pages and Grids add Export to excel
- When scaffolding Telerik UI for Blazor dialog forms: use a `TelerikWindow` sized `Width="500px" Height="fit-content"`, lay all fields out via `TelerikForm Columns="2" ColumnSpacing="1rem"`, and convert PascalCase field names to space-separated `LabelText` values. Only add `[Required]` to the form model for non-nullable columns — never add an explicit "not required" marker for nullable ones.
- Post-scaffold, a human should add section headers and promote long fields to a full-width row (e.g. wrap in a `FormItemsTemplate` or give the field its own `TelerikForm`/`FormGroup`).
- Telerik UI for Blazor requires a NuGet source that resolves `Telerik.UI.for.Blazor` (private feed, or a local feed pointing at an extracted offline installer's `packages` folder) and a license file at `~/.telerik/telerik-license.txt`. Both are machine-level setup, not part of the generated code.
