using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NationalInstruments.TestStand.Utility;
using NationalInstruments.TestStand.Interop.API;

namespace TestStandCLI
{
    internal class Program
    {
        private static Engine engine = null;
        private static bool debug = true;

        [STAThread]
        static void Main(string[] args)
        {
            // Create application domain, call MainEntryPoint, and
            // cleanup before return.
            if (debug) Console.WriteLine("Starting TestStand engine");
            int returnCode = LaunchTestStandApplicationInNewDomain.LaunchProtectedReturnCode(
               new LaunchTestStandApplicationInNewDomain.
                   MainEntryPointDelegateWithArgsReturnCode(MainEntryPoint),
               args,
               "TestStand CLI",
               new LaunchTestStandApplicationInNewDomain.
                   DisplayErrorMessageDelegate(DisplayErrorMessage),
               true);
            if (debug) Console.WriteLine("Return code: " + returnCode);
        }

        // Main entry point executed in new application domain.
        private static int MainEntryPoint(string[] args)
        {
            // TODO:  Improve args parsing.
            string sequenceName = args.GetValue(0).ToString();
            string sequenceFilePath = args.GetValue(1).ToString();
            // TODO: Add debug flag.

            // Obtain a reference to existing Engine.
            engine = new Engine();
            engine.LoadTypePaletteFilesEx(
                TypeConflictHandlerTypes.ConflictHandler_Error);

            Console.WriteLine("TestStand environment: " + engine.GetEnvironmentPath());
            Console.WriteLine("Sequence file path: " + sequenceFilePath);
            Console.WriteLine("Sequence name: " + sequenceName);

            SequenceFile sequenceFile = engine.GetSequenceFileEx(
                sequenceFilePath,
                GetSeqFileOptions.GetSeqFile_DoNotRunLoadCallback,
                TypeConflictHandlerTypes.ConflictHandler_Error);

            Execution execution = engine.NewExecution(
                sequenceFile,
                sequenceName,
                null,
                false,
                ExecutionTypeMask.ExecTypeMask_CloseWindowWhenDone |
                ExecutionTypeMask.ExecTypeMask_DiscardArgumentsWhenDone |
                ExecutionTypeMask.ExecTypeMask_InitiallyHidden |
                ExecutionTypeMask.ExecTypeMask_NotRestartable |
                ExecutionTypeMask.ExecTypeMask_TracingInitiallyOff);
            if (debug) Console.WriteLine("Execution Started - ID: " + execution.Id);

            engine.UIMessagePollingEnabled = true;
            bool stop = false;
            while (!stop)
            {
                if (!engine.IsUIMessageQueueEmpty)
                {
                    UIMessage msg = engine.GetUIMessage();
                    switch (msg.Event)
                    {
                        case UIMessageCodes.UIMsg_StartFileExecution:
                            if (debug) Console.WriteLine("   Event: " + msg.Event + " (Path: " + (msg.ActiveXData as SequenceFile).Path + ")");
                            break;
                        case UIMessageCodes.UIMsg_StartExecution:
                            if (debug) Console.WriteLine("   Event: " + msg.Event + " (ID: " + msg.Execution.Id + ")");
                            break;
                        case UIMessageCodes.UIMsg_EndFileExecution:
                            if(debug) Console.WriteLine("   Event: " + msg.Event + " (Path:" + (msg.ActiveXData as SequenceFile).Path + ")");
                            break;
                        case UIMessageCodes.UIMsg_EndExecution:
                            if (debug) Console.WriteLine("   Event: " + msg.Event + " (ID: " + msg.Execution.Id + ")");
                            if (msg.Execution.Id == execution.Id) stop = true;
                            break;
                        default:
                            if (debug) Console.WriteLine("   Event: " + msg.Event);
                            break;
                    }
                    if (msg.IsSynchronous) msg.Acknowledge();
                }                
            }
            engine.UIMessagePollingEnabled = false;

            if (debug) Console.WriteLine("Execution Ended");
            if (debug) Console.WriteLine("Sequence status: " + execution.ResultStatus);

            engine.ReleaseSequenceFileEx(
                sequenceFile,
                ReleaseSeqFileOptions.ReleaseSeqFile_DoNotRunUnloadCallback |
                ReleaseSeqFileOptions.ReleaseSeqFile_UnloadFile);
            engine.UnloadTypePaletteFiles();
            engine = null;
            
            return 0;

            //https://zone.ni.com/reference/en-XX/help/370052W-01/tsuiref/reftopics/applicationmgr_exitcode_p/
            //Runtime Error the error code of the run - time error
            //Normal Exit 0
            //Command line Error  1
            //Sequence Failed 2
            //Sequence Terminated 3
            //Sequence Aborted    4
            //Killed Threads  5
            //https://zone.ni.com/reference/en-XX/help/370052AA-01/tsapiref/reftopics/tserror/

        }
        // TODO: Proper error handling


        // Called if exception occurs in MainEntryPoint.
        private static void DisplayErrorMessage(string caption, string message)
        {
            Console.Error.WriteLine("Error: " + caption + "\n" + message);
        }
    }
}
