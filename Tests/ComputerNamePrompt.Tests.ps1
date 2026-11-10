$ErrorActionPreference = 'Stop'
$path = Join-Path $PSScriptRoot '../Core/Resources/ComputerNamePrompt.ps1'
$script = [IO.File]::ReadAllText($path)

# Intercept the two VB dialog calls while running the production script in PowerShell.
if (-not ('ComputerNameTestDialog' -as [type])) {
    Add-Type -TypeDefinition @'
public static class ComputerNameTestDialog
{
    public static int Prompts;
    public static string Prompt;
    public static string Title;
    public static string Error;
    public static string ErrorTitle;
    public static string Buttons;
    public static string Default;

    public static string InputBox(string prompt, string title, string defaultValue)
    {
        Prompt = prompt;
        Title = title;
        Default = defaultValue;
        switch (++Prompts)
        {
            case 1: return "123";
            case 2: return "SETUP-PC";
            default: throw new System.InvalidOperationException("A valid name should end the prompt loop.");
        }
    }

    public static void MsgBox(string message, string buttons, string title)
    {
        Error = message;
        ErrorTitle = title;
        Buttons = buttons;
    }
}
'@
}

$promptScript = [scriptblock]::Create($script.Replace('[Microsoft.VisualBasic.Interaction]', '[ComputerNameTestDialog]'))
function Get-UICulture { [pscustomobject]@{ Name = $case.Language } }
foreach ($case in @(
    @{ Language = 'zh-CN'; Title = '计算机名'; Prompt = '请输入有效的计算机名：'; Error = '“123”不是有效的计算机名。' },
    @{ Language = 'zh-Hans'; Title = '计算机名'; Prompt = '请输入有效的计算机名：'; Error = '“123”不是有效的计算机名。' },
    @{ Language = 'zh-SG'; Title = '计算机名'; Prompt = '请输入有效的计算机名：'; Error = '“123”不是有效的计算机名。' },
    @{ Language = 'en-US'; Title = 'Computer name'; Prompt = 'Enter a valid computer name:'; Error = "'123' is not a valid computer name." },
    @{ Language = 'de-DE'; Title = 'Computer name'; Prompt = 'Enter a valid computer name:'; Error = "'123' is not a valid computer name." }
)) {
    [ComputerNameTestDialog]::Prompts = 0
    [ComputerNameTestDialog]::Error = $null
    $result = @(& $promptScript)
    if ($result.Count -ne 1 -or $result[0] -cne 'SETUP-PC' -or [ComputerNameTestDialog]::Prompts -ne 2 -or
        [ComputerNameTestDialog]::Default -cne '123' -or [ComputerNameTestDialog]::Prompt -cne $case.Prompt -or
        [ComputerNameTestDialog]::Title -cne $case.Title -or [ComputerNameTestDialog]::ErrorTitle -cne $case.Title -or
        [ComputerNameTestDialog]::Error -cne $case.Error -or [ComputerNameTestDialog]::Buttons -cne 'Critical, OkOnly') {
        throw "Computer name prompt regression for UI language $($case.Language)."
    }
}
Write-Host 'Computer name prompt: 5 PowerShell language and validation scenarios passed.'
