rd .\BuildResults /S /Q
md .\BuildResults
rd .\MyProject\Bin\Release  /S /Q

set msBuildDir=%programfiles(x86)%\MSBuild\14.0\Bin
call "%msBuildDir%\msbuild.exe"  KannaBot.sln /p:Configuration=Debug /l:FileLogger,Microsoft.Build.Engine;logfile=Manual_MSBuild_ReleaseVersion_LOG.log

set mypath=%cd%
echo %mypath%
XCOPY "%mypath%\bin\Debug" "%mypath%\BuildResults" /d/y
echo "%mypath%\BuildResults\KannaBot.exe"
start "" "%mypath%\BuildResults\KannaBot.exe"
REM pause