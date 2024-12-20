# Wayback Diff Viewer
## Faster than the web client


### About
This is a small tool I put together to more easily comb through wayback machine internet archive results for legal research. It takes in a URL, processes the data, and returns a useable dataset with only textually unique versions of the page.

### Installation
Download either the Windows or Linux build from the [Latest Release](https://github.com/CanadianBaconBoi/WaybackMachineDiff/releases/latest)

Or, build it using the instructions below.

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
