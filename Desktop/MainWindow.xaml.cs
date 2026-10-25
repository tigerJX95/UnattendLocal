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
 public MainWindow(){InitializeComponent();DataContext=vm;InitializeSettingsSearch();_ = new WindowScrollPosition(this,Pages);SourceInitialized+=(_,_)=>UiTheme.UpdateTitleBar(this);RefreshThemeIcon();InitializeConfigurationDrop();InitializeLanguageMenu();Language=System.Windows.Markup.XmlLanguage.GetLanguage(UnattendLocal.Localization.UiText.Language);if(App.LanguageWarning is string warning)vm.Status=UnattendLocal.Localization.UiText.Source(warning);}
 private void InitializeLanguageMenu(){
  LanguagePickerMenu.Items.Clear();
  foreach(var language in UnattendLocal.Localization.UiText.Languages){
   var item=new MenuItem{Header=language.Name,Tag=language.Code,IsCheckable=false,IsChecked=language.Code==UnattendLocal.Localization.UiText.Language,Padding=new Thickness(12,8,16,8)};
   item.Click+=LanguageMenuItemClick;
   LanguagePickerMenu.Items.Add(item);
  }
 }
 private void LanguagePickerClick(object sender,RoutedEventArgs e){
  UpdateLanguageSelection(UnattendLocal.Localization.UiText.Language);
  LanguagePickerMenu.PlacementTarget=LanguagePickerButton;
  LanguagePickerMenu.Placement=System.Windows.Controls.Primitives.PlacementMode.Bottom;
  LanguagePickerMenu.IsOpen=true;
 }
 private void LanguageMenuItemClick(object sender,RoutedEventArgs e){
  if(sender is not MenuItem{Tag:string code}||!UnattendLocal.Localization.UiText.IsSupported(code))return;
  if(code==UnattendLocal.Localization.UiText.Language){UpdateLanguageSelection(code);return;}
  // Clear the old selection before updating localization resources to avoid transient double-checks.
  UpdateLanguageSelection(code);
  var pack=System.IO.Path.Combine(System.AppContext.BaseDirectory,"Languages",code+".json");bool rejected=System.IO.File.Exists(pack)&&!UnattendLocal.Localization.UiText.LoadExternalPack(code,pack,out _);
  UnattendLocal.Localization.UiText.SetLanguage(code);
  Language=System.Windows.Markup.XmlLanguage.GetLanguage(code);RefreshThemeIcon();
  if(rejected)vm.Status=UnattendLocal.Localization.UiText.Source("language.packRejected");
  if(!UiPreferences.SaveLanguage(code))vm.Status=UnattendLocal.Localization.UiText.Source("text.971741bbc9ca");
 }
 // The MenuItem check glyph is rendered by our theme template, not by the built-in toggle.
 // IsCheckable=false prevents WPF from toggling a second item before Click runs.
 private void UpdateLanguageSelection(string code){
  var items=LanguagePickerMenu.Items.OfType<MenuItem>().ToArray();
  foreach(var item in items)item.IsChecked=false;
  foreach(var item in items){
   if(item.Tag is string itemCode&&itemCode==code){item.IsChecked=true;break;}
  }
 }
 private void RefreshThemeIcon(){var dark=UiTheme.Current=="Dark";ThemeMoon.Visibility=dark?Visibility.Collapsed:Visibility.Visible;ThemeSun.Visibility=dark?Visibility.Visible:Visibility.Collapsed;var title=dark?global::UnattendLocal.Localization.UiText.Source("text.a52d332cfafb"):global::UnattendLocal.Localization.UiText.Source("text.a3538a688840");ThemeToggleTip.Content=title;System.Windows.Automation.AutomationProperties.SetName(ThemeToggleButton,UnattendLocal.Localization.UiText.Translate(title));}
 private void ThemeToggleClick(object s,RoutedEventArgs e){var saved=UiTheme.Apply(UiTheme.Current=="Dark"?"Light":"Dark");RefreshThemeIcon();if(!saved)vm.Status=global::UnattendLocal.Localization.UiText.Source("text.7e775a45f5e6");}
 private void AboutMenuClick(object s,RoutedEventArgs e){var menu=AboutMenuButton.ContextMenu;menu.PlacementTarget=AboutMenuButton;menu.Placement=System.Windows.Controls.Primitives.PlacementMode.Bottom;menu.IsOpen=true;}
 private void AboutSectionClick(object s,RoutedEventArgs e){if(s is MenuItem {Tag:string section}&&int.TryParse(section,out var index))Run(()=>new AboutWindow(index){Owner=this}.ShowDialog());}
 private void Run(Action action, bool locateConfiguration=false){try{action();}catch(Exception e){
  vm.Status=global::UnattendLocal.Localization.UiText.Source("text.63026f42b636");
  var location=locateConfiguration?ConfigurationErrorLocation.FromException(e):null;
  if(ErrorDialog.Show(this,e.Message,location:location?.Path,actionText:location?.ActionText??global::UnattendLocal.Localization.UiText.Source("text.90e01acb5c9e"))&&location is not null){
   if(location.UserIndex is int index&&index>=0&&index<vm.Plan.Users.Count)vm.SelectedUser=vm.Plan.Users[index];
   NavigateToSetting(location.Setting,focusEditor:true);
  }
 }}
 private void UserPasswordChanged(object s,RoutedEventArgs e){if(!loading&&!vm.IsLoadingPlan&&s is PasswordBox b&&b.DataContext is UserEntry u){u.Password=b.Password;u.NeedsPassword=false;}}
 private void UserContextChanged(object s,DependencyPropertyChangedEventArgs e){SyncUserPassword((PasswordBox)s);}
 private void UserPasswordLoaded(object s,RoutedEventArgs e){SyncUserPassword((PasswordBox)s);}
 private void SyncUserPassword(PasswordBox b){var previous=loading;loading=true;try{b.Password=(b.DataContext as UserEntry)?.Password??"";}finally{loading=previous;}}
 private void BuiltinPasswordChanged(object s,RoutedEventArgs e){if(!loading&&!vm.IsLoadingPlan){vm.Plan.BuiltinAdministratorPassword=((PasswordBox)s).Password;vm.Plan.NeedsBuiltinAdministratorPassword=false;}}
 private void BuiltinPasswordLoaded(object s,RoutedEventArgs e){var previous=loading;loading=true;try{((PasswordBox)s).Password=vm.Plan.BuiltinAdministratorPassword;}finally{loading=previous;}}
 private void WifiPasswordChanged(object s,RoutedEventArgs e){if(!loading&&!vm.IsLoadingPlan){vm.Plan.WifiPassword=((PasswordBox)s).Password;vm.Plan.NeedsWifiPassword=false;}}
 private void AddUser(object s,RoutedEventArgs e){int i=1;while(vm.Plan.Users.Any(u=>u.Name.Equals("User"+i,StringComparison.OrdinalIgnoreCase)))i++;var u=new UserEntry{Name=vm.Plan.ConfigurationMode==global::UnattendLocal.Localization.UiText.Source("text.cb1e0e22c7de")&&vm.Plan.CustomStartingPoint==global::UnattendLocal.Localization.UiText.Source("text.12a6c9d91dc8")?"":"User"+i,Administrator=vm.Plan.Users.Count==0};vm.Plan.Users.Add(u);vm.SelectedUser=u;}
 private void RemoveUser(object s,RoutedEventArgs e){if(vm.SelectedUser is {} u)vm.Plan.Users.Remove(u);vm.SelectedUser=vm.Plan.Users.FirstOrDefault();}
 private bool Discard()=>LocalizedMessageBox.Show(this,global::UnattendLocal.Localization.UiText.Source("text.b4357b2f2197"),global::UnattendLocal.Localization.UiText.Source("text.f3d0cbb84a11"),MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes;
 private void Load(InstallPlan p){var previous=loading;loading=true;try{vm.Plan=p;vm.SelectedUser=p.Users.FirstOrDefault();WifiPassword.Password=p.WifiPassword;BuiltinAdministratorPassword.Password=p.BuiltinAdministratorPassword;vm.Preview=global::UnattendLocal.Localization.UiText.Source("text.752e25c039af");vm.Status=p.XmlComponents.Any(row=>row.Enabled&&string.IsNullOrWhiteSpace(row.Content))?global::UnattendLocal.Localization.UiText.Source("text.e3f69b9966a6"):p.Scripts.Any(row=>row.IsRemovedType)?global::UnattendLocal.Localization.UiText.Source("text.e3dd9f9604bc"):global::UnattendLocal.Localization.UiText.Source("text.c8e08c93e59a");}finally{loading=previous;}}
 private void AddXmlComponentClick(object s,RoutedEventArgs e)=>Run(()=>{if(vm.Plan.XmlComponents.Count>=AdditionalXmlSettings.MaxCount)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.e0be0678cd02"));var row=new XmlComponentEntry();vm.Plan.XmlComponents.Add(row);vm.SelectedXmlComponent=row;});
 private void RemoveXmlComponentClick(object s,RoutedEventArgs e){if(vm.SelectedXmlComponent is {} row)vm.Plan.XmlComponents.Remove(row);}
 private void XmlComponentExampleClick(object s,RoutedEventArgs e)=>Run(()=>{if(vm.Plan.XmlComponents.Count>=AdditionalXmlSettings.MaxCount)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.e0be0678cd02"));var row=new XmlComponentEntry{Content=AdditionalXmlSettings.Example};vm.Plan.XmlComponents.Add(row);vm.SelectedXmlComponent=row;vm.Status=global::UnattendLocal.Localization.UiText.Source("text.fbe871ae011e");});
 private void CheckXmlComponentClick(object s,RoutedEventArgs e)=>Run(()=>{if(vm.SelectedXmlComponent is not {} row)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.d9c31bccd272"));_=AdditionalXmlSettings.Parse(row);vm.Status=global::UnattendLocal.Localization.UiText.Source("text.57e53e8bea9e");});
 private void QuickPresetClick(object s,RoutedEventArgs e){
  if(s is not Button {Tag:string kind})return;
  var plan=QuickPresets.Create(kind);
  string details=kind==QuickPresets.LocalAccount
   ? global::UnattendLocal.Localization.UiText.Source("text.c70dfacdd3e8")
   : global::UnattendLocal.Localization.UiText.Source("text.89a00025649c");
  if(LocalizedMessageBox.Show(this,global::UnattendLocal.Localization.UiText.Concat(new object?[] { details, global::UnattendLocal.Localization.UiText.Source("text.8066a4f44651") },new bool[]{false,false}),global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.50ef2f4cf6a4"), kind },new bool[]{false,false}),MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No)!=MessageBoxResult.Yes)return;
  Load(plan);Pages.SelectedIndex=0;vm.Status=global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.d4b08f2eb33f"), kind, global::UnattendLocal.Localization.UiText.Source("text.3027befd4ecc") },new bool[]{false,false,false});
 }
 private void NewCustomClick(object s,RoutedEventArgs e){if(Discard()){Load(InstallPlan.NewCustom());vm.Status=global::UnattendLocal.Localization.UiText.Source("text.bf343d68e487");}}
 private void NewClick(object s,RoutedEventArgs e){if(Discard())Load(InstallPlan.New());}
 private void ConfigurationModeChanged(object s,SelectionChangedEventArgs e){
  if(loading||vm.IsLoadingPlan||s is not ComboBox b||e.RemovedItems.Count==0||b.SelectedItem is not ComboBoxItem item||item.Content?.ToString()!=global::UnattendLocal.Localization.UiText.Source("text.cb1e0e22c7de"))return;
  var dialog=new Window{Owner=this,Title=global::UnattendLocal.Localization.UiText.Translate(global::UnattendLocal.Localization.UiText.Source("text.ceb0a769892d")),SizeToContent=SizeToContent.WidthAndHeight,WindowStartupLocation=WindowStartupLocation.CenterOwner,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false};
  var panel=new StackPanel{Margin=new Thickness(24),Width=480};panel.Children.Add(new TextBlock{Text=global::UnattendLocal.Localization.UiText.Translate(global::UnattendLocal.Localization.UiText.Source("text.f5e9b8f55f20")),TextWrapping=TextWrapping.Wrap});
  var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,16,0,0)};
  bool? choice=null;
  var reset=new Button{Content=global::UnattendLocal.Localization.UiText.Source("text.e4804b0350c5")};reset.Click+=(_,_)=>{choice=true;dialog.DialogResult=true;};
  var keep=new Button{Content=global::UnattendLocal.Localization.UiText.Source("text.f0866be5e5a3")};keep.Click+=(_,_)=>{choice=false;dialog.DialogResult=true;};
  var cancel=new Button{Content=global::UnattendLocal.Localization.UiText.Source("text.2cd0f3be8738"),IsCancel=true};buttons.Children.Add(reset);buttons.Children.Add(keep);buttons.Children.Add(cancel);panel.Children.Add(buttons);dialog.Content=panel;
  dialog.SourceInitialized+=(_,_)=>UiTheme.UpdateTitleBar(dialog);
  dialog.ShowDialog();var result=choice;if(result==true){Load(InstallPlan.NewCustom());vm.Status=global::UnattendLocal.Localization.UiText.Source("text.bd3484eaffe7");}
  else if(result==false){vm.Plan.ConfigurationMode=global::UnattendLocal.Localization.UiText.Source("text.cb1e0e22c7de");vm.Plan.CustomStartingPoint=global::UnattendLocal.Localization.UiText.Source("text.f0866be5e5a3");}
  else{loading=true;try{vm.Plan.ConfigurationMode=global::UnattendLocal.Localization.UiText.Source("text.933c2fd10345");}finally{loading=false;}}
 }
 private void OpenClick(object s,RoutedEventArgs e){
  var dialog=new OpenFileDialog{Filter=global::UnattendLocal.Localization.UiText.Translate(global::UnattendLocal.Localization.UiText.Source("text.7fba054e9c5b")),Title=global::UnattendLocal.Localization.UiText.Translate(global::UnattendLocal.Localization.UiText.Source("text.b3b3ae93ff98"))};
  if(dialog.ShowLocalized(this)==true)ImportConfigurationFile(dialog.FileName);
  else vm.Status=global::UnattendLocal.Localization.UiText.Source("text.2f558d0ef888");
 }
 private void ImportAnswerClick(object s,RoutedEventArgs e){
  var dialog=new OpenFileDialog{Filter=global::UnattendLocal.Localization.UiText.Translate(global::UnattendLocal.Localization.UiText.Source("text.8fb4efdfdbd7")),Title=global::UnattendLocal.Localization.UiText.Translate(global::UnattendLocal.Localization.UiText.Source("text.79ca8dae6c91"))};
  if(dialog.ShowLocalized(this)==true)ImportConfigurationFile(dialog.FileName);
  else vm.Status=global::UnattendLocal.Localization.UiText.Source("text.c22f829e67db");
 }
 private void ImportWifiProfileClick(object s,RoutedEventArgs e)=>Run(()=>{var d=new OpenFileDialog{Filter=global::UnattendLocal.Localization.UiText.Translate(global::UnattendLocal.Localization.UiText.Source("text.b05d59acd5ff")),Title=global::UnattendLocal.Localization.UiText.Translate(global::UnattendLocal.Localization.UiText.Source("text.83662e044811"))};if(d.ShowLocalized(this)!=true)return;var text=TextFileImport.Read(d.FileName,2L*1024*1024,global::UnattendLocal.Localization.UiText.Source("text.4d8c95c9efd6"));_=WifiSettings.ReadProfile(text);vm.Plan.WifiProfileXml=text;vm.Status=global::UnattendLocal.Localization.UiText.Source("text.21910a5ae514");});
 private void ValidateWifiProfileClick(object s,RoutedEventArgs e)=>Run(()=>{_=WifiSettings.ReadProfile(vm.Plan.WifiProfileXml);vm.Status=global::UnattendLocal.Localization.UiText.Source("text.2143ae1d0ce3");});
 private void ImportAppLockerClick(object s,RoutedEventArgs e)=>Run(()=>{var d=new OpenFileDialog{Filter=global::UnattendLocal.Localization.UiText.Translate(global::UnattendLocal.Localization.UiText.Source("text.ab7f0e83ac46")),Title=global::UnattendLocal.Localization.UiText.Translate(global::UnattendLocal.Localization.UiText.Source("text.cc905c3b5184"))};if(d.ShowLocalized(this)!=true)return;var text=TextFileImport.Read(d.FileName,AppControlSettings.MaxLength*4L,global::UnattendLocal.Localization.UiText.Source("text.e2f2859ef687"));_=AppControlSettings.ReadPolicy(text);if(!string.IsNullOrWhiteSpace(vm.Plan.AppLockerPolicyXml)&&LocalizedMessageBox.Show(this,global::UnattendLocal.Localization.UiText.Source("text.cb4f9f9d7056"),global::UnattendLocal.Localization.UiText.Source("text.cc905c3b5184"),MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;vm.Plan.AppLockerPolicyXml=text;vm.Status=global::UnattendLocal.Localization.UiText.Source("text.f4eb02fd233e");});
 private void ValidateAppLockerClick(object s,RoutedEventArgs e)=>Run(()=>{_=AppControlSettings.ReadPolicy(vm.Plan.AppLockerPolicyXml);vm.Status=global::UnattendLocal.Localization.UiText.Source("text.5c1b434854ec");});
 private void AppLockerExampleClick(object s,RoutedEventArgs e)=>Run(()=>{if(!string.IsNullOrWhiteSpace(vm.Plan.AppLockerPolicyXml)&&LocalizedMessageBox.Show(this,global::UnattendLocal.Localization.UiText.Source("text.589d0d4463c8"),global::UnattendLocal.Localization.UiText.Source("text.f5ea8a3828c8"),MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;vm.Plan.AppLockerPolicyXml=AppControlSettings.Example;vm.Status=global::UnattendLocal.Localization.UiText.Source("text.f5864e089baf");});
 private void SaveClick(object s,RoutedEventArgs e)=>Run(()=>{var d=new SaveFileDialog{Filter=global::UnattendLocal.Localization.UiText.Translate(global::UnattendLocal.Localization.UiText.Source("text.7fba054e9c5b")),FileName=global::UnattendLocal.Localization.UiText.Source("text.8552e8eed1a1")};if(d.ShowLocalized(this)!=true)return;var answer=LocalizedMessageBox.Show(this,global::UnattendLocal.Localization.UiText.Source("text.8bacde0493a7"),global::UnattendLocal.Localization.UiText.Source("text.47c61ce5cda2"),MessageBoxButton.YesNoCancel,MessageBoxImage.Question,MessageBoxResult.No);if(answer==MessageBoxResult.Cancel)return;AtomicFile.WriteAllText(d.FileName,PlanStore.Serialize(vm.Plan,answer==MessageBoxResult.Yes),new UTF8Encoding(false));vm.Status=global::UnattendLocal.Localization.UiText.Source("text.9a986abbc474");});
 private FrameworkElement? Invalid(DependencyObject root){
  if(root is UIElement element&&!element.IsEnabled)return null;
  if(root is FrameworkElement field&&System.Windows.Controls.Validation.GetHasError(root))return field;
  for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var invalid=Invalid(VisualTreeHelper.GetChild(root,i));if(invalid is not null)return invalid;}
  return null;
 }
 private byte[] Generate(){
  // Named controls retain binding errors even when their tab is not in the visual tree.
  var invalid=SettingsSearchCatalog.All.Select(setting=>FindName(setting.Target)).OfType<FrameworkElement>()
   .FirstOrDefault(field=>field.IsEnabled&&System.Windows.Controls.Validation.GetHasError(field))??Invalid(this);
  if(invalid is not null)throw new ConfigurationValidationException(invalid.Name,global::UnattendLocal.Localization.UiText.Source("text.4f7678afdc63"));return vm.Service.Generate(vm.Plan);}
 private void PreviewClick(object s,RoutedEventArgs e)=>Run(()=>{var bytes=Generate();vm.Preview=InstallationReview.ExportSummary(bytes)+"\n\n"+GeneratorService.Preview(bytes,vm.Plan);vm.Status=global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.66c560c48e92"), InstallationReview.ExportSummary(bytes) },new bool[]{false,false});ExportPage.IsSelected=true;}, locateConfiguration:true);
 private void ApplyRecommendationClick(object s,RoutedEventArgs e)=>Run(()=>{
  if(LocalizedMessageBox.Show(this,global::UnattendLocal.Localization.UiText.Source("text.e99546b2272a"),global::UnattendLocal.Localization.UiText.Source("text.a9087ee41397"),MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
  Recommendations.Apply(vm.Plan,vm.PendingRecommendation);vm.Preview=global::UnattendLocal.Localization.UiText.Source("text.142f2839b283");vm.Status=global::UnattendLocal.Localization.UiText.Source("text.9c12b69de53e");
 });
 private void RestoreRecommendationClick(object s,RoutedEventArgs e)=>Run(()=>{if(s is Button b&&b.Tag is string name){Recommendations.Restore(vm.Plan,name);vm.Preview=global::UnattendLocal.Localization.UiText.Source("text.79acf4fd8d44");vm.Status=global::UnattendLocal.Localization.UiText.Source("text.a9ab54c48804");}});
 private void InstallationOptionsClick(object s,RoutedEventArgs e){OobePage.IsSelected=true;Dispatcher.BeginInvoke(new Action(()=>InstallationOptions.BringIntoView()),System.Windows.Threading.DispatcherPriority.Loaded);}
 private void SelectDesktopItemsClick(object s,RoutedEventArgs e)=>Run(()=>{if(s is Button {Tag:string tag}){var icons=tag.StartsWith("icons:");DesktopSettings.SelectAll(vm.Plan,icons,tag.EndsWith(":all"));}});
 private void SelectVisualEffectsClick(object s,RoutedEventArgs e)=>Run(()=>{if(vm.IsCustomVisualEffects&&s is Button {Tag:string tag}&&tag is "all" or "none")VisualEffectsSettings.SelectAll(vm.Plan,tag=="all");});
 private string LayoutContent(string kind)=>kind switch {"taskbar"=>vm.Plan.TaskbarIconsXml,"tiles"=>vm.Plan.StartTilesXml,"pins"=>vm.Plan.StartPinsJson,_=>throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.041423d2ccdd"))};
 private void SetLayoutContent(string kind,string content){switch(kind){case "taskbar":vm.Plan.TaskbarIconsXml=content;break;case "tiles":vm.Plan.StartTilesXml=content;break;case "pins":vm.Plan.StartPinsJson=content;break;default:throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.041423d2ccdd"));}}
 private void ImportLayoutClick(object s,RoutedEventArgs e)=>Run(()=>{
  if(s is not Button {Tag:string kind})return;
  var d=new OpenFileDialog{Filter=global::UnattendLocal.Localization.UiText.Translate(kind=="pins"?global::UnattendLocal.Localization.UiText.Source("text.0488d9c42a3d"):global::UnattendLocal.Localization.UiText.Source("text.a65466e1a097"))};
  if(d.ShowLocalized(this)!=true)return;
  var content=TextFileImport.Read(d.FileName,ShellSettings.MaxDocumentLength*4L,global::UnattendLocal.Localization.UiText.Source("text.3500d767caae"));ShellSettings.ValidateDocument(kind,content);
  if(!string.IsNullOrWhiteSpace(LayoutContent(kind))&&LocalizedMessageBox.Show(this,global::UnattendLocal.Localization.UiText.Source("text.abd2dd56b959"),global::UnattendLocal.Localization.UiText.Source("text.f535358de46d"),MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
  SetLayoutContent(kind,content);vm.Status=global::UnattendLocal.Localization.UiText.Source("text.0f393bde48a4");
 });
 private void LayoutExampleClick(object s,RoutedEventArgs e)=>Run(()=>{
  if(s is not Button {Tag:string kind})return;
  if(!string.IsNullOrWhiteSpace(LayoutContent(kind))&&LocalizedMessageBox.Show(this,global::UnattendLocal.Localization.UiText.Source("text.7e265d8b7e73"),global::UnattendLocal.Localization.UiText.Source("text.8393b1190e6f"),MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
  SetLayoutContent(kind,kind switch {"taskbar"=>ShellSettings.TaskbarExample,"tiles"=>ShellSettings.TilesExample,"pins"=>ShellSettings.PinsExample,_=>throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.041423d2ccdd"))});vm.Status=global::UnattendLocal.Localization.UiText.Source("text.26582afc8a94");
 });
 private void ImportScriptFileClick(object s,RoutedEventArgs e){
  var d=new OpenFileDialog{Title=global::UnattendLocal.Localization.UiText.Translate(global::UnattendLocal.Localization.UiText.Source("text.c724ee9da759")),Filter=global::UnattendLocal.Localization.UiText.Translate(global::UnattendLocal.Localization.UiText.Source("text.2eacfaccc5f3")),Multiselect=false};
  if(d.ShowLocalized(this)!=true){vm.Status=global::UnattendLocal.Localization.UiText.Source("text.0686968bdaaa");return;}
  try{_=vm.ImportScriptFile(d.FileName);vm.Status=global::UnattendLocal.Localization.UiText.Source("text.f9097577f961");}
  catch(Exception error){vm.Status=global::UnattendLocal.Localization.UiText.Source("text.cfe65a0b90d3");ErrorDialog.Show(this,error.Message,global::UnattendLocal.Localization.UiText.Source("text.18c5568b93d5"));}
 }
 private void SaveScriptFileClick(object s,RoutedEventArgs e){
  if(vm.SelectedScript is not {} row){vm.Status=global::UnattendLocal.Localization.UiText.Source("text.83cabd7a568f");return;}
  try{
   string ext=ScriptFiles.Extension(row.Type);
   var d=new SaveFileDialog{Title=global::UnattendLocal.Localization.UiText.Translate(global::UnattendLocal.Localization.UiText.Source("text.8d0e77ce53ba")),Filter=global::UnattendLocal.Localization.UiText.Translate(global::UnattendLocal.Localization.UiText.Format("message.c12ca29aaaf7", new string[] { global::UnattendLocal.Localization.UiText.Arg((ext), 0, null), global::UnattendLocal.Localization.UiText.Arg((ext), 0, null) })),DefaultExt=ext,AddExtension=true,OverwritePrompt=true,FileName="script"+ext};
   if(d.ShowLocalized(this)!=true){vm.Status=global::UnattendLocal.Localization.UiText.Source("text.7349f02b9d4e");return;}
   ScriptFiles.Write(row,d.FileName);
   vm.Status=global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.245524589836"), ScriptFiles.EncodingHint(row.Type) },new bool[]{false,false});
  }catch(Exception error){vm.Status=global::UnattendLocal.Localization.UiText.Source("text.79a660a9296f");ErrorDialog.Show(this,error.Message,global::UnattendLocal.Localization.UiText.Source("text.3fb76b0a450a"));}
 }
 private void DuplicateScriptClick(object s,RoutedEventArgs e)=>Run(()=>{if(vm.DuplicateSelectedScript()!=null)vm.Status=global::UnattendLocal.Localization.UiText.Source("text.49d7d75f75cb");});
 private void SelectFilteredAppsClick(object s,RoutedEventArgs e)=>Run(()=>{if(s is not Button {Tag:string mode})return;var count=vm.FilteredAppRemovalChoices.Count;if(count==0){vm.Status=global::UnattendLocal.Localization.UiText.Source("text.af62195e1dff");return;}bool selected=mode=="select";if(selected&&LocalizedMessageBox.Show(this,global::UnattendLocal.Localization.UiText.Format("message.6713a8dff22c", new string[] { global::UnattendLocal.Localization.UiText.Arg((count), 0, null) }),global::UnattendLocal.Localization.UiText.Source("text.a5db3d650007"),MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;vm.SelectFilteredApps(selected);vm.Status=global::UnattendLocal.Localization.UiText.Format("message.fdb4c317e78a", new string[] { global::UnattendLocal.Localization.UiText.Arg(((selected?global::UnattendLocal.Localization.UiText.Source("text.c6cdcd00b6a5"):global::UnattendLocal.Localization.UiText.Source("text.2cd0f3be8738"))), 0, null), global::UnattendLocal.Localization.UiText.Arg((count), 0, null) });});
 private void AddScriptTemplateClick(object s,RoutedEventArgs e)=>Run(()=>vm.AddSelectedScriptTemplate());
 private void AddScriptClick(object s,RoutedEventArgs e)=>Run(()=>{if(vm.Plan.Scripts.Count>=60)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.bca97a516f08"));var row=new ScriptEntry();vm.Plan.Scripts.Add(row);vm.SelectedScript=row;});
 private void RemoveScriptClick(object s,RoutedEventArgs e){if(vm.SelectedScript is {} row)vm.Plan.Scripts.Remove(row);}
 private void MoveScriptClick(object s,RoutedEventArgs e){if(vm.SelectedScript is {} row&&s is Button {Tag:string direction}){var index=vm.Plan.Scripts.IndexOf(row);var target=index+(direction=="up"?-1:1);if(target>=0&&target<vm.Plan.Scripts.Count)vm.Plan.Scripts.Move(index,target);vm.SelectedScript=row;}}
 private void CustomScriptExampleClick(object s,RoutedEventArgs e)=>Run(()=>{if(vm.Plan.Scripts.Count>=60)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.bca97a516f08"));var network=s is Button {Tag:"network"};var row=new ScriptEntry{Enabled=false,Phase=network?"FirstLogon":"System",Type=network?"Ps1":"Cmd",Content=network?CustomScripts.NetworkExample:CustomScripts.DirectoryExample};vm.Plan.Scripts.Add(row);vm.SelectedScript=row;vm.Status=global::UnattendLocal.Localization.UiText.Source("text.011496a027e6");});
 private void ImageScriptExampleClick(object s,RoutedEventArgs e)=>Run(()=>{
  if(s is not Button {Tag:string kind}||kind is not ("wallpaper" or "lockscreen"))return;
  string current=kind=="wallpaper"?vm.Plan.WallpaperImageScript:vm.Plan.LockScreenImageScript;
  if(!string.IsNullOrWhiteSpace(current)&&LocalizedMessageBox.Show(this,global::UnattendLocal.Localization.UiText.Source("text.2878115cdb8d"),global::UnattendLocal.Localization.UiText.Source("text.a146f0457cab"),MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
  if(kind=="wallpaper")vm.Plan.WallpaperImageScript=ImageBackgroundSettings.DesktopExample;
  else vm.Plan.LockScreenImageScript=ImageBackgroundSettings.LockExample;
  vm.Status=global::UnattendLocal.Localization.UiText.Source("text.3fc478dffcf4");
 });
 private void DiskScriptExampleClick(object s,RoutedEventArgs e)=>Run(()=>{
  if(!string.IsNullOrWhiteSpace(vm.Plan.DiskPartScript)&&LocalizedMessageBox.Show(this,global::UnattendLocal.Localization.UiText.Source("text.09aa7388671d"),global::UnattendLocal.Localization.UiText.Source("text.79226b2f898e"),MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
  vm.Plan.DiskPartScript=DiskScriptSettings.Example;vm.Plan.ClearDiskConsent();vm.Status=global::UnattendLocal.Localization.UiText.Source("text.49f5ba2b6d7a");
 });
 private void ValidateLayoutClick(object s,RoutedEventArgs e)=>Run(()=>{if(s is Button {Tag:string kind}){ShellSettings.ValidateDocument(kind,LayoutContent(kind));vm.Status=global::UnattendLocal.Localization.UiText.Source("text.6fabc8fd41e7");}});
 private void StartFoldersIllustrationClick(object s,RoutedEventArgs e)=>Run(()=>new StartFoldersIllustrationWindow{Owner=this}.ShowDialog());
 private void ColorPickerClick(object s,RoutedEventArgs e)=>Run(()=>{
  bool accent=s is Button {Tag:"accent"};
  var dialog=new ColorPickerWindow(accent?global::UnattendLocal.Localization.UiText.Source("text.6dc739b4e978"):global::UnattendLocal.Localization.UiText.Source("text.8efdfa24f05f"),accent?vm.Plan.AccentColorHex:vm.Plan.WallpaperColorHex){Owner=this};
  if(dialog.ShowDialog()!=true)return;
  if(dialog.RestoreWindowsDefault){if(accent)vm.Plan.ColorSettingsMode=ColorSettings.Default;else vm.Plan.WallpaperMode=WallpaperSettings.Default;}
  else if(accent)vm.Plan.AccentColorHex=dialog.SelectedHex;else vm.Plan.WallpaperColorHex=dialog.SelectedHex;
 });
 private void ExportClick(object s,RoutedEventArgs e)=>ExportAnswerFile(false);
 private void ExportIsoClick(object s,RoutedEventArgs e)=>ExportAnswerFile(true);
 private void ExportAnswerFile(bool iso)=>Run(()=>{var bytes=Generate();if((InstallationSettings.Destructive(vm.Plan)||vm.HasImportedXmlRisk)&&LocalizedMessageBox.Show(this,global::UnattendLocal.Localization.UiText.Concat(new object?[] { InstallationReview.ExportSummary(bytes), "\n", InstallationSettings.DiskSummary(vm.Plan), (vm.HasImportedXmlRisk?global::UnattendLocal.Localization.UiText.Source("text.c4357e3b2c27"):""), "\n\n", DiskSafety.Risk, "\n\n", DiskSafety.Preparation, "\n\n", DiskSafety.Responsibility, global::UnattendLocal.Localization.UiText.Source("text.52007b38f82f") },new bool[]{false,false,false,false,false,false,false,false,false,false,false}),global::UnattendLocal.Localization.UiText.Source("text.74dff760c77e"),MessageBoxButton.YesNo,MessageBoxImage.Warning,MessageBoxResult.No)!=MessageBoxResult.Yes)return;bool manual=vm.Plan.ExportNonAutomaticFilename;var d=new SaveFileDialog{Title=global::UnattendLocal.Localization.UiText.Translate(iso?global::UnattendLocal.Localization.UiText.Source("text.c6c90fe1aedd"):global::UnattendLocal.Localization.UiText.Source("text.fd7a9b584e6d")),Filter=global::UnattendLocal.Localization.UiText.Translate(iso?global::UnattendLocal.Localization.UiText.Source("text.893861008ab4"):global::UnattendLocal.Localization.UiText.Source("text.d5d85641e8a3")),DefaultExt=iso?".iso":".xml",AddExtension=true,OverwritePrompt=true,FileName=iso?AnswerFileExport.IsoName(manual):AnswerFileExport.FileName(manual)};if(d.ShowLocalized(this)!=true)return;if(iso)AnswerFileExport.WriteIso(d.FileName,bytes,manual);else AnswerFileExport.WriteXml(d.FileName,bytes);vm.Status=global::UnattendLocal.Localization.UiText.Concat(new object?[] { (iso?global::UnattendLocal.Localization.UiText.Source("text.c1b71c21eff7"):global::UnattendLocal.Localization.UiText.Source("text.bc67c530f6df")), InstallationReview.ExportSummary(bytes) },new bool[] { false, false });}, locateConfiguration:true);
}
