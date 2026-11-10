Add-Type -AssemblyName 'Microsoft.VisualBasic';
function Test-ComputerName {
	[CmdletBinding()]
	param(
		[string]
		$Value
	);
	
	if( [string]::IsNullOrWhiteSpace( $Value ) ) {
		return $false;
	}
	
	if( $Value.Length -gt 15 ) {
		return $false;
	}
	
	if( $Value -match '\s' ) {
		return $false;
	}
	
	if( $Value -match '^\d+$' ) {
		return $false;
	}

	if( $Value.IndexOfAny( '{|}~[\]^'':;<=>?@!"#$%`()+/.,*&'.ToCharArray() ) -gt -1 ) {
		return $false;
	}
	
	return $true;
}
if( ( Get-UICulture ).Name -match '^(?:zh-CN|zh-SG|zh-Hans(?:-.+)?)$' ) {
	$title = '计算机名';
	$prompt = '请输入有效的计算机名：';
	$invalid = '“{0}”不是有效的计算机名。';
} else {
	$title = 'Computer name';
	$prompt = 'Enter a valid computer name:';
	$invalid = "'{0}' is not a valid computer name.";
}
$candidate = '';
while( $true ) {
	$candidate = [Microsoft.VisualBasic.Interaction]::InputBox( $prompt, $title, $candidate );
	if( Test-ComputerName -Value $candidate ) {
		return $candidate;
	} else {
		[Microsoft.VisualBasic.Interaction]::MsgBox( ( $invalid -f $candidate ), 'Critical, OkOnly', $title ) | Out-Null;
	}
}
