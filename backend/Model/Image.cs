using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace backend.Model
{
    public class Image
    {
        public int Id { get; set; }
        public string fileName { get; set; } = string.Empty;    // original filename (kept for UI)
        public string fileType { get; set; } = string.Empty;    // MIME, e.g. image/jpeg

        [Required]
        public string downloadUrl { get; set; } = string.Empty; // Firebase public URL

        public int DisplayOrder { get; set; } = 0; // Order for displaying images (0 = cover image)

        public int productId { get; set; }

        [JsonIgnore]
        public Product product { get; set; }
    }
}
