using CampusCore.Application.Abstractions;
using CampusCore.Application.Students;
using CampusCore.Domain.Entities;
using CampusCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CampusCore.Application.Tests;

[TestClass]
public sealed class StudentServiceTests
{
    private ApplicationDbContext _db = null!;
    private RecordingAuditWriter _audit = null!;
    private StudentService _service = null!;

    [TestInitialize]
    public void Initialize()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"campuscore-student-tests-{Guid.NewGuid():N}")
            .Options;
        _db = new ApplicationDbContext(options);
        _audit = new RecordingAuditWriter();
        _service = new StudentService(_db, _audit);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _db.DisposeAsync();
    }

    [TestMethod]
    public async Task CreateAsync_NormalizesInputAndWritesAudit()
    {
        var id = await _service.CreateAsync(new CreateStudentRequest(
            "  ADM-100  ",
            "  Ada  ",
            "  Lovelace  ",
            new DateOnly(2012, 12, 10),
            " ada@example.test ",
            " 12345 ",
            " Test address "), CancellationToken.None);

        var saved = await _db.Students.SingleAsync(x => x.Id == id);
        Assert.AreEqual("ADM-100", saved.AdmissionNumber);
        Assert.AreEqual("Ada", saved.FirstName);
        Assert.AreEqual("Lovelace", saved.LastName);
        Assert.AreEqual("ada@example.test", saved.Email);
        Assert.AreEqual("12345", saved.Phone);
        Assert.AreEqual("Test address", saved.AddressLine);
        Assert.AreEqual("student.created", _audit.Events.Single().Action);
    }

    [TestMethod]
    public async Task CreateAsync_RejectsNullAdmissionNumberAsBadInput()
    {
        var request = new CreateStudentRequest(
            null!,
            "Ada",
            "Lovelace",
            new DateOnly(2012, 12, 10),
            null,
            null,
            null);

        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(() => _service.CreateAsync(request, CancellationToken.None));

        Assert.AreEqual("Admission number is required.", exception.Message);
        Assert.AreEqual(0, await _db.Students.CountAsync());
    }

    [TestMethod]
    public async Task CreateAsync_RejectsDuplicateAdmissionBeforeWrite()
    {
        _db.Students.Add(new Student
        {
            AdmissionNumber = "ADM-1",
            FirstName = "Existing",
            LastName = "Student",
            DateOfBirth = new DateOnly(2011, 1, 1)
        });
        await _db.SaveChangesAsync();

        var request = new CreateStudentRequest("ADM-1", "New", "Student", new DateOnly(2012, 1, 1), null, null, null);

        var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => _service.CreateAsync(request, CancellationToken.None));

        Assert.AreEqual("Admission number already exists.", exception.Message);
        Assert.AreEqual(1, await _db.Students.CountAsync());
    }

    [TestMethod]
    public async Task SearchAsync_RejectsResourceExhaustingQueryLength()
    {
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            _service.SearchAsync(new string('x', 121), null, null, 1, 25, CancellationToken.None));

        Assert.AreEqual("Search query cannot exceed 120 characters.", exception.Message);
    }

    [TestMethod]
    public async Task AddGuardianAsync_MakesOnlyRequestedGuardianPrimary()
    {
        var student = new Student
        {
            AdmissionNumber = "ADM-2",
            FirstName = "Grace",
            LastName = "Hopper",
            DateOfBirth = new DateOnly(2011, 2, 2)
        };
        var existing = new Guardian
        {
            Student = student,
            Name = "Existing Guardian",
            Relationship = "Parent",
            IsPrimary = true
        };
        _db.AddRange(student, existing);
        await _db.SaveChangesAsync();

        var createdId = await _service.AddGuardianAsync(student.Id, new UpsertGuardianRequest(
            " New Guardian ",
            " Parent ",
            " guardian@example.test ",
            " 999 ",
            true), CancellationToken.None);

        Assert.IsNotNull(createdId);
        var guardians = await _db.Guardians.OrderBy(x => x.Name).ToListAsync();
        Assert.AreEqual(2, guardians.Count);
        Assert.AreEqual(1, guardians.Count(x => x.IsPrimary));
        Assert.IsFalse(guardians.Single(x => x.Id == existing.Id).IsPrimary);
        var created = guardians.Single(x => x.Id == createdId);
        Assert.IsTrue(created.IsPrimary);
        Assert.AreEqual("New Guardian", created.Name);
        Assert.AreEqual("Parent", created.Relationship);
    }

    [TestMethod]
    public async Task AddGuardianAsync_RejectsOversizedNameBeforeMutation()
    {
        var student = new Student
        {
            AdmissionNumber = "ADM-3",
            FirstName = "Katherine",
            LastName = "Johnson",
            DateOfBirth = new DateOnly(2012, 3, 3)
        };
        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(() => _service.AddGuardianAsync(
            student.Id,
            new UpsertGuardianRequest(new string('x', 201), "Parent", null, null, false),
            CancellationToken.None));

        Assert.AreEqual("Guardian name cannot exceed 200 characters.", exception.Message);
        Assert.AreEqual(0, await _db.Guardians.CountAsync());
    }

    private sealed class RecordingAuditWriter : IAuditWriter
    {
        public List<AuditEvent> Events { get; } = [];

        public Task WriteAsync(string action, string entityType, string entityId, object? safeMetadata = null, CancellationToken cancellationToken = default)
        {
            Events.Add(new AuditEvent(action, entityType, entityId));
            return Task.CompletedTask;
        }
    }

    private sealed record AuditEvent(string Action, string EntityType, string EntityId);
}
