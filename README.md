Avalonia desktop app, enter a url and diff between different versions of the page in markdown. Mostly useful for text only pages.

### Build commands
```git clone https://github.com/CanadianBaconBoi/WaybackMachineDiff/
cd WaybackMachineDiff
dotnet restore
dotnet publish -c Release -r [win-x64 or linux-x64] --self-contained true -p:PublishTrimmed=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true```
