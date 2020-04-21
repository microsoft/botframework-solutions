Param(
	[string] $version
)

if (-not $version) {
    Write-Host "Version for generator-bot-virtualassistant required!.  Please use the param -version" -ForegroundColor DarkRed
}

pushd templates/typescript/generator-bot-virtualassistant

npm install
npm version $($version) --allow-same-version
npm run lint

popd

if (-not(test-path ".\outputpackages"))
{
      New-Item -ItemType directory -Path ".\outputpackages"
}

pushd .\outputpackages

npm pack ..\templates\typescript\generator-bot-virtualassistant

popd
