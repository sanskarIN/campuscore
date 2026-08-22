using CampusCore.Domain.Entities;

namespace CampusCore.Domain.Tests;

[TestClass]
public sealed class StudentTests
{
    [TestMethod]
    public void DisplayName_TrimsAndCombinesNames()
    {
        var student = new Student { FirstName = "  Ada ", LastName = " Lovelace  " };

        Assert.AreEqual("Ada Lovelace", student.DisplayName);
    }

    [TestMethod]
    public void DisplayName_ToleratesMissingRuntimeNameValues()
    {
        var student = new Student { FirstName = null!, LastName = null! };

        Assert.AreEqual(string.Empty, student.DisplayName);
    }

    [TestMethod]
    public void Validate_RejectsMissingAdmissionNumber()
    {
        var student = ValidStudent();
        student.AdmissionNumber = "   ";

        AssertThrowsArgument(student.Validate, "Admission number is required.");
    }

    [TestMethod]
    public void Validate_RejectsMissingFirstName()
    {
        var student = ValidStudent();
        student.FirstName = string.Empty;

        AssertThrowsArgument(student.Validate, "First name is required.");
    }

    [TestMethod]
    public void Validate_RejectsFutureDateOfBirth()
    {
        var student = ValidStudent();
        student.DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        AssertThrowsArgument(student.Validate, "Date of birth cannot be in the future.");
    }

    [DataTestMethod]
    [DataRow("AdmissionNumber", 65, "Admission number cannot exceed 64 characters.")]
    [DataRow("FirstName", 121, "First name cannot exceed 120 characters.")]
    [DataRow("LastName", 121, "Last name cannot exceed 120 characters.")]
    [DataRow("Email", 255, "Email cannot exceed 254 characters.")]
    [DataRow("Phone", 41, "Phone cannot exceed 40 characters.")]
    public void Validate_RejectsPersistedValuesBeyondConfiguredLength(string property, int length, string message)
    {
        var student = ValidStudent();
        typeof(Student).GetProperty(property)!.SetValue(student, new string('x', length));

        AssertThrowsArgument(student.Validate, message);
    }

    [TestMethod]
    public void Validate_AcceptsValidStudent()
    {
        var student = ValidStudent();

        student.Validate();

        Assert.IsTrue(student.IsActive);
    }

    private static Student ValidStudent() => new()
    {
        AdmissionNumber = "ADM-001",
        FirstName = "Ada",
        LastName = "Lovelace",
        DateOfBirth = new DateOnly(2012, 12, 10)
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
