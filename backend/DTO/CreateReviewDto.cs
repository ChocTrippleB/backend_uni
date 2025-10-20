using System.ComponentModel.DataAnnotations;

namespace backend.DTO
{
    public class CreateReviewDto
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Overall rating must be between 1 and 5")]
        public int OverallRating { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Product quality rating must be between 1 and 5")]
        public int ProductQualityRating { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Communication rating must be between 1 and 5")]
        public int CommunicationRating { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Shipping speed rating must be between 1 and 5")]
        public int ShippingSpeedRating { get; set; }

        [MaxLength(200, ErrorMessage = "Review title cannot exceed 200 characters")]
        public string? ReviewTitle { get; set; }

        [MaxLength(2000, ErrorMessage = "Review text cannot exceed 2000 characters")]
        public string? ReviewText { get; set; }
    }
}
