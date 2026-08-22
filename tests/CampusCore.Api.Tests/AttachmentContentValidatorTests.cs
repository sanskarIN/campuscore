using System.IO.Compression;
using System.Text;
using CampusCore.Api.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CampusCore.Api.Tests;

[TestClass]
public sealed class AttachmentContentValidatorTests
{
    [TestMethod]
    public void LooksValid_AcceptsStructuredDocxArchive()
    {
        var bytes = OpenXmlArchive("word/document.xml");

        Assert.IsTrue(AttachmentContentValidator.LooksValid(".docx", bytes));
    }

    [TestMethod]
    public void LooksValid_AcceptsStructuredXlsxArchive()
    {
        var bytes = OpenXmlArchive("xl/workbook.xml");

        Assert.IsTrue(AttachmentContentValidator.LooksValid(".xlsx", bytes));
    }

    [TestMethod]
    public void LooksValid_RejectsGenericZipRenamedAsOfficeDocument()
    {
        var bytes = Archive("payload.txt");

        Assert.IsFalse(AttachmentContentValidator.LooksValid(".docx", bytes));
        Assert.IsFalse(AttachmentContentValidator.LooksValid(".xlsx", bytes));
    }

    [TestMethod]
    public void LooksValid_RejectsOfficeArchiveWithWrongPrimaryPart()
    {
        var xlsx = OpenXmlArchive("xl/workbook.xml");
        var docx = OpenXmlArchive("word/document.xml");

        Assert.IsFalse(AttachmentContentValidator.LooksValid(".docx", xlsx));
        Assert.IsFalse(AttachmentContentValidator.LooksValid(".xlsx", docx));
    }

    [TestMethod]
    public void LooksValid_RecognizesSimpleSignatureTypes()
    {
        Assert.IsTrue(AttachmentContentValidator.LooksValid(".pdf", "%PDF-1.7\n"u8.ToArray()));
        Assert.IsTrue(AttachmentContentValidator.LooksValid(".jpg", new byte[] { 0xFF, 0xD8, 0xFF, 0x00 }));
        Assert.IsTrue(AttachmentContentValidator.LooksValid(".txt", Encoding.UTF8.GetBytes("safe text")));
        Assert.IsFalse(AttachmentContentValidator.LooksValid(".txt", new byte[] { 0x41, 0x00, 0x42 }));
        Assert.IsFalse(AttachmentContentValidator.LooksValid(".exe", "MZ"u8.ToArray()));
    }

    private static byte[] OpenXmlArchive(string primaryPart) =>
        Archive("[Content_Types].xml", "_rels/.rels", primaryPart);

    private static byte[] Archive(params string[] entries)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var name in entries)
            {
                var entry = archive.CreateEntry(name);
                using var content = new StreamWriter(entry.Open(), Encoding.UTF8, leaveOpen: false);
                content.Write("<test />");
            }
        }
        return stream.ToArray();
    }
}
