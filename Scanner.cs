using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SetCsprojVer
{
    internal static class Scanner
    {
        private static Regex textVersionMatcher = new Regex(@"^\[assembly\: (?<prop>AssemblyVersion|AssemblyFileVersion|AssemblyInformationalVersion)\(\""(?<text>[^\""]*)\""\)\]");

        public static IEnumerable<VersionData> Scan(string info)
        {
            var fileContents = File.ReadAllLines(info);
            foreach (var versionData in Scan(info, fileContents).Where(v => v != null)) 
                yield return versionData;
        }

        public static IEnumerable<VersionData> Scan(string info, string[] fileContents)
        {
            var versionMatches =
                fileContents.Select((l, i) => new {Line = i, Data = l, Match = textVersionMatcher.Match(l)})
                    .Where(m => m.Match.Success)
                    .ToList();
            foreach (var i in versionMatches)
            {
                yield return ExtractVersion(info, fileContents, i.Line, i.Match.Groups["prop"].Value, i.Match.Groups["text"].Value);
            }
        }

        private static VersionData ExtractVersion(string filePath, string[] fileContents, int line, string property, string value)
        {
            var text = fileContents[line];
            if (property == "AssemblyInformationalVersion")
                return new VersionData
                {
                    Description = property,
                    Original = value,
                    LineNumber = line,
                    FilePath = filePath
                };

            var r = new Regex(@"(?<major>[0-9]*)\.(?<minor>[0-9]*)\.(?<build>[0-9]*)\.(?<revision>[0-9]*)");
            var match = r.Match(text);
            if (match.Success)
            {
                return new VersionData
                {
                    Description = property,
                    Major = match.Groups["major"].Value,
                    Minor = match.Groups["minor"].Value,
                    Build = match.Groups["build"].Value,
                    Revision = match.Groups["revision"].Value,
                    Original = value,
                    LineNumber = line,
                    FilePath = filePath
                };
            }

            return null;
        }
    }
}