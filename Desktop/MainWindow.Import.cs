using System.Windows;
using UnattendLocal.Core;
namespace UnattendLocal.Desktop;

public partial class MainWindow {
 private void InitializeConfigurationDrop(){
  AllowDrop=true;
  AddHandler(DragDrop.PreviewDragOverEvent,new DragEventHandler(ConfigurationDragOver),true);
  AddHandler(DragDrop.PreviewDropEvent,new DragEventHandler(ConfigurationDrop),true);
 }
 private void ConfigurationDragOver(object sender,DragEventArgs e){
  if(!e.Data.GetDataPresent(DataFormats.FileDrop))return;
  e.Handled=true;e.Effects=DragDropEffects.None;
  try{
   if(loading||vm.IsLoadingPlan)return;
   if(e.Data.GetData(DataFormats.FileDrop) is not string[] paths)return;
   _=PlanFileImport.SingleFile(paths);
   if((e.AllowedEffects&DragDropEffects.Copy)!=0)e.Effects=DragDropEffects.Copy;
  }catch{e.Effects=DragDropEffects.None;}
 }
 private void ConfigurationDrop(object sender,DragEventArgs e){
  if(!e.Data.GetDataPresent(DataFormats.FileDrop))return;
  e.Handled=true;e.Effects=DragDropEffects.None;
  if(loading||vm.IsLoadingPlan)return;
  try{
   if(e.Data.GetData(DataFormats.FileDrop) is not string[] paths)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.d7361479b693"));
   string path=PlanFileImport.SingleFile(paths);
   if(ImportConfigurationFile(path))e.Effects=DragDropEffects.Copy;
  }catch(Exception error){vm.Status=global::UnattendLocal.Localization.UiText.Source("text.94a9765decb7");ErrorDialog.Show(this,error.Message,global::UnattendLocal.Localization.UiText.Source("text.582f42199cd6"));}
 }
 private bool ImportConfigurationFile(string path){
  var previous=loading;loading=true;string type=global::UnattendLocal.Localization.UiText.Source("text.39932f24fe11");
  try{
   var kind=PlanFileImport.Kind(path);type=kind==PlanFileKind.AnswerXml?"XML":global::UnattendLocal.Localization.UiText.Source("text.c0c2246e71af");
   string text=PlanFileImport.Read(path);
   bool Confirm()=>LocalizedMessageBox.Show(this,
    kind==PlanFileKind.AnswerXml?
    global::UnattendLocal.Localization.UiText.Source("text.70563728940b"):
    global::UnattendLocal.Localization.UiText.Source("text.80e519e280d6"),
    global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.4a8d6841b402"), type },new bool[]{false,false}),MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No)==MessageBoxResult.Yes;
   bool accepted=kind==PlanFileKind.AnswerXml?vm.ImportXml(text,Confirm):vm.ImportJson(text,Confirm);
   if(!accepted){LocalizedMessageBox.Show(this,vm.Status,global::UnattendLocal.Localization.UiText.Concat(new object?[] { type, global::UnattendLocal.Localization.UiText.Source("text.00cd011fe5c0") },new bool[]{false,false}),MessageBoxButton.OK,MessageBoxImage.Information);return false;}
   string status=vm.Status;Load(vm.Plan);Pages.SelectedIndex=0;vm.Status=status;return true;
  }catch(Exception error){vm.Status=global::UnattendLocal.Localization.UiText.Concat(new object?[] { type, global::UnattendLocal.Localization.UiText.Source("text.ff00d66cc95b") },new bool[]{false,false});ErrorDialog.Show(this,error.Message,global::UnattendLocal.Localization.UiText.Concat(new object?[] { type, global::UnattendLocal.Localization.UiText.Source("text.01aaebc96af1") },new bool[]{false,false}));return false;}
  finally{loading=previous;}
 }
}
