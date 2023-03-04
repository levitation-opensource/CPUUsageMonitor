
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
using System.Diagnostics;

namespace CPUUsageMonitor
{
    partial class Program
    {

        static void OutputConsoleArgumentsHelp()
        {
            Console.WriteLine("\n");
            Console.WriteLine("A program to monitor CPU and other resource usage of processes.");
            Console.WriteLine("The program exits when some resource usage metrics exceed a specified threshold.");
            Console.WriteLine("Program arguments and their default values:");
            Console.WriteLine("\n");

            Console.WriteLine(string.Format("{0} ({1})", ArgShowHelp, "Shows help text"));



            OutputConsoleArgumentHelp(ArgFailIfNotResponding, ValueFailIfNotResponding, "Consider a process as failed when it is not responding.");
            OutputConsoleArgumentHelp(ArgCpuUsageThreshold, ValueCpuUsageThreshold, "Cpu usage threshold percent (inclusive) at which a program is considered as failed.");
            OutputConsoleArgumentHelp(ArgMemoryCommitThresholdMB, ValueMemoryCommitThresholdMB, "Memory commit threshold in MB (inclusive) at which a program is considered as failed.");
            OutputConsoleArgumentHelp(ArgWorkingSetThresholdMB, ValueWorkingSetThresholdMB, "Working set threshold in MB (inclusive) at which a program is considered as failed.");
            OutputConsoleArgumentHelp(ArgGdiHandlesThreshold, ValueGdiHandlesThreshold, "GDI handles threshold (inclusive) at which a program is considered as failed.");
            OutputConsoleArgumentHelp(ArgUserHandlesThreshold, ValueUserHandlesThreshold, "User handles threshold (inclusive) at which a program is considered as failed.");
            OutputConsoleArgumentHelp(ArgHandlesThreshold, ValueHandlesThreshold, "Handles threshold (inclusive) at which a program is considered as failed.");
            OutputConsoleArgumentHelp(ArgPagedPoolThresholdKB, ValuePagedPoolThresholdKB, "Paged pool threshold in KB (inclusive) at which a program is considered as failed.");
            OutputConsoleArgumentHelp(ArgNonPagedPoolThresholdKB, ValueNonPagedPoolThresholdKB, "NonPaged pool threshold in KB (inclusive) at which a program is considered as failed.");



            OutputConsoleArgumentHelp(ArgApplyCpuUsageThresholdPerCpu, ValueApplyCpuUsageThresholdPerCpu, "Whether the Cpu Usage Threshold percent applies to capability of one cpu or capability of all cpu-s.");



            OutputConsoleArgumentHelp(ArgOutageTimeBeforeGiveUpSeconds, ValueOutageTimeBeforeGiveUpSeconds, "How long outage should last before trigger is activated and CPUUsageMonitor quits. NB! This timeout starts only after the failure count specified with " + ArgOutageConditionNumChecks + " has been exceeded.");
            OutputConsoleArgumentHelp(ArgOutageConditionNumChecks, ValueOutageConditionNumChecks, "How many checks should fail before outage can be declared");
            OutputConsoleArgumentHelp(ArgPassedCheckIntervalMs, ValuePassedCheckIntervalMs, "How many ms to pause after a successful check");
            OutputConsoleArgumentHelp(ArgFailedCheckIntervalMs, ValueFailedCheckIntervalMs, "How many ms to pause after a failed check");


            OutputConsoleArgumentHelp(ArgProgramRegEx, "\"regular expression\"", "Regular expression with process name. NB! Do not include file extension .exe! If regular expression matches multiple processes then if at least one process matching the given expression exceeds the cpu usage limit then the monitored program is considered as failed. Multiple regular expressions can be specified. In this case the program exits only after ALL monitored regular expressions have triggered.");  //TODO: do we need to include file extension .dll ?
            OutputConsoleArgumentHelp(ArgFailIfNoMatchingProcesses, ValueFailIfNoMatchingProcesses, "Consider having no matching processes as a failure too. Failure is still declared only after outage condition repeat counts and timeouts have passed.");
            OutputConsoleArgumentHelp(ArgExecuteCommandOnFail, "\"" + ValueExecuteCommandOnFail + "\"", "Instead of quitting the monitor program, execute a command specified in this argument, and continue running the monitor program. To add execution arguments to this call, this argument must be the last one of the CPU Usage Monitor arguments.");
            //OutputConsoleArgumentHelp(ArgRestartProcessOnFail, ValueRestartProcessOnFail, "Instead of quitting the monitor program, restart the specific process that exceeded the thresholds, and continue running the monitor program.");    //TODO



            Console.WriteLine("\n");

        }   //static void OutputHelp()

        // ############################################################################

        public static void OutputConsoleArgumentHelp<T>(string name, T defaultValue, string description) 
        {
            Console.WriteLine(string.Format("{0}={1} ({2})", name, defaultValue, description));
        }

        // ############################################################################

    }
}
