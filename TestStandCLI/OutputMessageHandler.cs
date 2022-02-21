using NationalInstruments.TestStand.Interop.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestStandCLI
{
    internal static class OutputMessageHandler
    {

        public static void ProcessOutputMessage(OutputMessages outputMessages)
        {
            for (int i = 0; i < outputMessages.Count; i++)
            {
                ConsoleColor color = TestStandColorToConsoleColor(NumberToTestStandColor(outputMessages[i].TextColor));

                //Output Message: <message> <time stamp> <category> <severity> <step> <sequence> <sequence file> <execution> <thread id>
                String message = "Output Message:\t" +
                    outputMessages[i].Message + "\t" +
                    outputMessages[i].TimeStamp.ToString() + "\t" +
                    outputMessages[i].Category + "\t" +
                    outputMessages[i].Severity.ToString() + "\t";
                //TODO: add location parsing
                //Console.Write(tmpOutputMessages[i].FileLocations + "\t");
                //Console.Write(tmpOutputMessages[i].ExecutionLocations + "\t");

            }
        }

    private static TestStandColor NumberToTestStandColor(uint testStandColorNumber)
        {
            if (Enum.IsDefined(typeof(TestStandColor), testStandColorNumber))
                return (TestStandColor)Enum.ToObject(typeof(TestStandColor), testStandColorNumber);
            else return TestStandColor.tsDefaultColor;
        }

        private static ConsoleColor TestStandColorToConsoleColor(TestStandColor testStandColor)
        {
            switch (testStandColor)
            {
                case TestStandColor.tsRed: return ConsoleColor.Red;
                case TestStandColor.tsGreen: return ConsoleColor.Green;
                case TestStandColor.tsBlue: return ConsoleColor.Blue;
                case TestStandColor.tsCyan: return ConsoleColor.Cyan;
                case TestStandColor.tsMagenta: return ConsoleColor.Magenta;
                case TestStandColor.tsYellow: return ConsoleColor.Yellow;
                case TestStandColor.tsDarkRed: return ConsoleColor.DarkRed;
                case TestStandColor.tsDarkGreen: return ConsoleColor.DarkGreen;
                case TestStandColor.tsDarkBlue: return ConsoleColor.DarkBlue;
                case TestStandColor.tsDarkCyan: return ConsoleColor.DarkCyan;
                case TestStandColor.tsDarkMagenta: return ConsoleColor.DarkMagenta;
                case TestStandColor.tsDarkYellow: return ConsoleColor.DarkYellow;
                case TestStandColor.tsWhite: return ConsoleColor.White;
                case TestStandColor.tsLightGray: return ConsoleColor.Gray;
                case TestStandColor.tsGray: return ConsoleColor.Gray;
                case TestStandColor.tsDarkGray: return ConsoleColor.DarkGray;
                case TestStandColor.tsBlack: return ConsoleColor.Black;
                default: return ConsoleColor.White;
            }
        }

        private enum TestStandColor : uint
        {
            tsRed = 0x000000FF,
            tsGreen = 0x0000FF00,
            tsBlue = 0x00FF0000,
            tsCyan = 0x00FFFF00,
            tsMagenta = 0x00FF00FF,
            tsYellow = 0x0000FFFF,
            tsDarkRed = 0x00000080,
            tsDarkGreen = 0x00008000,
            tsDarkBlue = 0x00800000,
            tsDarkCyan = 0x00808000,
            tsDarkMagenta = 0x00800080,
            tsDarkYellow = 0x00008080,
            tsWhite = 0x00FFFFFF,
            tsLightGray = 0x00C0C0C0,
            tsGray = 0x00A0A0A0,
            tsDarkGray = 0x00808080,
            tsBlack = 0x00000000,
            tsDefaultColor = 0xFFFFFFFF
        }
    }
}
