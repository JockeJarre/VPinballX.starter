/*
The GPLv3+ License:

Copyright (C) 2023-2024 Richard Ludwig and contributors

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
using OpenMcdf;
using System.Runtime.InteropServices;
using Salaros.Configuration;
using System.ComponentModel;


namespace VPinballX.starter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static String[] mArgs = { };
        public static string strExeFilePath = AppDomain.CurrentDomain.BaseDirectory;
        public static string strExeFileName = AppDomain.CurrentDomain.FriendlyName;
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
                if (Environment.OSVersion.Version < new Version(6, 2))
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
        }
        private void Application_Startup(object sender, StartupEventArgs eventArgs)
        {
            if (eventArgs.Args.Length > 0)
            {
                mArgs = eventArgs.Args;
            }
            try
            {
                string strSettingsIniFilePath = Path.Combine(strExeFilePath, $"{strIniConfigFilename}");

                if (!FileOrDirectoryExists(strSettingsIniFilePath))
                {
                    const string strDefaultIniConfig =
            @";A Configuration file for VPinballX.starter
[VPinballX.starter]
;DefaultVersion when started without any table param.
DefaultVersion=10.80
LogVersions=1
[TableNameExceptions]
;If left string is found in the Table filename
;we will use the right string to add to the version number search
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
10.72=VPinballX74.exe
10.80=VPinballX85.exe
10.80x32=VPinballX85x32.exe
10.80GL=VPinballX85_GL.exe";

                    string strWelcomeString =
            $@"Welcome new VPinballX.starter user!

We could not find a ""{strIniConfigFilename}"" next to ""{strExeFileName}"". 

The file should look like this:

X-----X-----X-----X----X-----X-----X-----X-----X

{strDefaultIniConfig}

X-----X-----X-----X----X-----X-----X-----X-----X

VPinballX.starter can therefore be used as a VPinballX.exe replacement.

It works like this:

VPinballX.starter is started with exactly the same parameters as VPinballX.exe. First it loads the table file and finds out what version it was saved with. Then it takes that information and looks in the [VPinballX] table above to find out which version of VPinballX.xxx.exe you want to start with. It will then run the VPinballX.xxx.exe that you have configured using exactly the same parameters.

If it cannot find a version in the table the default entry under [VPinballX] will be used. If you simply double-click the VPinballX.starter without any table, the Default under [VPinballx.starter] is used.

This way the correct table version or the version you have chosen will always be used. Each time you start VPinballX, a log entry will be added to VPinballX.starter.log, telling which version was used. This can be turned of in the ini file.

Do you want to create this file now?";
                    int dialogResult = Native.MessageBoxW(IntPtr.Zero, strWelcomeString, $"{strExeFileName}: Welcome", Native.MB_YESNO);
                    if (dialogResult == Native.IDYES)
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

                var configFileFromPath = new ConfigParser(strSettingsIniFilePath);
                bool logVersions = (configFileFromPath["VPinballX.starter"]["LogVersions"] ?? "0").Equals("1");

                string tableFilename = "";

                if (logVersions) LogToFile($"VPinballX.starter called with [{strExeFileName} " + String.Join(" ", mArgs.Select(s => s.Contains(" ") ? $"\"{s}\"" : s).ToList()) + "]");

                foreach (string arg in mArgs)
                {
                    if (arg.Trim('"').EndsWith(".vpx", StringComparison.OrdinalIgnoreCase))
                    {
                        tableFilename = arg;
                        break;
                    }
                }
                string defaultFileVersion = configFileFromPath["VPinballX.starter"]["DefaultVersion"];

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
                        if (logVersions) LogToFile($"Table file \"{tableFilename}\" cannot be found! Please check your frontend software!");
                        throw new FileNotFoundException($"Table file\n\n{tableFilename}\n\n cannot be found!\nPlease check your frontend software!");
                    }

                    // Read the version of VPinballX.exe which saved this table
                    var cf = new CompoundFile(tableFilename);
                    try
                    {
                        var gameStorage = cf.RootStorage.GetStorage("GameStg");
                        var gameData = gameStorage.GetStream("GameData");

                        fileVersion = BitConverter.ToInt32(gameStorage.GetStream("Version").GetData(), 0);
                    }
                    finally
                    {
                        cf.Close();
                    }
                }
                string strFileVersion = $"{fileVersion / 100}.{fileVersion % 100}";
                if (configFileFromPath["VPinballX"][strFileVersion] == null)
                    strFileVersion = "Default";

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

                string vpxCommand = configFileFromPath["VPinballX"][strFileVersion] ?? configFileFromPath["VPinballX"]["Default"];

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


                StartAnotherProgram(vpxCommand, mArgs);
                //process.WaitForExit();
                //process.Close();
                //LogToFile("WaitForExit finished");
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

        void LogToFile(string logText)
        {
            using (var sw = File.AppendText(strLogFilename))
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + logText);
        }

        bool FileOrDirectoryExists(string name)
        {
            return Directory.Exists(name) || File.Exists(name);
        }
        void StartAnotherProgram(string programPath, string[] programArgs)
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
                // Add the Process to ChildProcessTracker.
                ChildProcessTracker.AddProcess(process);

                process.WaitForInputIdle(10000);


                process.WaitForExit();
                process.Close();
            }
        }

    }

}
