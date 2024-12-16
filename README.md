Avalonia desktop app, enter a url and diff between different versions of the page in markdown. Mostly useful for text only pages.
### Requirements
In order to build you need .Net 8.0 or up, download that here
https://dotnet.microsoft.com/en-us/download/dotnet/8.0

### Build commands
```bash
git clone https://github.com/CanadianBaconBoi/WaybackMachineDiff/
cd WaybackMachineDiff
dotnet restore
dotnet publish -c Release -r [win-x64 or linux-x64] --self-contained true -p:PublishTrimmed=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```
