namespace backend.DTO
{
    public class ImageDto
    {
        public int Id { get; set; }
        public string fileName { get; set; } = string.Empty;
        public string downloadUrl { get; set; } = string.Empty;

        public ImageDto(){}

        public ImageDto(int id, string fileName, string downloadUrl)
        {
            this.Id = id;
            this.fileName = fileName;
            this.downloadUrl = downloadUrl;
        }
    }
}
