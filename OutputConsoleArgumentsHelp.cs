
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
            Console.WriteLine("The program exits when some resource usage metrics exceed a specified treshold.");
            Console.WriteLine("Program arguments and their default values:");
            Console.WriteLine("\n");

            Console.WriteLine(string.Format("{0} ({1})", ArgShowHelp, "Shows help text"));



            OutputConsoleArgumentHelp(ArgFailIfNotResponding, ValueFailIfNotResponding, "Consider a process as failed when it is not responding.");
            OutputConsoleArgumentHelp(ArgCpuUsageTreshold, ValueCpuUsageTreshold, "Cpu usage treshold percent (inclusive) at which a program is considered as failed.");
            OutputConsoleArgumentHelp(ArgMemoryCommitTresholdMB, ValueMemoryCommitTresholdMB, "Memory commit treshold in MB (inclusive) at which a program is considered as failed.");
            OutputConsoleArgumentHelp(ArgWorkingSetTresholdMB, ValueWorkingSetTresholdMB, "Working set treshold in MB (inclusive) at which a program is considered as failed.");
            OutputConsoleArgumentHelp(ArgGdiHandlesTreshold, ValueGdiHandlesTreshold, "GDI handles treshold (inclusive) at which a program is considered as failed.");
            OutputConsoleArgumentHelp(ArgUserHandlesTreshold, ValueUserHandlesTreshold, "User handles treshold (inclusive) at which a program is considered as failed.");
            OutputConsoleArgumentHelp(ArgHandlesTreshold, ValueHandlesTreshold, "Handles treshold (inclusive) at which a program is considered as failed.");
            OutputConsoleArgumentHelp(ArgPagedPoolTresholdKB, ValuePagedPoolTresholdKB, "Paged pool treshold in KB (inclusive) at which a program is considered as failed.");
            OutputConsoleArgumentHelp(ArgNonPagedPoolTresholdKB, ValueNonPagedPoolTresholdKB, "NonPaged pool treshold in KB (inclusive) at which a program is considered as failed.");



            OutputConsoleArgumentHelp(ArgApplyCpuUsageTresholdPerCpu, ValueApplyCpuUsageTresholdPerCpu, "Whether the Cpu Usage Treshold percent applies to capability of one cpu or capability of all cpu-s.");



            OutputConsoleArgumentHelp(ArgOutageTimeBeforeGiveUpSeconds, ValueOutageTimeBeforeGiveUpSeconds, "How long outage should last before trigger is activated and CPUUsageMonitor quits. NB! This timeout starts only after the failure count specified with " + ArgOutageConditionNumChecks + " has been exceeded.");
            OutputConsoleArgumentHelp(ArgOutageConditionNumChecks, ValueOutageConditionNumChecks, "How many checks should fail before outage can be declared");
            OutputConsoleArgumentHelp(ArgPassedCheckIntervalMs, ValuePassedCheckIntervalMs, "How many ms to pause after a successful check");
            OutputConsoleArgumentHelp(ArgFailedCheckIntervalMs, ValueFailedCheckIntervalMs, "How many ms to pause after a failed check");


            OutputConsoleArgumentHelp(ArgProgramRegEx, "\"regular expression\"", "Regular expression with process name. NB! Do not include file extension .exe! If regular expression matches multiple processes then if at least one process matching the given expression exceeds the cpu usage limit then the expression is considered as failed. Multiple regular expressions can be specified. In this case the program exits only after ALL monitored regular expressions have failed.");  //TODO: do we need to include file extension .dll ?
            OutputConsoleArgumentHelp(ArgFailIfNoMatchingProcesses, ValueFailIfNoMatchingProcesses, "Consider having no matching processes as a failure too. Failure is still declared only after outage condition counts and timeouts have passed.");
            OutputConsoleArgumentHelp(ArgExecuteCommandOnFail, "\"" + ValueExecuteCommandOnFail + "\"", "Instead of quitting the monitor program, execute a command specified in this argument, and continue running the monitor program. To add execution arguments to this call, this argument must be the last one of the monitor arguments.");
            //OutputConsoleArgumentHelp(ArgRestartProcessOnFail, ValueRestartProcessOnFail, "Instead of quitting the monitor program, restart the specific process that exceeded the tresholds, and continue running the monitor program.");    //TODO



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
