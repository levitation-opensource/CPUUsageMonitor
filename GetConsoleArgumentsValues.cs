
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

        static void GetConsoleArgumentsValues(string[] args)
        {
            if (args.Length == 0)
            {
                ValueShowHelp = true;
                return;
            }

            string[] tokens;

            foreach (string arg in args)
            {
                if (ValueExecuteCommandOnFail != null && ValueExecuteCommandOnFail.Trim() != "")
                {
                    if (ValueExecuteCommandArgsOnFail == null)
                        ValueExecuteCommandArgsOnFail = string.Empty;
                    else
                        ValueExecuteCommandArgsOnFail += " ";

                    ValueExecuteCommandArgsOnFail += arg;

                    continue;   //NB!
                }


                tokens = arg.Split(new char[] { '=' }, 2);
                switch (tokens[0])
                {
                    case ArgShowHelp:
                        ValueShowHelp = true;
                        return;
                }

                // If no value for the argument is given, continue in next iteration
                if (tokens.Length < 2)
                {
                    Console.WriteLine("Unknown command line argument '{0}' was passed", tokens[0]);
                    continue;
                }

                string t = tokens[1]; 

                // Get the argument value
                switch (tokens[0])
                {
                    case ArgFailIfNotResponding:
                        GetConsoleArgumentValue2(t, ref ValueFailIfNotResponding);
                        break;

                    case ArgCpuUsageThreshold:
                        GetConsoleArgumentValue2(t, ref ValueCpuUsageThreshold);
                        break;

                    case ArgMemoryCommitThresholdMB:
                        GetConsoleArgumentValue2(t, ref ValueMemoryCommitThresholdMB);
                        break;

                    case ArgWorkingSetThresholdMB:
                        GetConsoleArgumentValue2(t, ref ValueWorkingSetThresholdMB);
                        break;

                    case ArgGdiHandlesThreshold:
                        GetConsoleArgumentValue2(t, ref ValueGdiHandlesThreshold);
                        break;

                    case ArgUserHandlesThreshold:
                        GetConsoleArgumentValue2(t, ref ValueUserHandlesThreshold);
                        break;

                    case ArgHandlesThreshold:
                        GetConsoleArgumentValue2(t, ref ValueHandlesThreshold);
                        break;

                    case ArgPagedPoolThresholdKB:
                        GetConsoleArgumentValue2(t, ref ValuePagedPoolThresholdKB);
                        break;

                    case ArgNonPagedPoolThresholdKB:
                        GetConsoleArgumentValue2(t, ref ValueNonPagedPoolThresholdKB);
                        break;



                    case ArgApplyCpuUsageThresholdPerCpu:
                        GetConsoleArgumentValue2(t, ref ValueApplyCpuUsageThresholdPerCpu);
                        break;



                    case ArgOutageTimeBeforeGiveUpSeconds:
                        GetConsoleArgumentValue2(t, ref ValueOutageTimeBeforeGiveUpSeconds);
                        break;

                    case ArgOutageConditionNumChecks:
                        GetConsoleArgumentValue2(t, ref ValueOutageConditionNumChecks);
                        break;

                    case ArgPassedCheckIntervalMs:
                        GetConsoleArgumentValue2(t, ref ValuePassedCheckIntervalMs);
                        break;

                    case ArgFailedCheckIntervalMs:
                        GetConsoleArgumentValue2(t, ref ValueFailedCheckIntervalMs);
                        break;



                    case ArgProgramRegEx:
                        string ValueProgramRegEx = null;
                        GetConsoleArgumentValue2(t, ref ValueProgramRegEx);
                        if (ValueProgramRegEx != null)
                            ValueProgramRegExes.Add(string.Intern(ValueProgramRegEx));
                        else
                            Debug.Assert(false);
                        break;

                    case ArgFailIfNoMatchingProcesses:
                        GetConsoleArgumentValue2(t, ref ValueFailIfNoMatchingProcesses);
                        break;

                    case ArgExecuteCommandOnFail:
                        GetConsoleArgumentValue2(t, ref ValueExecuteCommandOnFail);
                        break;

#if false   //TODO
                    case ArgRestartProcessOnFail:
                        GetConsoleArgumentValue2(t, ref ValueRestartProcessOnFail);
                        break;
#endif

                    default:
                        Console.WriteLine("Unknown command line argument '-{0}={1}' was passed", tokens[0], t);
                        break;
                }   //switch (tokens[0])
            }   //foreach (string arg in args)
        }   //static void GetArgumentValues(string[] args)

        // ############################################################################

        public static void GetConsoleArgumentValue2(string arg, ref float defaultValue)
        {
            defaultValue = GetConsoleArgumentValue(arg, defaultValue);
        }

        public static float GetConsoleArgumentValue(string arg, float defaultValue)
        {
            string value = GetConsoleArgumentValue(arg, (string)null);

            if (string.IsNullOrEmpty(value))
                return defaultValue;

            return Convert.ToSingle(value);
        }

        public static void GetConsoleArgumentValue2(string arg, ref float? defaultValue)
        {
            defaultValue = GetConsoleArgumentValue(arg, defaultValue);
        }

        public static float? GetConsoleArgumentValue(string arg, float? defaultValue)
        {
            string value = GetConsoleArgumentValue(arg, (string)null);

            if (string.IsNullOrEmpty(value))
                return defaultValue;

            return Convert.ToSingle(value);
        }

        public static void GetConsoleArgumentValue2(string arg, ref bool defaultValue)
        {
            defaultValue = GetConsoleArgumentValue(arg, defaultValue);
        }

        public static bool GetConsoleArgumentValue(string arg, bool defaultValue)
        {
            string value = GetConsoleArgumentValue(arg, (string)null);

            if (string.IsNullOrEmpty(value))
                return defaultValue;


            bool rval;
            if (Boolean.TryParse(value, out rval))
            {
                return rval;
            }
            else
            {
                value = value.ToLower();

                if (value == "yes")
                    return true;
                else if (value == "on")
                    return true;
                else if (value == "true")
                    return true;
                else if (value == "no")
                    return false;
                else if (value == "off")
                    return false;
                else if (value == "false")
                    return false;

                int val = Convert.ToInt32(value);

                if (val == 0)
                    return false;
                else if (val == 1)
                    return true;
                else
                    throw new FormatException();
            }
        }   //public static bool GetConsoleArgumentValue(string arg, bool defaultValue)

        public static void GetConsoleArgumentValue2(string arg, ref int defaultValue)
        {
            defaultValue = GetConsoleArgumentValue(arg, defaultValue);
        }

        public static int GetConsoleArgumentValue(string arg, int defaultValue)
        {
            string value = GetConsoleArgumentValue(arg, (string)null);

            if (string.IsNullOrEmpty(value))
                return defaultValue;

            return Convert.ToInt32(value);
        }

        public static void GetConsoleArgumentValue2(string arg, ref int? defaultValue)
        {
            defaultValue = GetConsoleArgumentValue(arg, defaultValue);
        }

        public static int? GetConsoleArgumentValue(string arg, int? defaultValue)
        {
            string value = GetConsoleArgumentValue(arg, (string)null);

            if (string.IsNullOrEmpty(value))
                return defaultValue;

            return Convert.ToInt32(value);
        }

        public static void GetConsoleArgumentValue2(string arg, ref long defaultValue)
        {
            defaultValue = GetConsoleArgumentValue(arg, defaultValue);
        }

        public static long GetConsoleArgumentValue(string arg, long defaultValue)
        {
            string value = GetConsoleArgumentValue(arg, (string)null);

            if (string.IsNullOrEmpty(value))
                return defaultValue;

            return Convert.ToInt64(value);
        }

        public static void GetConsoleArgumentValue2(string arg, ref long? defaultValue)
        {
            defaultValue = GetConsoleArgumentValue(arg, defaultValue);
        }

        public static long? GetConsoleArgumentValue(string arg, long? defaultValue)
        {
            string value = GetConsoleArgumentValue(arg, (string)null);

            if (string.IsNullOrEmpty(value))
                return defaultValue;

            return Convert.ToInt64(value);
        }

        public static void GetConsoleArgumentValue2(string arg, ref string defaultValue)
        {
            defaultValue = GetConsoleArgumentValue(arg, defaultValue);
        }

        /// <summary>
        /// Converts empty string to default string
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string GetConsoleArgumentValue(string arg, string defaultValue)
        {
            if (string.IsNullOrEmpty(arg))
                return defaultValue;

            return arg.Trim();
        }

        // ############################################################################

    }
}
