namespace IAMS.Application.Findings;

public static class EvidenceFileRules
{
    public const long MaxSizeBytes = 10 * 1024 * 1024; // 10 MB

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

    public static bool IsContentTypeAllowed(string contentType) => Allowed.ContainsKey(contentType);

    /// <summary>Returns the canonical extension for a content type, or null if not allowed.</summary>
    public static string? ExtensionFor(string contentType)
        => Allowed.TryGetValue(contentType, out var ext) ? ext : null;

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