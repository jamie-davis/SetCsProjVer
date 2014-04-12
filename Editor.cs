using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SetCsprojVer
{
    internal class VersionUpdate
    {
        public string VersionKey { get; private set; }
        public string Version { get; private set; }

        public VersionUpdate(string versionKey, string version)
        {
            VersionKey = versionKey;
            Version = version;
        }

        public VersionUpdate(string incrementName, VersionData version)
        {
            VersionKey = incrementName;
            Version = string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
        }
    }

    internal static class Editor
    {
        public static int Edit(IEnumerable<VersionUpdate> newVersions, string path, TextWriter output, bool dryRun)
        {
            var replacementVersions = newVersions.ToList();
            var fileContents = File.ReadAllLines(path);

            using (var writer = new StringWriter())
            {
                var file = Scanner.Scan(path, fileContents);
                var index = 0;
                foreach (var versionData in file.OrderBy(f => f.LineNumber))
                {
                    while (index < versionData.LineNumber)
                        writer.WriteLine(fileContents[index++]);

                    var newVersionString = MakeNewVersion(replacementVersions, versionData);

                    var replacementLine = string.Format(@"[assembly: {0}(""{1}"")]", versionData.Description, newVersionString);
                    writer.WriteLine(replacementLine);

                    ReportUpdate(path, fileContents, index, replacementLine, output);
                    ++index;
                }
                while (index < fileContents.Length)
                    writer.WriteLine(fileContents[index++]);

                //Add missing values to the end.
                foreach (var replacementVersion in replacementVersions)
                {
                    var replacementLine = string.Format(@"[assembly: {0}(""{1}"")]",replacementVersion.VersionKey, replacementVersion.Version);
                    writer.WriteLine(replacementLine);
                    ReportAddition(path, replacementLine, output);
                }

                if (!dryRun)
                    File.WriteAllText(path, writer.ToString());
            }

            return 0;
        }

        private static void ReportAddition(string path, string replacementLine, TextWriter output)
        {
            if (output == null) return;

            output.WriteLine(path);
            output.WriteLine("Adding:");
            output.WriteLine("    {0}", replacementLine);
        }

        private static void ReportUpdate(string path, string[] fileContents, int index, string replacementLine, TextWriter output)
        {
            if (output == null || replacementLine == fileContents[index]) return;
            
            output.WriteLine(path);
            output.WriteLine("Replacing:");
            output.WriteLine("    {0}", fileContents[index]);
            output.WriteLine("with");
            output.WriteLine("    {0}", replacementLine);
        }

        private static string MakeNewVersion(List<VersionUpdate> newVersionRequests, VersionData currentVersion)
        {
            var match = newVersionRequests.FirstOrDefault(v => v.VersionKey == currentVersion.Description);
            if (match == null)
                return currentVersion.Original;

            newVersionRequests.Remove(match);

            return match.Version;
        }
    }
}