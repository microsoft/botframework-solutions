Param(
	[string] $version
)

if (-not $version) {
    Write-Host "Version for generator-botbuilder-assistant required!.  Please use the param -version" -ForegroundColor DarkRed
}

pushd .\templates\Virtual-Assistant-Template\typescript\generator-botbuilder-assistant

npm install
npm version $($version) --allow-same-version
npm run lint
npm run copydeploymentscript

popd

if (-not(test-path ".\outputpackages"))
{
      New-Item -ItemType directory -Path ".\outputpackages"
}

pushd .\outputpackages

npm pack ..\templates\Virtual-Assistant-Template\typescript\generator-botbuilder-assistant

popd
