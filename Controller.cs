using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// ReSharper disable StringCompareIsCultureSpecific.3
// ReSharper disable SpecifyACultureInStringConversionExplicitly

namespace SetCsprojVer
{
    public static class Controller
    {
        public static int Process(Options options, TextWriter output)
        {
            var versionSet =
                !string.IsNullOrWhiteSpace(options.AssemblyFileVersion + options.AssemblyVersion +
                                           options.AssemblyInformationalVersion);

            var incRequested = options.IncrementPart != IncrementablePart.None;

            if (versionSet && incRequested)
            {
                output.WriteLine("Versions cannot be incremented at the same time as version numbers are being assigned.");
                return -1;
            }

            var allInfo = new InfoFinder(options.ProjectFolder);
            var versionData = allInfo.Infos
                .Select(i => new FileVersion { Path = i, Versions = Scanner.Scan(i).ToList() })
                .ToList();

            if (versionSet)
                return SetVersions(options, output, versionData);

            if (options.IncrementTarget != null)
                return IncrementVersion(options, output, versionData);

            if (options.Report)
                return ReportVersions(output, versionData);

            if (options.Get != null)
                return ReportVersion(options, output, versionData);
            return 0;
        }

        private static int ReportVersion(Options options, TextWriter output, List<FileVersion> versionData)
        {
            var requiredVersion = TargetToName(options.Get);
            var version = versionData.SelectMany(f => f.Versions).FirstOrDefault(v => v.Description == requiredVersion);
            if (version == null)
            {
                output.WriteLine("No {0} found.", requiredVersion);
                return -1;
            }

            var value = version.Original;
            output.WriteLine("{0} = {1}", requiredVersion, value);

            if (options.EnvironmentVariableName != null)
                Environment.SetEnvironmentVariable(options.EnvironmentVariableName, value, EnvironmentVariableTarget.User);
            return 0;
        }

        private static int ReportVersions(TextWriter output, List<FileVersion> versionData)
        {
            var versions = versionData
                .SelectMany(f => f.Versions)
                .Select(v => v.Description)
                .Distinct();

            foreach (var version in versions)
            {
                output.WriteLine("{0}:", version);
                var versionName = version;
                var versionNumbers = versionData
                    .SelectMany(f => f.Versions)
                    .Where(v => v.Description == versionName)
                    .Select(v => v.Original)
                    .Distinct();
                foreach (var versionNumber in versionNumbers)
                {
                    output.WriteLine("    {0}", versionNumber);
                }
            }

            return 0;
        }

        private static int SetVersions(Options options, TextWriter output, List<FileVersion> versionData)
        {
            var replacements = new List<VersionUpdate>();
            if (options.AssemblyFileVersion != null)
                replacements.Add(new VersionUpdate("AssemblyFileVersion", options.AssemblyFileVersion));
            if (options.AssemblyVersion != null)
                replacements.Add(new VersionUpdate("AssemblyVersion", options.AssemblyVersion));
            if (options.AssemblyInformationalVersion != null)
                replacements.Add(new VersionUpdate("AssemblyInformationalVersion", options.AssemblyInformationalVersion));

            foreach (var fileVersion in versionData)
            {
                var rc = Editor.Edit(replacements, fileVersion.Path, output, options.DryRun);
                if (rc != 0)
                    return rc;
            }

            if (options.EnvironmentVariableName != null)
            {
                var firstReplacement = replacements.FirstOrDefault();
                if (firstReplacement != null)
                    Environment.SetEnvironmentVariable(options.EnvironmentVariableName, firstReplacement.Version, EnvironmentVariableTarget.User);
            }

            return 0;
        }

        private static int IncrementVersion(Options options, TextWriter output, List<FileVersion> versionData)
        {
            if (options.IncrementPart == IncrementablePart.None)
            {
                output.WriteLine("Increment can only change build (b) or revision number (r).");
                return -1;
            }

            if (!Validator.Check(versionData, output))
            {
                output.WriteLine();
                output.WriteLine("All versions must be made consistent before they can be edited.");
                return -1;
            }

            var incrementName = TargetToName(options.IncrementTarget);
            if (incrementName == null)
            {
                output.WriteLine("Increment target not recognised.");
                return -1;
            }

            var canon = versionData.SelectMany(f => f.Versions).FirstOrDefault(v => v.Description == incrementName);
            if (canon == null)
            {
                output.WriteLine("No version found.");
                return -1;
            }

            string textVersion = options.IncrementPart == IncrementablePart.Revision 
                ? canon.Revision 
                : canon.Build;

            int newValue;
            if (!int.TryParse(textVersion, out newValue))
            {
                output.WriteLine("Unable to decode version: {0}", textVersion);
                return -1;
            }

            ++newValue;
            if (options.IncrementPart == IncrementablePart.Revision)
            {
                canon.Revision = newValue.ToString();
            }
            else
            {
                canon.Build = newValue.ToString();
                canon.Revision = "0";
            }

            var versionUpdate = new VersionUpdate(incrementName, canon);
            var versionUpdates = new[] { versionUpdate };
            foreach (var fileVersion in versionData)
            {
                var rc = Editor.Edit(versionUpdates, fileVersion.Path, output, options.DryRun);
                if (rc != 0)
                    return rc;
            }

            if (options.EnvironmentVariableName != null)
            {
                Environment.SetEnvironmentVariable(options.EnvironmentVariableName, versionUpdate.Version, EnvironmentVariableTarget.User);
                
            }

            return 0;
        }

        private static string TargetToName(string incrementTarget)
        {
            if (string.Compare(incrementTarget, "av", true) == 0)
                return "AssemblyVersion";

            if (string.Compare(incrementTarget, "fv", true) == 0)
                return "AssemblyFileVersion";

            if (string.Compare(incrementTarget, "iv", true) == 0)
                return "AssemblyInformationalVersion";

            return null;
        }
    }
}