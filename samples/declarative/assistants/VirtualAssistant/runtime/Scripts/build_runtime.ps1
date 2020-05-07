if ((dotnet --version) -lt '3.1.0') {
	throw "! dotnet core 3.1 is required, please refer following documents for help. https://dotnet.microsoft.com/download/dotnet-core/3.1"
	Break
}

# This command need dotnet core more than 3.0
dotnet user-secrets init

# Merge all streams into stdout
$result = dotnet build *>&1
# Evaluate success/failure
if($LASTEXITCODE -ne 0)
{
    # Failed, you can reconstruct stderr strings with:
    $ErrorString = $result -join [System.Environment]::NewLine
	throw $ErrorString
}
