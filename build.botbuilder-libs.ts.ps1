Param(
	[string] $version
)

if (-not $version) {
    Write-Host "Version for botbuilder-libs required!.  Please use the param -version" -ForegroundColor DarkRed
}

pushd .\lib\typescript

node .\common\scripts\install-run-rush.js install --no-link

node .\common\scripts\install-run-rush.js link

pushd .\botbuilder-solutions

npm version $($version) --allow-same-version
npm run build

popd

pushd .\botbuilder-skills

npm version $($version) --allow-same-version
npm run build

popd

popd

if (-not(test-path ".\outputpackages"))
{
      New-Item -ItemType directory -Path ".\outputpackages"
}

pushd .\outputpackages

npm pack ..\lib\typescript\botbuilder-solutions
npm pack ..\lib\typescript\botbuilder-skills

popd
