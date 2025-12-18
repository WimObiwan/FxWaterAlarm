dotnet publish --configuration Debug
pushd .
cd Admin/bin/Debug/net10.0/linux-x64/publish/
pwsh -NoExit -Command "Import-Module .\WaterAlarmAdmin.dll; Write-Warning 'You can attach the debugger to ""pwsh"" now.'"
popd
