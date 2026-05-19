using System.Text;
using BooksApi.Configuration;
using BooksApi.Data;
using BooksApi.Middleware;
using BooksApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- Bind environment variables (Render/Railway override appsettings) ---
// MONGO_URI, MONGO_DB, JWT_KEY, JWT_ISSUER, JWT_AUDIENCE, PORT
var mongoUriEnv = Environment.GetEnvironmentVariable("MONGO_URI");
var mongoDbEnv = Environment.GetEnvironmentVariable("MONGO_DB");
var jwtKeyEnv = Environment.GetEnvironmentVariable("JWT_KEY");
var jwtIssuerEnv = Environment.GetEnvironmentVariable("JWT_ISSUER");
var jwtAudienceEnv = Environment.GetEnvironmentVariable("JWT_AUDIENCE");

builder.Services.Configure<MongoDbSettings>(options =>
{
    builder.Configuration.GetSection("MongoDb").Bind(options);
    if (!string.IsNullOrWhiteSpace(mongoUriEnv)) options.ConnectionString = mongoUriEnv;
    if (!string.IsNullOrWhiteSpace(mongoDbEnv)) options.DatabaseName = mongoDbEnv;
});

builder.Services.Configure<JwtSettings>(options =>
{
    builder.Configuration.GetSection("Jwt").Bind(options);
    if (!string.IsNullOrWhiteSpace(jwtKeyEnv)) options.Key = jwtKeyEnv;
    if (!string.IsNullOrWhiteSpace(jwtIssuerEnv)) options.Issuer = jwtIssuerEnv;
    if (!string.IsNullOrWhiteSpace(jwtAudienceEnv)) options.Audience = jwtAudienceEnv;
});

// --- Services ---
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IPromoService, PromoService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<IPaymentCardService, PaymentCardService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReviewService, ReviewService>();

// --- JWT auth ---
var jwtConfig = new JwtSettings();
builder.Configuration.GetSection("Jwt").Bind(jwtConfig);
if (!string.IsNullOrWhiteSpace(jwtKeyEnv)) jwtConfig.Key = jwtKeyEnv;
if (!string.IsNullOrWhiteSpace(jwtIssuerEnv)) jwtConfig.Issuer = jwtIssuerEnv;
if (!string.IsNullOrWhiteSpace(jwtAudienceEnv)) jwtConfig.Audience = jwtAudienceEnv;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig.Issuer,
            ValidAudience = jwtConfig.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// --- CORS (open for mobile) ---
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()));

// --- Controllers + JSON ---
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// --- Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Kitab Bazari API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer. Format: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// --- Pipeline ---
app.UseMiddleware<ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Kitab Bazari API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

// --- Ensure indexes on startup ---
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
    try { await ctx.EnsureIndexesAsync(); }
    catch (Exception ex) { app.Logger.LogWarning(ex, "Could not create MongoDB indexes (DB unreachable?)"); }
}

app.Run();
