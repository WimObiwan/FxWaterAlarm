dotnet publish
pushd .
cd Admin/bin/Release/net8.0/linux-x64/publish/
pwsh -NoExit -Command "Import-Module .\WaterAlarmAdmin.dll"
popd
