/*
The GPLv3+ License:

Copyright (C) 2023-2026 Richard Ludwig and contributors

VPinballX.starter is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

VPinballX.starter is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details: <https://www.gnu.org/licenses/>.
*/

using System.Data;
using System.Windows;
using System.IO;
using System.Diagnostics;
using System.Management;
using OpenMcdf;
using System.Runtime.InteropServices;
using Salaros.Configuration;
using System.ComponentModel;
using System.Xml.Linq;
using System.Threading;
using System.Windows.Controls;


namespace VPinballX.starter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public List<string> mArgs = new List<string>();
        public static string strExeFilePath = AppDomain.CurrentDomain.BaseDirectory;
        public static string strExeFileName = AppDomain.CurrentDomain.FriendlyName + ".exe";
        public static string strIniConfigFilename = "VPinballX.starter.ini";
        public static string strLogFilename = Path.Combine(App.strExeFilePath, "VPinballX.starter.log");

        /// <summary>
        /// Allows processes to be automatically killed if this parent process unexpectedly quits.
        /// This feature requires Windows 8 or greater. On Windows 7, nothing is done.</summary>
        /// <remarks>References:
        ///  https://stackoverflow.com/a/4657392/386091
        ///  https://stackoverflow.com/a/9164742/386091 </remarks>
        public static class ChildProcessTracker
        {
            /// <summary>
            /// Add the process to be tracked. If our current process is killed, the child processes
            /// that we are tracking will be automatically killed, too. If the child process terminates
            /// first, that's fine, too.</summary>
            /// <param name="process"></param>
            public static void AddProcess(Process process)
            {
                if (s_jobHandle != IntPtr.Zero)
                {
                    bool success = AssignProcessToJobObject(s_jobHandle, process.Handle);
                    if (!success && !process.HasExited)
                        throw new Win32Exception();
                }
            }

            static ChildProcessTracker()
            {
                // This feature requires Windows 8 or later. To support Windows 7 requires
                //  registry settings to be added if you are using Visual Studio plus an
                //  app.manifest change.
                //  https://stackoverflow.com/a/4232259/386091
                //  https://stackoverflow.com/a/9507862/386091
                if (Environment.OSVersion.Version < new System.Version(6, 2))
                    return;

                // The job name is optional (and can be null) but it helps with diagnostics.
                //  If it's not null, it has to be unique. Use SysInternals' Handle command-line
                //  utility: handle -a ChildProcessTracker
                string jobName = "ChildProcessTracker" + Process.GetCurrentProcess().Id;
                s_jobHandle = CreateJobObject(IntPtr.Zero, jobName);

                var info = new JOBOBJECT_BASIC_LIMIT_INFORMATION();

                // This is the key flag. When our process is killed, Windows will automatically
                //  close the job handle, and when that happens, we want the child processes to
                //  be killed, too.
                info.LimitFlags = JOBOBJECTLIMIT.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;

                var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
                extendedInfo.BasicLimitInformation = info;

                int length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
                IntPtr extendedInfoPtr = Marshal.AllocHGlobal(length);
                try
                {
                    Marshal.StructureToPtr(extendedInfo, extendedInfoPtr, false);

                    if (!SetInformationJobObject(s_jobHandle, JobObjectInfoType.ExtendedLimitInformation,
                        extendedInfoPtr, (uint)length))
                    {
                        throw new Win32Exception();
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(extendedInfoPtr);
                }
            }

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
            static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string name);

            [DllImport("kernel32.dll")]
            static extern bool SetInformationJobObject(IntPtr job, JobObjectInfoType infoType,
                IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

            [DllImport("kernel32.dll", SetLastError = true)]
            static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

            // Windows will automatically close any open job handles when our process terminates.
            //  This can be verified by using SysInternals' Handle utility. When the job handle
            //  is closed, the child processes will be killed.
            private static readonly IntPtr s_jobHandle;
        }

        public enum JobObjectInfoType
        {
            AssociateCompletionPortInformation = 7,
            BasicLimitInformation = 2,
            BasicUIRestrictions = 4,
            EndOfJobTimeInformation = 6,
            ExtendedLimitInformation = 9,
            SecurityLimitInformation = 5,
            GroupInformation = 11
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public Int64 PerProcessUserTimeLimit;
            public Int64 PerJobUserTimeLimit;
            public JOBOBJECTLIMIT LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public UInt32 ActiveProcessLimit;
            public Int64 Affinity;
            public UInt32 PriorityClass;
            public UInt32 SchedulingClass;
        }

        [Flags]
        public enum JOBOBJECTLIMIT : uint
        {
            JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IO_COUNTERS
        {
            public UInt64 ReadOperationCount;
            public UInt64 WriteOperationCount;
            public UInt64 OtherOperationCount;
            public UInt64 ReadTransferCount;
            public UInt64 WriteTransferCount;
            public UInt64 OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }
        public static class Native
        {
            public const int MB_OK = (int)0x00000000L;
            public const int MB_OKCANCEL = (int)0x00000001L;
            public const int MB_ABORTRETRYIGNORE = (int)0x00000002L;
            public const int MB_YESNOCANCEL = (int)0x00000003L;
            public const int MB_YESNO = (int)0x00000004L;
            public const int MB_RETRYCANCEL = (int)0x00000005L;
            public const int MB_CANCELTRYCONTINUE = (int)0x00000006L;

            public const int MB_ICONHAND = (int)0x00000010L;
            public const int MB_ICONQUESTION = (int)0x00000020L;
            public const int MB_ICONEXCLAMATION = (int)0x00000030L;
            public const int MB_ICONASTERISK = (int)0x00000040L;

            public const int MB_USERICON = (int)0x00000080L;

            public const int MB_DEFBUTTON1 = (int)0x00000000L;
            public const int MB_DEFBUTTON2 = (int)0x00000100L;
            public const int MB_DEFBUTTON3 = (int)0x00000200L;
            public const int MB_DEFBUTTON4 = (int)0x00000300L;

            public const int MB_APPLMODAL = (int)0x00000000L;
            public const int MB_SYSTEMMODAL = (int)0x00001000L;
            public const int MB_TASKMODAL = (int)0x00002000L;

            public const int MB_HELP = (int)0x00004000L; // Help Button

            public const int MB_NOFOCUS = (int)0x00008000L;
            public const int MB_SETFOREGROUND = (int)0x00010000L;
            public const int MB_DEFAULT_DESKTOP_ONLY = (int)0x00020000L;

            public const int MB_TOPMOST = (int)0x00040000L;
            public const int MB_RIGHT = (int)0x00080000L;
            public const int MB_RTLREADING = (int)0x00100000L;

            public const int IDABORT = (int)3;
            public const int IDCANCEL = (int)2;
            public const int IDCONTINUE = (int)11;
            public const int IDIGNORE = (int)5;
            public const int IDNO = (int)7;
            public const int IDOK = (int)1;
            public const int IDRETRY = (int)4;
            public const int IDTRYAGAIN = (int)10;
            public const int IDYES = (int)6;

            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern int MessageBoxW(
            IntPtr hWnd,
            [param: MarshalAs(UnmanagedType.LPWStr)] string lpText,
            [param: MarshalAs(UnmanagedType.LPWStr)] string lpCaption,
            UInt32 uType);

            [DllImport("user32.dll")]
            public static extern bool SetForegroundWindow(IntPtr hWnd);

            [DllImport("user32.dll")]
            public static extern bool IsIconic(IntPtr hWnd);

            [DllImport("user32.dll")]
            public static extern IntPtr SetFocus(IntPtr hWnd);

            [DllImport("user32.dll")]
            public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

            public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

            [DllImport("user32.dll")]
            public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

            [DllImport("user32.dll")]
            public static extern short GetAsyncKeyState(int vKey);

            [DllImport("shell32.dll", CharSet = CharSet.Auto)]
            public static extern uint ExtractIconEx(
                string szFileName,
                int nIconIndex,
                IntPtr[]? phiconLarge,
                IntPtr[]? phiconSmall,
                uint nIcons);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool DestroyIcon(IntPtr hIcon);

            [DllImport("user32.dll")]
            public static extern bool GetKeyboardState(byte[] keyStates);
        }
        private void Application_Startup(object sender, StartupEventArgs eventArgs)
        {
            string parentProcessName = ParentProcessName();

            if (eventArgs.Args.Length > 0)
            {
                mArgs.AddRange(eventArgs.Args);
            }

            // Check keyboard state for ActivateConfig and ActivateSetting
            string activateConfigNumLock = "";
            string activateConfigScrollLock = "";
            string activateSettingNumLock = "";
            string activateSettingScrollLock = "";
            
            // Check if NumLock or ScrollLock keys are currently toggled
            // Using GetKeyboardState to properly detect toggle states of keyboard keys
            byte[] keyboardStateArray = new byte[256];
            Native.GetKeyboardState(keyboardStateArray);
            
            bool numLockPressed = (keyboardStateArray[0x90] & 0x01) != 0;
            bool scrollLockPressed = (keyboardStateArray[0x91] & 0x01) != 0;
            
            // Log keyboard state at startup
            string keyboardStatus = $"Keyboard state - NumLock: {(numLockPressed ? "ON" : "OFF")}, ScrollLock: {(scrollLockPressed ? "ON" : "OFF")}";
            LogToFile(keyboardStatus);
            
            // If keys are pressed, check for corresponding ActivateConfig and ActivateSetting entries
            if (numLockPressed)
            {
                activateConfigNumLock = "ActivateConfig.NumLock";
                activateSettingNumLock = "ActivateSetting.NumLock";
            }
            
            if (scrollLockPressed)
            {
                activateConfigScrollLock = "ActivateConfig.ScrollLock";
                activateSettingScrollLock = "ActivateSetting.ScrollLock";
            }

            // Extract table filename from args to determine INI location
            string tableFilename = "";
            foreach (string arg in mArgs)
            {
                if (arg.Trim('"').EndsWith(".vpx", StringComparison.OrdinalIgnoreCase))
                {
                    tableFilename = arg;
                    break;
                }
            }

            // Determine INI file path with fallback: prefer table directory, then exe directory
            string strSettingsIniFilePath = Path.Combine(strExeFilePath, strIniConfigFilename);
            if (!tableFilename.Equals(""))
            {
                // We have a table file, try to find INI in same directory
                char[] charsToTrim = { '-', '/', '"' };
                string tablePathToCheck = tableFilename.Trim(charsToTrim);

                // Make path absolute if needed
                if (tablePathToCheck.Length < 2 || !tablePathToCheck.Substring(1).StartsWith(":"))
                    tablePathToCheck = $"{Directory.GetCurrentDirectory()}\\{tablePathToCheck}";

                string? tableDirectory = Path.GetDirectoryName(tablePathToCheck);
                if (!string.IsNullOrEmpty(tableDirectory))
                {
                    string iniInTableDir = Path.Combine(tableDirectory, strIniConfigFilename);

                    // Prefer INI in table directory, fall back to exe directory
                    if (File.Exists(iniInTableDir))
                    {
                        strSettingsIniFilePath = iniInTableDir;
                    }
                }
            }

            // Check for ActivateConfig entries and load alternative config if needed
            if (!string.IsNullOrEmpty(activateConfigNumLock) || !string.IsNullOrEmpty(activateConfigScrollLock))
            {
                // Load the config file to check for ActivateConfig entries
                var tempConfigFile = new ConfigParser(strSettingsIniFilePath);
                
                // Check for ActivateConfig.NumLock or ActivateConfig.ScrollLock
                string activateConfigFile = "";
                if (!string.IsNullOrEmpty(activateConfigNumLock) && tempConfigFile["VPinballX.starter"] != null)
                {
                    activateConfigFile = tempConfigFile["VPinballX.starter"][activateConfigNumLock] ?? "";
                }
                if (string.IsNullOrEmpty(activateConfigFile) && !string.IsNullOrEmpty(activateConfigScrollLock) && tempConfigFile["VPinballX.starter"] != null)
                {
                    activateConfigFile = tempConfigFile["VPinballX.starter"][activateConfigScrollLock] ?? "";
                }
                
                // If we found an ActivateConfig file, use it instead
                if (!string.IsNullOrEmpty(activateConfigFile))
                {
                    string configPath = Path.Combine(strExeFilePath, activateConfigFile);
                    if (File.Exists(configPath))
                    {
                        LogToFile($"Switching to alternative config {configPath} ");
                        strSettingsIniFilePath = configPath;
                    }
                }
            }

            try
            {

                if (!FileOrDirectoryExists(strSettingsIniFilePath))
                {
                    const string strDefaultIniConfig =
            @";A Configuration file for VPinballX.starter
[VPinballX.starter]
;ActivateConfig allows you to switch to a different ini file when setting the state of the NumLock and ScrollLock keys before starting.
#ActivateConfig.NumLock=VpinballX.starter.NumLock.ini
#ActivateConfig.ScrollLock=VPinballX.starter.ScrollLock.ini

;DefaultVersion when started without any table param.
DefaultVersion=10.80
LogVersions=true

;Window activation (title, timeout in seconds)
ActivateWindow=""Visual Pinball Player"", timeout=10

;cmd files to run before and after a table has been started. Activate here:
PREPOSTactive=false

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

;The first argument will become the table name, complete command line parameters follow
FirstArgTableName=true

; Add parameters to the command line
#AddParameter=-Primary
; Add parameter only when '-play' is already in the command line parameters
#AddParameter.-play=-Minimized

[VPinballX.starter.10.81]
;                  ^^^^^ This is the version of the selected VPinballX.exe, not the version the table was created with!
;AddParameter can be added to a version specific section
#AddParameter=-First

;AddPath can be added to a version specific section, the PATH is amended with the value.
#AddPath=C:\Program Files\VPinballX\VPinballX85\

[TableNameExceptions.NumLockVR]
Table Name=x64

[TableNameExceptions]
;If left string is found in the Table filename we will use the right string to add to the version number search
Table Name=x32
Another Table=GL
x32=x32
GL=GL
;Revert to older VPX 7.4 for certain tables
X74=.RevertX7
Old table=.RevertX7

[VPinballX]
;Default value used when not found in the table below.
Default=VPinballX85.exe
Default.RevertX7=VPinballX74.exe

;File versions converted to the right VPinballXxx.exe
;10.72=VPinballX74.exe
;^^^^^ This is the version the table was created with and loaded from the .vpx file, not the version of the selected VPinballX.exe!
10.80=VPinballX85.exe
10.80x32=VPinballX85x32.exe
10.80GL=VPinballX85_GL.exe
";

                        string strWelcomeString = BuildWelcomeDialogRtf(strIniConfigFilename, strExeFileName, strDefaultIniConfig);
                        bool createConfigFile = ShowScrollableYesNoDialog(strWelcomeString, true, "Do you want to create this file now?");
                    if (createConfigFile)
                    {
                        using (StreamWriter sw = File.CreateText(strSettingsIniFilePath))
                        {
                            sw.Write(strDefaultIniConfig);
                        }
                        Native.MessageBoxW(IntPtr.Zero, $"The config file \"{strSettingsIniFilePath}\" is created. \n\nPlease modify it too your needs. Exiting.", $"{strExeFileName}: Welcome", Native.MB_OK);
                        Environment.Exit(0);
                    }
                    if (!FileOrDirectoryExists(strSettingsIniFilePath))
                    {
                        throw new FileNotFoundException($"Configuration \"{strSettingsIniFilePath}\" cannot be found!\n\nExiting");
                    }
                }
                string[] AllTrue = new string[] { "true", "1", "yes" };

                var configFileFromPath = new ConfigParser(strSettingsIniFilePath);
                
                // Check for ActivateSetting entries and modify the configuration if needed
                string activateSettingValue = "";
                if (!string.IsNullOrEmpty(activateSettingNumLock) || !string.IsNullOrEmpty(activateSettingScrollLock))
                {
                    // Check if we have ActivateSetting entries in the config
                    if (configFileFromPath["VPinballX.starter"] != null)
                    {
                        if (!string.IsNullOrEmpty(activateSettingNumLock) && configFileFromPath["VPinballX.starter"][activateSettingNumLock] != null)
                        {
                            activateSettingValue = configFileFromPath["VPinballX.starter"][activateSettingNumLock];
                        }
                        else if (!string.IsNullOrEmpty(activateSettingScrollLock) && configFileFromPath["VPinballX.starter"][activateSettingScrollLock] != null)
                        {
                            activateSettingValue = configFileFromPath["VPinballX.starter"][activateSettingScrollLock];
                        }
                    }
                }
                
                bool logVersions = AllTrue.Any((configFileFromPath["VPinballX.starter"]["LogVersions"] ?? "false").Trim().ToLower().Contains);

                // Window activation defaults
                string activateWindowTitle = "Visual Pinball Player";
                int activateWindowTimeoutMs = 10000;

                string activateWindowSetting = configFileFromPath["VPinballX.starter"]["ActivateWindow"];
                if (!string.IsNullOrWhiteSpace(activateWindowSetting))
                {
                    foreach (var part in activateWindowSetting.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        string trimmedPart = part.Trim();

                        if (trimmedPart.StartsWith("timeout", StringComparison.OrdinalIgnoreCase))
                        {
                            string[] timeoutSplit = trimmedPart.Split('=');
                            if (timeoutSplit.Length == 2 && Int32.TryParse(timeoutSplit[1], out int timeoutSeconds))
                                activateWindowTimeoutMs = timeoutSeconds * 1000;
                        }
                        else
                        {
                            activateWindowTitle = trimmedPart.Trim('"');
                        }
                    }
                }

                if (logVersions) LogToFile($"{parentProcessName} called VPinballX.starter with [{strExeFileName} " + String.Join(" ", mArgs.Select(s => s.Contains(" ") ? $"\"{s}\"" : s).ToList()) + "]");
                List<string> argsWithTable = new List<string>();
                string defaultFileVersion = StripQuotes(configFileFromPath["VPinballX.starter"]["DefaultVersion"]);

                if (object.Equals(defaultFileVersion, null))
                {
                    throw new ArgumentException($"No\n\n[VPinballX.starter]\nDefaultVersion=10.xx\n\nfound in the ini! ({strSettingsIniFilePath})");
                }
                var fileVersion = Int32.Parse(defaultFileVersion.Replace(".", String.Empty));


                if (!tableFilename.Equals(""))
                {
                    // Somewhat Strange VPinballX behavior copied here... Remove leading - or / and quotes
                    char[] charsToTrim = { '-', '/', '"' };
                    tableFilename = tableFilename.Trim(charsToTrim);

                    // Again somewhat Strange VPinballX behavior copied here... if not a Windows drive letter first, then add current directory.
                    if (!tableFilename.Substring(1).StartsWith(":"))
                        tableFilename = $"{Directory.GetCurrentDirectory()}\\{tableFilename}";

                    if (!FileOrDirectoryExists(tableFilename))
                    {
                        LogToFile($"Table file \"{tableFilename}\" cannot be found! Please check your frontend software!");
                        throw new FileNotFoundException($"Table file\n\n{tableFilename}\n\n cannot be found!\nPlease check your frontend software!");
                    }

                    // Read the version of VPinballX.exe which saved this table
                    using var cf = RootStorage.OpenRead(tableFilename);
                    var gameStorage = cf.OpenStorage("GameStg");
                    using var versionStream = gameStorage.OpenStream("Version");
                    byte[] versionBytes = new byte[4];
                    versionStream.Read(versionBytes, 0, 4);
                    fileVersion = BitConverter.ToInt32(versionBytes, 0);
                }
                string strFileVersion = $"{fileVersion / 100}.{fileVersion % 100}";
                if (configFileFromPath["VPinballX"][strFileVersion] == null)
                    strFileVersion = "Default";

                if (!tableFilename.Equals(""))
                {
                    // Check the TableNameExceptions either for a Table Name within the list or a specific alien VPX version used (e.g x64, x32 or GL)
                    if (configFileFromPath["TableNameExceptions"] != null)
                    {
                        foreach (var key in configFileFromPath["TableNameExceptions"].Keys)
                        {
                            if (tableFilename.Contains(key.Name))
                            {
                                if (logVersions) LogToFile($"Found {key.Name} in {tableFilename}");

                                if (configFileFromPath["VPinballX"][$"{strFileVersion}{key.ValueRaw}"] != null)
                                {
                                    strFileVersion = $"{strFileVersion}{key.ValueRaw}";
                                    break;
                                }
                            }
                        }
                    }
                }
                string vpxCommand = StripQuotes(configFileFromPath["VPinballX"][strFileVersion] ?? configFileFromPath["VPinballX"]["Default"]);

                if (object.Equals(vpxCommand, null))
                    throw new ArgumentException($"No\n\n[VPinballX]\n{strFileVersion}=VPinballXxx.exe\nor\n\n\n[VPinballX]\nDefault=VPinballXxx.exe\n\nfound in the ini! ({strSettingsIniFilePath})");

                if (!Path.IsPathFullyQualified(vpxCommand))
                    vpxCommand = Path.Combine(strExeFilePath, vpxCommand);

                if (logVersions)
                {
                    if (!object.Equals(tableFilename, ""))
                        LogToFile($"Found table version {strFileVersion} of \"{tableFilename}\" mapped to \"{vpxCommand}\"");
                    else
                        LogToFile($"Using default version {strFileVersion} mapped to \"{vpxCommand}\"");
                }

                bool PREPOSTactive = AllTrue.Any((configFileFromPath["VPinballX.starter"]["PREPOSTactive"] ?? "false").Trim().ToLower().Contains);

                if (PREPOSTactive && (!tableFilename.Equals("")))
                {
                    if (AllTrue.Any((configFileFromPath["VPinballX.starter"]["FirstArgTableName"] ?? "false").Trim().ToLower().Contains)){
                        // First arg is the table filename
                        if (AllTrue.Any((configFileFromPath["VPinballX.starter"]["FirstArgTableName"] ?? "false").Trim().ToLower().Contains))
                        {
                            argsWithTable.Add(tableFilename);
                        }
                        argsWithTable.AddRange(mArgs);
                    }
                    List<string> PREcmdExtensions = new List<string> {StripQuotes(configFileFromPath["VPinballX.starter"][$"PREcmdExtension.{parentProcessName}"]),
                                                 StripQuotes(configFileFromPath["VPinballX.starter"]["PREcmdExtension"] ?? ".pre.cmd") };

                    StartPrePostCommands(PREcmdExtensions, strSettingsIniFilePath, argsWithTable);
                    StartPrePostCommands(PREcmdExtensions, tableFilename, argsWithTable);
                }
                const string fallbackLauncherVersion = "99.9.9";
                string launcherFileVersion = fallbackLauncherVersion;

                try
                {
                    if (!string.IsNullOrWhiteSpace(vpxCommand) && File.Exists(vpxCommand))
                    {
                        FileVersionInfo vpxVersionInfo = FileVersionInfo.GetVersionInfo(vpxCommand);

                        if (vpxVersionInfo.FileMajorPart >= 0 && vpxVersionInfo.FileMinorPart >= 0)
                        {
                            int buildPart = vpxVersionInfo.FileBuildPart >= 0 ? vpxVersionInfo.FileBuildPart : 0;
                            launcherFileVersion = $"{vpxVersionInfo.FileMajorPart}.{vpxVersionInfo.FileMinorPart}.{buildPart}";
                        }
                    }
                }
                catch (Exception e)
                {
                    LogToFile($"Could not read file version from \"{vpxCommand}\": {e.Message}. Using fallback {fallbackLauncherVersion}.");
                }

                ConfigSection configSection = configFileFromPath[$"VPinballX.starter.{launcherFileVersion}"]??configFileFromPath["VPinballX.starter"];
                if (configSection != null)
                {
                    foreach (var key in configSection.Keys)
                    {
                        if (key.Name.StartsWith("AddParameter"))
                        {
                            if ( (key.Name.Contains(".") && mArgs.Contains(key.Name.Split(".").Last()) ) || ! key.Name.Contains("."))
                            {
                                foreach (string parameter in StripQuotes(configSection[key.Name]).Split(" "))
                                {
                                    mArgs.Add(parameter);
                                }
                                LogToFile($"Amend \"{key.Name}\" setting to the call parameters: {String.Join(" ", mArgs)}");
                            }
                        }

                        else if (key.Name.StartsWith("AddPATH"))
                        {
                            if ( (key.Name.Contains(".") && tableFilename.Contains(key.Name.Split(".").Last()) ) || ! key.Name.Contains("."))
                            {
                                string pathToAdd = StripQuotes(configSection[key.Name]);
                                foreach (string pathEntry in pathToAdd.Split(';', StringSplitOptions.RemoveEmptyEntries))
                                {
                                    string trimmedPathEntry = pathEntry.Trim();

                                    if (string.IsNullOrEmpty(trimmedPathEntry))
                                        continue;

                                    // Get the existing PATH
                                    string existingPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process) ?? "";

                                    if (existingPath.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                        .Any(existingEntry => existingEntry.Trim().Equals(trimmedPathEntry, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        LogToFile($"PATH setting already contains: {trimmedPathEntry} ");
                                        continue;
                                    }

                                    // Append your new entries (avoid duplicates if needed)
                                    string updatedPath = trimmedPathEntry + ";" + existingPath;

                                    // Set the PATH for the process scope
                                    Environment.SetEnvironmentVariable("PATH", updatedPath, EnvironmentVariableTarget.Process);
                                    LogToFile($"Amended the PATH setting: {trimmedPathEntry} ");
                                }
                            }
                        }
                    }
                }

                if (IsVPinballProcessRunning(vpxCommand))
                {
                    string warnText = $"Another VPinball process is already running. Close it before starting:\n{vpxCommand}";
                    LogToFile(warnText);
                    Native.MessageBoxW(IntPtr.Zero, warnText, $"{strExeFileName}: Already running", Native.MB_OK | Native.MB_ICONEXCLAMATION);
                    Environment.Exit(1);
                }

                StartAnotherProgram(vpxCommand, mArgs.ToArray(), true, activateWindowTitle, activateWindowTimeoutMs);
                if (PREPOSTactive && (!tableFilename.Equals("")))
                {
                    List<string> POSTcmdExtensions = new List<string> {StripQuotes(configFileFromPath["VPinballX.starter"][$"POSTcmdExtension.{parentProcessName}"]),
                                                 StripQuotes(configFileFromPath["VPinballX.starter"]["POSTcmdExtension"] ?? ".post.cmd") };

                    StartPrePostCommands(POSTcmdExtensions, tableFilename, argsWithTable);
                    StartPrePostCommands(POSTcmdExtensions, strSettingsIniFilePath, argsWithTable);
                }
                Environment.Exit(0);

            }
            catch (ArgumentException e)
            {
                Native.MessageBoxW(IntPtr.Zero, e.Message, $"{strExeFileName}: Configuration error", Native.MB_OK | Native.MB_ICONEXCLAMATION);
            }
            catch (FileNotFoundException e)
            {
                Native.MessageBoxW(IntPtr.Zero, e.Message, $"{strExeFileName}: File not found", Native.MB_OK | Native.MB_ICONHAND);
            }
            catch (Exception e)
            {
                Native.MessageBoxW(IntPtr.Zero, e.Message, $"{strExeFileName}: Unknown error", Native.MB_OK | Native.MB_ICONHAND);
            }
            Environment.Exit(1);

        }
        void StartPrePostCommands(List<string> prepostExtensions, string scriptBasedFilename, List<string> mArgs)
        {
            foreach (var prepostExtension in prepostExtensions)
            {
                if (prepostExtension is not null && !prepostExtension.Equals(""))
                {
                    string prepostCommand = Path.ChangeExtension(scriptBasedFilename, prepostExtension);

                    if (File.Exists(prepostCommand))
                    {
                        LogToFile($"Calling found PRE/POSTcmd: {prepostCommand}");
                        StartAnotherProgram(prepostCommand, mArgs.ToArray(), false); // Convert List<string> to string[] here
                    }
                }
            }
        }
        void LogToFile(string logText)
        {
            using (var sw = File.AppendText(strLogFilename))
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + logText);
        }

        bool ShowScrollableYesNoDialog(string message, bool messageIsRtf = false, string footerPrompt = "")
        {
            System.Windows.Media.ImageSource? dialogIcon = null;
            string iconPath = Path.Combine(strExeFilePath, "VPinballX.starter.ico");
            if (File.Exists(iconPath))
            {
                try
                {
                    dialogIcon = new System.Windows.Media.Imaging.BitmapImage(new Uri(iconPath, UriKind.Absolute));
                }
                catch
                {
                    // Ignore icon loading issues and continue without a custom icon.
                }
            }

            if (dialogIcon == null)
            {
                try
                {
                    string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrWhiteSpace(exePath))
                    {
                        IntPtr[] largeIcons = new IntPtr[1];
                        uint extracted = Native.ExtractIconEx(exePath, 0, largeIcons, null, 1);
                        if (extracted > 0 && largeIcons[0] != IntPtr.Zero)
                        {
                            dialogIcon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                                largeIcons[0],
                                Int32Rect.Empty,
                                System.Windows.Media.Imaging.BitmapSizeOptions.FromWidthAndHeight(64, 64));

                            Native.DestroyIcon(largeIcons[0]);
                        }
                    }
                }
                catch
                {
                    // Keep dialog functional even when icon extraction fails.
                }
            }

            var dialog = new Window
            {
                Title = BuildWindowTitleText(),
                Icon = dialogIcon,
                Width = 800,
                Height = 520,
                MinWidth = 640,
                MinHeight = 420,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.CanResize,
                ShowInTaskbar = true,
                Topmost = true,
                WindowStyle = WindowStyle.SingleBorderWindow,
                Background = System.Windows.Media.Brushes.White,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };

            dialog.Loaded += (_, __) =>
            {
                dialog.Activate();
                dialog.Focus();
                dialog.Topmost = false;
            };

            var root = new System.Windows.Controls.Grid
            {
                Margin = new Thickness(16)
            };

            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var dialogHeader = new System.Windows.Controls.Grid
            {
                Margin = new Thickness(0, 0, 0, 10)
            };
            dialogHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            dialogHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            if (dialogIcon != null)
            {
                var headerIcon = new System.Windows.Controls.Image
                {
                    Source = dialogIcon,
                    Width = 44,
                    Height = 44,
                    Margin = new Thickness(0, 0, 12, 0),
                    VerticalAlignment = VerticalAlignment.Top
                };
                System.Windows.Controls.Grid.SetColumn(headerIcon, 0);
                dialogHeader.Children.Add(headerIcon);
            }

            var headerText = new TextBlock
            {
                Text = BuildDialogHeaderText(),
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            System.Windows.Controls.Grid.SetColumn(headerText, 1);
            dialogHeader.Children.Add(headerText);

            System.Windows.Controls.Grid.SetRow(dialogHeader, 0);
            root.Children.Add(dialogHeader);

            var flowDocument = new System.Windows.Documents.FlowDocument
            {
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 13,
                PagePadding = new Thickness(0),
                TextAlignment = TextAlignment.Left
            };

            if (messageIsRtf)
            {
                if (!TryLoadRtfIntoFlowDocument(flowDocument, message))
                {
                    flowDocument.Blocks.Add(new System.Windows.Documents.Paragraph(
                        new System.Windows.Documents.Run(message))
                    {
                        Margin = new Thickness(0)
                    });
                }
            }
            else
            {
                flowDocument.Blocks.Add(new System.Windows.Documents.Paragraph(
                    new System.Windows.Documents.Run(message))
                {
                    Margin = new Thickness(0)
                });
            }

            var flowDocumentViewer = new FlowDocumentScrollViewer
            {
                Document = flowDocument,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                IsToolBarVisible = false,
                Margin = new Thickness(0, 0, 0, 16)
            };

            System.Windows.Controls.Grid.SetRow(flowDocumentViewer, 1);
            root.Children.Add(flowDocumentViewer);

            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var yesButton = new System.Windows.Controls.Button
            {
                Content = "Yes",
                MinWidth = 96,
                Margin = new Thickness(0, 0, 8, 0),
                IsDefault = true,
                Padding = new Thickness(14, 6, 14, 6)
            };

            var noButton = new System.Windows.Controls.Button
            {
                Content = "No",
                MinWidth = 96,
                IsCancel = true,
                Padding = new Thickness(14, 6, 14, 6)
            };

            yesButton.Click += (_, __) =>
            {
                dialog.DialogResult = true;
                dialog.Close();
            };

            noButton.Click += (_, __) =>
            {
                dialog.DialogResult = false;
                dialog.Close();
            };

            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);

            var footerGrid = new System.Windows.Controls.Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            if (!string.IsNullOrWhiteSpace(footerPrompt))
            {
                var promptText = new TextBlock
                {
                    Text = footerPrompt,
                    FontSize = 14,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 12, 0),
                    FontWeight = FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap
                };
                System.Windows.Controls.Grid.SetColumn(promptText, 0);
                footerGrid.Children.Add(promptText);
            }

            System.Windows.Controls.Grid.SetColumn(buttonPanel, 1);
            footerGrid.Children.Add(buttonPanel);

            System.Windows.Controls.Grid.SetRow(footerGrid, 2);
            root.Children.Add(footerGrid);

            dialog.Content = root;
            return dialog.ShowDialog() == true;
        }

        private static string BuildDialogHeaderText()
        {
            return "VPinballX.starter";
        }

        private static string BuildWindowTitleText()
        {
            string versionLabel = GetEmbeddedVersionLabel();
            if (string.IsNullOrWhiteSpace(versionLabel))
                return "VPinballX.starter © 2025-2026 Richard Ludwig";

            return $"VPinballX.starter {versionLabel} © 2025-2026 Richard Ludwig";
        }

        private static string GetEmbeddedVersionLabel()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();

                var informationalAttr = (System.Reflection.AssemblyInformationalVersionAttribute?)
                    Attribute.GetCustomAttribute(assembly, typeof(System.Reflection.AssemblyInformationalVersionAttribute));
                string? informationalVersion = informationalAttr?.InformationalVersion;

                if (!string.IsNullOrWhiteSpace(informationalVersion))
                {
                    string normalized = informationalVersion.Split('+')[0].Trim();
                    if (!string.IsNullOrWhiteSpace(normalized))
                        return $"v{normalized}";
                }

                System.Version? assemblyVersion = assembly.GetName().Version;
                if (assemblyVersion != null)
                    return $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";
            }
            catch
            {
                // Keep the header readable even if version metadata cannot be resolved.
            }

            return string.Empty;
        }

        private static bool TryLoadRtfIntoFlowDocument(System.Windows.Documents.FlowDocument flowDocument, string rtfText)
        {
            try
            {
                var range = new System.Windows.Documents.TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);
                byte[] rtfBytes = System.Text.Encoding.UTF8.GetBytes(rtfText);
                using var stream = new MemoryStream(rtfBytes);
                range.Load(stream, DataFormats.Rtf);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string BuildWelcomeDialogRtf(string iniConfigFilename, string exeFileName, string defaultIniConfig)
        {
            string escapedIniFilename = EscapeRtf(iniConfigFilename);
            string escapedExeFilename = EscapeRtf(exeFileName);
            string escapedIniBlock = EscapeRtf(defaultIniConfig)
                .Replace("\r\n", "\\line ")
                .Replace("\n", "\\line ");

            return "{\\rtf1\\ansi\\deff0" +
                   "{\\fonttbl{\\f0 Segoe UI;}{\\f1 Consolas;}}" +
                   "\\viewkind4\\uc1" +
                   "\\pard\\sa120 Welcome new VPinballX.starter user!\\par" +
                   "\\pard\\sa120 VPinballX.starter works like a launcher for VPinballX.exe.\\par" +
                   "\\pard\\sa120 1) Start it with the same parameters you use for VPinballX.exe.\\par" +
                   "\\pard\\sa120 2) If a table is provided, VPinballX.starter reads the table version and looks it up in the [VPinballX] section.\\par" +
                   "\\pard\\sa120 3) It starts the mapped VPinballX executable.\\par" +
                   "\\pard\\sa120 If no version match is found, what you define as [VPinballX] 'Default' is used. If no table is provided, [VPinballX.starter] 'DefaultVersion' is used.\\par" +
                   "\\pard\\sa120 A log entry can be written to VPinballX.starter.log for every launch (when enabled in the ini).\\par" +
                   "\\pard\\sa120 \\\"" + escapedIniFilename + "\\\" could not be found next to \\\"" + escapedExeFilename + "\\\" and will now be created using the template below." +
                   " You need to edit the ini file for your setup and it can be placed next to the executable or next to your table file.\\par" +
                   "\\pard\\sa60\\brdrb\\brdrs\\brdrw20\\brsp20\\par" +
                   "\\pard\\sa80\\f1\\fs20 " + escapedIniBlock + 
                   "\\pard\\sa60\\brdrb\\brdrs\\brdrw20\\brsp20\\par" +
                   "\\pard\\sa120 Please read the comments in the ini file for further instructions.\\par" +
                  "}";
        }

        private static string EscapeRtf(string text)
        {
            return text
                .Replace("\\", "\\\\")
                .Replace("{", "\\{")
                .Replace("}", "\\}");
        }

        bool FileOrDirectoryExists(string name)
        {
            return Directory.Exists(name) || File.Exists(name);
        }

        string StripQuotes(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return value ?? "";
            
            value = value.Trim();
            if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                (value.StartsWith("'") && value.EndsWith("'")))
            {
                return value.Substring(1, value.Length - 2);
            }
            return value;
        }
        string ParentProcessName()
        {
            try {
                var myId = Process.GetCurrentProcess().Id;
                var query = string.Format("SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {0}", myId);
                var search = new ManagementObjectSearcher("root\\CIMV2", query);
                var results = search.Get().GetEnumerator();
                results.MoveNext();
                var queryObj = results.Current;
                var parentId = (uint)queryObj["ParentProcessId"];
                var parent = Process.GetProcessById((int)parentId);
                return Path.GetFileNameWithoutExtension(parent.ProcessName);
            }
            catch (Exception e)
            {
                LogToFile($"ParentProcessName could not be found (can be referenced as 'anonymous' in the config): {e.Message}");
                return "anonymous";
            }
        }
        bool IsVPinballProcessRunning(string programPath)
        {
            string targetName = Path.GetFileNameWithoutExtension(programPath);
            try
            {
                if (Process.GetProcessesByName(targetName).Any())
                    return true;
                // Additional check for VPinball processes unless it is the current executable
                if (targetName.StartsWith("VPinball", StringComparison.OrdinalIgnoreCase))
                {
                    bool found = false;
                    foreach (var proc in Process.GetProcesses())
                    {
                        try
                        {
                            if (proc.ProcessName.StartsWith("VPinball", StringComparison.OrdinalIgnoreCase) & !proc.ProcessName.Equals(Process.GetCurrentProcess().ProcessName, StringComparison.OrdinalIgnoreCase))
                            {
                                found = true;
                                break;
                            }
                        }
                        catch { }
                        finally
                        {
                            proc.Dispose();
                        }
                    }
                    if (found)
                        return true;
                }
            }
            catch (Exception e)
            {
                LogToFile($"Process check failed for {programPath}: {e.Message}");
            }

            return false;
        }
        void StartAnotherProgram(string programPath, string[] programArgs, bool addTracker = true, string? activateWindowTitle = null, int activateWindowTimeoutMs = 0)
        {
            using (Process process = new Process())
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = programPath,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                foreach (var arg in programArgs)
                {
                    startInfo.ArgumentList.Add(arg);
                }
                process.StartInfo = startInfo;
                process.Start();
                if (addTracker)
                {
                    ChildProcessTracker.AddProcess(process);
                    process.WaitForInputIdle(10000);
                }

                // Try to activate the configured window title for up to the configured timeout
                if (!string.IsNullOrWhiteSpace(activateWindowTitle) && activateWindowTimeoutMs > 0)
                {
                    IntPtr hWnd = IntPtr.Zero;
                    DateTime deadline = DateTime.UtcNow.AddMilliseconds(activateWindowTimeoutMs);

                    while (DateTime.UtcNow < deadline)
                    {
                        hWnd = FindWindowByTitle(process.Id, activateWindowTitle);
                        if (hWnd != IntPtr.Zero)
                        {
                            if (!Native.IsIconic(hWnd))
                            {
                                Native.SetForegroundWindow(hWnd);
                                Native.SetFocus(hWnd);
                            }
                            break;
                        }
                        Thread.Sleep(100);
                    }
                }

                process.WaitForExit();
                process.Close();
            }
        }

        private IntPtr FindWindowByTitle(int processId, string windowTitle)
        {
            IntPtr foundHwnd = IntPtr.Zero;
            Native.EnumWindows((hWnd, lParam) =>
            {
                uint winProcId;
                Native.GetWindowThreadProcessId(hWnd, out winProcId);
                if (winProcId == processId)
                {
                    var sb = new System.Text.StringBuilder(256);
                    Native.GetWindowText(hWnd, sb, sb.Capacity);
                    if (sb.ToString() == windowTitle)
                    {
                        foundHwnd = hWnd;
                        return false; // stop enumeration
                    }
                }
                return true; // continue enumeration
            }, IntPtr.Zero);
            return foundHwnd;
        }

    }

}
