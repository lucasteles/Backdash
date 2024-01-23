[CmdletBinding()]
Param(
    [Parameter(Position = 0, Mandatory = $false, ValueFromRemainingArguments = $true)]
    [string[]]$BuildArguments
)

$ScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent
$BuildProjectFile = "$ScriptRoot\build\_build.csproj"

dotnet tool restore | Out-Null
dotnet build $BuildProjectFile /nodeReuse:false /p:UseSharedCompilation=false -nologo -clp:NoSummary --verbosity quiet
dotnet run --project $BuildProjectFile --no-build -- $BuildArguments
