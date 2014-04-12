using System.Collections.Generic;

namespace SetCsprojVer
{
    internal class FileVersion
    {
        public string Path { get; set; }
        public List<VersionData> Versions { get; set; }
    }
}