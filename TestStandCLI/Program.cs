using System;

using NationalInstruments.TestStand.Utility;
using NationalInstruments.TestStand.Interop.API;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.IO;

namespace TestStandCLI
{
    internal class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            WriteMessageWithColorAccent("[Starting TestStand CLI...]\n", ConsoleColor.DarkYellow);

            arguments = new CommandLineArguments(args);
            switch (arguments.Status)
            {
                case CommandLineArguments.ValidationStatus.Help:
                    arguments.DisplayHelp();
                    return (int)ReturnCode.Normal;
                case CommandLineArguments.ValidationStatus.Execute:
                    // Create application domain, call MainEntryPoint, and cleanup before return.
                    returnCode = LaunchTestStandApplicationInNewDomain.
                        LaunchProtectedReturnCode(
                        new LaunchTestStandApplicationInNewDomain.
                        MainEntryPointDelegateWithArgsReturnCode(MainEntryPoint),
                        new string[] { String.Format($"/env \"{arguments.EnvironmentFilePath}\"") },
                        "TestStand CLI",
                        new LaunchTestStandApplicationInNewDomain.
                        DisplayErrorMessageDelegate(DisplayErrorMessage),
                        true);
                    if (returnCode == -1000 ^ returnCode <= -17000) return (int)TSError.TS_Err_ProgramError;
                    return returnCode;
                default:
                    WriteMessageWithColorAccent("[Command Line Error:] ", ConsoleColor.Red, false);
                    arguments.DisplayError();
                    return (int)ReturnCode.CommandLineError;
            }
        }

        // Main entry point executed in new application domain.
        private static int MainEntryPoint(string[] args)
        {
            string[] status = new string[] { "Passed", "Failed", "Terminated", "Aborted" };
            SequenceFile sequenceFile = null;
            try
            {
                // Obtain a reference to existing Engine.
                engine = new Engine();
                engine.LoadTypePaletteFilesEx(
                    TypeConflictHandlerTypes.ConflictHandler_Error);

                sequenceFile = engine.GetSequenceFileEx(
                    arguments.SequenceFilePath,
                    GetSeqFileOptions.GetSeqFile_DoNotRunLoadCallback,
                    TypeConflictHandlerTypes.ConflictHandler_Error);

                string sequenceName = GetSequenceNameIfEmpty(sequenceFile, arguments.Sequence);

                Console.WriteLine("{0,-25}{1}", "TestStand environment:", engine.GetEnvironmentPath());
                Console.WriteLine("{0,-25}{1}", "Sequence file path:", arguments.SequenceFilePath);
                Console.WriteLine("{0,-25}{1}", "Sequence name:", sequenceName);

                Execution execution = engine.NewExecution(
                    sequenceFile,
                    sequenceName,
                    null,
                    false,
                    ExecutionTypeMask.ExecTypeMask_CloseWindowWhenDone |
                    ExecutionTypeMask.ExecTypeMask_DiscardArgumentsWhenDone |
                    ExecutionTypeMask.ExecTypeMask_InitiallyHidden |
                    ExecutionTypeMask.ExecTypeMask_InitiallySuspended |
                    ExecutionTypeMask.ExecTypeMask_NotRestartable |
                    ExecutionTypeMask.ExecTypeMask_TracingInitiallyOff);
                if (arguments.DebugEnabled) Console.WriteLine("Execution Started - ID: " + execution.Id);

                WriteMessageWithColorAccent("[Executing...]", ConsoleColor.DarkYellow);
                execution.RunTimeVariables.SetValString("TestStandCLI_Args", PropertyOptions.PropOption_InsertIfMissing, args.GetValue(2).ToString());
                execution.Resume();

                // TODO: Add Output Messages handling.
                engine.UIMessagePollingEnabled = true;
                engine.OutputMessagesEnabled = true;

                bool stop = false;
                while (!stop)
                {
                    if (!engine.IsUIMessageQueueEmpty)
                    {
                        UIMessage msg = engine.GetUIMessage();
                        switch (msg.Event)
                        {
                            case UIMessageCodes.UIMsg_StartFileExecution:
                                if (arguments.DebugEnabled) Console.WriteLine("   Event: " + msg.Event + " (Path: " + (msg.ActiveXData as SequenceFile).Path + ")");
                                break;
                            case UIMessageCodes.UIMsg_StartExecution:
                                if (arguments.DebugEnabled) Console.WriteLine("   Event: " + msg.Event + " (ID: " + msg.Execution.Id + ")");
                                break;
                            case UIMessageCodes.UIMsg_EndFileExecution:
                                if (arguments.DebugEnabled) Console.WriteLine("   Event: " + msg.Event + " (Path:" + (msg.ActiveXData as SequenceFile).Path + ")");
                                break;
                            case UIMessageCodes.UIMsg_EndExecution:
                                if (arguments.DebugEnabled) Console.WriteLine("   Event: " + msg.Event + " (ID: " + msg.Execution.Id + ")");
                                if (msg.Execution.Id == execution.Id) stop = true;
                                break;
                            case UIMessageCodes.UIMsg_AbortingExecution:
                                if (arguments.DebugEnabled) Console.WriteLine("   Event: " + msg.Event);
                                returnCode = (int)ReturnCode.Aborted;
                                stop = true;
                                break;
                            case UIMessageCodes.UIMsg_KillingExecutionThreads:
                                if (arguments.DebugEnabled) Console.WriteLine("   Event: " + msg.Event);
                                returnCode = (int)ReturnCode.Aborted;
                                stop = true;
                                break;
                            case UIMessageCodes.UIMsg_RuntimeError:
                                if (arguments.DebugEnabled) Console.WriteLine("   Event: " + msg.Event);
                                execution.Terminate();
                                break;
                            case UIMessageCodes.UIMsg_OutputMessages:
                                // Access OutputMessages
                                OutputMessages outputMessages = msg.ActiveXData as OutputMessages;
                                // Copy OutputMessages for private use.
                                OutputMessages tmpOutputMessages = engine.NewOutputMessages();
                                outputMessages.CopyMessagesToCollection(tmpOutputMessages);
                                for(int i = 0; i < tmpOutputMessages.Count; i++)
                                {
                                    //Output Message: <message> <time stamp> <category> <severity> <step> <sequence> <sequence file> <execution> <thread id>
                                    Console.Write("Output Message:\t" + tmpOutputMessages[i].Message + "\t");
                                    Console.Write(tmpOutputMessages[i].TimeStamp.ToString() + "\t");
                                    Console.Write(tmpOutputMessages[i].Category + "\t");
                                    Console.Write(tmpOutputMessages[i].Severity.ToString() + "\t");
                                    //Console.Write(tmpOutputMessages[i].FileLocations + "\t");
                                    //Console.Write(tmpOutputMessages[i].ExecutionLocations + "\t");
                                    tmpOutputMessages[i].TextColor.
                                }
                                break;
                            default:
                                if (arguments.DebugEnabled) Console.WriteLine("   Event: " + msg.Event);
                                break;
                        }
                        if (msg.IsSynchronous) msg.Acknowledge();
                    }
                }
                engine.UIMessagePollingEnabled = false;

                if (arguments.DebugEnabled && execution.ErrorObject.GetValBoolean("Occurred", PropertyOptions.PropOption_NoOptions))
                    Console.WriteLine("Runtime Error:\n   Code: " + execution.ErrorObject.GetValNumber("Code", PropertyOptions.PropOption_NoOptions) +
                        "\n   Message: " + execution.ErrorObject.GetValString("Msg", PropertyOptions.PropOption_NoOptions));
                if (arguments.DebugEnabled) Console.WriteLine("Execution Ended");

                WriteMessageWithColorAccent("Sequence status:\t[" + execution.ResultStatus + "]", (execution.ResultStatus == "Passed") ? ConsoleColor.Green : ConsoleColor.Red);

                if (returnCode != 0) returnCode = Array.IndexOf(status, execution.ResultStatus);
                if (returnCode < 0) returnCode = (int)TSError.TS_Err_NotSupported;
            }
            catch (COMException exception)
            {
                Console.Error.WriteLine("Error: " + exception.ErrorCode + "\n" + exception.Message);
                string tmpMessage = "";
                bool found = engine.GetErrorString((TSError)exception.ErrorCode, out tmpMessage);
                if (arguments.DebugEnabled && found) Console.Error.WriteLine("Exception: " + tmpMessage);
                returnCode = exception.ErrorCode;
            }
            finally
            {
                engine.UIMessagePollingEnabled = false;
                if (sequenceFile != null)
                {
                    engine.ReleaseSequenceFileEx(
                        sequenceFile,
                        ReleaseSeqFileOptions.ReleaseSeqFile_DoNotRunUnloadCallback |
                        ReleaseSeqFileOptions.ReleaseSeqFile_UnloadFile);
                }
                engine.UnloadTypePaletteFiles();
                engine = null;
            }
            return returnCode;
        }

        // Called if exception occurs in MainEntryPoint.
        private static void DisplayErrorMessage(string caption, string message)
        {
            WriteMessageWithColorAccent("[Main Entry Point Error:] ", ConsoleColor.Red, false);
            Console.WriteLine(caption + "\n\t" + message);
        }

        private static string GetSequenceNameIfEmpty(SequenceFile sequenceFile, string sequenceName)
        {
            if (sequenceName != "")
                return sequenceName;
            else if (sequenceFile.SequenceNameExists("MainSequence"))
                return "MainSequence";
            else
                return sequenceFile.GetSequence(0).Name;
        }

        // Writes a message to the standard output stream.
        // Allows to change the color of individual characters.
        // Color characters are marked with "[" and "]".
        private static void WriteMessageWithColorAccent(string message, ConsoleColor color, bool newline = true)
        {
            var tokens = Regex.Split(message, @"(\[[^\]]*\])");

            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];

                if (token.StartsWith("[") && token.EndsWith("]"))
                {
                    Console.ForegroundColor = color;
                    token = token.Substring(1, token.Length - 2);
                }

                Console.Write(token);
                Console.ResetColor();
            }
            if (newline) Console.WriteLine();
        }

        private static Engine engine;
        private static CommandLineArguments arguments;
        private enum ReturnCode
        {
            CommandLineError = -1,
            Normal = 0,
            Failed = 1,
            Terminated = 2,
            Aborted = 3,
            RunTimeError = 4
        }
        private static int returnCode = 0;
    }
}
