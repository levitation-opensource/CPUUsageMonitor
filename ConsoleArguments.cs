
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
        private const string ArgCpuUsageThreshold = "-cpuUsageThreshold";
        private const string ArgMemoryCommitThresholdMB = "-memoryCommitThresholdMB";
        private const string ArgWorkingSetThresholdMB = "-workingSetThresholdMB";
        private const string ArgGdiHandlesThreshold = "-gdiHandlesThreshold";
        private const string ArgUserHandlesThreshold = "-userHandlesThreshold";
        private const string ArgHandlesThreshold = "-handlesThreshold";
        private const string ArgPagedPoolThresholdKB = "-pagedPoolThresholdKB";
        private const string ArgNonPagedPoolThresholdKB = "-nonPagedPoolThresholdKB";

        private const string ArgApplyCpuUsageThresholdPerCpu = "-applyCpuUsageThresholdPerCpu";

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
        private static float ValueCpuUsageThreshold = 75;
        private static bool ValueApplyCpuUsageThresholdPerCpu = true;
        private static int ValueOutageTimeBeforeGiveUpSeconds = 30;
        private static int ValueOutageConditionNumChecks = 3;
        private static int ValuePassedCheckIntervalMs = 10000;
        private static int ValueFailedCheckIntervalMs = 5000;
        private static List<string> ValueProgramRegExes = new List<string>();    //TODO: use hashset instead
        private static bool ValueFailIfNoMatchingProcesses = false;
#else
        private static bool ValueShowHelp = false;

        private static bool ValueFailIfNotResponding = true;
        private static float? ValueCpuUsageThreshold = null; // 75;
        private static long? ValueMemoryCommitThresholdMB = null; // IntPtr.Size == 4 ? 1024 : 3072;
        private static long? ValueWorkingSetThresholdMB = null; // IntPtr.Size == 4 ? 1024 : 3072;
        private static long? ValueGdiHandlesThreshold = null;
        private static long? ValueUserHandlesThreshold = null;
        private static long? ValueHandlesThreshold = null;
        private static long? ValuePagedPoolThresholdKB = null;
        private static long? ValueNonPagedPoolThresholdKB = null;

        private static bool ValueApplyCpuUsageThresholdPerCpu = true;

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