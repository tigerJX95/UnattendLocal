using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using UnattendLocal.Localization;

namespace UnattendLocal.Core;

public sealed record AppLockerEventRow(int Id, string Outcome, string Time, string File);

public sealed record AppLockerEventReview(int Allowed, int Audit, int Blocked, int Errors, int Other,
    int Ignored, IReadOnlyList<AppLockerEventRow> Rows)
{
    public const int MaxBytes = 4 * 1024 * 1024;
    public const int MaxEvents = 20000;
    public const int MaxRows = 500;
    private static readonly XNamespace EventNamespace = "http://schemas.microsoft.com/win/2004/08/events/event";

    public static AppLockerEventReview Read(string path, CancellationToken cancellationToken = default) =>
        Parse(TextFileImport.Read(path, MaxBytes, UiText.Source("eventReview.tooLarge")), cancellationToken);

    public static AppLockerEventReview Parse(string xml, CancellationToken cancellationToken = default)
    {
        if (xml.Length > MaxBytes) throw new ArgumentException(UiText.Source("eventReview.tooLarge"));
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null,
            MaxCharactersInDocument = MaxBytes, MaxCharactersFromEntities = 0
        };
        // Check tree complexity before materializing it. No DTD, resolver, or file execution.
        using (var reader = XmlReader.Create(new StringReader(xml), settings))
        {
            int elements = 0;
            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (reader.Depth > 32 || reader.NodeType == XmlNodeType.Element && ++elements > 100000)
                    throw new ArgumentException(UiText.Source("eventReview.tooComplex"));
            }
        }
        using var safeReader = XmlReader.Create(new StringReader(xml), settings);
        var root = XDocument.Load(safeReader).Root;
        XElement[] events;
        if (root?.Name == EventNamespace + "Event") events = [root];
        else if (root is not null && root.Name.LocalName == "Events" &&
            (root.Name.Namespace == XNamespace.None || root.Name.Namespace == EventNamespace))
        {
            events = root.Elements().Take(MaxEvents + 1).ToArray();
            if (events.Any(e => e.Name != EventNamespace + "Event"))
                throw new ArgumentException(UiText.Source("eventReview.invalidRoot"));
        }
        else throw new ArgumentException(UiText.Source("eventReview.invalidRoot"));
        if (events.Length > MaxEvents) throw new ArgumentException(UiText.Source("eventReview.tooComplex"));

        int allowed = 0, audit = 0, blocked = 0, errors = 0, other = 0, ignored = 0;
        var rows = new List<AppLockerEventRow>();
        foreach (var entry in events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var systems = entry.Elements(EventNamespace + "System").Take(2).ToArray();
            var system = systems.Length == 1 ? systems[0] : null;
            var providers = system?.Elements(EventNamespace + "Provider").Take(2).ToArray() ?? [];
            var identifiers = system?.Elements(EventNamespace + "EventID").Take(2).ToArray() ?? [];
            if (providers.Length != 1 || (string?)providers[0].Attribute("Name") != "Microsoft-Windows-AppLocker" ||
                identifiers.Length != 1 || !int.TryParse(identifiers[0].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int id))
            { ignored++; continue; }
            string? outcome = null;
            switch (id)
            {
                case 8002 or 8005 or 8020 or 8023: allowed++; break;
                case 8003 or 8006 or 8021 or 8024: audit++; outcome = "eventReview.audit"; break;
                case 8004 or 8007 or 8022 or 8025: blocked++; outcome = "eventReview.blocked"; break;
                case 8000 or 8008 or 8027: errors++; outcome = "eventReview.error"; break;
                default: other++; break;
            }
            if (outcome is null || rows.Count >= MaxRows) continue;
            var payload = entry.Elements().Where(e => e.Name == EventNamespace + "EventData" || e.Name == EventNamespace + "UserData");
            string file = payload.SelectMany(e => e.Descendants()).FirstOrDefault(e =>
                e.Name.LocalName is "FilePath" or "FileName" || e.Name.LocalName == "Data" &&
                (string?)e.Attribute("Name") is "FilePath" or "FileName")?.Value ?? "—";
            string time = (string?)system?.Element(EventNamespace + "TimeCreated")?.Attribute("SystemTime") ?? "—";
            rows.Add(new(id, outcome, ReviewDisplay.Field(time), ReviewDisplay.Field(file)));
        }
        return new(allowed, audit, blocked, errors, other, ignored, rows);
    }

    public string Describe()
    {
        var lines = new List<string>
        {
            UiText.Source("eventReview.scope"),
            UiText.Format("eventReview.count", UiText.Arg(Allowed), UiText.Arg(Audit), UiText.Arg(Blocked),
                UiText.Arg(Errors), UiText.Arg(Other), UiText.Arg(Ignored))
        };
        if (Audit + Blocked + Errors > Rows.Count) lines.Add(UiText.Source("eventReview.partial"));
        if (Rows.Count == 0) lines.Add(UiText.Source("eventReview.noFindings"));
        lines.Add("");
        foreach (var row in Rows)
            lines.Add(UiText.Format("eventReview.row", UiText.Arg(row.Id), UiText.Source(row.Outcome),
                UiText.Arg(row.Time), UiText.Arg(row.File)));
        return UiText.Join("\n", lines);
    }
}
