# ClinicBooking

A clinic appointment booking application using ASP.NET Core MVC. Development follows one feature branch per user story.

## Current stage

US-01 establishes the MVC project structure. US-02 adds the database model and migration. US-03 adds sample doctors and schedules. US-04 adds the secretary's booking form. US-05 displays available 30-minute appointment slots. US-06 saves bookings and prevents concurrent conflicts. US-07 displays saved appointments on Home.

## Technology

- ASP.NET Core MVC on .NET 10 LTS
- Razor views, Bootstrap, and JavaScript
- Entity Framework Core 10.0.14, Code First, and SQL Server
- SQL Server LocalDB for Windows development

## Run locally

Install the .NET 10 SDK and SQL Server Express LocalDB (instance `MSSQLLocalDB`). `global.json` selects an installed stable .NET 10 SDK with feature-band roll-forward. Visual Studio is optional; use an IDE version that supports .NET 10 and `.slnx` solutions. A separate SQL Server instance can be used by overriding the connection string.

From the repository root:

```shell
dotnet restore ClinicBooking.slnx
dotnet tool restore
dotnet build ClinicBooking.slnx --no-restore
dotnet ef database update --project src/ClinicBooking.Web -- --environment Development
dotnet run --project src/ClinicBooking.Web --launch-profile http
```

Open http://localhost:5185. The migration creates the configured database and its schema. In Development, the database update command also seeds the sample data described below, even when there are no new migrations. Database migrations and seeding are not performed automatically at application startup.

For local HTTPS, trust the development certificate using `dotnet dev-certs https --trust`, then run with `--launch-profile https` and open https://localhost:7173.

Home displays saved appointments and a **New appointment** button. Open the form from that button, the navigation menu, or `/Appointments/Create`. Choose a doctor and date to load available 30-minute slots. Changing either selection clears the previously selected time. **Save appointment** validates the details and checks the selected time again before saving, then redirects to Home with a green confirmation and the new booking in the table. Errors and availability alerts use red styling.

`AppointmentAvailabilityService` generates complete slots within each working period, excludes overlapping appointments and slots that have already started, and returns distinct times in order. Adjacent appointments are allowed. The JavaScript form ignores outdated responses when selections change quickly. All dates and times use the clinic server's local time zone; configure it for the clinic location. C# database operations remain synchronous.

`AppointmentBookingService` saves the new patient and appointment in one transaction. A SQL Server application lock serializes booking attempts for the same doctor, including attempts from different application instances. Availability is rechecked after acquiring the lock. The lock is released on commit or rollback; every booking write must use this service. Failed saves roll back both records, and unavailable selections refresh the time options while preserving patient inputs.

Successful saves redirect to GET so refreshing the confirmation does not repeat the POST. The form currently registers a new patient for each booking; selecting an existing patient is outside this task's current scope. No schema migration is needed for US-06.

The **Saved appointments** table on Home shows the booking number, patient, doctor, date (`dd/MM/yyyy`), and start/end times (`HH:mm`). Rows are ordered chronologically, with the booking number breaking ties. An empty-state message appears when there are no bookings. On small screens, the table scrolls horizontally within its container. Form validation errors stay on New appointment; successful saves return to Home. US-07 does not change the database schema.

## Architecture

One MVC application with responsibilities separated into folders:

```text
ClinicBooking.slnx
src/
  ClinicBooking.Web/
    Controllers/             HTTP requests and responses
    Entities/                Clinic, Doctor, DoctorSchedule, Patient, Appointment
    Data/
      Configurations/        Relationships, column types, lengths, and constraints
      Migrations/            InitialCreate and model snapshot
      ApplicationDbContext.cs EF Core database session
      DevelopmentDataSeeder.cs Fictional development data
    Services/                Scheduling and booking business rules
    ViewModels/              Input and display models for views
    Views/                   Razor views and shared layouts
    wwwroot/                 CSS, JavaScript, and third-party assets
    Program.cs               Dependency injection and request pipeline
```

Requests will flow through controllers to services, which use the EF Core DbContext. Views render view models. `ApplicationDbContext` is registered with dependency injection using SQL Server and the default scoped lifetime. Entity configurations are discovered through `ApplyConfigurationsFromAssembly`.

The Services folder remains reserved for later booking logic. Empty folders contain `.gitkeep` files so the intended structure is visible in Git.

## Development sample data

`DevelopmentDataSeeder` adds the following sample data when running `dotnet ef database update` in Development:

| Record | Sample values |
| --- | --- |
| Clinic | Demo Clinic; Fictional demo location |
| Doctor | Dr. Ahmed; 16:00–20:00 every day except Friday (6 working periods) |
| Doctor | Dr. Sara; 10:00–14:00 Sunday through Thursday (5 working periods) |

This EF seeder adds doctors and schedules only. Existing records are matched by clinic and doctor names, and existing working weekdays are kept. Rerunning the command adds only missing records. Renaming a sample clinic or doctor causes a new sample record to be added.

The configured connection string is used, including User Secrets overrides such as `ClinicBookingDev`. No new migration is needed because the schema has not changed. Seeding is disabled outside Development.

### Optional demo patients and bookings

After the Development database update, run `scripts/demo-data.sql` against the **same database as the application**:

```shell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -d ClinicBooking -b -i scripts/demo-data.sql
```

Replace `ClinicBooking` with your configured database name, for example `ClinicBookingDev` when using that User Secrets override. Alternatively, open the SQL file in Visual Studio or SQL Server Management Studio, select that database, and execute it. Refresh Home to see the rows.

The script adds five fictional patients and up to five 30-minute appointments within the default working schedules, starting in the next week. Other slots remain free to try the form. Phone values `DEMO-001` through `DEMO-005` are fictional identifiers.

Rerunning it keeps the original date range, reuses the sample patients, and skips occupied slots. It does not delete or update existing records. It uses the booking service's doctor locks and one transaction. If you changed the schedules or already booked some times, fewer rows may be added. The SQL file is optional demo data, not a database backup: reviewers first create the schema using EF migrations, then run this file locally. No real patient data or database files are included.

## Configuration

- `appsettings.json` contains shared, non-sensitive settings.
- `appsettings.Development.json` contains the LocalDB connection string using Windows authentication, with no username or password.
- Launch profiles set the local environment and ports.
- Override `ConnectionStrings:DefaultConnection` using ASP.NET Core User Secrets or the `ConnectionStrings__DefaultConnection` environment variable. Never commit passwords, real patient data, or development certificates. User Secrets are enabled for the web project.
- Non-development environments must provide their own connection string; the application reports a clear configuration error when it is missing.
- `.gitignore` excludes build output, local tooling, and common secret files. Any ignored custom settings file must be explicitly loaded if used.

## Database rules

- Every entity has an integer identity primary key.
- All four foreign keys are required, and referenced records cannot be deleted while dependents exist.
- Names are required and limited to 150 characters; optional phone numbers to 30 characters; optional clinic addresses to 250 characters. Unicode text supports Arabic names.
- Birth dates use SQL `date`; weekly start/end times use `time`; appointment date/times use `datetime2`.
- Weekdays use `tinyint`: Sunday = 0 through Saturday = 6, enforced by a check constraint.
- Schedule start time and appointment start date/time must precede their respective end values, enforced by check constraints.
- Composite indexes support doctor/weekday schedule lookups and doctor/start-time appointment lookups. They are not uniqueness or overlap constraints.
- Appointments are interpreted in the clinic's local time. A multi-time-zone deployment would require an explicit time-zone policy.
- The form rejects blank names, future birth dates, past appointment dates, and unavailable selected times. Visits last 30 minutes and fit within working hours. The booking service rechecks availability under a transaction-owned doctor lock before inserting both records. Required SQL columns reject `NULL`, not empty text.

## Verify the database

With SQL Server command-line utilities installed, run the integration checks against the development database:

```shell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -d ClinicBooking -b -i scripts/verify-database.sql
dotnet ef migrations has-pending-model-changes --project src/ClinicBooking.Web -- --environment Development
```

The SQL script checks the five tables, four restricted foreign keys, three check constraints, lookup indexes, valid linked records, and 22 invalid operations. Fixture rows are rolled back, although SQL Server identity counters may advance. Use a disposable development database. It does not test booking overlap prevention, which is not implemented yet.

The local `dotnet-ef` tool version is pinned in `dotnet-tools.json` so migration commands use the same EF version as the project.

## Delivery workflow

Board statuses: **Backlog → In Progress → Done**.

Create a branch when starting its story, for example `feature/us-01-project-setup`. Subsequent stories follow `feature/us-02-database`, `feature/us-03-seed-data`, and the same naming convention. Merge a completed, verified feature through a pull request linked to its issue.
