using System.Text;
using System.Text.Json;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Localization;

static class EmbeddedBackgroundTests
{
    private const string Png = "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAAAAABX3VL4AAAADklEQVR42mOsZ2BiYAAAAo0AgyB/q8kAAAAASUVORK5CYII=";
    private const string Jpeg = "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/wAALCAACAAIBAREA/8QAFAABAAAAAAAAAAAAAAAAAAAAAf/EABQQAQAAAAAAAAAAAAAAAAAAAAD/2gAIAQEAAD8AH//Z";

    public static int Run(GeneratorService generator)
    {
        int count = 0;
        void Check(bool condition, string scenario)
        {
            if (!condition) throw new Exception("Embedded backgrounds: " + scenario);
            count++;
        }

        string directory = Directory.CreateTempSubdirectory("ul-background-").FullName;
        try
        {
            string pngPath = Path.Combine(directory, "wallpaper.png");
            string jpegPath = Path.Combine(directory, "lockscreen.jpeg");
            File.WriteAllBytes(pngPath, Convert.FromBase64String(Png));
            File.WriteAllBytes(jpegPath, Convert.FromBase64String(Jpeg));
            string wallpaper = ImageBackgroundSettings.ReadEmbeddedImage(pngPath);
            string lockScreen = ImageBackgroundSettings.ReadEmbeddedImage(jpegPath);
            Check(ImageBackgroundSettings.IsEmbeddedImageScript(wallpaper) &&
                ImageBackgroundSettings.IsEmbeddedImageScript(lockScreen), "PNG and JPEG recognized");
            Check(ImageBackgroundSettings.IsEmbeddedImageScript(wallpaper.Replace("\n", "\r\n")),
                "recognition accepts Windows line endings");
            Check(!ImageBackgroundSettings.IsEmbeddedImageScript(wallpaper + "\nWrite-Output 'custom'"),
                "custom edits remain visible");

            string invalid = Path.Combine(directory, "invalid.png");
            void Reject(byte[] bytes, string key)
            {
                File.WriteAllBytes(invalid, bytes);
                try { ImageBackgroundSettings.ReadEmbeddedImage(invalid); }
                catch (ArgumentException error) when (error.Message == UiText.Source(key)) { count++; return; }
                throw new Exception("Embedded backgrounds: expected " + key);
            }
            Reject([], "image.embed.size");
            Reject(new byte[ImageBackgroundSettings.MaxEmbeddedImageBytes + 1], "image.embed.size");
            Reject(Encoding.ASCII.GetBytes("not a PNG"), "image.embed.format");
            byte[] atLimit = Convert.FromBase64String(Png);
            Array.Resize(ref atLimit, ImageBackgroundSettings.MaxEmbeddedImageBytes);
            File.WriteAllBytes(invalid, atLimit);
            Check(ImageBackgroundSettings.IsEmbeddedImageScript(ImageBackgroundSettings.ReadEmbeddedImage(invalid)),
                "exactly 1 MiB stays within the script limit");
            File.Delete(pngPath);
            File.Delete(jpegPath);

            var plan = QuickPresets.Create(QuickPresets.LocalAccount);
            plan.WallpaperMode = WallpaperSettings.Script;
            plan.WallpaperImageScript = wallpaper;
            plan.LockScreenMode = ImageBackgroundSettings.LockScript;
            plan.LockScreenImageScript = lockScreen;
            var loadedPlan = PlanStore.Deserialize(PlanStore.Serialize(plan, true));
            Check(loadedPlan.WallpaperImageScript == wallpaper && loadedPlan.LockScreenImageScript == lockScreen,
                "JSON saves both loaders after the source files are removed");

            byte[] exported = generator.Generate(loadedPlan);
            string xml = Encoding.UTF8.GetString(exported);
            var doc = XmlMerge.Read(xml);
            Check(ImageBackgroundSettings.Inspect(doc, false).Script?.Trim() == wallpaper &&
                ImageBackgroundSettings.Inspect(doc, true).Script?.Trim() == lockScreen &&
                !xml.Contains(pngPath) && !xml.Contains(jpegPath),
                "export includes both complete loaders without the original paths");
            var comment = doc.DocumentElement!.ChildNodes.OfType<XmlComment>().Single(c => c.Value!.StartsWith("UnattendLocal:v1 "));
            using var snapshot = JsonDocument.Parse(Convert.FromBase64String(comment.Value!.Split(' ', 3)[2]));
            Check(snapshot.RootElement.GetProperty(nameof(InstallPlan.WallpaperImageScript)).GetString() == "" &&
                snapshot.RootElement.GetProperty(nameof(InstallPlan.LockScreenImageScript)).GetString() == "",
                "XML metadata does not duplicate image payloads");

            var imported = XmlPlanImport.Import(xml, generator);
            Check(imported.WallpaperMode == WallpaperSettings.Script && imported.LockScreenMode == ImageBackgroundSettings.LockScript &&
                imported.WallpaperImageScript.Trim() == wallpaper && imported.LockScreenImageScript.Trim() == lockScreen,
                "XML import restores both images from their loaders");
            imported.WallpaperImageScript = lockScreen;
            var replaced = XmlMerge.Read(Encoding.UTF8.GetString(generator.Generate(imported)));
            Check(ImageBackgroundSettings.Inspect(replaced, false).Script?.Trim() == lockScreen &&
                ImageBackgroundSettings.Inspect(replaced, true).Script?.Trim() == lockScreen, "reimported wallpaper can be replaced independently");

            foreach (bool english in new[] { false, true })
            {
                string preview = GeneratorService.Preview(exported, english: english);
                string marker = english ? "Embedded image data omitted from preview" : "内嵌图片数据仅在预览中省略";
                Check(preview.Contains(marker) && !preview.Contains(Png[..30]) && !preview.Contains(Jpeg[..30]),
                    "preview replaces the embedded data in both languages");
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }

        return count;
    }
}
