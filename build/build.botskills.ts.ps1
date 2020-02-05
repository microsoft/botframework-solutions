Param(
	[string] $version
)

if (-not $version) {
    Write-Host "Version for botskills CLI tool required!.  Please use the param -version" -ForegroundColor DarkRed
}

pushd tools\botskills

npm install
npm version $($version) --allow-same-version
npm run build

popd

if (-not(test-path ".\outputpackages"))
{
      New-Item -ItemType directory -Path ".\outputpackages"
}

pushd .\outputpackages

npm pack ..\tools\botskills

popd
