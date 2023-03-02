
#region Copyright (c) 2012, Roland Pihlakas
/////////////////////////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2012, Roland Pihlakas.
//
// Permission to copy, use, modify, sell and distribute this software
// is granted provided this copyright notice appears in all copies.
//
/////////////////////////////////////////////////////////////////////////////////////////
#endregion Copyright (c) 2012, Roland Pihlakas


using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Security;

namespace CPUUsageMonitor
{
    [SuppressUnmanagedCodeSecurity]     //SuppressUnmanagedCodeSecurity - For methods in this particular class, execution time is often critical. Security can be traded for additional speed by applying the SuppressUnmanagedCodeSecurity attribute to the method declaration. This will prevent the runtime from doing a security stack walk at runtime. - MSDN: Generally, whenever managed code calls into unmanaged code (by PInvoke or COM interop into native code), there is a demand for the UnmanagedCode permission to ensure all callers have the necessary permission to allow this. By applying this explicit attribute, developers can suppress the demand at run time. The developer must take responsibility for assuring that the transition into unmanaged code is sufficiently protected by other means. The demand for the UnmanagedCode permission will still occur at link time. For example, if function A calls function B and function B is marked with SuppressUnmanagedCodeSecurityAttribute, function A will be checked for unmanaged code permission during just-in-time compilation, but not subsequently during run time.
    public static partial class NativeMethods
    {
        public static readonly Version ServicePackVersion = GetServicePackVersion();

        public static readonly bool IsVistaOrServer2008OrNewer = Environment.OSVersion.Version.Major >= 6;
        public static readonly bool IsWin7OrNewer = (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 1) || Environment.OSVersion.Version.Major > 6;	//http://msdn.microsoft.com/en-us/library/ms724834(v=vs.85).aspx
        public static readonly bool IsServer2003OrVistaOrNewer = (Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 2) || Environment.OSVersion.Version.Major > 5;
        public static readonly bool IsXPSP3OrServer2003OrVistaOrNewer = IsServer2003OrVistaOrNewer || (Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor == 1 && ServicePackVersion.Major >= 3);

        // ############################################################################

        //http://stackoverflow.com/questions/2819934/detect-windows-7-in-net
        //http://stackoverflow.com/a/8406674/193017

        #region OSVERSIONINFOEX
        [StructLayout(LayoutKind.Sequential)]
        private struct OSVERSIONINFOEX
        {
            public int dwOSVersionInfoSize;
            public int dwMajorVersion;
            public int dwMinorVersion;
            public int dwBuildNumber;
            public int dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
            public short wServicePackMajor;
            public short wServicePackMinor;
            public short wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }
        #endregion OSVERSIONINFOEX


        [DllImport("kernel32.dll")]
        private static extern bool GetVersionEx(ref OSVERSIONINFOEX osVersionInfo);

        public static Version GetServicePackVersion()
        {
            OSVERSIONINFOEX osVersionInfo = new OSVERSIONINFOEX();
            osVersionInfo.dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEX));

            if (GetVersionEx(ref osVersionInfo))
            {
                Version result = new Version(osVersionInfo.wServicePackMajor, osVersionInfo.wServicePackMinor);
                return result;
            }
            else
            {
                return null;
            }
        }

        // ############################################################################

        private enum PROCESSINFOCLASS : int
        {
            ProcessBasicInformation = 0, // 0, q: PROCESS_BASIC_INFORMATION, PROCESS_EXTENDED_BASIC_INFORMATION
            ProcessQuotaLimits, // qs: QUOTA_LIMITS, QUOTA_LIMITS_EX
            ProcessIoCounters, // q: IO_COUNTERS
            ProcessVmCounters, // q: VM_COUNTERS, VM_COUNTERS_EX
            ProcessTimes, // q: KERNEL_USER_TIMES
            ProcessBasePriority, // s: KPRIORITY
            ProcessRaisePriority, // s: ULONG
            ProcessDebugPort, // q: HANDLE
            ProcessExceptionPort, // s: HANDLE
            ProcessAccessToken, // s: PROCESS_ACCESS_TOKEN
            ProcessLdtInformation, // 10
            ProcessLdtSize,
            ProcessDefaultHardErrorMode, // qs: ULONG
            ProcessIoPortHandlers, // (kernel-mode only)
            ProcessPooledUsageAndLimits, // q: POOLED_USAGE_AND_LIMITS
            ProcessWorkingSetWatch, // q: PROCESS_WS_WATCH_INFORMATION[]; s: void
            ProcessUserModeIOPL,
            ProcessEnableAlignmentFaultFixup, // s: BOOLEAN
            ProcessPriorityClass, // qs: PROCESS_PRIORITY_CLASS
            ProcessWx86Information,
            ProcessHandleCount, // 20, q: ULONG, PROCESS_HANDLE_INFORMATION
            ProcessAffinityMask, // s: KAFFINITY
            ProcessPriorityBoost, // qs: ULONG
            ProcessDeviceMap, // qs: PROCESS_DEVICEMAP_INFORMATION, PROCESS_DEVICEMAP_INFORMATION_EX
            ProcessSessionInformation, // q: PROCESS_SESSION_INFORMATION
            ProcessForegroundInformation, // s: PROCESS_FOREGROUND_BACKGROUND
            ProcessWow64Information, // q: ULONG_PTR
            ProcessImageFileName, // q: UNICODE_STRING
            ProcessLUIDDeviceMapsEnabled, // q: ULONG
            ProcessBreakOnTermination, // qs: ULONG
            ProcessDebugObjectHandle, // 30, q: HANDLE
            ProcessDebugFlags, // qs: ULONG
            ProcessHandleTracing, // q: PROCESS_HANDLE_TRACING_QUERY; s: size 0 disables, otherwise enables
            ProcessIoPriority, // qs: ULONG
            ProcessExecuteFlags, // qs: ULONG
            ProcessResourceManagement,
            ProcessCookie, // q: ULONG
            ProcessImageInformation, // q: SECTION_IMAGE_INFORMATION
            ProcessCycleTime, // q: PROCESS_CYCLE_TIME_INFORMATION
            ProcessPagePriority, // q: ULONG
            ProcessInstrumentationCallback, // 40
            ProcessThreadStackAllocation, // s: PROCESS_STACK_ALLOCATION_INFORMATION, PROCESS_STACK_ALLOCATION_INFORMATION_EX
            ProcessWorkingSetWatchEx, // q: PROCESS_WS_WATCH_INFORMATION_EX[]
            ProcessImageFileNameWin32, // q: UNICODE_STRING
            ProcessImageFileMapping, // q: HANDLE (input)
            ProcessAffinityUpdateMode, // qs: PROCESS_AFFINITY_UPDATE_MODE
            ProcessMemoryAllocationMode, // qs: PROCESS_MEMORY_ALLOCATION_MODE
            ProcessGroupInformation, // q: USHORT[]
            ProcessTokenVirtualizationEnabled, // s: ULONG
            ProcessConsoleHostProcess, // q: ULONG_PTR
            ProcessWindowInformation, // 50, q: PROCESS_WINDOW_INFORMATION
            ProcessHandleInformation, // q: PROCESS_HANDLE_SNAPSHOT_INFORMATION // since WIN8
            ProcessMitigationPolicy, // s: PROCESS_MITIGATION_POLICY_INFORMATION
            ProcessDynamicFunctionTableInformation,
            ProcessHandleCheckingMode,
            ProcessKeepAliveCount, // q: PROCESS_KEEPALIVE_COUNT_INFORMATION
            ProcessRevokeFileHandles, // s: PROCESS_REVOKE_FILE_HANDLES_INFORMATION
            MaxProcessInfoClass
        };

        //http://www.pinvoke.net/default.aspx/ntdll.ntqueryinformationprocess
        [DllImport("ntdll.dll", SetLastError = true)]
        static extern int NtQueryInformationProcess(IntPtr processHandle,
           PROCESSINFOCLASS processInformationClass, IntPtr processInformation,
            uint processInformationLength, IntPtr returnLength);

        [DllImport("ntdll.dll", SetLastError = true)]
        static extern int NtQueryInformationProcess(IntPtr processHandle,
           PROCESSINFOCLASS processInformationClass, out int processInformation,
            uint processInformationLength, IntPtr returnLength);

        [DllImport("ntdll.dll", SetLastError = true)]
        static extern int NtSetInformationProcess(IntPtr processHandle,
           PROCESSINFOCLASS processInformationClass, IntPtr processInformation,
            uint processInformationLength);

        [DllImport("ntdll.dll", SetLastError = true)]
        static extern int NtSetInformationProcess(IntPtr processHandle,
           PROCESSINFOCLASS processInformationClass, ref int processInformation,
            uint processInformationLength);

        public enum PROCESSIOPRIORITY : int
        {
            PROCESSIOPRIORITY_UNKNOWN = -1,

            PROCESSIOPRIORITY_VERY_LOW = 0,
            PROCESSIOPRIORITY_LOW,
            PROCESSIOPRIORITY_NORMAL,
            PROCESSIOPRIORITY_HIGH
        };

        public static bool NT_SUCCESS(int Status)
        {
            return (Status >= 0);
        }

        public static bool SetIOPriority(IntPtr processHandle, PROCESSIOPRIORITY ioPriority_in)
        {
            if (IsXPSP3OrServer2003OrVistaOrNewer)	//http://blogs.norman.com/2011/security-research/ntqueryinformationprocess-ntsetinformationprocess-cheat-sheet
            {
                int ioPriority = (int)ioPriority_in;
                int result = NtSetInformationProcess(processHandle, PROCESSINFOCLASS.ProcessIoPriority, ref ioPriority, sizeof(int));
                return NT_SUCCESS(result);
            }
            else
            {
                return false;
            }
        }

        public static PROCESSIOPRIORITY? GetIOPriority(IntPtr processHandle)
        {
            if (IsXPSP3OrServer2003OrVistaOrNewer)	//http://blogs.norman.com/2011/security-research/ntqueryinformationprocess-ntsetinformationprocess-cheat-sheet
            {
                int ioPriority;
                int result = NtQueryInformationProcess(processHandle, PROCESSINFOCLASS.ProcessIoPriority, out ioPriority, sizeof(int), IntPtr.Zero);
                if (NT_SUCCESS(result))
                    return (PROCESSIOPRIORITY)ioPriority;
                else
                    return null;
            }
            else
            {
                return null;
            }
        }

        public static bool SetPagePriority(IntPtr processHandle, int pagePriority_in)
        {
            if (IsVistaOrServer2008OrNewer)	//http://blogs.norman.com/2011/security-research/ntqueryinformationprocess-ntsetinformationprocess-cheat-sheet
            {
                int pagePriority = (int)pagePriority_in;
                int result = NtSetInformationProcess(processHandle, PROCESSINFOCLASS.ProcessPagePriority, ref pagePriority, sizeof(int));
                return NT_SUCCESS(result);
            }
            else
            {
                return false;
            }
        }

        public static int? GetPagePriority(IntPtr processHandle)
        {
            if (IsVistaOrServer2008OrNewer)	//http://blogs.norman.com/2011/security-research/ntqueryinformationprocess-ntsetinformationprocess-cheat-sheet
            {
                int pagePriority;
                int result = NtQueryInformationProcess(processHandle, PROCESSINFOCLASS.ProcessPagePriority, out pagePriority, sizeof(int), IntPtr.Zero);
                if (NT_SUCCESS(result))
                    return pagePriority;
                else
                    return null;
            }
            else
            {
                return null;
            }
        }

        // ############################################################################

        //http://www.pinvoke.net/default.aspx/user32/GetGuiResources.html?DelayRedirect=1
        //http://stackoverflow.com/questions/143206/how-do-you-obtain-current-window-handle-count-and-window-handle-limit-in-net

        public enum ResourceType
        {
            Gdi = 0,
            User = 1
        }

        /// uiFlags: 0 - Count of GDI objects
        /// uiFlags: 1 - Count of USER objects
        /// - Win32 GDI objects (pens, brushes, fonts, palettes, regions, device contexts, bitmap headers)
        /// - Win32 USER objects:
        ///     - WIN32 resources (accelerator tables, bitmap resources, dialog box templates, font resources, menu resources, raw data resources, string table entries, message table entries, cursors/icons)
        ///     - Other USER objects (windows, menus)
        ///
        [DllImport("User32.dll", SetLastError = true)]
        public static extern uint GetGuiResources(IntPtr hProcess, int uiFlags);

        public static uint GdiHandleCount(
#if !NET30
            this 
#endif
            Process process)
        {
            return GetGuiResources(process.Handle, (int)ResourceType.Gdi);
        }

        public static uint UserHandleCount(
#if !NET30
            this 
#endif
            Process process)
        {
            return GetGuiResources(process.Handle, (int)ResourceType.User);
        }

        // ############################################################################

        [DllImport("kernel32.dll")]
        internal static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

        //http://msdn.microsoft.com/en-us/library/windows/desktop/ms682606(v=vs.85).aspx
        [/*DllImport("kernel32.dll")*/DllImport("psapi.dll")]
        public static extern bool EmptyWorkingSet(IntPtr processHandle);

        // ############################################################################

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        // ############################################################################

        // http://hintdesk.com/Web/Source/Program.cs

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);

        public static uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        public static uint STANDARD_RIGHTS_READ = 0x00020000;
        public static uint TOKEN_ASSIGN_PRIMARY = 0x0001;
        public static uint TOKEN_DUPLICATE = 0x0002;
        public static uint TOKEN_IMPERSONATE = 0x0004;
        public static uint TOKEN_QUERY = 0x0008;
        public static uint TOKEN_QUERY_SOURCE = 0x0010;
        public static uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        public static uint TOKEN_ADJUST_GROUPS = 0x0040;
        public static uint TOKEN_ADJUST_DEFAULT = 0x0080;
        public static uint TOKEN_ADJUST_SESSIONID = 0x0100;
        public static uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);
        public static uint TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY |
            TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE |
            TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT |
            TOKEN_ADJUST_SESSIONID);

        [DllImport("kernel32.dll"/*, SetLastError = true*/)]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName,
            out LUID lpLuid);

        public const string SE_ASSIGNPRIMARYTOKEN_NAME = "SeAssignPrimaryTokenPrivilege";
        public const string SE_AUDIT_NAME = "SeAuditPrivilege";
        public const string SE_BACKUP_NAME = "SeBackupPrivilege";
        public const string SE_CHANGE_NOTIFY_NAME = "SeChangeNotifyPrivilege";
        public const string SE_CREATE_GLOBAL_NAME = "SeCreateGlobalPrivilege";
        public const string SE_CREATE_PAGEFILE_NAME = "SeCreatePagefilePrivilege";
        public const string SE_CREATE_PERMANENT_NAME = "SeCreatePermanentPrivilege";
        public const string SE_CREATE_SYMBOLIC_LINK_NAME = "SeCreateSymbolicLinkPrivilege";
        public const string SE_CREATE_TOKEN_NAME = "SeCreateTokenPrivilege";
        public const string SE_DEBUG_NAME = "SeDebugPrivilege";
        public const string SE_ENABLE_DELEGATION_NAME = "SeEnableDelegationPrivilege";
        public const string SE_IMPERSONATE_NAME = "SeImpersonatePrivilege";
        public const string SE_INC_BASE_PRIORITY_NAME = "SeIncreaseBasePriorityPrivilege";
        public const string SE_INCREASE_QUOTA_NAME = "SeIncreaseQuotaPrivilege";
        public const string SE_INC_WORKING_SET_NAME = "SeIncreaseWorkingSetPrivilege";
        public const string SE_LOAD_DRIVER_NAME = "SeLoadDriverPrivilege";
        public const string SE_LOCK_MEMORY_NAME = "SeLockMemoryPrivilege";
        public const string SE_MACHINE_ACCOUNT_NAME = "SeMachineAccountPrivilege";
        public const string SE_MANAGE_VOLUME_NAME = "SeManageVolumePrivilege";
        public const string SE_PROF_SINGLE_PROCESS_NAME = "SeProfileSingleProcessPrivilege";
        public const string SE_RELABEL_NAME = "SeRelabelPrivilege";
        public const string SE_REMOTE_SHUTDOWN_NAME = "SeRemoteShutdownPrivilege";
        public const string SE_RESTORE_NAME = "SeRestorePrivilege";
        public const string SE_SECURITY_NAME = "SeSecurityPrivilege";
        public const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";
        public const string SE_SYNC_AGENT_NAME = "SeSyncAgentPrivilege";
        public const string SE_SYSTEM_ENVIRONMENT_NAME = "SeSystemEnvironmentPrivilege";
        public const string SE_SYSTEM_PROFILE_NAME = "SeSystemProfilePrivilege";
        public const string SE_SYSTEMTIME_NAME = "SeSystemtimePrivilege";
        public const string SE_TAKE_OWNERSHIP_NAME = "SeTakeOwnershipPrivilege";
        public const string SE_TCB_NAME = "SeTcbPrivilege";
        public const string SE_TIME_ZONE_NAME = "SeTimeZonePrivilege";
        public const string SE_TRUSTED_CREDMAN_ACCESS_NAME = "SeTrustedCredManAccessPrivilege";
        public const string SE_UNDOCK_NAME = "SeUndockPrivilege";
        public const string SE_UNSOLICITED_INPUT_NAME = "SeUnsolicitedInputPrivilege";

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public UInt32 LowPart;
            public Int32 HighPart;
        }

        public const UInt32 SE_PRIVILEGE_ENABLED_BY_DEFAULT = 0x00000001;
        public const UInt32 SE_PRIVILEGE_ENABLED = 0x00000002;
        public const UInt32 SE_PRIVILEGE_REMOVED = 0x00000004;
        public const UInt32 SE_PRIVILEGE_USED_FOR_ACCESS = 0x80000000;

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_PRIVILEGES
        {
            public UInt32 PrivilegeCount;
            public LUID Luid;
            public UInt32 Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public UInt32 Attributes;
        }

        // Use this signature if you do not want the previous state
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle,
           [MarshalAs(UnmanagedType.Bool)]bool DisableAllPrivileges,
           ref TOKEN_PRIVILEGES NewState,
           UInt32 Zero,
           IntPtr Null1,
           IntPtr Null2);

        public static bool EnableSeIncBasePriorityPrivilege(Action<string> logCallback)     //TODO: do we need other privileges too?
        {
            //System.Diagnostics.Process.EnterDebugMode(); //TODO: use this method instead?

            return EnablePrivilege(SE_INC_BASE_PRIORITY_NAME, logCallback);
        }

        /// <summary>
        /// ms-help://MS.MSDNQTR.v90.en/dllproc/base/openprocess.htm :
        /// To open a handle to another local process and obtain full access rights, 
        /// you must enable the SeDebugPrivilege privilege.
        /// 
        /// See also 
        /// ms-help://MS.MSDNQTR.v90.en/secauthz/security/enabling_and_disabling_privileges_in_c__.htm
        /// for listing of privileges:
        /// SE_DEBUG_NAME - SeDebugPrivilege - Debug programs 
        /// SE_INCREASE_QUOTA_NAME - SeIncreaseQuotaPrivilege - Adjust memory quotas for a process 
        /// SE_TCB_NAME - SeTcbPrivilege - Act as part of the operating system 
        /// </summary>
        /// <returns></returns>
        //http://hintdesk.com/c-how-to-enable-sedebugprivilege
        //and http://hintdesk.com/Web/Source/Program.cs
        public static bool EnableSeDebugPrivilege(Action<string> logCallback)     //TODO: do we need other privileges too?
        {
            //System.Diagnostics.Process.EnterDebugMode(); //TODO: use this method instead?

            return EnablePrivilege(SE_DEBUG_NAME, logCallback);
        }

        /// <summary>
        /// See also 
        /// ms-help://MS.MSDNQTR.v90.en/secauthz/security/enabling_and_disabling_privileges_in_c__.htm
        /// for listing of privileges:
        /// SE_DEBUG_NAME - SeDebugPrivilege - Debug programs 
        /// SE_INCREASE_QUOTA_NAME - SeIncreaseQuotaPrivilege - Adjust memory quotas for a process 
        /// SE_TCB_NAME - SeTcbPrivilege - Act as part of the operating system 
        /// </summary>
        /// <returns></returns>
        //http://hintdesk.com/c-how-to-enable-sedebugprivilege
        //and http://hintdesk.com/Web/Source/Program.cs
        public static bool EnablePrivilege(string privilegeName, Action<string> logCallback)     //TODO: do we need other privileges too?
        {

            IntPtr hToken = IntPtr.Zero;
            LUID luidSEDebugNameValue;
            TOKEN_PRIVILEGES tkpPrivileges;

            try
            {
                if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out hToken))
                {
                    if (logCallback != null)
                        logCallback(string.Format("OpenProcessToken() failed, error = {0}. " + privilegeName + " will not be set", Marshal.GetLastWin32Error()));

                    return false;
                }
                else
                {
                    if (logCallback != null)
                        logCallback("OpenProcessToken() success");
                }

                if (!LookupPrivilegeValue(null, privilegeName, out luidSEDebugNameValue))
                {
                    if (logCallback != null)
                        logCallback(string.Format("LookupPrivilegeValue() for " + privilegeName + " failed, error = {0}. " + privilegeName + " is not available", Marshal.GetLastWin32Error()));

                    //CloseHandle(hToken);

                    return false;
                }
                else
                {
                    if (logCallback != null)
                        logCallback("LookupPrivilegeValue() success");
                }

                tkpPrivileges.PrivilegeCount = 1;
                tkpPrivileges.Luid = luidSEDebugNameValue;
                tkpPrivileges.Attributes = SE_PRIVILEGE_ENABLED;

                bool result;
                if (!AdjustTokenPrivileges(hToken, false, ref tkpPrivileges, 0, IntPtr.Zero, IntPtr.Zero))
                {
                    if (logCallback != null)
                        logCallback(string.Format("AdjustTokenPrivileges() failed, error = {0}. " + privilegeName + " is not available", Marshal.GetLastWin32Error()));

                    result = false;
                }
                else
                {
                    if (logCallback != null)
                        logCallback("AdjustTokenPrivileges() success, " + privilegeName + " is now available");

                    result = true;
                }

                return result;
            }
            finally
            {
                if (hToken != IntPtr.Zero)
                    CloseHandle(hToken);
            }

        }   //public static bool EnableSeDebugPrivilege()

        // ############################################################################

    }   //public partial class NativeMethods
}