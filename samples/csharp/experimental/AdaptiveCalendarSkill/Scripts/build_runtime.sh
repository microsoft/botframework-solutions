versionString=`dotnet --version`
versionNum=`echo $versionString | cut -d . -f 1`
if [[ $versionNum -lt 3 ]]
then
    echo "! dotnet core 3.0 is required, please refer following documents for help.
https://dotnet.microsoft.com/download/dotnet-core/3.0"
	exit 1
else
	dotnet user-secrets init
	dotnet build
fi
