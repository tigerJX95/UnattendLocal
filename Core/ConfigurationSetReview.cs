using UnattendLocal.Localization;

namespace UnattendLocal.Core;

public sealed record ConfigurationSetFile(string Source, string? Destination);

public sealed record ConfigurationSetReview(IReadOnlyList<ConfigurationSetFile> Files, int FileCount,
    int Skipped, bool Limited)
{
    public const int MaxEntries = 2000;
    public const int MaxRows = 500;
    private const int MaxDepth = 32;

    public static string PlanScope(InstallPlan plan)
    {
        if (plan.ImportedXml is not null)
            return UiText.Source("oemReview.plan.imported");

        return UiText.Source((plan.UseGeneratedPe, plan.UseConfigurationSet) switch
        {
            (true, true) => "oemReview.plan.peEnabled",
            (true, false) => "oemReview.plan.peDisabled",
            (false, true) => "oemReview.plan.setupEnabled",
            _ => "oemReview.plan.setupDisabled"
        });
    }

    public static string? Destination(string relativePath)
    {
        var parts = relativePath.Replace('/', '\\').Split('\\');
        if (parts.Length < 2 || parts.Skip(1).Any(InvalidWindowsName)) return null;
        string? root = parts[0] switch
        {
            "$1" => "%SystemDrive%",
            "$$" => "%WINDIR%",
            var drive when drive.Length == 1 && char.IsAsciiLetter(drive[0]) => drive.ToUpperInvariant() + ":",
            _ => null
        };
        return root is null ? null : root + "\\" + string.Join("\\", parts.Skip(1));
    }

    private static bool InvalidWindowsName(string part)
    {
        if (part.Length == 0 || part.EndsWith(' ') || part.EndsWith('.') ||
            part.Any(c => c < ' ' || c is ':' or '*' or '?' or '"' or '<' or '>' or '|'))
            return true;
        int dot = part.IndexOf('.');
        string stem = (dot < 0 ? part : part[..dot]).TrimEnd(' ').ToUpperInvariant();
        return stem is "CON" or "PRN" or "AUX" or "NUL" or "CONIN$" or "CONOUT$" ||
            stem.Length == 4 && stem[..3] is ("COM" or "LPT") && stem[3] is (>= '1' and <= '9' or '¹' or '²' or '³');
    }

    public static ConfigurationSetReview Read(string directory, CancellationToken cancellationToken = default)
    {
        var root = new DirectoryInfo(directory);
        if (!root.Name.Equals("$OEM$", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(UiText.Source("oemReview.invalidFolder"));
        var attributes = File.GetAttributes(root.FullName);
        if (!attributes.HasFlag(FileAttributes.Directory) || attributes.HasFlag(FileAttributes.ReparsePoint))
            throw new ArgumentException(UiText.Source("oemReview.invalidFolder"));

        var pending = new Stack<(string Path, int Depth)>();
        pending.Push((root.FullName, 0));
        var rows = new List<ConfigurationSetFile>();
        int visited = 0, files = 0, skipped = 0;
        bool limited = false;
        while (pending.TryPop(out var folder))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                // Recheck queued directories: a path may have changed since enumeration.
                if (File.GetAttributes(folder.Path).HasFlag(FileAttributes.ReparsePoint))
                { skipped++; continue; }
                foreach (string path in Directory.EnumerateFileSystemEntries(folder.Path))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (++visited > MaxEntries) { limited = true; pending.Clear(); break; }
                    try
                    {
                        var flags = File.GetAttributes(path);
                        if (flags.HasFlag(FileAttributes.ReparsePoint)) { skipped++; continue; }
                        if (flags.HasFlag(FileAttributes.Directory))
                        {
                            if (folder.Depth >= MaxDepth) { skipped++; limited = true; }
                            else pending.Push((path, folder.Depth + 1));
                            continue;
                        }
                        files++;
                        if (rows.Count < MaxRows)
                        {
                            string relative = Path.GetRelativePath(root.FullName, path);
                            rows.Add(new(relative, Destination(relative)));
                        }
                    }
                    catch (Exception error) when (error is IOException or UnauthorizedAccessException) { skipped++; }
                }
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                // A failed root must not look like a successfully reviewed empty directory.
                if (folder.Depth == 0) throw;
                skipped++;
            }
        }
        return new(rows.OrderBy(r => r.Source, StringComparer.OrdinalIgnoreCase).ToArray(), files, skipped, limited);
    }

    public string Describe()
    {
        var lines = new List<string>
        {
            UiText.Source("oemReview.scope"),
            UiText.Source("oemReview.mapping"),
            UiText.Format("oemReview.count", UiText.Arg(FileCount), UiText.Arg(Files.Count), UiText.Arg(Skipped))
        };
        if (Limited || FileCount > Files.Count || Skipped > 0) lines.Add(UiText.Source("oemReview.partial"));
        if (FileCount == 0) lines.Add(UiText.Source("oemReview.empty"));
        lines.Add("");
        foreach (var file in Files)
            lines.Add(UiText.Format("oemReview.row", UiText.Arg(ReviewDisplay.Field(file.Source)),
                file.Destination is {} target ? UiText.Arg(ReviewDisplay.Field(target)) : UiText.Source("oemReview.unmapped")));
        return UiText.Join("\n", lines);
    }
}
