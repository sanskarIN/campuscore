using System.Text;
using CampusCore.Application.Reports;
using CampusCore.Domain.Entities;
using CampusCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CampusCore.Application.Tests;

[TestClass]
public sealed class ReportServiceTests
{
    [TestMethod]
    public async Task ExportStudentsCsvAsync_NeutralizesSpreadsheetFormulaPrefixes()
    {
        await using var db = CreateDb();
        db.Students.Add(new Student
        {
            AdmissionNumber = "=2+2",
            FirstName = "+SUM(A1:A2)",
            LastName = "  @danger",
            DateOfBirth = new DateOnly(2012, 1, 2),
            Email = "-1+1@example.test",
            Phone = "123",
            IsActive = true
        });
        await db.SaveChangesAsync();
        var service = new ReportService(db);

        var bytes = await service.ExportStudentsCsvAsync(CancellationToken.None);
        var csv = Encoding.UTF8.GetString(bytes);

        StringAssert.Contains(csv, "\"'=2+2\"");
        StringAssert.Contains(csv, "\"'+SUM(A1:A2)\"");
        StringAssert.Contains(csv, "\"'  @danger\"");
        StringAssert.Contains(csv, "\"'-1+1@example.test\"");
    }

    [TestMethod]
    public async Task ExportStudentsCsvAsync_EscapesQuotesAndKeepsUtf8Bom()
    {
        await using var db = CreateDb();
        db.Students.Add(new Student
        {
            AdmissionNumber = "ADM-1",
            FirstName = "Ada \"Countess\"",
            LastName = "Lovelace",
            DateOfBirth = new DateOnly(2012, 1, 2),
            IsActive = true
        });
        await db.SaveChangesAsync();
        var service = new ReportService(db);

        var bytes = await service.ExportStudentsCsvAsync(CancellationToken.None);
        var preamble = Encoding.UTF8.GetPreamble();
        var csv = Encoding.UTF8.GetString(bytes);

        CollectionAssert.AreEqual(preamble, bytes[..preamble.Length]);
        StringAssert.Contains(csv, "\"Ada \"\"Countess\"\"\"");
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"campuscore-report-tests-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }
}
