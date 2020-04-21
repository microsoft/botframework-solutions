Param(
	[string] $version
)

if (-not $version) {
    Write-Host "Version for bot-libs required!.  Please use the param -version" -ForegroundColor DarkRed
}

pushd sdk\typescript\libraries

node .\common\scripts\install-run-rush.js install --no-link

node .\common\scripts\install-run-rush.js link

pushd .\bot-solutions

npm version $($version) --allow-same-version
npm run build

popd

popd

if (-not(test-path ".\outputpackages"))
{
      New-Item -ItemType directory -Path ".\outputpackages"
}

pushd .\outputpackages

npm pack ..\sdk\typescript\libraries\bot-solutions

popd
