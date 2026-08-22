using CampusCore.Application.Abstractions;
using CampusCore.Application.Academics;
using CampusCore.Domain.Entities;
using CampusCore.Domain.Enums;
using CampusCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CampusCore.Application.Tests;

[TestClass]
public sealed class AcademicServiceTests
{
    private ApplicationDbContext _db = null!;
    private RecordingAuditWriter _audit = null!;
    private AcademicService _service = null!;

    [TestInitialize]
    public void Initialize()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"campuscore-academic-tests-{Guid.NewGuid():N}")
            .Options;
        _db = new ApplicationDbContext(options);
        _audit = new RecordingAuditWriter();
        _service = new AcademicService(_db, _audit);
    }

    [TestCleanup]
    public async Task Cleanup() => await _db.DisposeAsync();

    [TestMethod]
    public async Task ResolveGradeAsync_UsesDefaultSchemeUnlessExplicitlySelected()
    {
        _db.GradeScales.AddRange(
            new GradeScale { Name = "Default", MinimumPercentage = 80, MaximumPercentage = 100, Grade = "A" },
            new GradeScale { Name = "Custom", MinimumPercentage = 80, MaximumPercentage = 100, Grade = "Excellent" });
        await _db.SaveChangesAsync();

        var defaultGrade = await _service.ResolveGradeAsync(90, CancellationToken.None);
        var customGrade = await _service.ResolveGradeAsync(90, " custom ", CancellationToken.None);

        Assert.AreEqual("A", defaultGrade?.Grade);
        Assert.AreEqual("Excellent", customGrade?.Grade);
    }

    [TestMethod]
    public async Task RecordMarkAsync_RejectsMoreThanTwoDecimalPlaces()
    {
        var request = new MarkUpsert(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Quiz", 9.999m, 10m);

        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(() => _service.RecordMarkAsync(request, CancellationToken.None));

        Assert.AreEqual("Score values cannot have more than two decimal places.", exception.Message);
    }

    [TestMethod]
    public async Task RecordMarkAsync_NormalizesAssessmentAndAuditsSuccessfulWrite()
    {
        var student = new Student { AdmissionNumber = "ADM-50", FirstName = "Ada", DateOfBirth = new DateOnly(2012, 1, 1) };
        var subject = new Subject { Code = "MATH", Name = "Mathematics" };
        var year = new AcademicYear { Name = "2026-27", StartsOn = new DateOnly(2026, 4, 1), EndsOn = new DateOnly(2027, 3, 31) };
        _db.AddRange(student, subject, year);
        await _db.SaveChangesAsync();

        var id = await _service.RecordMarkAsync(new MarkUpsert(student.Id, subject.Id, year.Id, "  Midterm  ", 42.5m, 50m), CancellationToken.None);

        var mark = await _db.Marks.SingleAsync(x => x.Id == id);
        Assert.AreEqual("Midterm", mark.AssessmentName);
        Assert.AreEqual(42.5m, mark.Score);
        Assert.AreEqual("mark.recorded", _audit.Events.Single());
    }

    [TestMethod]
    public async Task UpsertAttendanceAsync_RejectsUnknownEnumValue()
    {
        var request = new AttendanceUpsert(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), (AttendanceStatus)999, null);

        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(() => _service.UpsertAttendanceAsync(request, CancellationToken.None));

        Assert.AreEqual("Attendance status is invalid.", exception.Message);
    }

    [TestMethod]
    public async Task UpsertAttendanceAsync_RejectsOversizedNoteBeforeDatabaseLookup()
    {
        var request = new AttendanceUpsert(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), AttendanceStatus.Present, new string('x', 501));

        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(() => _service.UpsertAttendanceAsync(request, CancellationToken.None));

        Assert.AreEqual("Attendance note cannot exceed 500 characters.", exception.Message);
    }

    private sealed class RecordingAuditWriter : IAuditWriter
    {
        public List<string> Events { get; } = [];

        public Task WriteAsync(string action, string entityType, string entityId, object? safeMetadata = null, CancellationToken cancellationToken = default)
        {
            Events.Add(action);
            return Task.CompletedTask;
        }
    }
}
