using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SetCsprojVer
{
    internal static class Validator
    {
        public static bool Check(List<FileVersion> versionData, TextWriter output)
        {
            var versions = versionData
                .SelectMany(f => f.Versions.Select(v => new {v.Description, v.Original}))
                .Distinct()
                .ToList();
            var differentVersions = versions.GroupBy(g => g.Description).Where(c => c.Count() > 1).ToList();
            if (differentVersions.Any())
            {
                output.WriteLine("Inconsistent versions detected.");
                foreach (var differentVersion in differentVersions)
                {
                    output.WriteLine(differentVersion.Key);
                    foreach (var uniqueVersion in differentVersion)
                    {
                        output.WriteLine("    {0}", uniqueVersion.Original);
                        var matchedFiles = versionData.Where(v => v.Versions
                                                    .FirstOrDefault(f => uniqueVersion.Description == f.Description
                                                                         && uniqueVersion.Original == f.Original) != null)
                            .Select(s => s.Path);
                        foreach (var matchedFile in matchedFiles)
                        {
                            output.WriteLine("        {0}", matchedFile);
                        }
                    }
                }
                return false;
            }

            return true;
        }
    }
}