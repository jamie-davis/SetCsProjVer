using System.Collections.Generic;
using System.IO;
using System.Linq;

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

            if (options.IncrementPart == IncrementablePart.None)
            {
                output.WriteLine("Increment can only change build or revision number.");
                return -1;
            }

            return IncrementVersion(options, output, versionData);
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

            return 0;
        }

        private static int IncrementVersion(Options options, TextWriter output, List<FileVersion> versionData)
        {
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

            var versionUpdates = new[] { new VersionUpdate(incrementName, canon) };
            foreach (var fileVersion in versionData)
            {
                var rc = Editor.Edit(versionUpdates, fileVersion.Path, output, options.DryRun);
                if (rc != 0)
                    return rc;
            }

            return 0;
        }

        private static string TargetToName(string incrementTarget)
        {
            if (string.Compare(incrementTarget, "av", true) == 0)
                return "AssemblyVersion";

            if (string.Compare(incrementTarget, "af", true) == 0)
                return "AssemblyFileVersion";

            return null;
        }
    }
}