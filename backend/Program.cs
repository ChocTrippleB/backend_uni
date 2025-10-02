using backend.Data;
using backend.Services; // ?? make sure this matches your namespace
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

namespace backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // --- Firebase Admin (used for server auth tokens) ---
            var svcPath = builder.Configuration["Firebase:ServiceAccountPath"];
            var gCred = GoogleCredential
                .FromFile(svcPath)
                .CreateScoped("https://www.googleapis.com/auth/devstorage.read_write"); // <- important
            builder.Services.AddSingleton(gCred);


            // --- Database ---
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            // --- Controllers + JSON options ---
            builder.Services.AddControllers().AddJsonOptions(x =>
            {
                x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

            builder.Services.AddEndpointsApiExplorer();

            // --- CORS ---
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend",
                    policy =>
                    {
                        policy.SetIsOriginAllowed(origin =>
                            new Uri(origin).Host.Contains("localhost") ||
                            new Uri(origin).Host.StartsWith("192.") ||
                            new Uri(origin).Host.StartsWith("127.") ||
                            new Uri(origin).Host.StartsWith("10.") ||
                            new Uri(origin).Host.StartsWith("172."))
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                    });
            });

            // --- JWT Authentication ---
            var jwtConfig = builder.Configuration.GetSection("Jwt");
            var jwtKey = jwtConfig["Key"];
            var jwtIssuer = jwtConfig["Issuer"];
            var jwtAudience = jwtConfig["Audience"];

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!))
                    };
                });

            // --- Dependency Injection for custom services ---
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<IImageService, ImageService>();
            builder.Services.AddScoped<ICartService, CartService>();
            builder.Services.AddScoped<ICartItemService, CartItemService>();


            var app = builder.Build();

            // --- HTTP Pipeline ---
            app.UseDeveloperExceptionPage();
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors("AllowFrontend");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
