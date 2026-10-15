using System.IO.Compression;

namespace CampusCore.Api.Validation;

public static class AttachmentContentValidator
{
    public static bool LooksValid(string extension, ReadOnlyMemory<byte> content)
    {
        var bytes = content.Span;
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => bytes.StartsWith("%PDF-"u8),
            ".png" => bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".jpg" or ".jpeg" => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
            ".docx" => LooksLikeOpenXml(content, "word/document.xml"),
            ".xlsx" => LooksLikeOpenXml(content, "xl/workbook.xml"),
            ".txt" or ".csv" => !bytes.Contains((byte)0),
            _ => false
        };
    }

    private static bool LooksLikeOpenXml(ReadOnlyMemory<byte> content, string requiredDocumentPart)
    {
        if (content.Length < 4 || content.Span[0] != 0x50 || content.Span[1] != 0x4B) return false;
        try
        {
            using var stream = new MemoryStream(content.ToArray(), writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            if (archive.Entries.Count == 0 || archive.Entries.Count > 10_000) return false;
            return archive.GetEntry("[Content_Types].xml") is not null &&
                   archive.GetEntry("_rels/.rels") is not null &&
                   archive.GetEntry(requiredDocumentPart) is not null;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }
}
