namespace SetCsprojVer
{
    internal class VersionData
    {
        public string Description;
        public string Major;
        public string Minor;
        public string Build;
        public string Revision;
        public int LineNumber;
        public string FilePath;
        public string Original;

        public override string ToString()
        {
            return string.Format("{0} {1}.{2}.{3}.{4}", Description, Major, Minor, Build, Revision);
        }
    }
}