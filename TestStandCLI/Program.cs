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
        private static SequenceFile sequenceFile = null;

        [STAThread]
        static void Main(string[] args)
        {
            // Create application domain, call MainEntryPoint, and
            // cleanup before return.
            LaunchTestStandApplicationInNewDomain.LaunchProtected(
               new LaunchTestStandApplicationInNewDomain.
                   MainEntryPointDelegateWithArgs(MainEntryPoint),
               args,
               "TestStand Application",
               new LaunchTestStandApplicationInNewDomain.
                   DisplayErrorMessageDelegate(DisplayErrorMessage));
        }

        // Main entry point executed in new application domain.
        private static void MainEntryPoint(string[] args)
        {

            // Obtain a reference to existing Engine.
            Program.engine = new Engine();
            Program.engine.LoadTypePaletteFilesEx(
               TypeConflictHandlerTypes.ConflictHandler_Prompt, 0);

            // Launch your application here.
            // e.g., Application.Run(new MainForm());

            // Engine clean up.

            Program.engine = null;

        }

        // Called if exception occurs in MainEntryPoint.
        private static void DisplayErrorMessage(string caption, string message)
        {
        }
    }
}
