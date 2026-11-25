using UnattendLocal.Localization;

namespace UnattendLocal.Core;

public sealed record SetupLogPreview(string Text, bool Truncated)
{
    private const int MaxBytes = 256 * 1024;
    private static readonly string[] SupportedNames =
    [
        "setupact.log", "setuperr.log", "setupapi.dev.log", "setupapi.offline.log", "cbs_unattend.log",
        "DISM.log", "CBS.log", "OfflineUpdates.log"
    ];

    public static string FilePatterns { get; } = string.Join(';', SupportedNames);

    public static SetupLogPreview Read(string path)
    {
        if (!SupportedNames.Contains(Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException(UiText.Source("setupLogs.unsupported"));

        using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        long length = input.Length;
        Span<byte> header = stackalloc byte[(int)Math.Min(length, 4)];
        input.ReadExactly(header);
        var encoding = TextFileImport.DetectEncoding(header, out int bomLength);
        bool truncated = length - bomLength > MaxBytes;
        var buffer = new byte[(int)Math.Min(length - bomLength, MaxBytes)];
        long start = length - buffer.Length;

        var lineFeed = encoding.GetBytes("\n");
        var carriageReturn = encoding.GetBytes("\r");
        int width = lineFeed.Length;
        int alignment = (int)((start - bomLength) % width);
        bool startsOnNewLine = !truncated;
        bool afterCarriageReturn = false;
        if (truncated && alignment == 0 && start - bomLength >= width)
        {
            Span<byte> previous = stackalloc byte[width];
            input.Position = start - width;
            input.ReadExactly(previous);
            afterCarriageReturn = previous.SequenceEqual(carriageReturn);
            startsOnNewLine = afterCarriageReturn || previous.SequenceEqual(lineFeed);
        }

        input.Position = start;
        input.ReadExactly(buffer);

        ReadOnlySpan<byte> content = buffer;
        if (!startsOnNewLine)
        {
            // Skip the first, possibly incomplete line. For fixed-width Unicode,
            // look for the delimiter on a code-unit boundary relative to the BOM.
            int offset = (width - alignment) % width;
            while (offset <= buffer.Length - width)
            {
                var unit = content.Slice(offset, width);
                afterCarriageReturn = unit.SequenceEqual(carriageReturn);
                if (afterCarriageReturn || unit.SequenceEqual(lineFeed)) break;
                offset += width;
            }
            content = offset <= buffer.Length - width ? content[(offset + width)..] : [];
        }
        if (afterCarriageReturn && content.StartsWith(lineFeed))
            content = content[width..];

        string text = TextFileImport.Decode(content, encoding);
        if (text.Contains('\0'))
            throw new ArgumentException(UiText.Source("setupLogs.invalidText"));
        return new SetupLogPreview(text, truncated);
    }

    public string Filter(string query) => Filter(query, out _);

    public string Filter(string query, out int? matchingLines)
    {
        matchingLines = null;
        string term = query.Trim();
        if (term.Length == 0) return Text;

        using var reader = new StringReader(Text);
        var matches = new List<string>();
        while (reader.ReadLine() is { } line)
            if (line.Contains(term, StringComparison.OrdinalIgnoreCase))
                matches.Add(line);
        matchingLines = matches.Count;
        return string.Join(Environment.NewLine, matches);
    }
}
