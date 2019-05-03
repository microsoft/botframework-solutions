Param(
	[string] $version
)

if (-not $version) {
    Write-Host "Version required!.  Please use the param -version" -ForegroundColor DarkRed
	Break
}

pushd .\lib\typescript

rush update

pushd .\botskills

npm version $($version) --allow-same-version
npm run build

popd

pushd .\botbuilder-solutions

npm version $($version) --allow-same-version
npm run build

popd

pushd .\botbuilder-skills

npm version $($version) --allow-same-version
npm run build

popd

popd

pushd .\templates\Virtual-Assistant-Template\typescript\generator-botbuilder-assistant

npm version $($version) --allow-same-version
npm install

popd

if (-not(test-path ".\outputpackages"))
{
      New-Item -ItemType directory -Path ".\outputpackages"
}

pushd .\outputpackages

npm pack ..\lib\typescript\botskills
npm pack ..\lib\typescript\botbuilder-solutions
npm pack ..\lib\typescript\botbuilder-skills
npm pack ..\templates\Virtual-Assistant-Template\typescript\generator-botbuilder-assistant

dir

popd
