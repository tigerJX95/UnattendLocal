using System.Text;

namespace UnattendLocal.Core;

// Read from one open handle, enforce the byte limit throughout, and never
// replace malformed input with U+FFFD. Preserve all decoded text verbatim.
public static class TextFileImport
{
    public static string Read(string path, long maxBytes, string sizeError)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);
        if (maxBytes > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(maxBytes));
        using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (input.Length > maxBytes) throw new ArgumentException(sizeError);
        using var bytes = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = input.Read(buffer, 0, (int)Math.Min(buffer.Length, maxBytes - bytes.Length + 1))) > 0)
        {
            if (bytes.Length + read > maxBytes) throw new ArgumentException(sizeError);
            bytes.Write(buffer, 0, read);
        }
        return Decode(bytes.GetBuffer().AsSpan(0, (int)bytes.Length));
    }

    private static string Decode(ReadOnlySpan<byte> bytes)
    {
        Encoding encoding = new UTF8Encoding(false, true);
        int skip = 0;
        // UTF-32 LE must be checked before UTF-16 LE (same first two bytes).
        if (bytes.StartsWith(new byte[] { 0xff, 0xfe, 0, 0 })) { encoding = new UTF32Encoding(false, true, true); skip = 4; }
        else if (bytes.StartsWith(new byte[] { 0, 0, 0xfe, 0xff })) { encoding = new UTF32Encoding(true, true, true); skip = 4; }
        else if (bytes.StartsWith(new byte[] { 0xef, 0xbb, 0xbf })) skip = 3;
        else if (bytes.StartsWith(new byte[] { 0xff, 0xfe })) { encoding = new UnicodeEncoding(false, true, true); skip = 2; }
        else if (bytes.StartsWith(new byte[] { 0xfe, 0xff })) { encoding = new UnicodeEncoding(true, true, true); skip = 2; }
        try { return encoding.GetString(bytes[skip..]); }
        catch (DecoderFallbackException)
        {
            throw new ArgumentException("文件编码无效，无法完整读取。\n\n请另存为 UTF-8，或带 BOM 的 UTF-16 / UTF-32 后重新导入；ANSI / GBK 等编码需先转换。");
        }
    }
}
