-- Run against a disposable development database after applying migrations.
-- All fixture rows are rolled back. SQL Server identity values may still advance.
SET NOCOUNT ON;
SET XACT_ABORT OFF;

IF DB_NAME() <> N'ClinicBooking'
    THROW 51000, 'Run this script against the ClinicBooking development database.', 1;

BEGIN TRANSACTION;
BEGIN TRY
    IF (SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID(N'dbo')
        AND name IN (N'Clinics', N'Doctors', N'DoctorSchedules', N'Patients', N'Appointments')) <> 5
        THROW 51000, 'Expected all five application tables.', 1;

    IF (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id IN
        (OBJECT_ID(N'dbo.Doctors'), OBJECT_ID(N'dbo.DoctorSchedules'), OBJECT_ID(N'dbo.Appointments'))
        AND delete_referential_action = 0 AND is_disabled = 0 AND is_not_trusted = 0) <> 4
        THROW 51000, 'Expected four enabled, trusted foreign keys with restricted deletes.', 1;

    IF (SELECT COUNT(*) FROM sys.check_constraints WHERE name IN
        (N'CK_DoctorSchedules_DayOfWeek', N'CK_DoctorSchedules_TimeRange', N'CK_Appointments_DateTimeRange')
        AND is_disabled = 0 AND is_not_trusted = 0) <> 3
        THROW 51000, 'Expected three enabled, trusted check constraints.', 1;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.DoctorSchedules') AND name = N'IX_DoctorSchedules_DoctorId_DayOfWeek')
        THROW 51000, 'Doctor schedule lookup index is missing.', 1;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.Appointments') AND name = N'IX_Appointments_DoctorId_StartDateTime')
        THROW 51000, 'Appointment lookup index is missing.', 1;

    INSERT INTO dbo.Clinics (Name, Address) VALUES (N'Verification Clinic', NULL);
    DECLARE @ClinicId int = CONVERT(int, SCOPE_IDENTITY());

    INSERT INTO dbo.Doctors (Name, Phone, ClinicId) VALUES (N'Verification Doctor', NULL, @ClinicId);
    DECLARE @DoctorId int = CONVERT(int, SCOPE_IDENTITY());

    INSERT INTO dbo.Patients (Name, BirthDate, Phone) VALUES (N'Verification Patient', '20000101', NULL);
    DECLARE @PatientId int = CONVERT(int, SCOPE_IDENTITY());

    INSERT INTO dbo.DoctorSchedules (DoctorId, DayOfWeek, StartTime, EndTime)
        VALUES (@DoctorId, 6, '16:00', '18:00'), (@DoctorId, 6, '19:00', '20:00');

    INSERT INTO dbo.Appointments (DoctorId, PatientId, StartDateTime, EndDateTime)
        VALUES (@DoctorId, @PatientId, '2030-01-05T16:00:00', '2030-01-05T16:30:00');

    IF NOT EXISTS (SELECT 1 FROM dbo.Appointments a
        JOIN dbo.Doctors d ON d.Id = a.DoctorId
        JOIN dbo.Clinics c ON c.Id = d.ClinicId
        JOIN dbo.Patients p ON p.Id = a.PatientId
        WHERE c.Id = @ClinicId AND p.Id = @PatientId)
        THROW 51000, 'Valid records did not preserve the expected relationships.', 1;

    DECLARE @Cases TABLE (Label nvarchar(100), Statement nvarchar(max), ExpectedError int);
    INSERT INTO @Cases VALUES
        (N'Required clinic name', N'INSERT dbo.Clinics(Name) VALUES(NULL)', 515),
        (N'Required doctor name', N'INSERT dbo.Doctors(Name,ClinicId) VALUES(NULL,@ClinicId)', 515),
        (N'Required patient name', N'INSERT dbo.Patients(Name,BirthDate) VALUES(NULL,''20000101'')', 515),
        (N'Required birth date', N'INSERT dbo.Patients(Name,BirthDate) VALUES(N''Test'',NULL)', 515),
        (N'Clinic name length', N'INSERT dbo.Clinics(Name) VALUES(REPLICATE(N''x'',151))', 2628),
        (N'Clinic address length', N'INSERT dbo.Clinics(Name,Address) VALUES(N''Test'',REPLICATE(N''x'',251))', 2628),
        (N'Doctor name length', N'INSERT dbo.Doctors(Name,ClinicId) VALUES(REPLICATE(N''x'',151),@ClinicId)', 2628),
        (N'Doctor phone length', N'INSERT dbo.Doctors(Name,Phone,ClinicId) VALUES(N''Test'',REPLICATE(N''x'',31),@ClinicId)', 2628),
        (N'Patient name length', N'INSERT dbo.Patients(Name,BirthDate) VALUES(REPLICATE(N''x'',151),''20000101'')', 2628),
        (N'Patient phone length', N'INSERT dbo.Patients(Name,BirthDate,Phone) VALUES(N''Test'',''20000101'',REPLICATE(N''x'',31))', 2628),
        (N'Doctor clinic FK', N'INSERT dbo.Doctors(Name,ClinicId) VALUES(N''Test'',-1)', 547),
        (N'Schedule doctor FK', N'INSERT dbo.DoctorSchedules(DoctorId,DayOfWeek,StartTime,EndTime) VALUES(-1,0,''16:00'',''20:00'')', 547),
        (N'Appointment doctor FK', N'INSERT dbo.Appointments(DoctorId,PatientId,StartDateTime,EndDateTime) VALUES(-1,@PatientId,''2030-01-05T16:00:00'',''2030-01-05T16:30:00'')', 547),
        (N'Appointment patient FK', N'INSERT dbo.Appointments(DoctorId,PatientId,StartDateTime,EndDateTime) VALUES(@DoctorId,-1,''2030-01-05T16:00:00'',''2030-01-05T16:30:00'')', 547),
        (N'Weekday range', N'INSERT dbo.DoctorSchedules(DoctorId,DayOfWeek,StartTime,EndTime) VALUES(@DoctorId,7,''16:00'',''20:00'')', 547),
        (N'Reversed schedule times', N'INSERT dbo.DoctorSchedules(DoctorId,DayOfWeek,StartTime,EndTime) VALUES(@DoctorId,0,''20:00'',''16:00'')', 547),
        (N'Equal schedule times', N'INSERT dbo.DoctorSchedules(DoctorId,DayOfWeek,StartTime,EndTime) VALUES(@DoctorId,0,''16:00'',''16:00'')', 547),
        (N'Reversed appointment times', N'INSERT dbo.Appointments(DoctorId,PatientId,StartDateTime,EndDateTime) VALUES(@DoctorId,@PatientId,''2030-01-05T17:00:00'',''2030-01-05T16:30:00'')', 547),
        (N'Equal appointment times', N'INSERT dbo.Appointments(DoctorId,PatientId,StartDateTime,EndDateTime) VALUES(@DoctorId,@PatientId,''2030-01-05T16:00:00'',''2030-01-05T16:00:00'')', 547),
        (N'Restrict clinic deletion', N'DELETE dbo.Clinics WHERE Id=@ClinicId', 547),
        (N'Restrict doctor deletion', N'DELETE dbo.Doctors WHERE Id=@DoctorId', 547),
        (N'Restrict patient deletion', N'DELETE dbo.Patients WHERE Id=@PatientId', 547);

    DECLARE @Label nvarchar(100), @Statement nvarchar(max), @ExpectedError int, @Rejected bit;
    DECLARE VerificationCases CURSOR LOCAL FAST_FORWARD FOR SELECT Label, Statement, ExpectedError FROM @Cases;
    OPEN VerificationCases;
    FETCH NEXT FROM VerificationCases INTO @Label, @Statement, @ExpectedError;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Rejected = 0;
        BEGIN TRY
            EXEC sp_executesql @Statement, N'@ClinicId int, @DoctorId int, @PatientId int',
                @ClinicId, @DoctorId, @PatientId;
        END TRY
        BEGIN CATCH
            IF ERROR_NUMBER() <> @ExpectedError AND NOT (@ExpectedError = 2628 AND ERROR_NUMBER() = 8152)
                THROW;
            SET @Rejected = 1;
        END CATCH;

        IF @Rejected = 0
        BEGIN
            DECLARE @Message nvarchar(2048) = N'Expected the database to reject: ' + @Label;
            THROW 51000, @Message, 1;
        END;

        PRINT N'PASS: ' + @Label;
        FETCH NEXT FROM VerificationCases INTO @Label, @Statement, @ExpectedError;
    END;
    CLOSE VerificationCases;
    DEALLOCATE VerificationCases;

    ROLLBACK TRANSACTION;
    PRINT N'PASS: Schema, indexes, valid relationships, and 22 rejection cases. Fixture rows rolled back.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
