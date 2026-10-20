using System.Text;
using UnattendLocal.Core;
using UnattendLocal.Desktop;

static class ErrorLocationTests
{
    public static int Run(GeneratorService generator)
    {
        int count=0;
        void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Console.WriteLine("PASS error location "+name);}
        InstallPlan Plan(){var p=InstallPlan.New();p.Users[0].Password="LocationTest1!";p.UseStaticIPv4=true;p.StaticIPv4Mac="02-AB-00-00-00-10";p.StaticIPv4Address="192.168.100.20";return p;}
        void Expect(InstallPlan p,string field,string target,int? index=null){
            string before=PlanStore.Serialize(p,true);
            try{_=generator.Generate(p);}catch(ConfigurationValidationException error){
                Check(error.Field==field&&error.UserIndex==index,"generator identifies field "+field);
                var location=ConfigurationErrorLocation.FromException(error);
                Check(location is not null&&location.Setting.Target==target&&location.UserIndex==index,"location resolves exact target "+target);
                Check(PlanStore.Serialize(p,true)==before,"error lookup does not mutate plan "+field);
                return;
            }
            throw new Exception("Expected typed validation error "+field);
        }
        var p=Plan();p.StaticIPv4Mac="";Expect(p,nameof(p.StaticIPv4Mac),"StaticIPv4MacEditor");
        p=Plan();p.StaticIPv4Address="192.168.100.0";Expect(p,nameof(p.StaticIPv4Address),"StaticIPv4AddressEditor");
        p=Plan();p.UseStaticIPv4SubnetMask=true;p.StaticIPv4SubnetMask="/";Expect(p,nameof(p.StaticIPv4SubnetMask),"StaticIPv4SubnetEditor");
        p=Plan();p.StaticIPv4Prefix=0;Expect(p,nameof(p.StaticIPv4SubnetMask),"StaticIPv4SubnetEditor");
        p=Plan();p.StaticIPv4Gateway="192.168.99.1";Expect(p,nameof(p.StaticIPv4Gateway),"StaticIPv4GatewayEditor");
        p=Plan();p.StaticIPv4Dns="1.1.1.1,1.1.1.1";Expect(p,nameof(p.StaticIPv4Dns),"StaticIPv4DnsEditor");
        foreach(string field in new[]{nameof(p.StaticIPv6Address),nameof(p.StaticIPv6Prefix),nameof(p.StaticIPv6Gateway),nameof(p.StaticIPv6Dns)}){
            p=Plan();p.UseStaticIPv4=false;p.UseStaticIPv6=true;p.StaticIPv6Address="2001:db8:1::20";
            if(field==nameof(p.StaticIPv6Address))p.StaticIPv6Address="bad";
            if(field==nameof(p.StaticIPv6Prefix))p.StaticIPv6Prefix=0;
            if(field==nameof(p.StaticIPv6Gateway))p.StaticIPv6Gateway="2001:db8:2::1";
            if(field==nameof(p.StaticIPv6Dns))p.StaticIPv6Dns="bad";
            Expect(p,field,field+"Editor");
        }
        p=Plan();p.UseSetupDrivers=true;p.SetupDriverPaths="";Expect(p,nameof(p.SetupDriverPaths),"SetupDriverPathsEditor");
        p=Plan();p.Users.Add(new UserEntry{Name="Second",Administrator=false});Expect(p,"Users.Password","UserPasswordEditor",1);
        p=Plan();p.Users[0].Name="";Expect(p,"Users.Name","UserNameEditor",0);
        p=Plan();p.UseBuiltinAdministrator=true;p.BuiltinAdministratorPassword="";Expect(p,nameof(p.BuiltinAdministratorPassword),"BuiltinAdministratorPassword");
        p=Plan();p.EditionMode="指定自己的产品密钥";p.CustomProductKey="";Expect(p,nameof(p.CustomProductKey),"SettingAnchor044");
        p=Plan();p.UseActivationKey=true;p.ActivationProductKey="";Expect(p,nameof(p.ActivationProductKey),"SettingAnchor047");
        p=Plan();p.AutoLogon=false;p.KeepSensitiveFiles=false;Expect(p,nameof(p.KeepSensitiveFiles),"SettingAnchor140");
        p=Plan();p.TimeZone="";p.TimeZoneMode="指定时区";Expect(p,nameof(p.TimeZone),"SettingAnchor022");
        p=Plan();string original=Encoding.UTF8.GetString(generator.Generate(p)).Replace("192.168.100.20/24","192.168.100.20/0");
        p.UseStaticIPv4=false;p.ImportedXml=new ImportedXml{SourceXml=original,StaticIPv4ReadOnly=true};Expect(p,nameof(p.ImportedXml),"SettingAnchor000");
        Check(ConfigurationErrorLocation.FromException(new ArgumentException("MAC 前缀 /24 密码错误")) is null,"free text is never guessed into a field");
        Check(ConfigurationErrorLocation.FromField("unknown-property") is null,"unknown field provides no misleading destination");
        var binding=ConfigurationErrorLocation.FromField("SettingAnchor057");
        Check(binding?.Setting.PageIndex==3&&binding.Setting.Target=="SettingAnchor057","binding numeric errors resolve the actual control");
        Check(ConfigurationErrorLocation.FromField(nameof(p.StaticIPv6Dns))?.Setting.PageIndex==5,"IPv6 errors retain network page destination");
        Check(ConfigurationErrorLocation.FromField("Users.Password",1)?.Path.Contains("第 2 个账户")==true,"account destination shows the relevant row");
        Check(ConfigurationErrorLocation.FromField(nameof(p.ImportedXml))?.ActionText=="查看导入说明", "source XML errors offer source guidance rather than an editable network field");
        return count;
    }
}
