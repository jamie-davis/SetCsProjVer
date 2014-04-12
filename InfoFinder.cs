using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SetCsprojVer
{
    internal class InfoFinder
    {
        public InfoFinder(string root)
        {
            if (Directory.Exists(root))
                infos = FindAllInfos("AssemblyInfo.cs", root).ToList();
            else
                infos = new List<string>();
        }

        private List<string> infos;
        public IEnumerable<string> Infos { get { return infos; } } 

        private IEnumerable<string> FindAllInfos(string fileName, string root)
        {
            return Directory.EnumerateDirectories(root).SelectMany(d => FindAllInfos(fileName, d))
                .Concat(Directory.EnumerateFiles(root).Where(f => string.Compare(Path.GetFileName(f), fileName, true) == 0));
        }
    }
}