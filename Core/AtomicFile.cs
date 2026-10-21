using System.Text;

namespace UnattendLocal.Core;

// A failed write must not truncate an existing configuration. Stage in the
// destination directory so the final rename stays on the same filesystem.
public static class AtomicFile
{
    public static void WriteAllText(string destination, string text, Encoding encoding)
        => WriteAllBytes(destination, [.. encoding.GetPreamble(), .. encoding.GetBytes(text)]);

    public static void WriteAllBytes(string destination, byte[] bytes)
        => Write(destination, stream => stream.Write(bytes));

    internal static void Write(string destination, Action<FileStream> write)
    {
        string full = Path.GetFullPath(destination);
        string temporary = Path.Combine(Path.GetDirectoryName(full)!, ".unattend-save-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                write(output);
                output.Flush(flushToDisk: true);
            }
            // Do not fall back to deleting or copying over the original on failure.
            File.Move(temporary, full, overwrite: true);
        }
        finally
        {
            try { File.Delete(temporary); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
