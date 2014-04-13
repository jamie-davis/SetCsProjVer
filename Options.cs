using System.Collections.Generic;
using System.Text.RegularExpressions;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable StringCompareIsCultureSpecific.3
// ReSharper disable StringCompareIsCultureSpecific.2


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
        public bool Report { get; set; }
        public string EnvironmentVariableName { get; set; }
        public string Get { get; set; }

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
            return ValidateReportSetting(options, errorList) 
                && ValidateGetSetting(options, errorList)
                && ValidateIncSetting(options, errorList) 
                && ValidateThatSomeActionIsSpecified(options, errorList)
                && ValidateEnvSetting(options, errorList) 
                && ValidateVersionNumberFormats(options, errorList);
        }

        private static bool ValidateVersionNumberFormats(Options options, IList<string> errorList)
        {
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

        private static bool ValidateEnvSetting(Options options, IList<string> errorList)
        {
            if (options.EnvironmentVariableName != null && !UpdateOperationSelected(options) && !IsGetSet(options))
            {
                errorList.Add("An environment variable can only be set if a version update or get is selected.");
                return false;
            }

            if (options.EnvironmentVariableName != null)
            {
                if (GetNumVersionsSet(options) > 1)
                {
                    errorList.Add(@"The /env option is not valid when setting multiple version numbers.");
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateThatSomeActionIsSpecified(Options options, IList<string> errorList)
        {
            if (!UpdateOperationSelected(options) && !ReadOperationSelected(options))
            {
                errorList.Add("A version update, report or get must be selected.");
                return false;
            }

            return true;
        }

        private static bool UpdateOperationSelected(Options options)
        {
            return IsVersionSet(options) || IsIncSet(options);
        }

        private static bool ReadOperationSelected(Options options)
        {
            return options.Report || IsGetSet(options);
        }

        private static bool IsGetSet(Options options)
        {
            return options.Get != null;
        }

        private static bool ValidateIncSetting(Options options, IList<string> errorList)
        {
            if (!IsIncSet(options)) return true;

            if (options.IncrementPart == IncrementablePart.None)
            {
                errorList.Add("Invalid increment part.");
                return false;
            }

            if (!IsValidVersionName(options.IncrementTarget, false))
            {
                errorList.Add("Invalid version specified for Inc. Specify FV or AV only.");
                return false;
            }

            return true;
        }

        private static bool ValidateGetSetting(Options options, IList<string> errorList)
        {
            if (!IsGetSet(options)) return true;

            if (IsVersionSet(options) || IsIncSet(options) || options.Report)
            {
                errorList.Add("Get cannot be combined with other options.");
                return false;
            }

            if (!IsValidVersionName(options.Get, true))
            {
                errorList.Add("Invalid version specified for Get. Specify FV, AV, or IV only.");
                return false;
            }

            return true;
        }

        private static bool IsValidVersionName(string name, bool allowIv)
        {
            return string.Compare(name, "fv", true) == 0
                   || string.Compare(name, "av", true) == 0
                   || (allowIv && string.Compare(name, "iv", true) == 0);

        }

        private static bool ValidateReportSetting(Options options, IList<string> errorList)
        {
            if (options.Report && (IsVersionSet(options) || IsIncSet(options)))
            {
                errorList.Add("Report cannot be combined with other options.");
                return false;
            }

            return true;
        }

        private static bool IsIncSet(Options options)
        {
            return options.IncrementTarget != null;
        }

        private static int GetNumVersionsSet(Options options)
        {
            var numVersionsSet = 0;
            if (options.AssemblyVersion != null) ++numVersionsSet;
            if (options.AssemblyFileVersion != null) ++numVersionsSet;
            if (options.AssemblyInformationalVersion != null) ++numVersionsSet;
            return numVersionsSet;

        }
        private static bool IsVersionSet(Options options)
        {
            return GetNumVersionsSet(options) > 0;
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