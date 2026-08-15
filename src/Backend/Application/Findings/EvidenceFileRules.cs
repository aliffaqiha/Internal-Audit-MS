namespace IAMS.Application.Findings;

public static class EvidenceFileRules
{
    public const long MaxSizeBytes = 10 * 1024 * 1024; // 10 MB

    /// <summary>File-format family detected from magic bytes.</summary>
    public enum FileFamily
    {
        Unknown,
        Pdf,
        Png,
        Jpeg,
        OfficeZip,   // .xlsx / .docx (PK/ZIP container)
        OfficeOle2   // .xls / .doc (OLE2 compound document)
    }

    private static readonly Dictionary<string, string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        ["application/pdf"] = ".pdf",
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] = ".xlsx",
        ["application/vnd.ms-excel"] = ".xls",
        ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = ".docx",
        ["application/msword"] = ".doc"
    };

    private static readonly Dictionary<string, FileFamily> ContentTypeFamily = new(StringComparer.OrdinalIgnoreCase)
    {
        ["application/pdf"] = FileFamily.Pdf,
        ["image/png"] = FileFamily.Png,
        ["image/jpeg"] = FileFamily.Jpeg,
        ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] = FileFamily.OfficeZip,
        ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = FileFamily.OfficeZip,
        ["application/vnd.ms-excel"] = FileFamily.OfficeOle2,
        ["application/msword"] = FileFamily.OfficeOle2
    };

    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] Ole2Signature = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };

    public static bool IsContentTypeAllowed(string contentType) => Allowed.ContainsKey(contentType);

    /// <summary>Returns the canonical extension for a content type, or null if not allowed.</summary>
    public static string? ExtensionFor(string contentType)
        => Allowed.TryGetValue(contentType, out var ext) ? ext : null;

    /// <summary>
    /// Reads the magic bytes of the uploaded stream and returns the detected file family.
    /// The stream is rewound to its original position so it can be uploaded afterwards.
    /// Returns <see cref="FileFamily.Unknown"/> for unrecognized content.
    /// </summary>
    public static FileFamily SniffFileFamily(Stream content)
    {
        var originalPosition = content.Position;
        var header = new byte[8];
        var read = 0;
        try
        {
            read = content.Read(header, 0, header.Length);
        }
        finally
        {
            content.Position = originalPosition;
        }

        if (read >= 5 && header[0] == '%' && header[1] == 'P' && header[2] == 'D' && header[3] == 'F' && header[4] == '-')
            return FileFamily.Pdf;

        if (read >= 8 && header.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature))
            return FileFamily.Png;

        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return FileFamily.Jpeg;

        if (read >= 2 && header[0] == 0x50 && header[1] == 0x4B)
            return FileFamily.OfficeZip;

        if (read >= 8 && header.AsSpan(0, Ole2Signature.Length).SequenceEqual(Ole2Signature))
            return FileFamily.OfficeOle2;

        return FileFamily.Unknown;
    }

    /// <summary>Whether the declared content type matches the family detected from the file bytes.</summary>
    public static bool MatchesFamily(string contentType, FileFamily family)
        => ContentTypeFamily.TryGetValue(contentType, out var expected) && expected == family;

    /// <summary>
    /// Sanitizes an uploaded filename: strips any directory path and path-traversal
    /// attempts, removes control/invalid characters, and falls back to a safe name.
    /// </summary>
    public static string SanitizeFileName(string? fileName)
    {
        var insecure = new[] { "..", "/", "\\", ":", "*", "?", "\"", "<", ">", "|" };
        var safe = (fileName ?? "evidence").Trim()
            .Split('\\', '/')
            .Last();

        foreach (var token in insecure)
            safe = safe.Replace(token, "_", StringComparison.Ordinal);

        safe = new string(safe.Where(c => !char.IsControl(c)).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(safe) ? "evidence.bin" : safe;
    }
}