
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


using System.Collections.Generic;
using System.Diagnostics;

namespace CPUUsageMonitor
{
    partial class Program
    {
        private const string ArgShowHelp = "-help";

        private const string ArgFailIfNotResponding = "-failIfNotResponding";
        private const string ArgCpuUsageTreshold = "-cpuUsageTreshold";
        private const string ArgMemoryCommitTresholdMB = "-memoryCommitTresholdMB";
        private const string ArgWorkingSetTresholdMB = "-workingSetTresholdMB";
        private const string ArgGdiHandlesTreshold = "-gdiHandlesTreshold";
        private const string ArgUserHandlesTreshold = "-userHandlesTreshold";
        private const string ArgHandlesTreshold = "-handlesTreshold";
        private const string ArgPagedPoolTresholdKB = "-pagedPoolTresholdKB";
        private const string ArgNonPagedPoolTresholdKB = "-nonPagedPoolTresholdKB";

        private const string ArgApplyCpuUsageTresholdPerCpu = "-applyCpuUsageTresholdPerCpu";

        private const string ArgOutageTimeBeforeGiveUpSeconds = "-outageTimeBeforeGiveUpSeconds";
        private const string ArgOutageConditionNumChecks = "-outageConditionNumChecks";
        private const string ArgPassedCheckIntervalMs = "-passedCheckIntervalMs";
        private const string ArgFailedCheckIntervalMs = "-failedCheckIntervalMs";

        private const string ArgProgramRegEx = "-programRegEx";
        private const string ArgFailIfNoMatchingProcesses = "-failIfNoMatchingProcesses";
        private const string ArgExecuteCommandOnFail = "-executeCommandOnFail";
        //private const string ArgRestartProcessOnFail = "-restartProcessOnFail";   //TODO  

#if DEBUG && false
        private static bool ValueShowHelp = false;
        private static float ValueCpuUsageTreshold = 75;
        private static bool ValueApplyCpuUsageTresholdPerCpu = true;
        private static int ValueOutageTimeBeforeGiveUpSeconds = 30;
        private static int ValueOutageConditionNumChecks = 3;
        private static int ValuePassedCheckIntervalMs = 10000;
        private static int ValueFailedCheckIntervalMs = 5000;
        private static List<string> ValueProgramRegExes = new List<string>();    //TODO: use hashset instead
        private static bool ValueFailIfNoMatchingProcesses = false;
#else
        private static bool ValueShowHelp = false;

        private static bool ValueFailIfNotResponding = true;
        private static float? ValueCpuUsageTreshold = null; // 75;
        private static long? ValueMemoryCommitTresholdMB = null; // IntPtr.Size == 4 ? 1024 : 3072;
        private static long? ValueWorkingSetTresholdMB = null; // IntPtr.Size == 4 ? 1024 : 3072;
        private static long? ValueGdiHandlesTreshold = null;
        private static long? ValueUserHandlesTreshold = null;
        private static long? ValueHandlesTreshold = null;
        private static long? ValuePagedPoolTresholdKB = null;
        private static long? ValueNonPagedPoolTresholdKB = null;

        private static bool ValueApplyCpuUsageTresholdPerCpu = true;

        private static int ValueOutageTimeBeforeGiveUpSeconds = 30;
        private static int ValueOutageConditionNumChecks = 3;
        private static int ValuePassedCheckIntervalMs = 10000;
        private static int ValueFailedCheckIntervalMs = 5000;

        private static List<string> ValueProgramRegExes = new List<string>();    //TODO: use hashset instead
        private static bool ValueFailIfNoMatchingProcesses = false;
        private static string ValueExecuteCommandOnFail = null; 
        private static string ValueExecuteCommandArgsOnFail = null;  
        //private static bool ValueRestartProcessOnFail = false;    //TODO
#endif

    }
}