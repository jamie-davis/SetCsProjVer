using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SetCsprojVer
{
    public enum IncrementablePart
    {
        None,
        Build, 
        Revision
    }

    public class Options
    {
        private static Regex _versionNumberCheckRegex = new Regex(@"(?<major>\d+)\.(?<minor>\d+)\.(?<build>\d+)\.(?<revision>\d+)");

        public string ProjectFolder { get; set; }
        public string AssemblyVersion { get; set; }
        public string AssemblyFileVersion { get; set; }
        public string AssemblyInformationalVersion { get; set; }
        public string IncrementTarget { get; set; }
        public IncrementablePart IncrementPart { get; set; } 
        public bool DryRun { get; set; }

        public void SetIncTarget(string targetString, string partString)
        {
            IncrementTarget = targetString;
            if (string.Compare(partString, "b", true) == 0)
                IncrementPart = IncrementablePart.Build;
            if (string.Compare(partString, "r", true) == 0)
                IncrementPart = IncrementablePart.Revision;
        }

        public static bool Validator(Options options, IList<string> errorList)
        {
            if (options.IncrementTarget != null && options.IncrementPart == IncrementablePart.None)
            {
                errorList.Add("Invalid increment part.");
                return false;
            }

            string detail;
            if (!IsValidVersionNumber(options.AssemblyFileVersion, out detail))
            {
                errorList.Add(MakeVersionErrorMessage("Invalid assembly file version.", detail));
                return false;
            }

            if (!IsValidVersionNumber(options.AssemblyVersion, out detail))
            {
                errorList.Add(MakeVersionErrorMessage("Invalid assembly version.", detail));
                return false;
            }

            return true;
        }

        private static string MakeVersionErrorMessage(string message, string detail)
        {
            if (string.IsNullOrEmpty(detail))
                return message;

            return string.Format("{0} {1}", message, detail);
        }

        private static bool IsValidVersionNumber(string version, out string detail)
        {
            if (version == null)
            {
                detail = null;
                return true;
            }

            var match = _versionNumberCheckRegex.Match(version);
            if (!match.Success)
            {
                detail = "The value does not have the correct format.";
                return false;
            }
            detail = null;
            return true;

        }
    }
}