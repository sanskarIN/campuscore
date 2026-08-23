using CampusCore.Api.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CampusCore.Api.Tests;

[TestClass]
public sealed class ProductionConfigurationValidatorTests
{
    [TestMethod]
    public void Validate_IgnoresProductionRulesOutsideProduction()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>());

        ProductionConfigurationValidator.Validate(configuration, isProduction: false);
    }

    [TestMethod]
    public void Validate_AcceptsExplicitProductionConfiguration()
    {
        var configuration = BuildConfiguration(ValidProductionValues());

        ProductionConfigurationValidator.Validate(configuration, isProduction: true);
    }

    [TestMethod]
    public void Validate_RejectsDevelopmentJwtKey()
    {
        var values = ValidProductionValues();
        values["Jwt:Key"] = "campuscore-development-only-not-for-production-1234567890";

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(BuildConfiguration(values), isProduction: true));

        StringAssert.Contains(exception.Message, "Jwt:Key");
    }

    [TestMethod]
    public void Validate_RejectsLocalDatabasePasswordPlaceholder()
    {
        var values = ValidProductionValues();
        values["ConnectionStrings:Database"] = "Host=db;Database=campuscore;Username=campuscore;Password=campuscore-local-only";

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(BuildConfiguration(values), isProduction: true));

        StringAssert.Contains(exception.Message, "ConnectionStrings:Database");
    }

    [TestMethod]
    public void Validate_RejectsWildcardAllowedHosts()
    {
        var values = ValidProductionValues();
        values["AllowedHosts"] = "*";

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(BuildConfiguration(values), isProduction: true));

        StringAssert.Contains(exception.Message, "AllowedHosts");
    }

    [TestMethod]
    public void Validate_AllowsBootstrapKeyToBeRemovedAfterInitialSetup()
    {
        var values = ValidProductionValues();
        values["BootstrapAdmin:Key"] = string.Empty;

        ProductionConfigurationValidator.Validate(BuildConfiguration(values), isProduction: true);
    }

    [TestMethod]
    public void Validate_RejectsConfiguredDevelopmentBootstrapKey()
    {
        var values = ValidProductionValues();
        values["BootstrapAdmin:Key"] = "campuscore-local-bootstrap-only";

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(BuildConfiguration(values), isProduction: true));

        StringAssert.Contains(exception.Message, "BootstrapAdmin:Key");
    }

    private static Dictionary<string, string?> ValidProductionValues() => new()
    {
        ["Jwt:Key"] = "A-production-jwt-key-with-more-than-thirty-two-characters",
        ["ConnectionStrings:Database"] = "Host=db;Database=campuscore;Username=campuscore;Password=a-strong-production-password",
        ["AllowedHosts"] = "campus.example.edu",
        ["BootstrapAdmin:Key"] = "A-production-bootstrap-key"
    };

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
