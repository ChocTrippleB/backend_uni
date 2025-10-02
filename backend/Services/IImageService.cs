using backend.Model;

namespace backend.Services
{
    public interface IImageService
    {
        Task<Image> UploadAsync(IFormFile file, int productId);
        Task<List<Image>> UploadManyAsync(List<IFormFile> files, int productId);
        Task<List<Image>> GetByProductAsync(int productId);
        Task DeleteAsync(int imageId);
    }

}
