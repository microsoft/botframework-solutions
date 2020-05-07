versionString=`dotnet --version`
function version_lt() { test "$(echo "$@" | tr " " "\n" | sort -rV | head -n 1)" != "$1"; }
if version_lt $versionString "3.1.0";
then
    echo "! dotnet core 3.1 is required, please refer following documents for help.
https://dotnet.microsoft.com/download/dotnet-core/3.1"
	exit 1
else
	dotnet user-secrets init
	dotnet build
fi
