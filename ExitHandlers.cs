
#region Copyright (c) 2010 - 2014, Roland Pihlakas
/////////////////////////////////////////////////////////////////////////////////////////
// 
// Copyright (c) 2010 - 2014, Roland Pihlakas.
// 
// Permission to copy, use, modify, sell and distribute this software
// is granted provided this copyright notice appears in all copies.
// 
/////////////////////////////////////////////////////////////////////////////////////////
#endregion Copyright (c) 2010 - 2014, Roland Pihlakas


using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Security;
#if NET4
using System.Runtime.ExceptionServices;
#endif
using System.IO;

namespace CPUUsageMonitor
{
    [SuppressUnmanagedCodeSecurity]     //SuppressUnmanagedCodeSecurity - For methods in this particular class, execution time is often critical. Security can be traded for additional speed by applying the SuppressUnmanagedCodeSecurity attribute to the method declaration. This will prevent the runtime from doing a security stack walk at runtime. - MSDN: Generally, whenever managed code calls into unmanaged code (by PInvoke or COM interop into native code), there is a demand for the UnmanagedCode permission to ensure all callers have the necessary permission to allow this. By applying this explicit attribute, developers can suppress the demand at run time. The developer must take responsibility for assuring that the transition into unmanaged code is sufficiently protected by other means. The demand for the UnmanagedCode permission will still occur at link time. For example, if function A calls function B and function B is marked with SuppressUnmanagedCodeSecurityAttribute, function A will be checked for unmanaged code permission during just-in-time compilation, but not subsequently during run time.
    public static class ExitHandler
    {
        public delegate void Action();  //.net 3.0 does not contain Action delegate declaration

        private static readonly object exceptionLoggerLock = new object();

        private static volatile bool UnhandledExceptionHandler_inited = false;

        private static readonly object UnhandledExceptionHandler_init_lock = new object();

        /// <returns>True if the init was just performed, False if the handler was already inited.</returns>
        public static bool InitUnhandledExceptionHandler()
        {
            return InitUnhandledExceptionHandler(/*Helpers.IsConsoleWindowForReal*/false);
        }

        /// <param name="showDialog">Note that once the handler is inited the effect of this flag cannot be changed by calling this method again.</param>
        /// <returns>True if the init was just performed, False if the handler was already inited.</returns>
        public static bool InitUnhandledExceptionHandler(bool showDialog)
        {
            if (!UnhandledExceptionHandler_inited)
            {
                lock (UnhandledExceptionHandler_init_lock)
                {
                    if (!UnhandledExceptionHandler_inited)
                    {
                        {
                            // Add the event handler for handling UI thread exceptions to the event.
                            Application.ThreadException += new ThreadExceptionEventHandler(
                                                                    (sender, t) => ThreadException(sender, t, showDialog));

                            // Set the unhandled exception mode to force all Windows Forms errors to go through
                            // our handler.
                            //Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.Automatic);

                            // Add the event handler for handling non-UI thread exceptions to the event. 
                            AppDomain.CurrentDomain.UnhandledException +=
                                new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);   //TODO!!! Starting with the .NET Framework version 4, this event is not raised for exceptions that corrupt the state of the process, such as stack overflows or access violations, unless the event handler is security-critical and has the HandleProcessCorruptedStateExceptionsAttribute attribute.
                        }

                        UnhandledExceptionHandler_inited = true;
                        return true;
                    }
                }
            }

            return false;

        }   //public static void InitUnhandledExceptionHandler()

        //################################################################

        // Handle the UI exceptions by showing a dialog box, and asking the user whether
        // or not they wish to abort execution.
#if NET4        
        [HandleProcessCorruptedStateExceptions]     //TODO: is this useful here?
#endif
        [SecurityCritical]
        /*
            http://msdn.microsoft.com/en-us/library/system.runtime.exceptionservices.handleprocesscorruptedstateexceptionsattribute.aspx :
            The CLR delivers the corrupted process state exception to applicable exception clauses only in methods that have both the HandleProcessCorruptedStateExceptionsAttribute and SecurityCriticalAttribute attributes. 
            You can also add the <legacyCorruptedStateExceptionsPolicy> element to your application's configuration file. This will ensure that corrupted state exceptions are delivered to your exception handlers without the HandleProcessCorruptedStateExceptionsAttribute or SecurityCriticalAttribute attribute
        */
        private static void ThreadException(object sender, ThreadExceptionEventArgs t, bool showDialog)
        {
            lock (exceptionLoggerLock)
            {
                try
                {
                    Exception ex = (Exception)t.Exception;
                    string errorMsg = DateTime.Now + " Unhandled thread exception: " + Environment.NewLine;

                    // Since we can't prevent the app from terminating, log this to the event log.
                    errorMsg += ex.GetType() + Environment.NewLine +
                        ex.Message + Environment.NewLine +
                        "Stack Trace:" + Environment.NewLine +
                        ex.StackTrace + Environment.NewLine;

                    while (ex.InnerException != null)
                    {
                        errorMsg += Environment.NewLine;
                        errorMsg += "Inner exception: " + ex.GetType() + ": " + ex.InnerException.Message + Environment.NewLine;
                        errorMsg += "Inner exception stacktrace: " + Environment.NewLine + ex.InnerException.StackTrace + Environment.NewLine;

                        ex = ex.InnerException;     //loop
                    }

                    errorMsg += Environment.NewLine;

                    File.AppendAllText("UnhandledExceptions.log", errorMsg);


                    if (showDialog)
                    {
                        DialogResult result = DialogResult.OK;
                        result = MessageBox.Show
                        (
                            errorMsg,
                            "Thread Exception",
                            MessageBoxButtons.OKCancel,
                            MessageBoxIcon.Stop
                        );

                        // Exits the program when the user clicks Cancel.
                        if (result == DialogResult.Cancel)
                            Application.Exit();
                    }   //if (showDialog)

                }
                catch (Exception exc2)
                {
                    try
                    {
                        string errorMsg = DateTime.Now + " Unhandled exception in thread exception handler: " + Environment.NewLine;

                        errorMsg += exc2.GetType() + Environment.NewLine +
                            exc2.Message + Environment.NewLine +
                            "Stack Trace:" + Environment.NewLine +
                            exc2.StackTrace + Environment.NewLine;

                        while (exc2.InnerException != null)
                        {
                            errorMsg += Environment.NewLine;
                            errorMsg += "Inner exception: " + exc2.GetType() + ": " + exc2.InnerException.Message + Environment.NewLine;
                            errorMsg += "Inner exception stacktrace: " + Environment.NewLine + exc2.InnerException.StackTrace + Environment.NewLine;

                            exc2 = exc2.InnerException;     //loop
                        }

                        errorMsg += Environment.NewLine;

                        File.AppendAllText("UnhandledExceptions.log", errorMsg);


                        if (showDialog)
                        {
                            MessageBox.Show
                            (
                                errorMsg,
                                "Fatal Thread Exception",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Stop
                            );
                        }
                    }
#pragma warning disable 0168    //warning CS0168: The variable 'ex' is declared but never used
                    catch (Exception exc3)
#pragma warning restore 0168    //warning CS0168: The variable 'ex' is declared but never used
                    {

                    }
                }
                finally
                {
                    if (!showDialog)
                        Application.Exit();
                }

            }   //lock (exceptionLoggerLock)

        }   //internal static void ThreadException(object sender, ThreadExceptionEventArgs t)

        //################################################################

        /// NOTE: This exception cannot be kept from terminating the application - it can only 
        /// log the event, and inform the user about it. 
#if NET4        
        [HandleProcessCorruptedStateExceptions]
#endif
        [SecurityCritical]
        /*
            http://msdn.microsoft.com/en-us/library/system.runtime.exceptionservices.handleprocesscorruptedstateexceptionsattribute.aspx :
            The CLR delivers the corrupted process state exception to applicable exception clauses only in methods that have both the HandleProcessCorruptedStateExceptionsAttribute and SecurityCriticalAttribute attributes. 
            You can also add the <legacyCorruptedStateExceptionsPolicy> element to your application's configuration file. This will ensure that corrupted state exceptions are delivered to your exception handlers without the HandleProcessCorruptedStateExceptionsAttribute or SecurityCriticalAttribute attribute
        */
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            lock (exceptionLoggerLock)
            {
                try
                {
                    Exception ex = (Exception)e.ExceptionObject;
                    string errorMsg = DateTime.Now + " Unhandled domain exception: " + Environment.NewLine;

                    // Since we can't prevent the app from terminating, log this to the event log.
                    errorMsg += ex.GetType() + Environment.NewLine +
                        ex.Message + Environment.NewLine +
                        "Stack Trace:" + Environment.NewLine +
                        ex.StackTrace + Environment.NewLine;

                    while (ex.InnerException != null)
                    {
                        errorMsg += Environment.NewLine;
                        errorMsg += "Inner exception: " + ex.GetType() + ": " + ex.InnerException.Message + Environment.NewLine;
                        errorMsg += "Inner exception stacktrace: " + Environment.NewLine + ex.InnerException.StackTrace + Environment.NewLine;

                        ex = ex.InnerException;     //loop
                    }

                    errorMsg += Environment.NewLine;

                    File.AppendAllText("UnhandledExceptions.log", errorMsg);
                }
                catch (Exception exc2)
                {
                    try
                    {
                        string errorMsg = DateTime.Now + " Unhandled exception in domain exception handler: " + Environment.NewLine;

                        errorMsg += exc2.GetType() + Environment.NewLine +
                            exc2.Message + Environment.NewLine +
                            "Stack Trace:" + Environment.NewLine +
                            exc2.StackTrace + Environment.NewLine;

                        while (exc2.InnerException != null)
                        {
                            errorMsg += Environment.NewLine;
                            errorMsg += "Inner exception: " + exc2.GetType() + ": " + exc2.InnerException.Message + Environment.NewLine;
                            errorMsg += "Inner exception stacktrace: " + Environment.NewLine + exc2.InnerException.StackTrace + Environment.NewLine;

                            exc2 = exc2.InnerException;     //loop
                        }

                        errorMsg += Environment.NewLine;

                        File.AppendAllText("UnhandledExceptions.log", errorMsg);
                    }
#pragma warning disable 0168    //warning CS0168: The variable 'ex' is declared but never used
                    catch (Exception exc3)
#pragma warning restore 0168    //warning CS0168: The variable 'ex' is declared but never used
                    {

                    }
                }
                finally
                {
                    Application.Exit();
                }
            }   //lock (exceptionLoggerLock)
        }   //private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)

        //################################################################
        //
        //################################################################

        public static void TriggerExit()
        {
            TerminationFlag = true;
            TriggerExitEvent();
        }

        public static void DoExit()
        {
            if (!_terminationFlag)  //exit is faster when we do not call it again during exit
            {
                //see http://stackoverflow.com/questions/554408/why-would-application-exit-fail-to-work

                if (Application.MessageLoop)
                {
                    // Use this since we are a WinForms app 
                    Application.Exit();
                }
                else
                {
                    // Use this since we are a console app 
                    Environment.Exit(1);
                }
            }
        }

        private static volatile bool _terminationFlag;
        public static bool TerminationFlag
        {
            get { return _terminationFlag; }
            internal set { _terminationFlag = value; }
        }

        private volatile static int exiting = 0;

        public delegate void ExitEventHandler(bool hasShutDownStarted);
        public static event ExitEventHandler ExitEvent;
        public static event ExitEventHandler ExitEventOnce;

        //see ms-help://MS.VSCC.v90/MS.MSDNQTR.v90.en/dllproc/base/handlerroutine.htm and http://stackoverflow.com/questions/474679/capture-console-exit-c

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlEventHandler handler, bool add);

        private delegate bool ConsoleCtrlEventHandler(CtrlType sig);
        private static ConsoleCtrlEventHandler _ConsoleCtrlHandler;  //For example, if there is only one of this callback in your entire program, then putting it in a static field will keep the GC's hands off of it - http://go4answers.webhost4life.com/Example/gc-eating-callback-169278.aspx

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static void TriggerExitEvent()
        {
            if (ExitEvent != null)
                ExitEvent(Environment.HasShutdownStarted);

#pragma warning disable 0420    //warning CS0420: 'Algorithms_and_Structures.ExitHandler.exiting': a reference to a volatile field will not be treated as volatile
            if (Interlocked.CompareExchange(ref exiting, 1, 0) == 0)
#pragma warning restore 0420
            {
                if (ExitEventOnce != null)
                    ExitEventOnce(Environment.HasShutdownStarted);
            }
        }

        private static bool ConsoleCtrlHandler(CtrlType sig)
        {
            switch (sig)
            {
                case CtrlType.CTRL_C_EVENT:
                case CtrlType.CTRL_LOGOFF_EVENT:
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                case CtrlType.CTRL_CLOSE_EVENT:
                default:
                    TerminationFlag = true;
                    TriggerExitEvent();
                    break;
            }

            /*
             * Return FALSE. If none of the registered handler functions returns TRUE, the default handler terminates the process. 
             * Return TRUE. In this case, no other handler functions are called, and the system displays a pop-up dialog box that asks the user whether 
             * to terminate the process. The system also displays this dialog box if the process does not respond within a certain time-out period 
             * (5 seconds for CTRL_CLOSE_EVENT, and 20 seconds for CTRL_LOGOFF_EVENT or CTRL_SHUTDOWN_EVENT). 
            */

            //#warning TODO!!!: sleep longer but abort sleep when process has completed, using WaitOne(timeout)

            //Thread.Sleep(20);    //give the process time to terminate properly
            Thread.Sleep(2500);    //give the process time to terminate properly

            return false;   //If the function handles the control signal, it should return TRUE. If it returns FALSE, the next handler function in the list of handlers for this process is used.
        }

        //################################################################

        private static bool HookingDone = false;
        private static readonly object hooking_lock = new object();

        public static void HookSessionEnding()
        {
            HookSessionEnding(null, true);
        }

        public static void HookSessionEnding(Action additionalHooking)
        {
            HookSessionEnding(additionalHooking, true);
        }

        public static void HookSessionEnding(bool startMessageLoop)
        {
            HookSessionEnding(null, startMessageLoop);
        }

        public static void HookSessionEnding(Action additionalHooking, bool startMessageLoop)
        {
            if (!HookingDone)
            {
                lock (hooking_lock)     //hook only once
                {
                    if (!HookingDone)   //double-checked lock
                    {
#if NET4 || USE_REACTIVE_EXTENSIONS
                        ManualResetEventSlim hookingDone = startMessageLoop ? new ManualResetEventSlim(false) : null;
#else
                        ManualResetEvent hookingDone = startMessageLoop ? new ManualResetEvent(false) : null;
#endif

                        Action hookingCode = () =>
                        {
                            HookSessionEnding_WorkerThread();
                            if (additionalHooking != null)
                                additionalHooking();

                            if (startMessageLoop)
                            {
                                hookingDone.Set();

                                Application.Run();  //start message loop

                                while (!TerminationFlag)
                                {
                                    while (!TerminationFlag/* && !Do_ReHookConsoleCtrlHandler*/)
                                        Thread.Sleep(100);  //keep thread alive to process events here in this thread
                                }
                            }   //if (startMessageLoop)
                        };  //Action hookingCode = () =>

                        if (startMessageLoop)
                        {
                            var thread = new Thread(() => hookingCode());     //var thread = new Thread(() =>
                            thread.SetApartmentState(ApartmentState.STA);     //When SystemEvents is initialized in a user-interactive application, if the thread of the application causing the SystemEvents initialization is marked as STA (single-threaded apartment, the default in a Windows Forms application), SystemEvents expects that this thread will have a message loop on which SystemEvents can piggyback. (Thus, if you were to erroneously mark a console application as STA and didn’t manually pump messages, none of the message-based events on SystemEvents would be raised.) If, however, the initializing thread is MTA (multithreaded apartment, the default in a console application in C#), SystemEvents will spawn its own thread. That thread will execute a message loop. - http://msdn.microsoft.com/en-us/magazine/cc163417.aspx
                            thread.IsBackground = true;
                            thread.Start();
                            hookingDone.WaitOne();
#if NET4 || USE_REACTIVE_EXTENSIONS
                            hookingDone.Dispose();
#else
                            hookingDone.Close();
#endif
                        }   //if (startMessageLoop)

                    }   //if (!HookingDone)
                }   //lock (hooking_lock)
            }   //if (!HookingDone)

        }   //public static void HookSessionEnding(Action additionalHooking)

        public static void ReHookConsoleCtrlHandler()
        {

#if NET4 || USE_REACTIVE_EXTENSIONS
            ManualResetEventSlim hookingDone = new ManualResetEventSlim(false);
#else
            ManualResetEvent hookingDone = new ManualResetEvent(false);
#endif

            var thread = new Thread(() =>
            {
                ReHookConsoleCtrlHandler_WorkerThread();
                hookingDone.Set();
                Application.Run();  //start message loop
                while (!TerminationFlag)
                    Thread.Sleep(100);  //keep thread alive to process events here in this thread
            });
            thread.SetApartmentState(ApartmentState.STA);     //When SystemEvents is initialized in a user-interactive application, if the thread of the application causing the SystemEvents initialization is marked as STA (single-threaded apartment, the default in a Windows Forms application), SystemEvents expects that this thread will have a message loop on which SystemEvents can piggyback. (Thus, if you were to erroneously mark a console application as STA and didn’t manually pump messages, none of the message-based events on SystemEvents would be raised.) If, however, the initializing thread is MTA (multithreaded apartment, the default in a console application in C#), SystemEvents will spawn its own thread. That thread will execute a message loop. - http://msdn.microsoft.com/en-us/magazine/cc163417.aspx
            thread.IsBackground = true;
            thread.Start();
            hookingDone.WaitOne();

#if NET4 || USE_REACTIVE_EXTENSIONS
            hookingDone.Dispose();
#else
            hookingDone.Close();
#endif
        }

        private static void ReHookConsoleCtrlHandler_WorkerThread()
        {
            SetConsoleCtrlHandler(_ConsoleCtrlHandler, true);
        }

        private static void HookSessionEnding_WorkerThread()
        {



            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
            //Console.TreatControlCAsInput = false;

            //see http://stackoverflow.com/questions/529867/does-application-applicationexit-event-work-to-be-notified-of-exit-in-non-winform
            //NB! TODO: this does not catch windows shutdown
            Application.ThreadExit += new EventHandler(OnAppMainThreadExit);
            Application.ApplicationExit += new EventHandler(OnAppMainThreadExit);
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(OnAppMainThreadExit);
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnAppMainThreadExit);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnAppMainThreadExitUH);



            Microsoft.Win32.SystemEvents.SessionEnding += new Microsoft.Win32.SessionEndingEventHandler(OnSessionEnding);



            // see http://stackoverflow.com/questions/474679/capture-console-exit-c
            // react to close window event
            _ConsoleCtrlHandler += new ConsoleCtrlEventHandler(ConsoleCtrlHandler);
            ReHookConsoleCtrlHandler_WorkerThread();


            Console.CancelKeyPress += new ConsoleCancelEventHandler(ConsoleCancelEventHandler);

        }   //private static void HookSessionEnding()

        //################################################################

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            TerminationFlag = true;
            TriggerExitEvent();
        }

        /// <summary>
        /// Ctrl+C handler
        /// </summary>
        private static void ConsoleCancelEventHandler(object sender, ConsoleCancelEventArgs e)
        {
            TerminationFlag = true;
            TriggerExitEvent();
        }

        //################################################################

        private static void OnAppMainThreadExit(object sender, EventArgs e)
        {
            TerminationFlag = true;
            TriggerExitEvent();
        }

        private static void OnAppMainThreadExitUH(object sender, UnhandledExceptionEventArgs e)
        {
            TerminationFlag = true;
            TriggerExitEvent();
        }

        private static void OnSessionEnding(object sender, Microsoft.Win32.SessionEndingEventArgs e)
        {
            // the user session is ending

            TerminationFlag = true;
            TriggerExitEvent();
        }

        //################################################################

    }   //public static class ExitHandler
}
