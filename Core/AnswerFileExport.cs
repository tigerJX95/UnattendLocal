using DiscUtils.Iso9660;

namespace UnattendLocal.Core;

// Export packaging never changes the validated XML or adds installation media.
public static class AnswerFileExport
{
    public static string FileName(bool manual) => manual ? "notautounattend.xml" : "autounattend.xml";
    public static string IsoName(bool manual) => Path.ChangeExtension(FileName(manual), ".iso");
    public static string Usage(bool manual) => manual
        ? "notautounattend.xml 不以默认文件名参与安装自动发现。请在 Windows PE 中按实际盘符运行，例如：setup.exe /Unattend:D:\\notautounattend.xml。重命名或显式调用后仍会执行其中的安装和磁盘操作。"
        : "autounattend.xml 位于介质根目录时，Windows 安装程序可能自动读取。使用前须核对全部配置及磁盘操作。";
    public const string IsoDescription = "ISO 仅包含根目录下的一份完整应答 XML，不包含 Windows 镜像，也不能独立启动。虚拟机从 Windows 安装介质启动，并将此 ISO 挂载为另一台光驱；实际盘符由 Windows PE 决定。ISO 与 XML 一样可能包含可还原凭据。";

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
