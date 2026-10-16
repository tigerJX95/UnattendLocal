using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using Microsoft.Win32;
using UnattendLocal.Core;
namespace UnattendLocal.Desktop;
public partial class MainWindow : Window {
 private readonly MainViewModel vm=new(); private bool loading;
 public MainWindow(){InitializeComponent();DataContext=vm;InitializeSettingsSearch();_ = new WindowScrollPosition(this,Pages);SourceInitialized+=(_,_)=>UiTheme.UpdateTitleBar(this);RefreshThemeIcon();}
 private void RefreshThemeIcon(){var dark=UiTheme.Current=="Dark";ThemeMoon.Visibility=dark?Visibility.Collapsed:Visibility.Visible;ThemeSun.Visibility=dark?Visibility.Visible:Visibility.Collapsed;var title=dark?"切换为浅色主题":"切换为深色主题";ThemeToggleTip.Content=title;System.Windows.Automation.AutomationProperties.SetName(ThemeToggleButton,title);}
 private void ThemeToggleClick(object s,RoutedEventArgs e){var saved=UiTheme.Apply(UiTheme.Current=="Dark"?"Light":"Dark");RefreshThemeIcon();if(!saved)vm.Status="主题已切换，但未能保存主题偏好";}
 private void Run(Action action){try{action();}catch(Exception e){vm.Status="配置未通过检查";MessageBox.Show(this,e.Message,"请检查配置",MessageBoxButton.OK,MessageBoxImage.Warning);}}
 private void UserPasswordChanged(object s,RoutedEventArgs e){if(!loading&&s is PasswordBox b&&b.DataContext is UserEntry u){u.Password=b.Password;u.NeedsPassword=false;}}
 private void UserContextChanged(object s,DependencyPropertyChangedEventArgs e){SyncUserPassword((PasswordBox)s);}
 private void UserPasswordLoaded(object s,RoutedEventArgs e){SyncUserPassword((PasswordBox)s);}
 private void SyncUserPassword(PasswordBox b){var previous=loading;loading=true;try{b.Password=(b.DataContext as UserEntry)?.Password??"";}finally{loading=previous;}}
 private void BuiltinPasswordChanged(object s,RoutedEventArgs e){if(!loading){vm.Plan.BuiltinAdministratorPassword=((PasswordBox)s).Password;vm.Plan.NeedsBuiltinAdministratorPassword=false;}}
 private void BuiltinPasswordLoaded(object s,RoutedEventArgs e){var previous=loading;loading=true;try{((PasswordBox)s).Password=vm.Plan.BuiltinAdministratorPassword;}finally{loading=previous;}}
 private void WifiPasswordChanged(object s,RoutedEventArgs e){if(!loading){vm.Plan.WifiPassword=((PasswordBox)s).Password;vm.Plan.NeedsWifiPassword=false;}}
 private void AddUser(object s,RoutedEventArgs e){int i=1;while(vm.Plan.Users.Any(u=>u.Name.Equals("User"+i,StringComparison.OrdinalIgnoreCase)))i++;var u=new UserEntry{Name=vm.Plan.ConfigurationMode=="自定义配置"&&vm.Plan.CustomStartingPoint=="基础配置"?"":"User"+i,Administrator=vm.Plan.Users.Count==0};vm.Plan.Users.Add(u);vm.SelectedUser=u;}
 private void RemoveUser(object s,RoutedEventArgs e){if(vm.SelectedUser is {} u)vm.Plan.Users.Remove(u);vm.SelectedUser=vm.Plan.Users.FirstOrDefault();}
 private bool Discard()=>MessageBox.Show(this,"是否放弃当前未保存的修改？","切换方案",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes;
 private void Load(InstallPlan p){loading=true;try{vm.Plan=p;vm.SelectedUser=p.Users.FirstOrDefault();WifiPassword.Password=p.WifiPassword;BuiltinAdministratorPassword.Password=p.BuiltinAdministratorPassword;vm.Preview="方案已载入，请检查与预览。";vm.Status=p.Scripts.Any(row=>row.IsRemovedType)?"方案已载入；旧 JScript 脚本已停用，内容保留，请移除或手动改写":"方案已载入；未保存的密码和密钥需重新输入";}finally{loading=false;}}
 private void NewCustomClick(object s,RoutedEventArgs e){if(Discard()){Load(InstallPlan.NewCustom());vm.Status="已新建自定义配置，请逐项选择或填写";}}
 private void NewClick(object s,RoutedEventArgs e){if(Discard())Load(InstallPlan.New());}
 private void ConfigurationModeChanged(object s,SelectionChangedEventArgs e){
  if(loading||s is not ComboBox b||e.RemovedItems.Count==0||b.SelectedItem is not ComboBoxItem item||item.Content?.ToString()!="自定义配置")return;
  var dialog=new Window{Owner=this,Title="开始自定义配置",SizeToContent=SizeToContent.WidthAndHeight,WindowStartupLocation=WindowStartupLocation.CenterOwner,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false};
  var panel=new StackPanel{Margin=new Thickness(24),Width=480};panel.Children.Add(new TextBlock{Text="请选择自定义配置的起点。\n从基础值重新开始会清空当前填写的账户、凭据和其他配置。\n保留当前设置则继续调整已有内容，不会自动应用推荐值。",TextWrapping=TextWrapping.Wrap});
  var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,16,0,0)};
  bool? choice=null;
  var reset=new Button{Content="从基础值重新开始"};reset.Click+=(_,_)=>{choice=true;dialog.DialogResult=true;};
  var keep=new Button{Content="保留当前设置"};keep.Click+=(_,_)=>{choice=false;dialog.DialogResult=true;};
  var cancel=new Button{Content="取消",IsCancel=true};buttons.Children.Add(reset);buttons.Children.Add(keep);buttons.Children.Add(cancel);panel.Children.Add(buttons);dialog.Content=panel;
  dialog.SourceInitialized+=(_,_)=>UiTheme.UpdateTitleBar(dialog);
  dialog.ShowDialog();var result=choice;if(result==true){Load(InstallPlan.NewCustom());vm.Status="已从基础值开始自定义配置";}
  else if(result==false){vm.Plan.ConfigurationMode="自定义配置";vm.Plan.CustomStartingPoint="保留当前设置";}
  else{loading=true;try{vm.Plan.ConfigurationMode="推荐配置";}finally{loading=false;}}
 }
 private void OpenClick(object s,RoutedEventArgs e)=>Run(()=>{var d=new OpenFileDialog{Filter="配置方案 (*.json)|*.json"};if(d.ShowDialog(this)==true){var p=PlanStore.Deserialize(File.ReadAllText(d.FileName));if(Discard())Load(p);}});
 private void ImportAnswerClick(object s,RoutedEventArgs e){
  var d=new OpenFileDialog{Filter="Windows 应答 XML (*.xml)|*.xml",Title="导入 Windows 应答配置"};
  if(d.ShowDialog(this)!=true){vm.Status="已取消 XML 文件选择，当前配置未改变。";return;}
  try{
   if(new FileInfo(d.FileName).Length>32*1024*1024)throw new ArgumentException("应答 XML 文件超过 32 MiB。");
   bool accepted=vm.ImportXml(File.ReadAllText(d.FileName),()=>MessageBox.Show(this,"导入 XML 将替换当前界面中的配置，当前未保存的修改将被放弃。\nXML 中未识别的设置和脚本会保留原文；无法可靠合并的修改会在导出时被阻止。\n\n选择“是”：导入并替换当前配置。\n选择“否”：取消整次导入，保留当前配置。\n是否继续导入？","确认导入应答 XML",MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No)==MessageBoxResult.Yes);
   if(!accepted){MessageBox.Show(this,vm.Status,"XML 导入已取消",MessageBoxButton.OK,MessageBoxImage.Information);return;}
   string status=vm.Status;Load(vm.Plan);Pages.SelectedIndex=0;vm.Status=status;
  }catch(Exception error){vm.Status="XML 导入失败，当前配置未改变。";MessageBox.Show(this,error.Message,"XML 导入失败",MessageBoxButton.OK,MessageBoxImage.Warning);}
 }
 private void ImportWifiProfileClick(object s,RoutedEventArgs e)=>Run(()=>{var d=new OpenFileDialog{Filter="无线配置 XML (*.xml)|*.xml",Title="导入 WLANProfile 无线配置"};if(d.ShowDialog(this)!=true)return;if(new FileInfo(d.FileName).Length>2*1024*1024)throw new ArgumentException("无线配置文件超过 2 MiB。");var text=File.ReadAllText(d.FileName);_=WifiSettings.ReadProfile(text);vm.Plan.WifiProfileXml=text;vm.Status="无线配置 XML 已导入，请核对内容和目标环境";});
 private void ValidateWifiProfileClick(object s,RoutedEventArgs e)=>Run(()=>{_=WifiSettings.ReadProfile(vm.Plan.WifiProfileXml);vm.Status="无线配置 XML 结构检查通过；实际连接取决于目标环境";});
 private void ImportAppLockerClick(object s,RoutedEventArgs e)=>Run(()=>{var d=new OpenFileDialog{Filter="AppLocker 策略 XML (*.xml)|*.xml",Title="导入 AppLocker 策略"};if(d.ShowDialog(this)!=true)return;if(new FileInfo(d.FileName).Length>AppControlSettings.MaxLength*4L)throw new ArgumentException("AppLocker 策略文件超过 8 MiB。");var text=File.ReadAllText(d.FileName);_=AppControlSettings.ReadPolicy(text);if(!string.IsNullOrWhiteSpace(vm.Plan.AppLockerPolicyXml)&&MessageBox.Show(this,"导入将替换编辑器中的策略，是否继续？","导入 AppLocker 策略",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;vm.Plan.AppLockerPolicyXml=text;vm.Status="策略结构检查通过；请在目标环境审核规则及恢复方案";});
 private void ValidateAppLockerClick(object s,RoutedEventArgs e)=>Run(()=>{_=AppControlSettings.ReadPolicy(vm.Plan.AppLockerPolicyXml);vm.Status="AppLocker XML 结构检查通过，不代表规则适合目标系统";});
 private void AppLockerExampleClick(object s,RoutedEventArgs e)=>Run(()=>{if(!string.IsNullOrWhiteSpace(vm.Plan.AppLockerPolicyXml)&&MessageBox.Show(this,"审核示例将替换当前策略内容，是否继续？","载入 AppLocker 示例",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;vm.Plan.AppLockerPolicyXml=AppControlSettings.Example;vm.Status="已载入审核示例；仅供学习，请结合目标环境调整与验证";});
 private void SaveClick(object s,RoutedEventArgs e)=>Run(()=>{var d=new SaveFileDialog{Filter="配置方案 (*.json)|*.json",FileName="安装方案.json"};if(d.ShowDialog(this)!=true)return;var answer=MessageBox.Show(this,"是否将账户密码（含内置 Administrator）、Wi-Fi 密码、完整无线配置 XML 及自定义产品密钥写入方案？\n脚本始终按原文保存；脚本内的密码、令牌不会自动省略。\n导入 XML 时，选择“是”会同时保存完整 XML 原文；选择“否”不保存原文，下次须重新导入 XML 才能导出。\n选择“否”将省略密码、自定义密钥和整段无线配置 XML，下次载入需重新输入或导入。\n选择“是”会以明文保存密码和自定义密钥。","保存凭据",MessageBoxButton.YesNoCancel,MessageBoxImage.Question,MessageBoxResult.No);if(answer==MessageBoxResult.Cancel)return;File.WriteAllText(d.FileName,PlanStore.Serialize(vm.Plan,answer==MessageBoxResult.Yes),new UTF8Encoding(false));vm.Status="方案已保存";});
 private static bool Invalid(DependencyObject root){if(root is UIElement element&&!element.IsEnabled)return false;if(System.Windows.Controls.Validation.GetHasError(root))return true;for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)if(Invalid(VisualTreeHelper.GetChild(root,i)))return true;return false;}
 private byte[] Generate(){if(Invalid(this))throw new ArgumentException("请修正输入框中的无效值（例如密码到期天数或账户锁定参数）。");return vm.Service.Generate(vm.Plan);}
 private void PreviewClick(object s,RoutedEventArgs e)=>Run(()=>{vm.Preview=GeneratorService.Preview(Generate());vm.Status="配置与 XML 结构检查通过；预览已遮蔽密码和产品密钥";ExportPage.IsSelected=true;});
 private void ApplyRecommendationClick(object s,RoutedEventArgs e)=>Run(()=>{
  if(MessageBox.Show(this,"应用推荐值将覆盖推荐涉及的开关和策略。\n保留账户、密码、密钥、Wi-Fi 信息、计算机名、语言区域、架构和 SKU。磁盘恢复为手动模式，危险操作确认将取消。\n是否继续？","应用推荐配置",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
  Recommendations.Apply(vm.Plan,vm.PendingRecommendation);vm.Preview="推荐配置已应用，请补齐账户与网络信息，再检查与预览。";vm.Status="推荐值已应用；所有设置均可修改";
 });
 private void RestoreRecommendationClick(object s,RoutedEventArgs e)=>Run(()=>{if(s is Button b&&b.Tag is string name){Recommendations.Restore(vm.Plan,name);vm.Preview="设置已更新，请重新检查与预览。";vm.Status="已恢复此项推荐值";}});
 private void InstallationOptionsClick(object s,RoutedEventArgs e){OobePage.IsSelected=true;Dispatcher.BeginInvoke(new Action(()=>InstallationOptions.BringIntoView()),System.Windows.Threading.DispatcherPriority.Loaded);}
 private void SelectDesktopItemsClick(object s,RoutedEventArgs e)=>Run(()=>{if(s is Button {Tag:string tag}){var icons=tag.StartsWith("icons:");DesktopSettings.SelectAll(vm.Plan,icons,tag.EndsWith(":all"));}});
 private void SelectVisualEffectsClick(object s,RoutedEventArgs e)=>Run(()=>{if(vm.IsCustomVisualEffects&&s is Button {Tag:string tag}&&tag is "all" or "none")VisualEffectsSettings.SelectAll(vm.Plan,tag=="all");});
 private string LayoutContent(string kind)=>kind switch {"taskbar"=>vm.Plan.TaskbarIconsXml,"tiles"=>vm.Plan.StartTilesXml,"pins"=>vm.Plan.StartPinsJson,_=>throw new ArgumentException("布局类型无效。")};
 private void SetLayoutContent(string kind,string content){switch(kind){case "taskbar":vm.Plan.TaskbarIconsXml=content;break;case "tiles":vm.Plan.StartTilesXml=content;break;case "pins":vm.Plan.StartPinsJson=content;break;default:throw new ArgumentException("布局类型无效。");}}
 private void ImportLayoutClick(object s,RoutedEventArgs e)=>Run(()=>{
  if(s is not Button {Tag:string kind})return;
  var d=new OpenFileDialog{Filter=kind=="pins"?"开始菜单 JSON (*.json)|*.json":"布局 XML (*.xml)|*.xml"};
  if(d.ShowDialog(this)!=true)return;
  if(new FileInfo(d.FileName).Length>ShellSettings.MaxDocumentLength*4L)throw new ArgumentException("布局文件过大，请选择不超过 8 MiB 的文件。");
  var content=File.ReadAllText(d.FileName);ShellSettings.ValidateDocument(kind,content);
  if(!string.IsNullOrWhiteSpace(LayoutContent(kind))&&MessageBox.Show(this,"导入将替换当前编辑器内容，是否继续？","导入布局",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
  SetLayoutContent(kind,content);vm.Status="布局已导入并通过结构检查，请核对应用 ID 与目标系统快捷方式路径";
 });
 private void LayoutExampleClick(object s,RoutedEventArgs e)=>Run(()=>{
  if(s is not Button {Tag:string kind})return;
  if(!string.IsNullOrWhiteSpace(LayoutContent(kind))&&MessageBox.Show(this,"载入示例将替换当前编辑器内容，是否继续？","载入布局示例",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
  SetLayoutContent(kind,kind switch {"taskbar"=>ShellSettings.TaskbarExample,"tiles"=>ShellSettings.TilesExample,"pins"=>ShellSettings.PinsExample,_=>throw new ArgumentException("布局类型无效。")});vm.Status="示例已载入，请按目标电脑的应用与快捷方式路径修改";
 });
 private void DuplicateScriptClick(object s,RoutedEventArgs e)=>Run(()=>{if(vm.DuplicateSelectedScript()!=null)vm.Status="脚本已复制到原条目下方，副本默认停用，请修改并核对后启用";});
 private void SelectFilteredAppsClick(object s,RoutedEventArgs e)=>Run(()=>{if(s is not Button {Tag:string mode})return;var count=vm.FilteredAppRemovalChoices.Count;if(count==0){vm.Status="当前没有匹配项目";return;}bool selected=mode=="select";if(selected&&MessageBox.Show(this,$"将勾选当前显示的 {count} 项，安装时会尝试移除这些应用或系统能力。是否继续？","批量选择应用移除",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;vm.SelectFilteredApps(selected);vm.Status=$"已{(selected?"勾选":"取消")}当前显示的 {count} 项，其他项目保持原选择";});
 private void AddScriptClick(object s,RoutedEventArgs e)=>Run(()=>{if(vm.Plan.Scripts.Count>=60)throw new ArgumentException("最多 60 条脚本。");var row=new ScriptEntry();vm.Plan.Scripts.Add(row);vm.SelectedScript=row;});
 private void RemoveScriptClick(object s,RoutedEventArgs e){if(vm.SelectedScript is {} row)vm.Plan.Scripts.Remove(row);}
 private void MoveScriptClick(object s,RoutedEventArgs e){if(vm.SelectedScript is {} row&&s is Button {Tag:string direction}){var index=vm.Plan.Scripts.IndexOf(row);var target=index+(direction=="up"?-1:1);if(target>=0&&target<vm.Plan.Scripts.Count)vm.Plan.Scripts.Move(index,target);vm.SelectedScript=row;}}
 private void CustomScriptExampleClick(object s,RoutedEventArgs e)=>Run(()=>{if(vm.Plan.Scripts.Count>=60)throw new ArgumentException("最多 60 条脚本。");var network=s is Button {Tag:"network"};var row=new ScriptEntry{Enabled=false,Phase=network?"FirstLogon":"System",Type=network?"Ps1":"Cmd",Content=network?CustomScripts.NetworkExample:CustomScripts.DirectoryExample};vm.Plan.Scripts.Add(row);vm.SelectedScript=row;vm.Status="示例已新增，默认关闭；核对脚本后启用";});
 private void ImageScriptExampleClick(object s,RoutedEventArgs e)=>Run(()=>{
  if(s is not Button {Tag:string kind}||kind is not ("wallpaper" or "lockscreen"))return;
  string current=kind=="wallpaper"?vm.Plan.WallpaperImageScript:vm.Plan.LockScreenImageScript;
  if(!string.IsNullOrWhiteSpace(current)&&MessageBox.Show(this,"载入示例将替换当前图片加载脚本，是否继续？","载入示例",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
  if(kind=="wallpaper")vm.Plan.WallpaperImageScript=ImageBackgroundSettings.DesktopExample;
  else vm.Plan.LockScreenImageScript=ImageBackgroundSettings.LockExample;
  vm.Status="示例已载入，请准备安装时可读取的对应图片，或修改脚本";
 });
 private void DiskScriptExampleClick(object s,RoutedEventArgs e)=>Run(()=>{
  if(!string.IsNullOrWhiteSpace(vm.Plan.DiskPartScript)&&MessageBox.Show(this,"载入示例将替换当前分区脚本，是否继续？","载入分区示例",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
  vm.Plan.DiskPartScript=DiskScriptSettings.Example;vm.Plan.ClearDiskConsent();vm.Status="分区示例已载入：清空磁盘 0；请修改脚本并单独核对安装目标（示例为磁盘 0 / 分区 3）";
 });
 private void ValidateLayoutClick(object s,RoutedEventArgs e)=>Run(()=>{if(s is Button {Tag:string kind}){ShellSettings.ValidateDocument(kind,LayoutContent(kind));vm.Status="布局结构检查通过；应用是否存在及实际效果需在目标 Windows 验证";}});
 private void ExportClick(object s,RoutedEventArgs e)=>Run(()=>{var bytes=Generate();if((InstallationSettings.Destructive(vm.Plan)||vm.HasImportedXmlRisk)&&MessageBox.Show(this,InstallationSettings.DiskSummary(vm.Plan)+(vm.HasImportedXmlRisk?"\n导入的原 XML 还包含磁盘配置或 Windows PE 命令；须核对全部原文操作范围。":"")+"\n\n"+DiskSafety.Risk+"\n\n"+DiskSafety.Preparation+"\n\n"+DiskSafety.Responsibility+"\n\n确认已核对完整操作范围、完成备份并获得授权，继续导出？","危险操作：导出前核对",MessageBoxButton.YesNo,MessageBoxImage.Warning,MessageBoxResult.No)!=MessageBoxResult.Yes)return;var d=new SaveFileDialog{Filter="应答文件 (*.xml)|*.xml",FileName="autounattend.xml"};if(d.ShowDialog(this)!=true)return;File.WriteAllBytes(d.FileName,bytes);vm.Status="应答文件已导出";});
}
