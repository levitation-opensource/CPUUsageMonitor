
#region Copyright (c) 2012 - 2013, Roland Pihlakas
/////////////////////////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2012 - 2013, Roland Pihlakas.
//
// Permission to copy, use, modify, sell and distribute this software
// is granted provided this copyright notice appears in all copies.
//
/////////////////////////////////////////////////////////////////////////////////////////
#endregion Copyright (c) 2012 - 2013, Roland Pihlakas

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Runtime;
using System.Reflection;
using System.Security;

namespace CPUUsageMonitor
{
    // Summary:
    //     Encapsulates a method that takes no parameters and does not return a value.
    public delegate void Action();  //.net 3.0 does not contain Action delegate declaration

    [SuppressUnmanagedCodeSecurity]     //SuppressUnmanagedCodeSecurity - For methods in this particular class, execution time is often critical. Security can be traded for additional speed by applying the SuppressUnmanagedCodeSecurity attribute to the method declaration. This will prevent the runtime from doing a security stack walk at runtime. - MSDN: Generally, whenever managed code calls into unmanaged code (by PInvoke or COM interop into native code), there is a demand for the UnmanagedCode permission to ensure all callers have the necessary permission to allow this. By applying this explicit attribute, developers can suppress the demand at run time. The developer must take responsibility for assuring that the transition into unmanaged code is sufficiently protected by other means. The demand for the UnmanagedCode permission will still occur at link time. For example, if function A calls function B and function B is marked with SuppressUnmanagedCodeSecurityAttribute, function A will be checked for unmanaged code permission during just-in-time compilation, but not subsequently during run time.
    partial class Program
    {
        [DllImport("kernel32.dll")] 
        internal static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

        [/*DllImport("kernel32.dll")*/DllImport("psapi.dll")]
        internal static extern bool EmptyWorkingSet(IntPtr processHandle);

        static MultiAdvancedChecker multiChecker = null;
        internal static CpuUsageComputer cpuUsageComputer;

        // ############################################################################

        [STAThread]         //prevent message loop creation
        static void Main(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine(Application.ProductName + " / Version: " + Application.ProductVersion);
            Console.WriteLine();


            GetConsoleArgumentsValues(args);


            if (ValueShowHelp)
            {
                OutputConsoleArgumentsHelp();
                return;
            }


            ExitHandler.InitUnhandledExceptionHandler();
            ExitHandler.HookSessionEnding();
            ExitHandler.ExitEventOnce += ExitEventHandler;



            NativeMethods.EnableSeIncBasePriorityPrivilege(null);


            Process CurrentProcess = Process.GetCurrentProcess();
            IntPtr handle = CurrentProcess.Handle;
            
            CurrentProcess.PriorityClass = ProcessPriorityClass.RealTime;
            CurrentProcess.PriorityBoostEnabled = true;

            NativeMethods.SetPagePriority(handle, 1);   //NB! lowest Page priority
            NativeMethods.SetIOPriority(handle, NativeMethods.PROCESSIOPRIORITY.PROCESSIOPRIORITY_NORMAL);   //ensure that we do not inherit low IO priority from the parent process or something like that


            GC.Collect(2, GCCollectionMode.Forced);     //collect now all unused startup info because later we will be relatively steady state and will not need any more much memory management
            GCSettings.LatencyMode = GCLatencyMode.Batch; //most intrusive mode - most efficient   //This option affects only garbage collections in generation 2; generations 0 and 1 are always non-concurrent because they finish very fast.  - http://msdn.microsoft.com/en-us/library/ee787088(v=VS.110).aspx#workstation_and_server_garbage_collection
            try
            {
                //GC.WaitForFullGCComplete();   //cob roland: the exception cannot be caught when the method name is written inline, see also http://stackoverflow.com/questions/3546580/why-is-it-not-possible-to-catch-missingmethodexception
                typeof(GC).InvokeMember("WaitForFullGCComplete",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod,
                    null, null, null);    //see ms-help://MS.VSCC.v90/MS.MSDNQTR.v90.en/fxref_mscorlib/html/9926d3b0-b0ef-e965-bc72-9ee34bf84df5.htm
            }
            catch (MissingMethodException)  //GC.WaitForFullGCComplete() is only available on .NET SP1 versions
            {
                Thread.Sleep(100);
            }


            AutoResetEvent GC_WaitForPendingFinalizers_done = new AutoResetEvent(false);
            Thread thread = new Thread(() =>
            {
                // Wait for all finalizers to complete before continuing.
                // Without this call to GC.WaitForPendingFinalizers, 
                // the worker loop below might execute at the same time 
                // as the finalizers.
                // With this call, the worker loop executes only after
                // all finalizers have been called.
                GC.WaitForPendingFinalizers();

                GC_WaitForPendingFinalizers_done.Set();
            });
            thread.Name = "GC.WaitForPendingFinalizers thread";
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;    //Background threads are identical to foreground threads, except that background threads do not prevent a process from terminating.
            thread.Start();

            GC_WaitForPendingFinalizers_done.WaitOne(10000);    //NB! prevent hangs



            NativeMethods.SetProcessWorkingSetSize(handle, new IntPtr(-1), new IntPtr(-1));   //empty the working set
            NativeMethods.EmptyWorkingSet(handle);   //empty the working set



            float? cpuUsageThreshold = ValueCpuUsageThreshold;
            if (!ValueApplyCpuUsageThresholdPerCpu && cpuUsageThreshold.HasValue)
                cpuUsageThreshold *= Environment.ProcessorCount;


        startMonitoring:
            bool cancelFlag = false;
            try 
            {
                int updateInterval = Math.Min(ValuePassedCheckIntervalMs, ValueFailedCheckIntervalMs);

                cpuUsageComputer = new CpuUsageComputer(ValueProgramRegExes, 
                                            /*updateInterval, */ValueFailedCheckIntervalMs, ValuePassedCheckIntervalMs);
                cpuUsageComputer.StartMonitoringThread();


                multiChecker = new MultiAdvancedChecker(ValueProgramRegExes);
                {
                    multiChecker.CheckUntilOutageOrCancel
                    (
                        ValueOutageTimeBeforeGiveUpSeconds,
                        ValueOutageConditionNumChecks,
                        ValuePassedCheckIntervalMs,
                        ValueFailedCheckIntervalMs,
                        ValueFailIfNoMatchingProcesses,
                        ValueFailIfNotResponding,
                        cpuUsageThreshold,
                        ValueMemoryCommitThresholdMB,
                        ValueWorkingSetThresholdMB,
                        ValueGdiHandlesThreshold,
                        ValueUserHandlesThreshold,
                        ValueHandlesThreshold,
                        ValuePagedPoolThresholdKB,
                        ValueNonPagedPoolThresholdKB,
                        /*checkSuccessCallback = */null
                    );
                }
            }
            finally 
            {
                cancelFlag = multiChecker.GetCancelFlag();

                //cpuUsageComputer.exitFlag = true;

                if (multiChecker != null)
                    multiChecker.Dispose();
                multiChecker = null;
            }

            bool exit = true;
            if (ValueExecuteCommandOnFail != null && ValueExecuteCommandOnFail.Trim() != "")
            {
                exit = false;


                if (!cancelFlag)   //NB!
                {
                    if (ValueExecuteCommandArgsOnFail == null)
                        ValueExecuteCommandArgsOnFail = string.Empty;


                    Console.WriteLine(string.Format("Executing command: {0} {1}", ValueExecuteCommandOnFail, ValueExecuteCommandArgsOnFail));


                    var startInfo = new ProcessStartInfo(ValueExecuteCommandOnFail, ValueExecuteCommandArgsOnFail);
                    
                    //startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.CreateNoWindow = true;
                    startInfo.UseShellExecute = false;// true;  //needs to be false to redirect steams
                    //startInfo.UseShellExecute = true;

                    startInfo.ErrorDialog = false;  //NB!

                    startInfo.RedirectStandardError = true;     //TODO: single-char output capture
                    startInfo.RedirectStandardOutput = true;     //TODO: single-char output capture
                    startInfo.RedirectStandardInput = true;     //TODO!!!

                    using (Process commandProcess = new Process())
                    {

                        commandProcess.StartInfo = startInfo;

                        commandProcess.OutputDataReceived += new DataReceivedEventHandler(commandProcess_OutputDataReceived);
                        commandProcess.ErrorDataReceived += new DataReceivedEventHandler(commandProcess_ErrorDataReceived);

#if USE_INPUT_REDIRECT                       
                        int stopInputThread = 0;
                        Thread inputThread = new Thread(() => 
                        {
                            while (Interlocked.CompareExchange(ref stopInputThread, 0, 0) == 0)     //volatile read
                            {
                                var keyInfo = Console.ReadKey(/*display = */true);
                                if (keyInfo.KeyChar != 0)
                                    commandProcess.StandardInput.Write(keyInfo.KeyChar);
                                else
                                    Thread.Sleep(100);
                            }
                        });
                        inputThread.SetApartmentState(ApartmentState.STA);
#endif


                        commandProcess.Start();

#if USE_INPUT_REDIRECT                       
                        commandProcess.StandardInput.AutoFlush = true;  //TODO!!! write here all your keystrokes

                        inputThread.Start();
#endif


                        commandProcess.BeginOutputReadLine();
                        commandProcess.BeginErrorReadLine();

                        commandProcess.WaitForExit();   //NB!
#if USE_INPUT_REDIRECT                       
                        stopInputThread = 1;
                        inputThread.Join();
#endif

                    }   //using (Process commandProcess = Process.Start(startInfo))


                    goto startMonitoring;   //restart monitoring
                
                }   //f (!multiChecker.GetCancelFlag())

            }   //if (ValueExecuteCommandOnFail != null && ValueExecuteCommandOnFail.Trim() != "")

            
            if (exit)
            {
                //exit the program...

                cpuUsageComputer.exitFlag = true;
            }
        }

        static void commandProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

        static void commandProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

        // ############################################################################

        static void ExitEventHandler(bool hasShutDownStarted)
        {
            cpuUsageComputer.exitFlag = true;

            if (multiChecker != null)
                multiChecker.SetCancelFlag();

            Console.WriteLine("Exiting...");
            ExitHandler.DoExit();
        }
    }   //partial class Program

    // ############################################################################

    public class MultiAdvancedChecker : IDisposable 
    {
        internal static IntPtr CurrentProcessHandle;

        List<AdvancedChecker> advancedCheckers = new List<AdvancedChecker>();
        List<Thread> advancedCheckerThreads = new List<Thread>();

        volatile bool cancelFlag = false;
        volatile int currentOutageHostCount = 0;

        // ############################################################################

        public MultiAdvancedChecker(List<string> ProgramRegExes)
        {
            foreach (string ProgramRegEx in ProgramRegExes)
                advancedCheckers.Add(new AdvancedChecker(ProgramRegEx));
        }
#if false
        public MultiAdvancedChecker(IEnumerable<string> hosts, IEnumerable<string> sourceHosts)
        {
            using (IEnumerator<string> sourceHostEnumerator = sourceHosts.GetEnumerator())
            {
                foreach (string host in hosts)
                {
                    string sourceHost;
                    if (sourceHostEnumerator.MoveNext())
                        sourceHost = sourceHostEnumerator.Current;
                    else
                        sourceHost = null;

                    advancedCheckers.Add(new AdvancedCheckers(host, sourceHost));
                }
            }
        }
#endif
        // ############################################################################

        public bool GetCancelFlag()
        {
            return this.cancelFlag;
        }

        public void SetCancelFlag()
        {
            this.cancelFlag = true;

            foreach (var advancedChecker in advancedCheckers)     //propagate cancel flag to all checkers
                advancedChecker.SetCancelFlag();
        }

        // ############################################################################

        public void Dispose()
        {
            if (advancedCheckers != null)
            {
                //foreach (var advancedChecker in advancedCheckers)
                //    advancedChecker.Dispose();

                advancedCheckers = null;
            }
        }

        // ############################################################################

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outageTimeBeforeGiveUpSeconds"></param>
        /// <param name="outageConditionNumChecks"></param>
        /// <param name="passedCheckIntervalMs"></param>
        /// <param name="failedCheckIntervalMs"></param>
        /// <param name="timeoutMs"></param>
        /// <param name="checkSuccessCallback">NB! checkSuccessCallback should be <b>threadsafe</b> since it can be called from multiple threads simultaneously</param>
        /// <returns></returns>
        public bool CheckUntilOutageOrCancel
        (
            int outageTimeBeforeGiveUpSeconds, 
            int outageConditionNumChecks, 
            int passedCheckIntervalMs, 
            int failedCheckIntervalMs, 
            bool failIfNoMatchingProcesses, 
            bool failIfNotResponding, 
            float? cpuUsageThreshold, 
            long? memoryCommitThresholdMB,
            long? workingSetThresholdMB,
            long? gdiHandlesThreshold,
            long? userHandlesThreshold,
            long? handlesThreshold,
            long? pagedPoolThresholdKB,
            long? nonPagedPoolThresholdKB,
            Action checkSuccessCallback
        )
        {
            currentOutageHostCount = 0;

            //start all parallel checker threads
            foreach (var advancedChecker1 in advancedCheckers)
            {
                AdvancedChecker advancedChecker = advancedChecker1;    //NB! need to copy so that each thread has separate checker variable instance

                Thread thread = new Thread(t => 
                {
                    Thread.CurrentThread.Priority = ThreadPriority.Highest;


                    bool outageBegun = false;
                    do
                    {
                        bool success = advancedChecker.CheckUntilOutageOrCancel
                        (
                            outageBegun,
                            outageTimeBeforeGiveUpSeconds,
                            outageConditionNumChecks,
                            passedCheckIntervalMs,
                            failedCheckIntervalMs,
                            failIfNoMatchingProcesses,
                            failIfNotResponding,
                            cpuUsageThreshold,
                            memoryCommitThresholdMB,
                            workingSetThresholdMB,
                            gdiHandlesThreshold,
                            userHandlesThreshold,
                            handlesThreshold,
                            pagedPoolThresholdKB,
                            nonPagedPoolThresholdKB,
                            () =>
                            {
                                //Debugger.Break();

                                if (checkSuccessCallback != null)
                                    checkSuccessCallback();

                                Debug.Assert(currentOutageHostCount > 0);
#pragma warning disable 0420    //warning CS0420: 'CheckTool.MultiAdvancedChecker.currentOutageHostCount': a reference to a volatile field will not be treated as volatile
                                Interlocked.Decrement(ref currentOutageHostCount);
#pragma warning restore 0420
                                Debug.Assert(currentOutageHostCount >= 0);

                                outageBegun = false; //NB!
                            }
                        );

                        if (this.cancelFlag)
                            return;
                        Debug.Assert(!success);


                        if (!outageBegun)     //NB! count each checker's outage only once per outage begin
                        {
#pragma warning disable 0420    //warning CS0420: 'CheckTool.MultiAdvancedChecker.currentOutageHostCount': a reference to a volatile field will not be treated as volatile
                            Interlocked.Increment(ref currentOutageHostCount);
#pragma warning restore 0420
                            outageBegun = true;
                        }

                        //check whether there is a global outage occurring
                        if (currentOutageHostCount == advancedCheckers.Count)
                        {
                            foreach (var otherChecker in advancedCheckers)     //propagate cancel flag to all checkers but do not set cancel flag in current MultiChecker object. Actually this should not be necessary since all checkers should be exiting anyway
                                otherChecker.SetCancelFlag();

                            return;     //now here we quit from the loop
                        }   //if (currentOutageHostCount == advancedCheckers.Count)
                    }
                    while (true);   //NB! repeat the checker even when it encountered an outage
                });
                thread.Start();

                advancedCheckerThreads.Add(thread);
            
            }   //foreach (var advancedChecker in advancedCheckers)


            Thread.CurrentThread.Priority = ThreadPriority.Highest;     //NB! this thread also has highest priority since when something is misbehaving then we need to close this program fast to enable the parent .bat file to continue running



            GC.Collect(2, GCCollectionMode.Forced);     //collect now all unused startup info because later we will be relatively steady state and will not need any more much memory management
            GC.WaitForFullGCComplete();

            GCSettings.LatencyMode = GCLatencyMode.Batch; //most intrusive mode - most efficient   //This option affects only garbage collections in generation 2; generations 0 and 1 are always non-concurrent because they finish very fast.  - http://msdn.microsoft.com/en-us/library/ee787088(v=VS.110).aspx#workstation_and_server_garbage_collection

            CurrentProcessHandle = Process.GetCurrentProcess().Handle;
            TrimWorkingSet(CurrentProcessHandle);



            //sit here until all checker threads have exited
            foreach (var thread in advancedCheckerThreads)
            {
                thread.Join();
            }

            return this.cancelFlag;
        }

        internal static void TrimWorkingSet(IntPtr handle)
        {
            try
            {
                Program.SetProcessWorkingSetSize(handle, new IntPtr(-1), new IntPtr(-1));   //empty the working set
            }
            catch
            {
            }

            try
            {
                Program.EmptyWorkingSet(handle);   //empty the working set
            }
            catch
            {
            }
        }

    }   //class MultiAdvancedChecker 

    // ############################################################################

    public class AdvancedChecker //: BasicChecker
    {
        volatile bool cancelFlag = false;

        private string ProgramRegEx;

        // ############################################################################

        public AdvancedChecker(string ProgramRegEx)
            //: base(ProgramRegEx)
        {
            this.ProgramRegEx = ProgramRegEx;
        }
#if false
        public AdvancedCheckers(string host, string sourceHost)
            : base(host, sourceHost)
        {
        }
#endif
        // ############################################################################

        public void SetCancelFlag()
        {
            this.cancelFlag = true;
        }

        // ############################################################################

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outageTimeBeforeGiveUpSeconds"></param>
        /// <param name="outageConditionNumChecks"></param>
        /// <param name="passedCheckIntervalMs"></param>
        /// <param name="failedCheckIntervalMs"></param>
        /// <param name="timeoutMs"></param>
        /// <param name="checkSuccessCallback">NB! The callback is called only <b>once</b> after each outage end</param>
        /// <returns></returns>
        public bool CheckUntilOutageOrCancel
        (
            bool outer_outageState, 
            int outageTimeBeforeGiveUpSeconds, 
            int outageConditionNumChecks, 
            int passedCheckIntervalMs, 
            int failedCheckIntervalMs, 
            bool failIfNoMatchingProcesses, 
            bool failIfNotResponding, 
            float? cpuUsageThreshold, 
            long? memoryCommitThresholdMB,
            long? workingSetThresholdMB,
            long? gdiHandlesThreshold,
            long? userHandlesThreshold,
            long? handlesThreshold,
            long? pagedPoolThresholdKB,
            long? nonPagedPoolThresholdKB,
            Action checkSuccessCallback
        )
        {
            DateTime? outageBegin = null;
            do
            {
                bool success = CheckUntilOutageOrCancel
                (
                    outageBegin != null,
                    outageConditionNumChecks, 
                    passedCheckIntervalMs, 
                    failedCheckIntervalMs,
                    failIfNoMatchingProcesses,
                    failIfNotResponding,
                    cpuUsageThreshold,
                    memoryCommitThresholdMB,
                    workingSetThresholdMB,
                    gdiHandlesThreshold,
                    userHandlesThreshold,
                    handlesThreshold,
                    pagedPoolThresholdKB,
                    nonPagedPoolThresholdKB,
                    () => 
                    {
                        if (outer_outageState)    //NB! propagate the success message only when the outage was started
                        {
                            //Debugger.Break();

                            if (checkSuccessCallback != null)
                                checkSuccessCallback();

                            outer_outageState = false;
                        }
                            
                        outageBegin = null;     //reset outage status
                    }
                );

                if (this.cancelFlag)
                    break;
                Debug.Assert(!success);

                DateTime now = DateTime.UtcNow;
                if (outageBegin == null)
                {
                    outageBegin = now;
                }
                else
                {
                    TimeSpan outageDuration = now - outageBegin.Value;

                    if (outageDuration.TotalSeconds >= outageTimeBeforeGiveUpSeconds)    //should we give up?
                        break;
                }
            }
            while (true);

            return this.cancelFlag;
        }

        // ############################################################################

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outageConditionNumChecks"></param>
        /// <param name="passedCheckIntervalMs"></param>
        /// <param name="failedCheckIntervalMs"></param>
        /// <param name="timeoutMs"></param>
        /// <param name="checkSuccessCallback">NB! The callback is called only <b>once</b> after each outage end</param>
        /// <returns></returns>
        public bool CheckUntilOutageOrCancel
        (
            bool outer_outageState, 
            int outageConditionNumChecks, 
            int passedCheckIntervalMs, 
            int failedCheckIntervalMs, 
            bool failIfNoMatchingProcesses, 
            bool failIfNotResponding, 
            float? cpuUsageThreshold, 
            long? memoryCommitThresholdMB,
            long? workingSetThresholdMB,
            long? gdiHandlesThreshold,
            long? userHandlesThreshold,
            long? handlesThreshold,
            long? pagedPoolThresholdKB,
            long? nonPagedPoolThresholdKB,
            Action checkSuccessCallback
        )
        {
            this.cancelFlag = false;

            int outageCount = 0;
            bool success;
            int last_expectedTimeDiffMs = 0;
            do
            {
                //success = base.CheckHost(cpuUsageThreshold);
                long? memoryCommitMB;
                long? workingSetMB;
                long? gdiHandles;
                long? userHandles;
                long? handles;
                long? pagedPoolKB;
                long? nonPagedPoolKB;

                bool notRunning;
                bool notResponding;

                double? cpu_usage = Program.cpuUsageComputer.GetCpuUsageForRegex
                (
                    out memoryCommitMB, 
                    out workingSetMB,
                    out gdiHandles,
                    out userHandles,
                    out handles,
                    out pagedPoolKB,
                    out nonPagedPoolKB,

                    out notRunning, 
                    out notResponding, 

                    ProgramRegEx, 
                    last_expectedTimeDiffMs, 
                    failIfNoMatchingProcesses, 
                    failIfNotResponding
                );
                
                success = true;
                if (failIfNoMatchingProcesses && !cpu_usage.HasValue)
                    success = false;
                else if (notResponding)
                    success = false;

                else if (cpu_usage.HasValue && cpuUsageThreshold.HasValue && cpu_usage >= cpuUsageThreshold)
                    success = false;
                else if (memoryCommitMB.HasValue && memoryCommitThresholdMB.HasValue && memoryCommitMB >= workingSetThresholdMB)
                    success = false;
                else if (workingSetMB.HasValue && workingSetThresholdMB.HasValue && workingSetMB >= workingSetThresholdMB)
                    success = false;
                else if (gdiHandles.HasValue && gdiHandlesThreshold.HasValue && gdiHandles >= gdiHandlesThreshold)
                    success = false;
                else if (userHandles.HasValue && userHandlesThreshold.HasValue && userHandles >= userHandlesThreshold)
                    success = false;
                else if (handles.HasValue && handlesThreshold.HasValue && handles >= handlesThreshold)
                    success = false;
                else if (pagedPoolKB.HasValue && pagedPoolThresholdKB.HasValue && pagedPoolKB >= pagedPoolThresholdKB)
                    success = false;
                else if (nonPagedPoolKB.HasValue && nonPagedPoolThresholdKB.HasValue && nonPagedPoolKB >= nonPagedPoolThresholdKB)
                    success = false;


                if (success)
                {
                    Console.WriteLine(string.Format("Check OK: {0} - {1} %   {2} MB Commit   {3} MB WS   {4} GDI   {5} User   {6} Handles   {7} KB Paged Pool   {8} KB NP Pool", 
                        ProgramRegEx, 
                        (cpu_usage.HasValue ? cpu_usage.Value.ToString("F2") : "N/A"),
                        (memoryCommitMB.HasValue ? memoryCommitMB.Value.ToString() : "N/A"),
                        (workingSetMB.HasValue ? workingSetMB.Value.ToString() : "N/A"),
                        (gdiHandles.HasValue ? gdiHandles.Value.ToString() : "N/A"),
                        (userHandles.HasValue ? userHandles.Value.ToString() : "N/A"),
                        (handles.HasValue ? handles.Value.ToString() : "N/A"),
                        (pagedPoolKB.HasValue ? pagedPoolKB.Value.ToString() : "N/A"),
                        (nonPagedPoolKB.HasValue ? nonPagedPoolKB.Value.ToString() : "N/A")
                    ));


                    if (outer_outageState)
                    {
                        Program.cpuUsageComputer.DecrementFailedCheckersCount();
                    }
                    if (outageCount == 1) //NB! separate decrement of failure counter
                    {
                        Program.cpuUsageComputer.DecrementFailedCheckersCount();
                    }


                    if (outer_outageState)    //NB! propagate the success message only when the outage was started
                    {
                        //Debugger.Break();

                        if (checkSuccessCallback != null)
                            checkSuccessCallback();

                        outer_outageState = false;
                    }

                    outageCount = Math.Max(0, outageCount - 1);

                    if (!this.cancelFlag)  
                    {
                        if (outageCount == 0)  //roland 4.06.2013
                        {
                            //Thread.Sleep(passedCheckIntervalMs);
                            int sleepStep = 1000;
                            for (int i = 0; i < passedCheckIntervalMs; i += sleepStep)
                            {
                                if (!this.cancelFlag)
                                    Thread.Sleep(Math.Min(sleepStep, passedCheckIntervalMs - i));
                            }

                            last_expectedTimeDiffMs = passedCheckIntervalMs;
                        }
                        else    //if (outageCount == 0)
                        {
                            //Thread.Sleep(passedCheckIntervalMs);
                            int sleepStep = 1000;
                            for (int i = 0; i < failedCheckIntervalMs; i += sleepStep)
                            {
                                if (!this.cancelFlag)
                                    Thread.Sleep(Math.Min(sleepStep, failedCheckIntervalMs - i));
                            }

                            last_expectedTimeDiffMs = failedCheckIntervalMs;
                        }
                    }   //if (!this.cancelFlag)  
                }
                else    //if (success)
                {
                    if (
                        cpu_usage.HasValue
                        || memoryCommitMB.HasValue
                        || workingSetMB.HasValue
                        || gdiHandles.HasValue
                        || userHandles.HasValue
                        || handles.HasValue
                        || pagedPoolKB.HasValue
                        || nonPagedPoolKB.HasValue
                    )
                    {
                        Console.WriteLine(string.Format("Check FAILED: {0} - {1} %   {2} MB Commit   {3} MB WS   {4} GDI   {5} User   {6} Handles   {7} KB Paged Pool   {8} KB NP Pool    {9}",
                            ProgramRegEx,
                            (cpu_usage.HasValue ? cpu_usage.Value.ToString("F2") : "N/A"),
                            (memoryCommitMB.HasValue ? memoryCommitMB.Value.ToString() : "N/A"),
                            (workingSetMB.HasValue ? workingSetMB.Value.ToString() : "N/A"),
                            (gdiHandles.HasValue ? gdiHandles.Value.ToString() : "N/A"),
                            (userHandles.HasValue ? userHandles.Value.ToString() : "N/A"),
                            (handles.HasValue ? handles.Value.ToString() : "N/A"),
                            (pagedPoolKB.HasValue ? pagedPoolKB.Value.ToString() : "N/A"),
                            (nonPagedPoolKB.HasValue ? nonPagedPoolKB.Value.ToString() : "N/A"),
                            notResponding ? "NOT RESPONDING" : ""
                        ));
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Check N/A: {0} - PROGRAM NOT RUNNING", ProgramRegEx));
                    }


                    if (outageCount == 0)
                        Program.cpuUsageComputer.IncrementFailedCheckersCount();


                    outageCount++;
                    if (!this.cancelFlag 
                        //&& outageCount < outageConditionNumChecks)   //sleep only when outage count not exceeded   //cob roland: sleep also when outage count is exceeded since we are likely going to repeat the loop
                    )
                    {
                        //Thread.Sleep(failedCheckIntervalMs);
                        int sleepStep = 1000;
                        for (int i = 0; i < failedCheckIntervalMs; i += sleepStep)
                        {
                            if (!this.cancelFlag)
                                Thread.Sleep(Math.Min(sleepStep, failedCheckIntervalMs - i));
                        }

                        last_expectedTimeDiffMs = failedCheckIntervalMs;
                    }
                }    //if (success)

                MultiAdvancedChecker.TrimWorkingSet(MultiAdvancedChecker.CurrentProcessHandle);
            }
            while (outageCount < outageConditionNumChecks && !this.cancelFlag);

            return this.cancelFlag;

        }   //public bool CheckUntilOutageOrCancel()

    }   //class AdvancedChecker

    // ############################################################################

    public class ObjRef<T>
    {
        public T Value;

        public ObjRef(T value_in)
        {
            Value = value_in;
        }
    }

    // ############################################################################

    public class CpuUsageComputer
    {
        public volatile bool exitFlag = false;

        //private readonly int sleepMs;

        private readonly bool AllRegexesAlphaNumericOnly = true;
        private readonly List<string> regex_strings = null;

        private List<KeyValuePair<string, Regex>> regexes_compiled;
        private Dictionary<int, TimeSpan> prev_cpu_times = new Dictionary<int, TimeSpan>();
        private volatile Dictionary<string, double> cpu_usages = null; // = new Dictionary<string, double>();
        private bool firstLoop = true;

        private volatile Dictionary<string, long> memoryCommitsInMB = new Dictionary<string, long>();
        private volatile Dictionary<string, long> workingSetInMB = new Dictionary<string, long>();
        private volatile Dictionary<string, long> gdiHandlesDict = new Dictionary<string, long>();
        private volatile Dictionary<string, long> userHandlesDict = new Dictionary<string, long>();
        private volatile Dictionary<string, long> handlesDict = new Dictionary<string, long>();
        private volatile Dictionary<string, long> pagedPoolInKB = new Dictionary<string, long>();
        private volatile Dictionary<string, long> nonPagedPoolInKB = new Dictionary<string, long>();

        private volatile Dictionary<string, bool> notRespondingProcesses = null;    //this dictionary is actually used as hashset

        private readonly ManualResetEvent updatedEvent = new ManualResetEvent(false);

        private volatile ObjRef<long> prev_stopWatch_time = null;
        private readonly object measurementTimeDiff_lock = new object();

        private volatile int FailedCheckersCount = 0;

        private readonly int FailedCheckIntervalMs;
        private readonly int PassedCheckIntervalMs;

        private volatile int currSleepMs = 0;

        // ############################################################################

        public void IncrementFailedCheckersCount()
        {
            Interlocked.Increment(ref FailedCheckersCount);
        }

        public void DecrementFailedCheckersCount()
        {
            Interlocked.Decrement(ref FailedCheckersCount);
            Debug.Assert(FailedCheckersCount >= 0);
        }

        // ############################################################################

        public CpuUsageComputer(List<string> regExes, /*int sleepMs, */int FailedCheckIntervalMs, int PassedCheckIntervalMs)
        {
            //this.sleepMs = sleepMs;
            this.FailedCheckIntervalMs = FailedCheckIntervalMs;
            this.PassedCheckIntervalMs = PassedCheckIntervalMs;



            Regex alphaNumericCheck = new Regex("^[a-z0-9_-]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);



            this.regexes_compiled = new List<KeyValuePair<string, Regex>>(regExes.Count);

            foreach (var regex_str in regExes)
            {
                var add_regex = new Regex
                (
                    "^" + regex_str + "$",    //auto-lock the regex endpoints
                    RegexOptions.Compiled
                    //| RegexOptions.CultureInvariant
                    | RegexOptions.ExplicitCapture
                    | RegexOptions.IgnoreCase
                );

                this.regexes_compiled.Add(new KeyValuePair<string, Regex>(regex_str, add_regex));


                if (!alphaNumericCheck.IsMatch(regex_str))
                {
                    this.AllRegexesAlphaNumericOnly = false;
                }

            }   //foreach (var regex_str in regExes)


            this.regex_strings = regExes;

#if false
            if (this.AllRegexesAlphaNumericOnly)
                Console.WriteLine("All regexes are alphanumeric only. This saves CPU due to the possibility to take list of only matching processes.");
            else
                Console.WriteLine("All regexes are NOT alphanumeric only. Checking for such regex takes more CPU due to the need to take list of all running processes.");
#endif
        }   //public CpuUsageComputer(List<string> regExes)

        // ############################################################################

        public double? GetCpuUsageForRegex
        (
            out long? memoryCommitMB,
            out long? workingSetMB,
            out long? gdiHandles,
            out long? userHandles,
            out long? handles,
            out long? pagedPoolKB,
            out long? nonPagedPoolKB,
    
            out bool notRunning, 
            out bool notResponding,

            string regex, int expectedTimeDiffMs, 
            bool failIfNoMatchingProcesses, bool failIfNotResponding
        )
        {

            Monitor.Enter(measurementTimeDiff_lock);    //We need this lock in order to prevent race condition resetting the updatedEvent immediately AFTER it has been set by monitoring thread.

            TimeSpan measurementTimeDiff = GetMeasurementTimeDiff();    //time since previous measurement


            //if (measurementTimeDiff < TimeSpan.FromMilliseconds(expectedTimeDiffMs / 2))
            if (measurementTimeDiff < TimeSpan.FromMilliseconds(currSleepMs / 2))
            {
                Monitor.Exit(measurementTimeDiff_lock);
                //return previous result
            }
            else
            {
                updatedEvent.Reset();
                Monitor.Exit(measurementTimeDiff_lock);     //NB! exit lock only after resetting the event

                //wait for update and return updated result
                //updatedEvent.WaitOne();
                int sleepStep = 1000;
                while (!exitFlag && !updatedEvent.WaitOne(0))
                {
                    if (!exitFlag)
                        Thread.Sleep(sleepStep);    //use coarse wait
                }
            }


            memoryCommitMB = null;
            workingSetMB = null;
            gdiHandles = null;
            userHandles = null;
            handles = null;
            pagedPoolKB = null;
            nonPagedPoolKB = null;

            notRunning = false;
            notResponding = false;

            if (failIfNotResponding && notRespondingProcesses.ContainsKey(regex))   //this dictionary is actually used as HashSet
                //return double.MaxValue; //always fails
                notResponding = true;



            long memoryCommitMBResult;
            if (memoryCommitsInMB.TryGetValue(regex, out memoryCommitMBResult))
                memoryCommitMB = memoryCommitMBResult;

            long workingSetMBResult;
            if (workingSetInMB.TryGetValue(regex, out workingSetMBResult))
                workingSetMB = workingSetMBResult;

            long gdiHandlesResult;
            if (gdiHandlesDict.TryGetValue(regex, out gdiHandlesResult))
                gdiHandles = gdiHandlesResult;

            long userHandlesResult;
            if (userHandlesDict.TryGetValue(regex, out userHandlesResult))
                userHandles = userHandlesResult;

            long handlesResult;
            if (handlesDict.TryGetValue(regex, out handlesResult))
                handles = handlesResult;

            long pagedPoolKBResult;
            if (pagedPoolInKB.TryGetValue(regex, out pagedPoolKBResult))
                pagedPoolKB = pagedPoolKBResult;

            long nonPagedPoolKBResult;
            if (nonPagedPoolInKB.TryGetValue(regex, out nonPagedPoolKBResult))
                nonPagedPoolKB = nonPagedPoolKBResult;



            double result;
            if (cpu_usages == null)     //first poll not completed yet
            {
                return null;
            }
            else if (cpu_usages.TryGetValue(regex, out result))
            {
                return result * 100.0;
            }
            else if (failIfNoMatchingProcesses)
            {
                notRunning = true;
                return null;
            }
            else
            {
                return null;
            }
        }   //public double? GetCpuUsageForRegex

        // ############################################################################

        public void StartMonitoringThread()
        {
            var thread = new Thread(() => CpuUsageComputerThread());
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        public void CpuUsageComputerThread()
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;


            while (!exitFlag)
            {
                Dictionary<int, TimeSpan> new_cpu_times = new Dictionary<int, TimeSpan>();
                Dictionary<string, double> new_cpu_usages = new Dictionary<string, double>();

                Dictionary<string, bool> new_notRespondingProcesses = new Dictionary<string, bool>();
                Dictionary<string, long> new_memoryCommitsInMB = new Dictionary<string, long>();
                Dictionary<string, long> new_workingSetInMB = new Dictionary<string, long>();
                Dictionary<string, long> new_gdiHandlesDict = new Dictionary<string, long>();
                Dictionary<string, long> new_userHandlesDict = new Dictionary<string, long>();
                Dictionary<string, long> new_handlesDict = new Dictionary<string, long>();
                Dictionary<string, long> new_pagedPoolInKB = new Dictionary<string, long>();
                Dictionary<string, long> new_nonPagedPoolInKB = new Dictionary<string, long>();


                Process[] processList;
                if (AllRegexesAlphaNumericOnly)
                {
                    List<Process> selectedProcesses = new List<Process>();

                    foreach (var regex in regex_strings)
                    {
                        var processList1 = Process.GetProcessesByName(regex);   //TODO: use performance counters instead.   //TODO: actually looking at Reflector it is visible that this method is not faster than the GetProcesses()
                        selectedProcesses.AddRange(processList1);
                    }

                    processList = selectedProcesses.ToArray();
                }
                else    //if (AllRegexesAlphaNumericOnly)
                {
                    processList = Process.GetProcesses();   //NB! it would be more correct to take process list inside the lock but the difference is so small that it is better to postpone taking the lock
                }


                Monitor.Enter(measurementTimeDiff_lock);    //We need this lock in order to prevent race condition resetting the updatedEvent immediately AFTER it has been set by monitoring thread.

                long currentTime;
                TimeSpan measurementTimeDiff = GetMeasurementTimeDiff(out currentTime);



                foreach (var process in processList)
                {
                    foreach (var regex_kvp in regexes_compiled)
                    {
                        if (regex_kvp.Value.IsMatch(process.ProcessName))
                        {
                            string regex_str = regex_kvp.Key;



                            TimeSpan newTotalCPUTime = new TimeSpan(0);
                            bool notResponding = false;
                            long memoryUsage;
                            long workingSet;    //TODO
                            long gdiHandles;
                            long userHandles;
                            long handles;
                            long pagedPool;    //TODO
                            long nonPagedPool;    //TODO
                            try
                            {
                                notResponding = !process.Responding;
                                //memoryUsage = process.VirtualMemorySize64;    //Virtual Bytes is the current size, in bytes, of the virtual address space the process is using. Use of virtual address space does not necessarily imply corresponding use of either disk or main memory pages. Virtual space is finite, and the process can limit its ability to load libraries.
                                memoryUsage = process.PrivateMemorySize64  //Task Manager Vista: Commit Size - Private Bytes is the current size, in bytes, of memory that this process has allocated that cannot be shared with other processes.
                                                    + process.NonpagedSystemMemorySize64  //Task Manager: Paged Pool  //usuall small
                                                    + process.PagedSystemMemorySize64   //Task Manager: NonPaged Pool usually small
                                                    ;
                                workingSet = process.WorkingSet64;
                                gdiHandles = NativeMethods.GdiHandleCount(process);
                                userHandles = NativeMethods.UserHandleCount(process);
                                handles = process.HandleCount;
                                pagedPool = process.PagedSystemMemorySize64;
                                nonPagedPool = process.NonpagedSystemMemorySize64;
                                newTotalCPUTime = process.TotalProcessorTime;
                            }
#pragma warning disable CS0168  //warning CS0168: The variable 'ex' is declared but never used
                            catch (InvalidOperationException ex)    //the process has exited
#pragma warning restore CS0168
                            {
                                //continue;
                                goto nextProcess;
                            }
                            catch (Win32Exception ex)
                            {
                                if (ex.NativeErrorCode == 5)    //access denied
                                {
                                    Console.WriteLine("PID: {0,6} access denied", process.Id);
                                    //continue;
                                    goto nextProcess;
                                }
                                else
                                {
                                    throw ex;
                                }
                            }



                            if (notResponding)
                                //new_notRespondingProcesses.Add(regex_str, true);
                                new_notRespondingProcesses[regex_str] = true;   //NB! we cannot add since the regex may match multiple processes which both do not respond, and then there is already the entry in this dictionary



                            //long memoryUsageMB = (memoryUsage + 512 * 1024) / 1024 / 1024;   //+512 * 1024 for rounding
                            long memoryUsageMB = memoryUsage / 1024 / 1024;   //NB! we dont round
                            if (new_memoryCommitsInMB.ContainsKey(regex_str))
                            {
                                new_memoryCommitsInMB[regex_str] = Math.Max(new_memoryCommitsInMB[regex_str], memoryUsageMB);     //if one process in the matching group exceeds the limit then all should be considered as exceeding the limit
                            }
                            else
                            {
                                new_memoryCommitsInMB.Add(regex_str, memoryUsageMB);
                            }


                            //long memoryUsageMB = (memoryUsage + 512 * 1024) / 1024 / 1024;   //+512 * 1024 for rounding
                            long workingSetMB = workingSet / 1024 / 1024;   //NB! we dont round
                            if (new_workingSetInMB.ContainsKey(regex_str))
                            {
                                new_workingSetInMB[regex_str] = Math.Max(new_workingSetInMB[regex_str], workingSetMB);     //if one process in the matching group exceeds the limit then all should be considered as exceeding the limit
                            }
                            else
                            {
                                new_workingSetInMB.Add(regex_str, workingSetMB);
                            }


                            if (new_gdiHandlesDict.ContainsKey(regex_str))
                            {
                                new_gdiHandlesDict[regex_str] = Math.Max(new_gdiHandlesDict[regex_str], gdiHandles);     //if one process in the matching group exceeds the limit then all should be considered as exceeding the limit
                            }
                            else
                            {
                                new_gdiHandlesDict.Add(regex_str, gdiHandles);
                            }


                            if (new_userHandlesDict.ContainsKey(regex_str))
                            {
                                new_userHandlesDict[regex_str] = Math.Max(new_userHandlesDict[regex_str], userHandles);     //if one process in the matching group exceeds the limit then all should be considered as exceeding the limit
                            }
                            else
                            {
                                new_userHandlesDict.Add(regex_str, userHandles);
                            }


                            if (new_handlesDict.ContainsKey(regex_str))
                            {
                                new_handlesDict[regex_str] = Math.Max(new_handlesDict[regex_str], handles);     //if one process in the matching group exceeds the limit then all should be considered as exceeding the limit
                            }
                            else
                            {
                                new_handlesDict.Add(regex_str, handles);
                            }


                            //long pagedPoolKB = (pagedPool + 512) / 1024;   //+512 * 1024 for rounding
                            long pagedPoolKB = pagedPool / 1024;   //NB! we dont round
                            if (new_pagedPoolInKB.ContainsKey(regex_str))
                            {
                                new_pagedPoolInKB[regex_str] = Math.Max(new_pagedPoolInKB[regex_str], pagedPoolKB);     //if one process in the matching group exceeds the limit then all should be considered as exceeding the limit
                            }
                            else
                            {
                                new_pagedPoolInKB.Add(regex_str, pagedPoolKB);
                            }


                            //long nonPagedPoolKB = (pagedPool + 512) / 1024;   //+512 * 1024 for rounding
                            long nonPagedPoolKB = nonPagedPool / 1024;   //NB! we dont round
                            if (new_nonPagedPoolInKB.ContainsKey(regex_str))
                            {
                                new_nonPagedPoolInKB[regex_str] = Math.Max(new_nonPagedPoolInKB[regex_str], nonPagedPoolKB);     //if one process in the matching group exceeds the limit then all should be considered as exceeding the limit
                            }
                            else
                            {
                                new_nonPagedPoolInKB.Add(regex_str, nonPagedPoolKB);
                            }



                            new_cpu_times.Add(process.Id, newTotalCPUTime);

                            TimeSpan prev_cpu_time;
                            if (prev_cpu_times.TryGetValue(process.Id, out prev_cpu_time))
                            {
                                TimeSpan diff = newTotalCPUTime - prev_cpu_time;

                                double cpu_usage_percent = (double)diff.Ticks / (double)measurementTimeDiff.Ticks;

                                if (new_cpu_usages.ContainsKey(regex_str))
                                {
                                    new_cpu_usages[regex_str] = Math.Max(new_cpu_usages[regex_str], cpu_usage_percent);     //if one process in the matching group exceeds the limit then all should be considered as exceeding the limit
                                }
                                else
                                {
                                    new_cpu_usages.Add(regex_str, cpu_usage_percent);
                                }
                            }
                            

                        }   //if (regex.Matches(process.ProcessName))
                    }   //foreach (var regex in regexes_compiled)

                nextProcess:;

                }   //foreach (var process in processList)


                prev_cpu_times = new_cpu_times;
                if (!firstLoop)
                    cpu_usages = new_cpu_usages;
                else
                    firstLoop = false;
                prev_stopWatch_time = new ObjRef<long>(currentTime);

                notRespondingProcesses = new_notRespondingProcesses;    //NB! inside lock to synchronize update timing with measurements
                memoryCommitsInMB = new_memoryCommitsInMB;
                workingSetInMB = new_workingSetInMB;
                gdiHandlesDict = new_gdiHandlesDict;
                userHandlesDict = new_userHandlesDict;
                handlesDict = new_handlesDict;
                pagedPoolInKB = new_pagedPoolInKB;
                nonPagedPoolInKB = new_nonPagedPoolInKB;


                
                Monitor.Exit(measurementTimeDiff_lock);
                
                updatedEvent.Set();



                //sleep longer when all checks are "passed"

                currSleepMs = FailedCheckersCount > 0 ? FailedCheckIntervalMs : PassedCheckIntervalMs;

                int sleepStep = 1000;
                for (int i = 0; i < currSleepMs; i += sleepStep)
                {
                    currSleepMs = FailedCheckersCount > 0 ? FailedCheckIntervalMs : PassedCheckIntervalMs;

                    if (!exitFlag)
                    {
                        Thread.Sleep(Math.Min(sleepStep, currSleepMs - i));

                        currSleepMs = FailedCheckersCount > 0 ? FailedCheckIntervalMs : PassedCheckIntervalMs;
                    }
                }

            }   //while (!exitFlag)
        }   //public static void CpuUsageComputerThread(int sleepMs)

        // ############################################################################

        /// <summary>
        /// Time since previous measurement
        /// </summary>
        private TimeSpan GetMeasurementTimeDiff()
        {
            long currentTime;
            return GetMeasurementTimeDiff(out currentTime);
        }

        /// <summary>
        /// Time since previous measurement
        /// </summary>
        private TimeSpan GetMeasurementTimeDiff(out long currentTime)
        {
            var prev_stopWatch_time_local = prev_stopWatch_time;

            currentTime = Stopwatch.GetTimestamp();
            long measurementTimeDiffTicks;
            if (prev_stopWatch_time_local != null)
                measurementTimeDiffTicks = unchecked(currentTime - prev_stopWatch_time_local.Value);
            else
                measurementTimeDiffTicks = 1;

            TimeSpan measurementTimeDiff = TimeSpan.FromSeconds((double)measurementTimeDiffTicks / (double)Stopwatch.Frequency);    //NB! verified in Reflector that this method does proper rounding

            return measurementTimeDiff;
        }
    }   //public class CpuUsageComputer


}
