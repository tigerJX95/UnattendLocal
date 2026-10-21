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
   if(e.Data.GetData(DataFormats.FileDrop) is not string[] paths)throw new ArgumentException("无法读取拖入的文件，本次未导入。");
   string path=PlanFileImport.SingleFile(paths);
   if(ImportConfigurationFile(path))e.Effects=DragDropEffects.Copy;
  }catch(Exception error){vm.Status="文件导入失败，当前配置未改变。";ErrorDialog.Show(this,error.Message,"文件导入失败");}
 }
 private bool ImportConfigurationFile(string path){
  var previous=loading;loading=true;string type="文件";
  try{
   var kind=PlanFileImport.Kind(path);type=kind==PlanFileKind.AnswerXml?"XML":"JSON 方案";
   string text=PlanFileImport.Read(path);
   bool Confirm()=>MessageBox.Show(this,
    kind==PlanFileKind.AnswerXml?
    "导入 XML 将替换当前界面中的配置，当前未保存的修改将被放弃。\n已识别设置会映射到界面，其他内容保留原文；无法可靠合并的修改会在导出时被阻止。\n\n选择“是”：导入并替换当前配置。\n选择“否”：取消整次导入，保留当前配置。\n是否继续导入？":
    "载入 JSON 方案将替换当前界面中的配置，当前未保存的修改将被放弃。\n方案中省略的密码和密钥需重新输入。\n\n选择“是”：载入并替换当前配置。\n选择“否”：取消整次载入，保留当前配置。\n是否继续？",
    "确认导入"+type,MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No)==MessageBoxResult.Yes;
   bool accepted=kind==PlanFileKind.AnswerXml?vm.ImportXml(text,Confirm):vm.ImportJson(text,Confirm);
   if(!accepted){MessageBox.Show(this,vm.Status,type+"导入已取消",MessageBoxButton.OK,MessageBoxImage.Information);return false;}
   string status=vm.Status;Load(vm.Plan);Pages.SelectedIndex=0;vm.Status=status;return true;
  }catch(Exception error){vm.Status=type+"导入失败，当前配置未改变。";ErrorDialog.Show(this,error.Message,type+"导入失败");return false;}
  finally{loading=previous;}
 }
}
