using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ApprovalTests;
using ApprovalTests.Reporters;
using ConsoleToolkit.CommandLineInterpretation;
using NUnit.Framework;
using SetCsprojVer;

namespace TestSetCsprojVer
{
    [TestFixture]
    [UseReporter(typeof (WinMergeReporter))]
    public class AcceptanceTests
    {
        private static string _path = @".\TestFiles";
        private string _mixedVersionsPath;
        private string _matchedVersionsPath;

        [SetUp]
        public void SetUp()
        {
            if (Directory.Exists(_path))
                Directory.Delete(_path, true);

            Directory.CreateDirectory(_path);
            CopyDirectory("MockProjectStructures", _path);

            _mixedVersionsPath = "\"" + Path.Combine(_path, "NonUniqueVersions") + "\"";
            _matchedVersionsPath = "\"" + Path.Combine(_path, "MatchedVersions") + "\"";
        }

        private void CopyDirectory(string sourceDirectory, string destDirectory)
        {
            if (!Directory.Exists(destDirectory))
                Directory.CreateDirectory(destDirectory);

            foreach (var dir in Directory.EnumerateDirectories(sourceDirectory))
                CopyDirectory(dir, Path.Combine(destDirectory, Path.GetFileNameWithoutExtension(dir)));

            foreach (var file in Directory.EnumerateFiles(sourceDirectory))
                File.Copy(file, Path.Combine(destDirectory, Path.GetFileName(file)));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_path))
                Directory.Delete(_path, true);
        }

        [Test]
        public void NoParamsShowUsage()
        {
            var result = ExecuteTest("");
            Approvals.Verify(result);
        }

        [Test]
        public void MixedVersionNumbersIsAnError()
        {
            var commandLine = _mixedVersionsPath + " /inc:FV,r";
            var result = ExecuteTest(commandLine, _mixedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void SetFileVersionMakesNoUpdatesForDryRun()
        {
            var commandLine = _mixedVersionsPath + " /FV:2.3.4.5 /D";
            var result = ExecuteTest(commandLine, _mixedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void SetFileVersionMakesAllFileVersionsMatch()
        {
            var commandLine = _mixedVersionsPath + " /FV:2.3.4.5";
            var result = ExecuteTest(commandLine, _mixedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void SetAssemblyVersionMakesAllAssemblyVersionsMatch()
        {
            var commandLine = _mixedVersionsPath + " /AV:2.3.4.5";
            var result = ExecuteTest(commandLine, _mixedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void SetAssemblyVersionMakesNoChangesInDryRun()
        {
            var commandLine = _mixedVersionsPath + " /AV:2.3.4.5 /D";
            var result = ExecuteTest(commandLine, _mixedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void SetInfoVersionMakesAllInfoVersionsMatch()
        {
            var commandLine = _mixedVersionsPath + " /IV:InfoTest1.0";
            var result = ExecuteTest(commandLine, _mixedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void SetInfoVersionMakesNoChangesInDryRun()
        {
            var commandLine = _mixedVersionsPath + " /IV:InfoTest1.0 /D";
            var result = ExecuteTest(commandLine, _mixedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void IncrementingAssemblyFileVersionRevisionChangesAll()
        {
            var commandLine = _matchedVersionsPath + " /inc:FV,r";
            var result = ExecuteTest(commandLine, _matchedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void IncrementingAssemblyFileVersionRevisionMakesNoChangesInDryRun()
        {
            var commandLine = _matchedVersionsPath + " /inc:FV,r /D";
            var result = ExecuteTest(commandLine, _matchedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void IncrementingAssemblyVersionRevisionChangesAll()
        {
            var commandLine = _matchedVersionsPath + " /inc:AV,r";
            var result = ExecuteTest(commandLine, _matchedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void IncrementingAssemblyVersionRevisionMakesNoChangesInDryRun()
        {
            var commandLine = _matchedVersionsPath + " /inc:AV,r /D";
            var result = ExecuteTest(commandLine, _matchedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void IncrementingAssemblyFileVersionBuildChangesAll()
        {
            //Set the revision value to 1 to prove it is zeroed by incrementing build
            RunProgram(_matchedVersionsPath + " /inc:FV,r", null, null);

            //Run the test
            var commandLine = _matchedVersionsPath + " /inc:FV,b";
            var result = ExecuteTest(commandLine, _matchedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void IncrementingAssemblyVersionBuildChangesAll()
        {
            //Set the revision value to 1 to prove it is zeroed by incrementing build
            RunProgram(_matchedVersionsPath + " /inc:AV,r", null, null);

            //Run the test
            var commandLine = _matchedVersionsPath + " /inc:AV,b";
            var result = ExecuteTest(commandLine, _matchedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void InvalidIncrementTargetIsAnError()
        {
            var commandLine = _matchedVersionsPath + " /inc:XV,r";
            var result = ExecuteTest(commandLine, _matchedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void InvalidIncrementPartIsAnError()
        {
            var commandLine = _matchedVersionsPath + " /inc:AV,x";
            var result = ExecuteTest(commandLine, _matchedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void InvalidVersionFormatIsAnError()
        {
            var commandLine = _mixedVersionsPath + " /FV:x.3.4.5";
            var result = ExecuteTest(commandLine, _mixedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void InvalidCommandLineOptionIsAnError()
        {
            var commandLine = _mixedVersionsPath + "/invalid /FV:x.3.4.5";
            var result = ExecuteTest(commandLine, _mixedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void MultipleIncrementOptionsIsInvalid()
        {
            var commandLine = _mixedVersionsPath + " /inc:fv,r /inc:fv,r";
            var result = ExecuteTest(commandLine, _mixedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void EnvOptionSetsEnvironmentVariableFromInc()
        {
            var commandLine = _matchedVersionsPath + " /inc:fv,r /env:variable";
            var result = ExecuteTest(commandLine, _matchedVersionsPath, "variable");
            Approvals.Verify(result);
        }

        [Test]
        public void EnvOptionSetsEnvironmentVariableFromIncInDryRun()
        {
            var commandLine = _matchedVersionsPath + " /inc:fv,r /env:variable /D";
            var result = ExecuteTest(commandLine, _matchedVersionsPath, "variable");
            Approvals.Verify(result);
        }

        [Test]
        public void EnvOptionSetsEnvironmentVariableFromFileVersionSetting()
        {
            var commandLine = _matchedVersionsPath + " /FV:1.2.3.4 /env:variable";
            var result = ExecuteTest(commandLine, _matchedVersionsPath, "variable");
            Approvals.Verify(result);
        }

        [Test]
        public void EnvOptionSetsEnvironmentVariableFromFileAssemblyVersionSetting()
        {
            var commandLine = _matchedVersionsPath + " /AV:1.2.3.4 /env:variable";
            var result = ExecuteTest(commandLine, _matchedVersionsPath, "variable");
            Approvals.Verify(result);
        }

        [Test]
        public void EnvOptionSetsEnvironmentVariableFromFileInfoVersionSetting()
        {
            var commandLine = _matchedVersionsPath + " /IV:anything /env:variable";
            var result = ExecuteTest(commandLine, _matchedVersionsPath, "variable");
            Approvals.Verify(result);
        }

        [Test]
        public void ReportAndIncIsInvalid()
        {
            var commandLine = _matchedVersionsPath + " /R /inc:fv,r";
            var result = ExecuteTest(commandLine, _matchedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void ReportAndFileVersionIsInvalid()
        {
            var commandLine = _matchedVersionsPath + " /FV:1.0.0.2 /report";
            var result = ExecuteTest(commandLine, _matchedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void ReportAndAssemblyVersionIsInvalid()
        {
            var commandLine = _matchedVersionsPath + " /AV:1.0.0.2 /report";
            var result = ExecuteTest(commandLine, _matchedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void ReportAndInfoVersionIsInvalid()
        {
            var commandLine = _matchedVersionsPath + " /IV:1.0.0.2 /report";
            var result = ExecuteTest(commandLine, _matchedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void ReportAndEnvVariableIsInvalid()
        {
            var commandLine = _matchedVersionsPath + " /env:var /report";
            var result = ExecuteTest(commandLine, _matchedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void ReportShowsAllUniqueVersions()
        {
            var commandLine = _mixedVersionsPath + " /report";
            var result = ExecuteTest(commandLine, _mixedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void GetAndIncIsInvalid()
        {
            var commandLine = _matchedVersionsPath + " /G:fv /inc:fv,r";
            var result = ExecuteTest(commandLine, _matchedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void GetAndFileVersionIsInvalid()
        {
            var commandLine = _matchedVersionsPath + " /FV:1.0.0.2 /get:fv";
            var result = ExecuteTest(commandLine, _matchedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void GetAndAssemblyVersionIsInvalid()
        {
            var commandLine = _matchedVersionsPath + " /AV:1.0.0.2 /get:fv";
            var result = ExecuteTest(commandLine, _matchedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void GetAndInfoVersionIsInvalid()
        {
            var commandLine = _matchedVersionsPath + " /IV:1.0.0.2 /get:fv";
            var result = ExecuteTest(commandLine, _matchedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void GetAndReportIsInvalid()
        {
            var commandLine = _matchedVersionsPath + " /R /get:fv";
            var result = ExecuteTest(commandLine, _matchedVersionsPath);
            Approvals.Verify(result);
        }

        [Test]
        public void GetAndInvalidVersionIsAnError()
        {
            var commandLine = _matchedVersionsPath + " /get:xv";
            var result = ExecuteTest(commandLine, _matchedVersionsPath);
            Approvals.Verify(result);
        }
 
        [Test]
        public void GetDisplaysFileVersion()
        {
            var commandLine = _mixedVersionsPath + " /get:fv";
            var result = ExecuteTest(commandLine, _mixedVersionsPath);
            Approvals.Verify(result);
        }
 
        [Test]
        public void GetDisplaysAssemblyVersion()
        {
            var commandLine = _mixedVersionsPath + " /get:av";
            var result = ExecuteTest(commandLine, _mixedVersionsPath);
            Approvals.Verify(result);
        }
 
        [Test]
        public void GetDisplaysInfoVersion()
        {
            var commandLine = _mixedVersionsPath + " /get:iv";
            var result = ExecuteTest(commandLine, _mixedVersionsPath);
            Approvals.Verify(result);
        }
 
        [Test]
        public void GetSetsEnvironmentVariable()
        {
            var commandLine = _mixedVersionsPath + " /get:iv /env:got";
            var result = ExecuteTest(commandLine, _mixedVersionsPath, "got");
            Approvals.Verify(result);
        }

        private static string ExecuteTest(string commandLine, string targetFilesRoot = null, string environmentVariableName = null)
        {
            string result;
            using (var output = new StringWriter())
            {
                output.WriteLine("Command Line Arguments: {0}", commandLine);
                output.WriteLine();

                RunProgram(commandLine, output, environmentVariableName);

                if (targetFilesRoot != null)
                   ReportFileVersionInfo(output, targetFilesRoot);
                result = output.ToString();
            }

            Console.WriteLine(result);

            return result;
        }

        private static void RunProgram(string commandLine, StringWriter output, string environmentVariableName)
        {
            var args = CommandLineTokeniser.Tokenise(commandLine);

            if (output == null)
            {
                using (var tempOutput = new StringWriter())
                    SetCsprojVerProgram.Execute(args, output, 80);
            }
            else
                SetCsprojVerProgram.Execute(args, output, 80);

            if (environmentVariableName != null)
            {
                output.WriteLine();

                var environmentVariableValue = Environment.GetEnvironmentVariable(environmentVariableName, EnvironmentVariableTarget.User);
                output.WriteLine("Environment variable: {0} = {1}", environmentVariableName, environmentVariableValue);
            }
        }

        private static void ReportFileVersionInfo(StringWriter output, string targetFilesRoot)
        {
            output.WriteLine();
            output.WriteLine("File Versions after run:");

            foreach (var file in AllFiles(RemoveDelimiters(targetFilesRoot)))
            {
                ReportVersions(output, file);
            }
        }

        private static void ReportVersions(StringWriter output, string file)
        {
            var tellTales = new[]
            {
                "[assembly: AssemblyVersion",
                "[assembly: AssemblyFileVersion",
                "[assembly: AssemblyInformationalVersion",
            };

            output.WriteLine("    {0}", file);

            var lines = File.ReadAllLines(file);
            foreach (var match in lines.Where(l => tellTales.Any(t => l.StartsWith(t))))
                output.WriteLine("        {0}", match);
        }

        private static string RemoveDelimiters(string path)
        {
            if (path.StartsWith("\""))
                return path.Substring(1, path.Length - 2);
            return path;
        }

        private static IEnumerable<string> AllFiles(string root)
        {
            if (Directory.Exists(root))
            {
                return Directory.EnumerateDirectories(root).SelectMany(AllFiles)
                    .Concat(Directory.EnumerateFiles(root));
            }

            return new string[]{};
        }
    }
}
