using DiscUtils.Iso9660;

namespace UnattendLocal.Core;

// Export packaging never changes the validated XML or adds installation media.
public static class AnswerFileExport
{
    public static string FileName(bool manual) => manual ? "notautounattend.xml" : "autounattend.xml";
    public static string IsoName(bool manual) => Path.ChangeExtension(FileName(manual), ".iso");
    public static string Usage(bool manual) => manual
        ? global::UnattendLocal.Localization.UiText.Source("text.66cbc0734496")
        : global::UnattendLocal.Localization.UiText.Source("text.db75fd2bcffb");
    public static readonly string IsoDescription = global::UnattendLocal.Localization.UiText.Source("text.8d102d415bbe");

    public static void WriteXml(string destination, byte[] validatedXml) => AtomicFile.WriteAllBytes(destination, validatedXml);

    public static void WriteIso(string destination, byte[] validatedXml, bool manual)
    {
        ArgumentNullException.ThrowIfNull(validatedXml);
        // Joliet retains the full filename required by Windows Setup (beyond 8.3).
        var builder = new CDBuilder { UseJoliet = true, VolumeIdentifier = "UNATTENDLOCAL" };
        builder.AddFile(FileName(manual), validatedXml);
        using var image = builder.Build();
        AtomicFile.Write(destination, stream => image.CopyTo(stream));
    }
}
