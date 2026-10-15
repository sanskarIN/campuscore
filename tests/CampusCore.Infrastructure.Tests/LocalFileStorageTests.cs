using System.Text;
using CampusCore.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CampusCore.Infrastructure.Tests;

[TestClass]
public sealed class LocalFileStorageTests
{
    private string _root = null!;
    private LocalFileStorage _storage = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "CampusCore.Tests", Guid.NewGuid().ToString("N"));
        var configuration = new ConfigurationManager
        {
            ["Storage:RootPath"] = _root
        };
        _storage = new LocalFileStorage(configuration);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public async Task SaveAndOpenAsync_RoundTripsBytesWithOpaqueNormalizedName()
    {
        var expected = Encoding.UTF8.GetBytes("fictional CampusCore attachment");
        await using var input = new MemoryStream(expected);

        var storedName = await _storage.SaveAsync(input, "PDF");

        Assert.IsTrue(storedName.EndsWith(".pdf", StringComparison.Ordinal));
        Assert.AreEqual(36, storedName.Length);
        Assert.IsFalse(storedName.Contains("CampusCore", StringComparison.OrdinalIgnoreCase));

        await using var result = await _storage.OpenReadAsync(storedName);
        Assert.IsNotNull(result);
        using var output = new MemoryStream();
        await result.CopyToAsync(output);
        CollectionAssert.AreEqual(expected, output.ToArray());
    }

    [TestMethod]
    public async Task DeleteAsync_RemovesStoredFile()
    {
        await using var input = new MemoryStream([1, 2, 3]);
        var storedName = await _storage.SaveAsync(input, ".txt");

        await _storage.DeleteAsync(storedName);

        var reopened = await _storage.OpenReadAsync(storedName);
        Assert.IsNull(reopened);
    }

    [DataTestMethod]
    [DataRow("../outside.txt")]
    [DataRow("folder/file.txt")]
    [DataRow("folder\\file.txt")]
    [DataRow("")]
    [DataRow("   ")]
    public async Task OpenReadAsync_RejectsNonOpaqueStoredNames(string storedName)
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
        {
            await _storage.OpenReadAsync(storedName);
        });
    }

    [DataTestMethod]
    [DataRow(".tar/gz")]
    [DataRow(".way-too-long-extension")]
    [DataRow(".pdf?")]
    public async Task SaveAsync_RejectsUnsafeExtension(string extension)
    {
        await using var input = new MemoryStream([1]);

        await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
        {
            await _storage.SaveAsync(input, extension);
        });
    }

    [TestMethod]
    public async Task OpenReadAsync_ReturnsNullWhenOpaqueFileDoesNotExist()
    {
        var result = await _storage.OpenReadAsync($"{Guid.NewGuid():N}.pdf");

        Assert.IsNull(result);
    }
}
