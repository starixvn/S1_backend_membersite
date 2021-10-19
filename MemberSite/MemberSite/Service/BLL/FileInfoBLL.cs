namespace MemberSite.Service.BLL
{
    public class FileInfoBLL
    {
        public string FileName { get; private set; }

        public FileInfoBLL SetFileName(string fileName)
        {
            FileName = fileName;
            return this;
        }

        public FileInfoBLL ExcludePath()
        {
            FileName = FileName.Substring(FileName.LastIndexOf('\\') + 1);
            return this;
        }

        public FileInfoBLL SetFilenameExtension(string extension)
        {
            FileName = $"{FileName.Substring(0, FileName.LastIndexOf('.'))}.{extension}";
            return this;
        }
    }
}