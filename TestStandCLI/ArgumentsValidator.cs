using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandCLI
{
    // Static methods for command-line arguments validation.
    internal static class ArgumentsValidator
    {

        // ==================================================
        // Methods
        // ==================================================

        public static bool IsHelp(string[] arguments)
        {
            return (arguments.Contains("/?") || arguments.Contains("/help", StringComparer.OrdinalIgnoreCase));
        }

        public static bool IsValid(string argument, out Command command)
        {
            int commandIndex = validCommands.FindIndex(arg => arg.ValidString == argument);
            if (commandIndex >= 0)
            {
                command = validCommands[commandIndex].Command;
                return true;
            }
            command = Command.None;
            return false;
        }

        public static bool IsAlreadyUsed(Command command)
        {
            int commandIndex = validCommands.FindIndex(arg => arg.Command == command);
            if (commandIndex >= 0 && !usedCommands.Contains(commandIndex))
            {
                usedCommands.Add(commandIndex);
                return false;
            }
            return commandIndex >= 0;
        }

        public static bool ContainsRequired()
        {
            int[] requiredCommands = validCommands.Select((arg, i) => arg.Required ? i : -1).Where(i => i != -1).ToArray();
            if (requiredCommands.Any())
            {
                foreach (int i in requiredCommands)
                {
                    if (!usedCommands.Contains(i)) return false;
                }
            }
            return true;
        }

        // ==================================================
        // Type Definitions
        // ==================================================

        public enum Command
        {
            None,
            Run,
            Debug,
            Args,
            Env
        }

        // A structure for defining the properties of individual commands.
        private struct ArgumentProperties
        {
            private readonly string validString;
            private readonly bool required;
            private readonly Command command;

            public ArgumentProperties(string validString, bool required, Command command)
            {
                this.validString = validString;
                this.required = required;
                this.command = command;
            }

            public string ValidString { get { return validString; } }
            public bool Required { get { return required; } }
            public Command Command { get { return command; } }
        }

        // List of supported commands.
        private static readonly List<ArgumentProperties> validCommands = new List<ArgumentProperties>()
        {
             new ArgumentProperties ("/run", true, Command.Run),
             new ArgumentProperties ("/debug", false, Command.Debug),
             new ArgumentProperties ("/args", false, Command.Args),
             new ArgumentProperties ("/env", false, Command.Env)
        };

        // Command counter. Used to determine if commands have been used the correct number of times.
        private static readonly List<int> usedCommands = new List<int>();

    }
}
