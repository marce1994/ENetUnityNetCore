dotnet publish -o ./Build ./UDP.Core
copy %cd%\Build\UDP.Core.dll %cd%\UDP.Client.Unity\Assets\Plugins\x86_64 /Y