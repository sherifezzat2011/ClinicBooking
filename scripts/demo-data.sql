-- Run after the Development database migration, against the same database as the app.
-- Fictional patients only. DEMO-* phone values are sample identifiers, not real numbers.
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.Appointments', N'U') IS NULL
    THROW 51000, 'Run the EF Core database update before this script.', 1;

DECLARE @Patients TABLE (Number int PRIMARY KEY, Name nvarchar(150), BirthDate date, Phone nvarchar(30));
INSERT INTO @Patients (Number, Name, BirthDate, Phone)
SELECT names.Number, names.Name, DATEADD(day, names.Number * 97, CONVERT(date, '19850101')),
       CONCAT('DEMO-', RIGHT(CONCAT('000', names.Number), 3))
FROM (VALUES
    (1, N'Omar Hassan'), (2, N'Mariam Ali'), (3, N'Nour Ahmed'), (4, N'Salma Adel'),
    (5, N'Youssef Samir')
) names(Number, Name);

DECLARE @Doctors TABLE (Id int PRIMARY KEY);
INSERT INTO @Doctors
SELECT d.Id FROM dbo.Doctors d
JOIN dbo.Clinics c ON c.Id = d.ClinicId
WHERE c.Name = N'Demo Clinic' AND d.Name IN (N'Dr. Ahmed', N'Dr. Sara');

IF (SELECT COUNT(*) FROM @Doctors) <> 2
    THROW 51000, 'Run database update in Development to add Demo Clinic, Dr. Ahmed and Dr. Sara.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @LockResult int;
    EXEC @LockResult = sys.sp_getapplock
        @Resource = N'ClinicBooking:DemoData', @LockMode = 'Exclusive',
        @LockOwner = 'Transaction', @LockTimeout = 10000;
    IF @LockResult < 0 THROW 51001, 'Could not lock demo data. Try again.', 1;

    -- Use the same doctor locks as the booking service.
    DECLARE @DoctorId int = 0, @Resource nvarchar(255);
    WHILE EXISTS (SELECT 1 FROM @Doctors WHERE Id > @DoctorId)
    BEGIN
        SELECT @DoctorId = MIN(Id) FROM @Doctors WHERE Id > @DoctorId;
        SET @Resource = CONCAT('ClinicBooking:Doctor:', @DoctorId);
        EXEC @LockResult = sys.sp_getapplock
            @Resource = @Resource, @LockMode = 'Exclusive',
            @LockOwner = 'Transaction', @LockTimeout = 10000;
        IF @LockResult < 0 THROW 51001, 'Could not lock a doctor. Try again.', 1;
    END;

    -- Keep the original date range when rerun, including on a later day.
    DECLARE @StartDate date;
    SELECT @StartDate = MIN(CONVERT(date, a.StartDateTime))
    FROM dbo.Appointments a
    JOIN dbo.Patients p ON p.Id = a.PatientId
    JOIN @Patients demo ON demo.Phone = p.Phone AND demo.Name = p.Name AND demo.BirthDate = p.BirthDate
    JOIN @Doctors d ON d.Id = a.DoctorId;

    IF @StartDate IS NULL
        SET @StartDate = DATEADD(day, 7, CONVERT(date, GETDATE()));
    -- Monday of the chosen week; independent of SET DATEFIRST and language.
    SET @StartDate = DATEADD(day, -(DATEDIFF(day, '19000101', @StartDate) % 7), @StartDate);

    INSERT INTO dbo.Patients (Name, BirthDate, Phone)
    SELECT demo.Name, demo.BirthDate, demo.Phone FROM @Patients demo
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.Patients p
        WHERE p.Phone = demo.Phone AND p.Name = demo.Name AND p.BirthDate = demo.BirthDate
    );
    DECLARE @PatientsAdded int = @@ROWCOUNT;

    DECLARE @Bookings TABLE (Number int PRIMARY KEY, DoctorId int, StartDateTime datetime2, EndDateTime datetime2);
    ;WITH Days AS (
        SELECT DATEADD(day, n, @StartDate) AS VisitDate FROM (VALUES (0),(1),(2),(3),(4),(5),(6)) days(n)
    ), Slots AS (
        SELECT DISTINCT d.Id AS DoctorId,
            DATEADD(minute, slotOffset.Minutes,
                DATEADD(day, DATEDIFF(day, '19000101', days.VisitDate), CAST(s.StartTime AS datetime2))) AS StartDateTime
        FROM @Doctors d
        CROSS JOIN Days days
        CROSS APPLY (
            SELECT TOP (1) schedule.StartTime, schedule.EndTime
            FROM dbo.DoctorSchedules schedule
            WHERE schedule.DoctorId = d.Id
              AND schedule.DayOfWeek = (DATEDIFF(day, '19000107', days.VisitDate) % 7)
            ORDER BY schedule.StartTime, schedule.Id
        ) s
        CROSS JOIN (VALUES (0), (60)) slotOffset(Minutes)
        WHERE DATEADD(minute, slotOffset.Minutes + 30, CAST(s.StartTime AS datetime2)) <= CAST(s.EndTime AS datetime2)
    )
    INSERT INTO @Bookings
    SELECT TOP (5) ROW_NUMBER() OVER (ORDER BY StartDateTime, DoctorId), DoctorId,
           StartDateTime, DATEADD(minute, 30, StartDateTime)
    FROM Slots ORDER BY StartDateTime, DoctorId;

    INSERT INTO dbo.Appointments (DoctorId, PatientId, StartDateTime, EndDateTime)
    SELECT b.DoctorId, patient.Id, b.StartDateTime, b.EndDateTime
    FROM @Bookings b
    JOIN @Patients demo ON demo.Number = b.Number
    CROSS APPLY (
        SELECT TOP (1) p.Id FROM dbo.Patients p
        WHERE p.Phone = demo.Phone AND p.Name = demo.Name AND p.BirthDate = demo.BirthDate
        ORDER BY p.Id
    ) patient
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.Appointments a
        WHERE a.DoctorId = b.DoctorId
          AND a.StartDateTime < b.EndDateTime AND a.EndDateTime > b.StartDateTime
    );
    DECLARE @AppointmentsAdded int = @@ROWCOUNT;

    COMMIT;
    SELECT DB_NAME() AS DatabaseName, @PatientsAdded AS PatientsAdded,
           @AppointmentsAdded AS AppointmentsAdded, @StartDate AS FromDate,
           DATEADD(day, 6, @StartDate) AS ToDate;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
