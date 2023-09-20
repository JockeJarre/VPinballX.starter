# VPinballX.starter
Small tool to start the right VPinballX version depending on the table file

Using a small ini file similar to this one:

``` ini
;A Configuration file for VPinballX.starter
[VPinballX.starter]
;DefaultVersion when started without any table param.
DefaultVersion=10.80
LogVersions=1
[VPinballX]
;Default value to be used if not found in the table below.
Default=VPinballX72.exe
;File versions converted to the right VPinballXxx.exe
10.60=VPinballX62.exe
10.70=VPinballX71.exe
10.72=VPinballX72.exe
10.80=VPinballX85.exe
```

With this information VPinballX.starter can be used as a replacement for VPinballX.exe.

It works like this:

VPinballX.starter is started with exactly the same parameters as VPinballX.exe.

First it loads the table file and finds out what version it was saved with. Then it takes this information
and looks in the [VPinballX] table above to find out which version of VPinballX.xxx.exe you want to start.

It will then run the VPinballXxx.exe that you have configured with exactly the same parameters.

If you simply double-click the VPinballX.starter without a table, the default entry under [VPinballX.starter] will be used.

Or if it cannot find a version stored in the table, the default in [VPinballX] will be used.

In this way, the correct table version or the version you have selected will be used.
Each time you start VPinballX.starter, a log entry will be added to VPinballX.starter.log telling which version was used.
This can be disabled with setting LogVersions=0.
