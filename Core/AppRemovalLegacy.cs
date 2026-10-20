using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

// A narrowly recognized historical helper format. Never infer execution from URL comments.
public static partial class AppRemovalSettings
{
    public const string LegacyDescription = "已还原旧版应用移除脚本，可通过清单编辑；未修改移除清单时保留原脚本，修改后将已识别的移除流程转换为当前生成器格式，其他 XML 内容保留。";
    private static readonly (string Current, string Legacy)[] LegacyNames =
    [
        ("RemovePackage.ps1", "RemovePackages.ps1"),
        ("RemoveCapability.ps1", "RemoveCapabilities.ps1"),
        ("RemoveFeature.ps1", "RemoveFeatures.ps1")
    ];
    private const string BlockPattern = @"(?m)^\t\{\n(?<body>(?:\t\t[^\n]*\n)*)\t\};\n";
    private const string SelectorPattern = @"\A\s*\$selectors = @\(\n(?<values>(?:\t'[^'\n]+';\n)+)\);\n(?<tail>[\s\S]+)\z";
    private static Match[] RawBlocks(string body) => Regex.Matches(body.Replace("\r\n", "\n"), BlockPattern, RegexOptions.None, TimeSpan.FromSeconds(1)).Cast<Match>().ToArray();
    private const string OldOneDrive = "Remove-Item -LiteralPath 'C:\\Users\\Default\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs\\OneDrive.lnk', 'C:\\Windows\\System32\\OneDriveSetup.exe', 'C:\\Windows\\SysWOW64\\OneDriveSetup.exe' -ErrorAction 'Continue';";
    private static bool HasLegacyRemoval(XmlDocument doc) => Files(doc,"Specialize.ps1").Any(f=>RawBlocks(f.InnerText).Any(m=>Normal(m.Groups["body"].Value)==OldOneDrive)) || LegacyNames.Any(n => Files(doc,n.Legacy).Length > 0 || doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Any(f => f.InnerText.Contains(Root+n.Legacy,StringComparison.OrdinalIgnoreCase)));

    // Construct the historical logging wrapper around the fixed generator's commands.
    private static string LegacyTail(string name)
    {
        string tail = helperTails[name];
        int start = tail.IndexOf("$installed = & $getCommand;", StringComparison.Ordinal);
        string loop = tail[start..].Replace("if( $found = $installed | Where-Object -FilterScript $filterCommand ) {", "$found = $installed | Where-Object -FilterScript $filterCommand;\n\tif( $found ) {", StringComparison.Ordinal).Replace("${type}","$type",StringComparison.Ordinal);
        string legacy = LegacyNames.Single(n=>n.Current==name).Legacy;
        return tail[..start]+"$logfile = '"+Root+legacy[..^4]+".log';\n& {\n"+loop+"\n} *>&1 | Out-String -Width 1KB -Stream >> $logfile;";
    }
    private static XmlDocument? NormalizeLegacy(XmlDocument source)
    {
        source=XmlImportPaths.Inspection(source);
        _ = Templates.Value;
        if (!HasLegacyRemoval(source)) return null;
        var doc = (XmlDocument)source.CloneNode(true);
        var specialize = Files(doc,"Specialize.ps1");
        if (specialize.Length != 1) return null;
        foreach (var pair in LegacyNames)
        {
            var files = Files(doc,pair.Legacy);
            bool mentioned = source.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Any(f=>f.InnerText.Contains(Root+pair.Legacy,StringComparison.OrdinalIgnoreCase));
            if (files.Length == 0 && !mentioned) continue;
            if (files.Length != 1 || Files(doc,pair.Current).Length != 0) return null;
            var match = Regex.Match(files[0].InnerText.Replace("\r\n","\n"),SelectorPattern,RegexOptions.None,TimeSpan.FromSeconds(1));
            if (!match.Success || Normal(match.Groups["tail"].Value) != Normal(LegacyTail(pair.Current))) return null;
            string call = "& '"+Root+pair.Legacy+"';";
            if (Blocks(specialize[0].InnerText).Count(b=>b==call) != 1) return null;
            // Additional references could execute the removal twice or from an unsupported phase.
            var references = doc.SelectNodes("//*[local-name()='File' or local-name()='Path' or local-name()='CommandLine']")!.Cast<XmlElement>().SelectMany(f=>f.InnerText.Replace("\r\n","\n").Split('\n')).Where(l=>l.Contains(Root+pair.Legacy,StringComparison.OrdinalIgnoreCase)).ToArray();
            if (references.Length != 1 || references[0].Trim() != call) return null;
            files[0].SetAttribute("path",Root+pair.Current);
            files[0].InnerText = "$selectors = @(\n"+match.Groups["values"].Value+");\n"+helperTails[pair.Current];
            specialize[0].InnerText = specialize[0].InnerText.Replace(Root+pair.Legacy,Root+pair.Current,StringComparison.Ordinal);
        }
        specialize[0].InnerText = Regex.Replace(specialize[0].InnerText.Replace("\r\n","\n"),BlockPattern,m=>Normal(m.Groups["body"].Value)==OldOneDrive?oneDriveBlock:m.Value,RegexOptions.None,TimeSpan.FromSeconds(1));
        var state = InspectCurrent(doc);
        if (state.Ambiguous) return null;
        // Require extraction, unique stage invocation and the default-user registration chain.
        foreach (string stage in Stages)
        {
            var required = state.Selected.SelectMany(id=>Templates.Value[id].Custom[stage]).ToArray();
            if (stage=="Specialize.ps1") required=required.Concat(LegacyNames.Where(n=>Files(doc,n.Current).Length==1).Select(n=>"& '"+Root+n.Current+"';")).ToArray();
            var active = AdditionalSetupSettings.KnownBlocks(doc,stage);
            if (required.Any(b=>active.Count(v=>v==b)!=1)) return null;
        }
        // Original selector ordering can differ from current generation. Once verified,
        // use the fixed generator's order for an actual edit, avoiding line-context conflicts.
        foreach(var pair in LegacyNames)
        {
            var files=Files(doc,pair.Current);if(files.Length==0)continue;
            var values=state.Selected.Order().SelectMany(id=>Templates.Value[id].Selectors.GetValueOrDefault(pair.Current,[]));
            files[0].InnerText="$selectors = @(\n"+string.Concat(values.Select(v=>"\t'"+v+"';\n"))+");\n"+helperTails[pair.Current];
        }
        return doc;
    }
    internal static string[] KnownLegacyHelpers(XmlDocument source) => NormalizeLegacy(source)==null ? [] : LegacyNames.Where(n=>Files(source,n.Legacy).Length==1).Select(n=>n.Legacy).ToArray();
    internal static bool IsEditableLegacy(XmlDocument source) => NormalizeLegacy(source)!=null;
    internal static XmlDocument MergeImported(InstallPlan p,XmlDocument updated)
    {
        var imported=p.ImportedXml!;
        var source=StaticIPv4Settings.PrepareMergeSource(XmlMerge.Read(imported.SourceXml),imported.BaselineXml,updated);
        var normalized=NormalizeLegacy(source);
        if(normalized==null || InspectCurrent(normalized).Selected.Order().SequenceEqual(p.RemovedApps.Order()))
            return XmlMerge.Apply(source.OuterXml,imported.BaselineXml,updated);
        return XmlMerge.Apply(normalized.OuterXml,imported.BaselineXml,updated,MergeRemovalBlocks);
    }
    private static string? MergeRemovalBlocks(string path,string source,string before,string after)
    {
        var stage=Stages.FirstOrDefault(s=>path.Equals(Root+s,StringComparison.OrdinalIgnoreCase));
        if(stage==null || Skeleton(source)!=wrappers[stage] || Skeleton(before)!=wrappers[stage] || Skeleton(after)!=wrappers[stage])return null;
        var owned=Templates.Value.Values.SelectMany(t=>t.Custom[stage]).ToHashSet();
        if(stage=="Specialize.ps1")foreach(var pair in LegacyNames)owned.Add("& '"+Root+pair.Current+"';");
        var oldBlocks=RawBlocks(before);var nextBlocks=RawBlocks(after);
        string Key(Match m)=>Normal(m.Groups["body"].Value);
        var oldApps=oldBlocks.Where(m=>owned.Contains(Key(m))).ToArray();
        var nextApps=nextBlocks.Where(m=>owned.Contains(Key(m))).ToArray();
        if(oldApps.Select(Key).SequenceEqual(nextApps.Select(Key)))return null;
        // A complete known block is the edit boundary. Extra source commands stay in place.
        string Patch(string input)
        {
            input=input.Replace("\r\n","\n");
            foreach(var old in oldApps)
            {
                var matches=RawBlocks(input).Where(m=>Key(m)==Key(old)).ToArray();
                if(matches.Length!=1)throw new ArgumentException("旧版应用移除命令无法唯一匹配，请重新导入原 XML。");
                if(nextApps.Any(m=>Key(m)==Key(old)))continue;
                input=input.Remove(matches[0].Index,matches[0].Length);
            }
            foreach(var added in nextApps.Where(m=>!oldApps.Any(o=>Key(o)==Key(m))))
            {
                if(RawBlocks(input).Any(m=>Key(m)==Key(added)))throw new ArgumentException("新增应用移除命令与原脚本重叠，未导出。");
                var blocks=RawBlocks(input);int at=-1;
                foreach(var preceding in nextBlocks.TakeWhile(m=>m.Index<added.Index).Reverse())
                {
                    var anchors=blocks.Where(m=>Key(m)==Key(preceding)).ToArray();
                    if(anchors.Length==1){at=anchors[0].Index+anchors[0].Length;break;}
                }
                if(at<0)foreach(var following in nextBlocks.Where(m=>m.Index>added.Index))
                {
                    var anchors=blocks.Where(m=>Key(m)==Key(following)).ToArray();
                    if(anchors.Length==1){at=anchors[0].Index;break;}
                }
                if(at<0)at=input.IndexOf("$scripts = @(\n",StringComparison.Ordinal)+"$scripts = @(\n".Length;
                input=input.Insert(at,added.Value);
            }
            return input;
        }
        string patchedSource=Patch(source),patchedBefore=Patch(before);
        return XmlMerge.MergeScript(patchedSource,patchedBefore,after,path);
    }
    private static void UpgradeLegacyPlan(InstallPlan p)
    {
        if (p.ImportedXml is not { OriginalOmitted:false, AppRemovalImportVersion:<2 } data || data.SourceXml.Length==0 || data.BaselineXml.Length==0) return;
        var source = XmlMerge.Read(data.SourceXml);
        var normalized = NormalizeLegacy(source);
        if (normalized == null) return;
        var state = InspectCurrent(normalized);
        {
            var previous = InspectCurrent(source);
            if (!previous.Selected.Order().SequenceEqual(p.RemovedApps.Order()))
            {
                data.Blockers=data.Blockers.Append("旧方案的应用移除选择与原脚本不一致，请重新导入原 XML 后编辑。").Distinct().ToArray();
                return;
            }
            var before = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(p))!;
            before.ImportedXml=null;before.ConfirmWipeDisk=before.ConfirmFormatPartition=before.ConfirmDiskScript=true;
            var after = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!;
            after.RemovedApps=new(state.Selected);
            try
            {
                var service=new GeneratorService();
                string baseline=XmlMerge.Apply(data.BaselineXml,Encoding.UTF8.GetString(service.GenerateStandalone(before)),XmlMerge.Read(Encoding.UTF8.GetString(service.GenerateStandalone(after)))).OuterXml;
                data.BaselineXml=baseline;p.RemovedApps=new(state.Selected);
            }
            catch(Exception e) when(e is ArgumentException or ConfigurationException or XmlException)
            {
                data.Blockers=data.Blockers.Append("旧方案的应用移除编辑基准无法可靠迁移，请重新导入原 XML。").Distinct().ToArray();return;
            }
        }
        data.AppRemovalImportVersion=2;data.AppRemovalReadOnly=false;
    }
}
