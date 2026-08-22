using CampusCore.Application.Reports;
using CampusCore.Domain.Entities;
using CampusCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CampusCore.Application.Tests;

[TestClass]
public sealed class ReportCardServiceTests
{
    [TestMethod]
    public async Task GetAsync_AggregatesAssessmentsAndUsesSelectedGradeScheme()
    {
        await using var db = CreateDb();
        var student = new Student
        {
            AdmissionNumber = "ADM-90",
            FirstName = "Ada",
            LastName = "Lovelace",
            DateOfBirth = new DateOnly(2012, 1, 1)
        };
        var year = new AcademicYear
        {
            Name = "2026-27",
            StartsOn = new DateOnly(2026, 4, 1),
            EndsOn = new DateOnly(2027, 3, 31)
        };
        var mathematics = new Subject { Code = "MATH", Name = "Mathematics" };
        var science = new Subject { Code = "SCI", Name = "Science" };
        db.AddRange(student, year, mathematics, science);
        await db.SaveChangesAsync();
        db.Marks.AddRange(
            new Mark { StudentId = student.Id, SubjectId = mathematics.Id, AcademicYearId = year.Id, AssessmentName = "Quiz", Score = 45, MaximumScore = 50 },
            new Mark { StudentId = student.Id, SubjectId = mathematics.Id, AcademicYearId = year.Id, AssessmentName = "Exam", Score = 40, MaximumScore = 50 },
            new Mark { StudentId = student.Id, SubjectId = science.Id, AcademicYearId = year.Id, AssessmentName = "Exam", Score = 8, MaximumScore = 10 });
        db.GradeScales.AddRange(
            new GradeScale { Name = "Default", MinimumPercentage = 80, MaximumPercentage = 100, Grade = "A" },
            new GradeScale { Name = "Default", MinimumPercentage = 0, MaximumPercentage = 79.99m, Grade = "B" },
            new GradeScale { Name = "Strict", MinimumPercentage = 90, MaximumPercentage = 100, Grade = "S" },
            new GradeScale { Name = "Strict", MinimumPercentage = 0, MaximumPercentage = 89.99m, Grade = "N" });
        await db.SaveChangesAsync();
        var service = new ReportCardService(db);

        var defaultReport = await service.GetAsync(student.Id, year.Id, CancellationToken.None);
        var strictReport = await service.GetAsync(student.Id, year.Id, "Strict", CancellationToken.None);

        Assert.IsNotNull(defaultReport);
        Assert.IsNotNull(strictReport);
        Assert.AreEqual(84.55m, defaultReport.OverallPercentage);
        Assert.AreEqual("A", defaultReport.OverallGrade);
        Assert.AreEqual("N", strictReport.OverallGrade);
        Assert.AreEqual(2, defaultReport.Subjects.Count);
        Assert.AreEqual(85m, defaultReport.Subjects.Single(x => x.SubjectCode == "MATH").Percentage);
        Assert.AreEqual(80m, defaultReport.Subjects.Single(x => x.SubjectCode == "SCI").Percentage);
    }

    [TestMethod]
    public async Task GetAsync_ReturnsNullForUnknownStudentOrYear()
    {
        await using var db = CreateDb();
        var service = new ReportCardService(db);

        Assert.IsNull(await service.GetAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"campuscore-report-card-tests-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }
}
