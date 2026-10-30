# ClinicBooking

A clinic appointment booking application using ASP.NET Core MVC. Development follows one feature branch per user story.

## Current stage

US-01 establishes the project structure and a working home page. Patient registration, doctor availability, bookings, and the appointments table will be delivered in subsequent stories.

## Technology

- ASP.NET Core MVC on .NET 10 LTS
- Razor views, Bootstrap, and JavaScript
- Planned in US-02: Entity Framework Core 10, Code First, and SQL Server

## Run locally

Install the .NET 10 SDK. `global.json` selects an installed stable .NET 10 SDK with feature-band roll-forward. Visual Studio is optional; use an IDE version that supports .NET 10 and `.slnx` solutions.

From the repository root:

```shell
dotnet restore ClinicBooking.slnx
dotnet build ClinicBooking.slnx --no-restore
dotnet run --project src/ClinicBooking.Web --launch-profile http
```

Open http://localhost:5185. This stage does not require SQL Server or a connection string.

For local HTTPS, trust the development certificate using `dotnet dev-certs https --trust`, then run with `--launch-profile https` and open https://localhost:7173.

## Architecture

One MVC application with responsibilities separated into folders:

```text
ClinicBooking.slnx
src/
  ClinicBooking.Web/
    Controllers/             HTTP requests and responses
    Entities/                Persistent domain entities (US-02)
    Data/
      Configurations/        EF Core entity mappings (US-02)
      Migrations/            Versioned database changes (US-02)
    Services/                Scheduling and booking business rules
    ViewModels/              Input and display models for views
    Views/                   Razor views and shared layouts
    wwwroot/                 CSS, JavaScript, and third-party assets
    Program.cs               Dependency injection and request pipeline
```

Requests will flow through controllers to services, which use the EF Core DbContext. Views render view models. Dependency injection and environment-specific configuration use ASP.NET Core's built-in facilities.

Empty folders contain `.gitkeep` files so the intended structure is visible in Git. Entities and database code are deferred to US-02.

## Configuration

- `appsettings.json` contains shared, non-sensitive settings.
- `appsettings.Development.json` contains non-sensitive development overrides.
- Launch profiles set the local environment and ports.
- Use environment variables or ASP.NET Core User Secrets for credentials when database configuration is introduced. Never commit passwords, real patient data, or development certificates.
- `.gitignore` excludes build output, local tooling, and common secret files. Any ignored custom settings file must be explicitly loaded if used.

## Delivery workflow

Board statuses: **Backlog → In Progress → Done**.

Create a branch when starting its story, for example `feature/us-01-project-setup`. Subsequent stories follow `feature/us-02-database`, `feature/us-03-seed-data`, and the same naming convention. Merge a completed, verified feature through a pull request linked to its issue.
