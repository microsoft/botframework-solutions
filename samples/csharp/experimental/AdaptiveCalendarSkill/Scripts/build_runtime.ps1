if ((dotnet --version) -lt 3) {
	throw "! dotnet core 3.0 is required, please refer following documents for help. https://dotnet.microsoft.com/download/dotnet-core/3.0"
	Break
}

# This command need dotnet core more than 3.0
dotnet user-secrets init

dotnet build
