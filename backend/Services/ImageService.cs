using backend.Data;
using backend.Model;
using Firebase.Storage;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class ImageService : IImageService
    {
        private readonly AppDbContext _db;
        private readonly FirebaseStorage _storage;
        private readonly int _maxPerProduct = 3;
        private readonly long _maxBytes = 5 * 1024 * 1024; // 5MB

        public ImageService(AppDbContext db, IConfiguration cfg, GoogleCredential gCred)
        {
            _db = db;

            _storage = new FirebaseStorage(
                        cfg["Firebase:StorageBucket"],
                        new FirebaseStorageOptions
                        {
                            AuthTokenAsyncFactory = async () =>
                                await gCred.UnderlyingCredential.GetAccessTokenForRequestAsync(), // already scoped
                            ThrowOnCancel = true
                        });
        }

        public async Task<Image> UploadAsync(IFormFile file, int productId)
        {
            if (!await _db.Products.AnyAsync(p => p.Id == productId))
                throw new ArgumentException("Product not found");

            var existingCount = await _db.Images.CountAsync(i => i.productId == productId);
            if (existingCount >= _maxPerProduct)
                throw new InvalidOperationException("Max images reached");

            if (file.Length == 0) throw new InvalidOperationException("Empty file");
            if (file.Length > _maxBytes) throw new InvalidOperationException("Image too large. Maximum size is 5MB");

            var img = new Image
            {
                fileName = file.FileName,
                fileType = file.ContentType,
                productId = productId,
                downloadUrl = "pending"
            };

            _db.Images.Add(img);
            await _db.SaveChangesAsync();

            var ext = Path.GetExtension(file.FileName);
            using var stream = file.OpenReadStream();

            var url = await _storage
                .Child("products")
                .Child(productId.ToString())
                .Child($"{img.Id}{ext.ToLowerInvariant()}")
                .PutAsync(stream);

            img.downloadUrl = url;
            await _db.SaveChangesAsync();

            return img;
        }

        public async Task<List<Image>> UploadManyAsync(List<IFormFile> files, int productId)
        {
            var results = new List<Image>();
            foreach (var f in files)
            {
                results.Add(await UploadAsync(f, productId));
            }
            return results;
        }

        public async Task<List<Image>> GetByProductAsync(int productId) =>
            await _db.Images.Where(i => i.productId == productId).ToListAsync();

        public async Task DeleteAsync(int imageId)
        {
            var img = await _db.Images.FindAsync(imageId);
            if (img == null) return;

            var ext = Path.GetExtension(img.fileName);
            await _storage
                .Child("products")
                .Child(img.productId.ToString())
                .Child($"{img.Id}{ext}")
                .DeleteAsync();

            _db.Images.Remove(img);
            await _db.SaveChangesAsync();
        }
    }

}
