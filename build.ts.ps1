Param(
	[string] $pkgversion,
	[string] $cliversion
)

if (-not $pkgversion) {
    Write-Host "Version for botbuilder-* required!.  Please use the param -pkgversion" -ForegroundColor DarkRed
	Break
}

if (-not $cliversion) {
    Write-Host "Version for botskills CLI required!.  Please use the param -cliversion" -ForegroundColor DarkRed
	Break
}

pushd .\lib\typescript

rush update

pushd .\botskills

npm version $($cliversion) --allow-same-version
npm run build

popd

pushd .\botbuilder-solutions

npm version $($pkgversion) --allow-same-version
npm run build

popd

pushd .\botbuilder-skills

npm version $($pkgversion) --allow-same-version
npm run build

popd

popd

pushd .\templates\Virtual-Assistant-Template\typescript\generator-botbuilder-assistant

npm version $($pkgversion) --allow-same-version
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
