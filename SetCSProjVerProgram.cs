using System;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ConsoleToolkit.CommandLineInterpretation;

namespace SetCsprojVer
{
    public static class SetCsprojVerProgram
    {
        static void Main(string[] args)
        {
            var rc = Execute(args, Console.Out, Console.WindowWidth);
            if (rc != 0)
                Environment.Exit(rc);
        }

        public static int Execute(string[] args, TextWriter output, int consoleWidth)
        {
            var config = ConfigureCommandLine();

            var interpreter = new CommandLineInterpreter(config);
            string[] errors;
            var options = interpreter.Interpret(args, out errors) as Options;
            if (options == null)
            {
                foreach (var error in errors)
                {
                    output.WriteLine(error);
                    output.WriteLine();
                    output.WriteLine(config.Describe(consoleWidth));
                    return -1;
                }
            }

            return Controller.Process(options, output);
        }

        private static CommandLineInterpreterConfiguration ConfigureCommandLine()
        {
            var config = new CommandLineInterpreterConfiguration(CommandLineParserConventions.MsDosConventions);
            config.Parameters<Options>("SetCsprojVer")
                .Description(@"SetCsprojVer is a simple tool to update the version number in AssemblyInfo.cs files in a project tree.

A project folder should be supplied, and this will scanned for AssemblyInfo.cs files. Each file found will be edited to select the appropriate version number.")
                .Positional("projectfolder")
                    .Description("The base folder for the scan.")
                .Option("AV", o => o.AssemblyVersion)
                    .Description("Set the AssemblyVersion to the specified value. This must be a 4 part version number, if specified. e.g. /AV:1.0.0.0")
                .Option("FV", o => o.AssemblyFileVersion)
                    .Description("Set the AssemblyFileVersion to the specified value. This must be a 4 part version number, if specified. e.g. /FV:1.0.0.0")
                .Option("IV", o => o.AssemblyInformationalVersion)
                    .Description("Set the AssemblyInformationalVersion to the specified value. e.g. \"/IV:1.0 beta\"")
                .Option<string, string>("inc", (o, s, p) => o.SetIncTarget(s, p))
                    .Description("Increment a component of either the AssemblyVersion (AV) or the AssemblyFileVersion (FV). The component incremented is either the build (b) or revision (r). " + Environment.NewLine +
                                 "Specify [AV|FV],[B|R], e.g. /inc:av,b")
                    .Alias("I")
                .Option("dryrun")
                    .Alias("D")
                    .Description("Perform a dry run. No updates will be made. e.g. /dryrun or /D")
                .Validator(Options.Validator);
            return config;
        }
    }
}
