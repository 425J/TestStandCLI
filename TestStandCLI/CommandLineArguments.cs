using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TestStandCLI
{
    internal class CommandLineArguments
    {
        // ==================================================
        // Methods
        // ==================================================

        public CommandLineArguments(string[] args)
        {
            if (ArgumentsValidator.IsHelp(args)) Status = ValidationStatus.Help;
            else
            {
                ArgumentsValidator.Command command;
                for (int i = 0; i < args.Length; i++)
                {
                    if (!ArgumentsValidator.IsValid(args[i], out command))
                    {
                        HandleInvalidCommandError(args, i);
                        return;
                    }
                    if (ArgumentsValidator.IsAlreadyUsed(command))
                    {
                        HandleDuplicatedCommandError(args, i);
                        return;
                    }
                    switch (command)
                    {
                        case ArgumentsValidator.Command.Run:
                            if (HandleRunCommand(args, ref i)) break;
                            return;
                        case ArgumentsValidator.Command.Debug:
                            HandleDebugCommand();
                            break;
                        case ArgumentsValidator.Command.Args:
                            if (HandleArgsCommand(args, ref i)) break;
                            return;
                        case ArgumentsValidator.Command.Env:
                            if (HandleEnvCommand(args, ref i)) break;
                            return;
                    }
                }
                if (!ArgumentsValidator.ContainsRequired())
                {
                    HandleRequiredCommandMissingError();
                    return;
                }
                Status = ValidationStatus.Execute;
            }
        }

        public void DisplayHelp()
        {
            const string helpText =
@"TestStand Command-Line Interface
Executes TestStand sequence files silently.

Usage:
    TestStandCLI.exe /help
    TestStandCLI.exe /run <file path>[:<sequence name>][ /debug][ /args <arg1>[, <arg2>, <arg3>, ..., <argN>]][ /env <file path>]
    
    NOTES:
        - Commands are case-sensitive.
        - Commands can be specified in any order.
        - Commands parameters must be surrounded by quotation marks if containing spaces.
        - None of the commands can be used more than once.

Supported Commands:
    /help                                       - Display help. Alternatively use '/?'.
    /run <file path>[@<sequence name>]          - Use this command to specify TestStand sequence file to run.
                                                  Optionally, you can specify a sequence to run. If it is not explicitly specified,
                                                  the first sequence returned by the 'SequenceFile.GetSequence()' method will be run.
    /debug                                      - Use this command to enable debug messages.
    /args <arg1>[, <arg2>, <arg3>, ..., <argN>] - Use this command to specify 'Execution.RunTimeVariables.TestStandCLI_Args'.
                                                  This variable can be used to pass parameters from the command-line to the called sequence.
                                                  'TestStandCLI_Args' is an array of strings. Parameters should be delimited with a comma.
    /env <file path>                            - Use this command to specify TestStand environment file path (*.tsenv).

Examples:
    TestStandCLI.exe /debug /run ""C:\Examples\Mobile Device Test.seq""
    TestStandCLI.exe /run ""C:\Examples\Mobile Device Test.seq:MainSequence"" /env ""C:\My Environment\Example.tsenv""
    TestStandCLI.exe /run ""C:\Examples\MySequence.seq"" /args ""I hate Mondays, I love Fridays"" (args parameter will be convert into {""I hate Mondays"", ""I love Fridays""})

";
            Console.WriteLine(helpText);
        }

        public void DisplayError()
        {
            switch (Status)
            {
                case ValidationStatus.RequiredCommandMissingError:
                    Console.Write("At least one required command is missing. ");
                    break;
                case ValidationStatus.DuplicatedCommandError:
                    Console.Write($"Command '{commandWithError}' was used more than once. ");
                    break;
                case ValidationStatus.InvalidParameterError:
                    Console.Write($"Command '{commandWithError}' has invalid parameter(s). ");
                    break;
                case ValidationStatus.InvalidCommandError:
                    Console.Write($"Command '{commandWithError}' is unknown. ");
                    break;
                default:
                    Console.WriteLine($"No command line errors.");
                    return;
            }
            Console.WriteLine("Use '/?' or '/help' to lists all supported commands.");
        }

        private bool HandleRunCommand(string[] arguments, ref int currentIndex)
        {
            if (++currentIndex < arguments.Length)
            {
                string tempParameter = arguments[currentIndex];
                if (tempParameter.Contains('@'))
                {
                    string[] seqFileAndSeq = tempParameter.Split('@');
                    if (File.Exists(seqFileAndSeq[0]))
                    {
                        SequenceFilePath = seqFileAndSeq[0];
                        Sequence = seqFileAndSeq[1];
                        return true;
                    }
                }
                else
                {
                    if (File.Exists(tempParameter))
                    {
                        SequenceFilePath = tempParameter;
                        Sequence = "";
                        return true;
                    }
                }
            }
            commandWithError = arguments[currentIndex - 1];
            Status = ValidationStatus.InvalidParameterError;
            return false;
        }

        private bool HandleDebugCommand()
        {
            DebugEnabled = true;
            return true;
        }

        private bool HandleArgsCommand(string[] arguments, ref int currentIndex)
        {
            if (++currentIndex < arguments.Length)
            {
                ExecutionArguments = arguments[currentIndex].Split(',').ToList();
                return true;
            }
            commandWithError = arguments[currentIndex - 1];
            Status = ValidationStatus.InvalidParameterError;
            return false;
        }

        private bool HandleEnvCommand(string[] arguments, ref int currentIndex)
        {
            if (++currentIndex < arguments.Length && File.Exists(arguments[currentIndex]))
            {
                EnvironmentFilePath = arguments[currentIndex];
                return true;
            }
            commandWithError = arguments[currentIndex - 1];
            Status = ValidationStatus.InvalidParameterError;
            return false;
        }

        private void HandleInvalidCommandError(string[] arguments, int currentIndex)
        {
            commandWithError = arguments[currentIndex];
            Status = ValidationStatus.InvalidCommandError;
        }

        private void HandleDuplicatedCommandError(string[] arguments, int currentIndex)
        {
            commandWithError = arguments[currentIndex];
            Status = ValidationStatus.DuplicatedCommandError;
        }

        private void HandleRequiredCommandMissingError()
        {
            Status = ValidationStatus.RequiredCommandMissingError;
        }

        // ==================================================
        // Type Definitions
        // ==================================================

        public string SequenceFilePath { get; private set; }
        public string Sequence { get; private set; }
        public bool DebugEnabled { get; private set; }
        public List<string> ExecutionArguments { get; private set; }
        public string EnvironmentFilePath { get; private set; }
        public ValidationStatus Status { get; private set; }

        private string commandWithError;

        public enum ValidationStatus
        {
            RequiredCommandMissingError = -4,
            DuplicatedCommandError = -3,
            InvalidParameterError = -2,
            InvalidCommandError = -1,
            Help = 0,
            Execute = 1
        }
    }
}
