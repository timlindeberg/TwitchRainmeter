@echo on
echo Killing Rainmeter
powershell kill -n Rainmeter

set RAINMETER_DIR=%ProgramW6432%\Rainmeter
set DIST=%RAINMETER_DIR%\Plugins
set TARGET_DIR=%*

echo "%TARGET_DIR%\TwitchChat.dll"
echo "%DIST%\TwitchChat.dll"
echo f | xcopy /yfo "%TARGET_DIR%\TwitchChat.dll" "%DIST%\TwitchChat.dll"

exit 0
