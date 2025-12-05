using backend.Data;
using backend.Model;
using Firebase.Storage;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace backend.Controllers
{ 
    [ApiController]
    [Route("api/v1/images/[controller]")]
    public class ImagesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _cfg;
        private readonly GoogleCredential _gCred;
        private readonly string _bucket;
        private readonly int _maxPerProduct;
        private readonly long _maxBytes; // 2 MB default

        private static readonly HashSet<string> AllowedMime = new(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/png", "image/webp" };

        private static readonly HashSet<string> AllowedExt = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp" };

        public ImagesController(AppDbContext db, IConfiguration cfg)
        {
            _db = db;
            _cfg = cfg;

            _bucket = _cfg["Firebase:StorageBucket"]
                      ?? throw new InvalidOperationException("Firebase:StorageBucket missing");
            var svcPath = _cfg["Firebase:ServiceAccountPath"]
                      ?? throw new InvalidOperationException("Firebase:ServiceAccountPath missing");

            _gCred = GoogleCredential.FromFile(svcPath)
                     .CreateScoped("https://www.googleapis.com/auth/devstorage.full_control");

            _maxPerProduct = int.TryParse(_cfg["Images:MaxPerProduct"], out var mpp) ? mpp : 3;
            var maxMb = int.TryParse(_cfg["Images:MaxFileMB"], out var mb) ? mb : 2;
            _maxBytes = maxMb * 1024L * 1024L;
        }

        private FirebaseStorage NewStorage() => new FirebaseStorage(
            _bucket,
            new FirebaseStorageOptions
            {
                // Use service account access token for each call
                AuthTokenAsyncFactory = async () =>
                    await _gCred.UnderlyingCredential.GetAccessTokenForRequestAsync(),
                ThrowOnCancel = true
            });

        // ---------- Upload one or many ----------
        [HttpPost("upload/{productId:int}")]
        public async Task<IActionResult> Upload(int productId, [FromForm] List<IFormFile> files)
        {
            if (files == null || files.Count == 0) return BadRequest("No files.");
            if (!await _db.Products.AnyAsync(p => p.Id == productId))
                return NotFound($"Product {productId} not found.");

            var existingCount = await _db.Images.CountAsync(i => i.productId == productId);
            if (existingCount + files.Count > _maxPerProduct)
                return BadRequest($"Max {_maxPerProduct} images per product. " +
                                  $"Already have {existingCount}, trying to add {files.Count}.");

            var storage = NewStorage();
            var results = new List<Image>();

            foreach (var file in files)
            {
                if (file.Length == 0) return BadRequest("Empty file.");
                if (file.Length > _maxBytes) return BadRequest($"File too big. Max {_maxBytes / (1024 * 1024)} MB.");

                var ext = Path.GetExtension(file.FileName);
                if (!AllowedExt.Contains(ext)) return BadRequest($"Unsupported extension: {ext}");

                if (!AllowedMime.Contains(file.ContentType))
                    return BadRequest($"Unsupported MIME: {file.ContentType}");

                // 1) Create DB row first to get Image.Id
                var img = new Image
                {
                    fileName = file.FileName,         // keep original name for UI
                    fileType = file.ContentType,
                    downloadUrl = "pending",
                    productId = productId
                };
                _db.Images.Add(img);
                await _db.SaveChangesAsync(); // now we have img.Id

                // 2) Upload to Firebase at deterministic key: products/{productId}/{imageId}{ext}
                var blobName = $"{img.Id}{ext.ToLowerInvariant()}";
                string url;
                using (var stream = file.OpenReadStream())
                {
                    url = await storage
                        .Child("products")
                        .Child(productId.ToString())
                        .Child(blobName)
                        .PutAsync(stream);
                }

                // 3) Update DB with final URL
                img.downloadUrl = url;
                results.Add(img);
            }

            await _db.SaveChangesAsync();
            return Ok(results);
        }

        // ---------- List images for a product ----------
        [HttpGet("product/{productId:int}")]
        public async Task<IActionResult> ListByProduct(int productId)
        {
            var images = await _db.Images
                .Where(i => i.productId == productId)
                .OrderBy(i => i.Id)
                .ToListAsync();

            return Ok(images);
        }

        // ---------- Get image record (metadata) ----------
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var img = await _db.Images.FindAsync(id);
            if (img == null) return NotFound();
            return Ok(img);
        }

        // ---------- Delete (Firebase + DB) ----------
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var img = await _db.Images.FindAsync(id);
            if (img == null) return NotFound();

            var productId = img.productId;
            var ext = Path.GetExtension(img.fileName);
            if (string.IsNullOrWhiteSpace(ext))
            {
                // Fallback: parse extension from the download URL if needed
                ext = ParseExtFromFirebaseUrl(img.downloadUrl) ?? ".jpg";
            }

            var blobName = $"{img.Id}{ext.ToLowerInvariant()}";

            // 1) Delete from Firebase
            var storage = NewStorage();
            await storage
                .Child("products")
                .Child(productId.ToString())
                .Child(blobName)
                .DeleteAsync();

            // 2) Delete from DB
            _db.Images.Remove(img);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // ---------- Set as Cover Image ----------
        [HttpPatch("{id:int}/set-cover")]
        public async Task<IActionResult> SetAsCover(int id)
        {
            var img = await _db.Images.FindAsync(id);
            if (img == null) return NotFound();

            // Get all images for this product
            var productImages = await _db.Images
                .Where(i => i.productId == img.productId)
                .ToListAsync();

            // Set the selected image to DisplayOrder = 0 (cover)
            // Set all other images to DisplayOrder > 0
            foreach (var image in productImages)
            {
                if (image.Id == id)
                {
                    image.DisplayOrder = 0;
                }
                else if (image.DisplayOrder == 0)
                {
                    // Push the previous cover image down
                    image.DisplayOrder = 1;
                }
            }

            await _db.SaveChangesAsync();

            return Ok(new { message = "Cover image updated successfully" });
        }

        // Try to infer file extension from Firebase download URL if original name lacked it
        private static string? ParseExtFromFirebaseUrl(string url)
        {
            // .../o/products%2F123%2F45.png?alt=media&token=...
            var m = Regex.Match(url, @"\/o\/.+%2F(?<name>[^?]+)\?");
            if (!m.Success) return null;
            var encodedName = m.Groups["name"].Value; // e.g. 45.png
            var name = Uri.UnescapeDataString(encodedName);
            return Path.GetExtension(name);
        }
    }
}

