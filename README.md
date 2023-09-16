# VPinballX.starter
Small tool to start the right VPinballX version depending on the table file

Welcome new VPinballX.starter user!

Using a small ini file similar to this one:

``` ini
[VPinballX.starter]
;DefaultVersion is used when VPinballX.starter is started without any table vpx file parameter.
DefaultVersion=10.80

[VPinballX]
;This Default value is used when the version set in the table vpx file isn't configured below.
Default=VPinballX72.exe
;The different versions stored in vpx files and converted to the right VPinballX.exe
10.60=VPinballX62.exe
10.70=VPinballX71.exe
10.72=VPinballX72.exe
10.80=VPinballX85.exe
```

With this information VPinballX.starter can be used as a replacement for VPinballX.exe.

It works like this:

VPinballX.starter is started with exactly the same parameters as VPinballX.exe.

First it loads the table file and finds out what version it was saved with. Then it takes that information
and looks in the [VPinballX] table above to find out which version of VPinballX.xxx.exe you want to start.

It will then run the VPinballX.xxx.exe that you have configured using exactly the same parameters.

If it cannot find a version in the table, or if you simply double-click the VPinballX.starter without any table,
the default entry under [VPinballx.starter] will be used.

This way the correct table version or the version you have chosen will be used.
Each time you start VPinballX, a log entry will be added to VPinballX.starter.log, telling which version was used.
