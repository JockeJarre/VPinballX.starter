# VPinballX.starter
Small tool to start the right VPinballX version depending on the table file

Using a small ini file similar to this one:

``` ini
;A Configuration file for VPinballX.starter
[VPinballX.starter]
;DefaultVersion when started without any table param.
DefaultVersion=10.80
LogVersions=1
;cmd files to run before and after a table has been started. Activate here:
PREPOSTactive=false
;The first argument will become the table name, complete command line parameters follow
FirstArgTableName=true
;The filename extension for VPinballX.starter.pre.cmd and tablename.pre.cmd
PREcmdExtension=.pre.cmd
POSTcmdExtension=.post.cmd

;you can have different settings depending on the caller:
;First VPinballX.starter.preexplorer.cmd then VPinballX.starter.pre.cmd
#PREcmdExtension.explorer=.preexplorer.cmd
#POSTcmdExtension.explorer=.postexplorer.cmd

; If the parent process cannot be found (Pinup popper show up as 'anonymous')
#PREcmdExtension.anonymous=.preanon.cmd
#POSTcmdExtension.anonymous=.preanon.cmd

; Add parameters to the command line
AddParameter=-Primary
AddParameter.-play=-Minimized

[TableNameExceptions]
;If left string is found in the Table filename
;we will use the right string to add to the version number search
Table Name=x32
Another Table=GL
x32=x32
GL=GL

[VPinballX]
;Default value to be used if not found in the table below.
Default=VPinballX72.exe
;File versions converted to the right VPinballXxx.exe
10.60=VPinballX62.exe
10.70=VPinballX71.exe
10.72=VPinballX72.exe
10.80=VPinballX85.exe
10.80x32=VPinballX85x32.exe
10.80GL=VPinballX85_GL.exe
```

With this information, VPinballX.starter can be used as a replacement for VPinballX.exe.

It works like this:

VPinballX.starter is started with exactly the same parameters as VPinballX.exe.

First it loads the table file and finds out what version it was saved with. Then it takes that information
and looks in the [VPinballX] table above to find out which version of VPinballX.xxx.exe you want to start.

It will then run the VPinballXxx.exe that you have configured with exactly the same parameters.

If you simply double-click the VPinballX.starter without a table, the default entry under [VPinballX.starter] will be used.

Or if it cannot find a version stored in the table, it will use the default in [VPinballX].

In this way, the correct table version or the version you have selected will be used.
Each time you start VPinballX.starter, a log entry will be added to VPinballX.starter.log stating which version was used.
This can be disabled by setting LogVersions=0.
# Table Name Exceptions
Exceptions to the automated finding the right executable can be defined in the [TableNameExceptions] table.
The Exceptions are either that a string is added to the table filename hinting that this table should use the x32 version instead,
or can be parts of a table filename pointing to the same string.
This string will be added when searched for the executable. 
E.g starting a table needing 10.80 and having an exception "GL" will look for 10.80GL in the VPinballX table.
This is made using pure string handling, there is NO logic behind the strings in the ini file.

# Download

The latest version can always be found under [Releases](https://github.com/JockeJarre/VPinballX.starter/releases) on the right of the github page.
Builds for smaller [issues](https://github.com/JockeJarre/VPinballX.starter/issues), can be found under [Actions](https://github.com/JockeJarre/VPinballX.starter/actions) but you need a github login to access them.

# How to set it up

Copy VPinballX.starter.exe next to your VPinballX.exe files. Double click on VPinballX.starter.exe and follow the instructions.
It will create a template VPinballX.starter.ini that you will need to edit to your liking. 
The values 10.72, 10.80 (10.74 has no specific version saved) and so on all come from the pinball tables saved in different versions of VPinballX.exe.

Normally it should be enough to have two of them, an exception and the two defaults:

``` ini
[VPinballX.starter]
DefaultVersion=10.74
[TableNameExceptions]
x32=x32
[VPinballX]
Default=VPinballX64.74.exe
10.74x32=VPinballX32.74.exe
10.74=VPinballX64.74.exe
10.80x32=VPinballX32.85.exe
10.80=VPinballX64.85.exe
```

Once you are happy with VPinballX.starter.exe, you can rename it to VPinballX.exe;
it will take care of starting the right version independent if you are using Explorer, PinballX, PinballY or Pinup Popper.

# PRE and POST cmd files

The settings PREPOSTactive, PREcmd and POSTcmd can be used to run windows batch cmd files before and after a certain table is opened.
PREcmd can be used to setup the stage for a certain table, loading pictures or whatever comes to mind. And POSTcmd is then used to cleanup afterwards.
As the default settings tells, it will search for a file called <tablename>.pre.cmd and call it if found.
If you start "Blood Machines (VPW 2022).vpx" it will try to find "Blood Machines (VPW 2022).pre.cmd" before starting the table.
After the table has quit, "Blood Machines (VPW 2022).post.cmd" will be searched. It will also search for VPinballX.starter.pre/post.cmd and call it for every table started.

When VPinballX.starter is triggered it tries to find the process name of the caller. When starting a table from Windows Explorer, the caller is "explorer".
This information can be used to have different PRE and POST scripts depending on the caller:

``` ini
;A Configuration file for VPinballX.starter
[VPinballX.starter]
;cmd files to run before and after a table has been started. Activate here:
PREPOSTactive=false
PREcmdExtension=.pre.cmd
POSTcmdExtension=.post.cmd
;you can have different settings depending on the caller: (Pinup popper show up as anonymous)
PREcmdExtension.explorer=.explorerpre.cmd
POSTcmdExtension.explorer=.explorerpost.cmd
```

**Be sure to not start anything in these cmd batch files which block the script!**

# Adding VPX parameters

It is possible to add parameters to the VPX calls by setting certain Configuration options:

``` ini
; Add parameters to the command line
AddParameter=-Primary
AddParameter.-play=-Minimized
```

AddParameter on it's own means it is always added independent on other command line options. `AddParameter.-play` means that the parameters are only added if the current command line contain `-play`. The above example set VPX to use the Primary screen and when started as "play", the VPX windows is minimized.
