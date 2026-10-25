using System.Globalization;
using System.Xml;
namespace UnattendLocal.Core;
public static class HardwareSettings {
 public record Item(string Property,string RegistryValue,string Label);
 public static IReadOnlyList<Item> Items {get;}=new Item[]{
  new(nameof(InstallPlan.BypassTPMCheck),"BypassTPMCheck",global::UnattendLocal.Localization.UiText.Source("text.03d4d9f03919")),
  new(nameof(InstallPlan.BypassSecureBootCheck),"BypassSecureBootCheck",global::UnattendLocal.Localization.UiText.Source("text.443cc44b8f86")),
  new(nameof(InstallPlan.BypassRAMCheck),"BypassRAMCheck",global::UnattendLocal.Localization.UiText.Source("text.3d712e0edce3"))};
 public static bool Enabled(InstallPlan p,Item item)=>p.BypassHardware||(bool)typeof(InstallPlan).GetProperty(item.Property)!.GetValue(p)!;
 public static bool AllEnabled(InstallPlan p)=>Items.All(item=>Enabled(p,item));
 public static void NormalizeLegacy(InstallPlan p){if(!p.BypassHardware)return;foreach(var item in Items)typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p,true);p.BypassHardware=false;}
 public static string Summary(InstallPlan p){var names=Items.Where(item=>Enabled(p,item)).Select(item=>item.Label).ToList();return global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.440b02b2826e"), (names.Count==0?global::UnattendLocal.Localization.UiText.Source("text.a9baa16ce15e"):global::UnattendLocal.Localization.UiText.Join("、", names, false)) },new bool[]{false,false});}
 // Full selection uses the unchanged native generator. Partial selection writes only those same supported registry keys.
 public static void Apply(XmlDocument doc,XmlNamespaceManager ns,InstallPlan p){
  if(AllEnabled(p))return;
  var selected=Items.Where(item=>Enabled(p,item)).ToList();if(selected.Count==0)return;
  var components=doc.SelectNodes("//u:settings[@pass='windowsPE']/u:component[@name='Microsoft-Windows-Setup']",ns)!;
  if(components.Count==0)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.f42ae5b3434e"));
  const string space="urn:schemas-microsoft-com:unattend";
  XmlElement Add(XmlElement parent,string name,string? text=null){var node=doc.CreateElement(name,space);if(text!=null)node.InnerText=text;parent.AppendChild(node);return node;}
  foreach(XmlElement setup in components){
   var sync=setup.SelectSingleNode("u:RunSynchronous",ns) as XmlElement??Add(setup,"RunSynchronous");
   var next=sync.SelectNodes("u:RunSynchronousCommand/u:Order",ns)!.Cast<XmlNode>().Select(node=>int.Parse(node.InnerText,CultureInfo.InvariantCulture)).DefaultIfEmpty(0).Max()+1;
   foreach(var item in selected){
    var cmd=Add(sync,"RunSynchronousCommand");var action=doc.CreateAttribute("wcm","action","http://schemas.microsoft.com/WMIConfig/2002/State");action.Value="add";cmd.Attributes.Append(action);
    Add(cmd,"Order",(next++).ToString(CultureInfo.InvariantCulture));Add(cmd,"Description",item.Label);
    Add(cmd,"Path",$"reg.exe add \"HKLM\\SYSTEM\\Setup\\LabConfig\" /v {item.RegistryValue} /t REG_DWORD /d 1 /f");
   }
  }
 }
}

