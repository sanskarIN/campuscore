using CampusCore.Domain.Entities;

namespace CampusCore.Domain.Tests;

[TestClass]
public sealed class AcademicTests
{
    [TestMethod]
    public void AcademicYearValidate_RejectsBlankName()
    {
        var year = ValidYear();
        year.Name = " ";

        AssertThrowsArgument(year.Validate, "Academic year name is required.");
    }

    [TestMethod]
    public void AcademicYearValidate_RejectsNonIncreasingDateRange()
    {
        var year = ValidYear();
        year.EndsOn = year.StartsOn;

        AssertThrowsArgument(year.Validate, "Academic year end date must be after its start date.");
    }

    [TestMethod]
    public void AcademicYearValidate_AcceptsValidRange()
    {
        var year = ValidYear();

        year.Validate();

        Assert.IsTrue(year.IsActive);
    }

    [TestMethod]
    public void MarkPercentage_RoundsToTwoDecimalPlaces()
    {
        var mark = new Mark { Score = 2m, MaximumScore = 3m };

        Assert.AreEqual(66.67m, mark.Percentage);
    }

    [TestMethod]
    public void MarkPercentage_ReturnsZeroForInvalidMaximum()
    {
        var zero = new Mark { Score = 10m, MaximumScore = 0m };
        var negative = new Mark { Score = 10m, MaximumScore = -1m };

        Assert.AreEqual(0m, zero.Percentage);
        Assert.AreEqual(0m, negative.Percentage);
    }

    private static AcademicYear ValidYear() => new()
    {
        Name = "2026-27",
        StartsOn = new DateOnly(2026, 4, 1),
        EndsOn = new DateOnly(2027, 3, 31),
        IsActive = true
    };

    private static void AssertThrowsArgument(Action action, string expectedMessage)
    {
        try
        {
            action();
            Assert.Fail("Expected ArgumentException was not thrown.");
        }
        catch (ArgumentException exception)
        {
            Assert.AreEqual(expectedMessage, exception.Message);
        }
    }
}
