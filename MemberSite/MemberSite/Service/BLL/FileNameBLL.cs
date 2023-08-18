namespace MemberSite.Service.BLL
{
    public class FileNameBLL
    {
        public string FileName { get; private set; }

        public FileNameBLL SetFileName(string fileName)
        {
            FileName = fileName;
            return this;
        }

        public FileNameBLL ExcludePath()
        {
            FileName = FileName.Substring(FileName.LastIndexOf('\\') + 1);
            return this;
        }

        public FileNameBLL SetFilenameExtension(string extension)
        {
            FileName = $"{FileName.Substring(0, FileName.LastIndexOf('.'))}.{extension}";
            return this;
        }
    }
}