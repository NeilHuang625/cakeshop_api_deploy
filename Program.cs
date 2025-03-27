using cakeshop_api.Hubs;
using cakeshop_api.Services;
using cakeshop_api.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var mongoSettings = builder.Configuration.GetSection("MongoDB");
var mongoClient = new MongoClient(mongoSettings["ConnectionString"]);
var database = mongoClient.GetDatabase(mongoSettings["DatabaseName"]);

var connectionString = mongoSettings["ConnectionString"];
var databaseName = mongoSettings["DatabaseName"];

Console.WriteLine($"MongoDB ConnectionString: {connectionString}");
Console.WriteLine($"MongoDB DatabaseName: {databaseName}");

if (string.IsNullOrEmpty(databaseName))
{
    throw new ArgumentNullException(nameof(databaseName), "Database name is missing!");
}

builder.Services.AddSingleton(database);
builder.Services.AddSignalR();

builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<CakeService>();
builder.Services.AddSingleton<CartService>();
builder.Services.AddSingleton<CategoryService>();
builder.Services.AddSingleton<OrderService>();
builder.Services.AddSingleton<JwtHelper>();
builder.Services.AddSingleton<CloudflareR2Service>();

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", builder => builder.WithOrigins("https://main.d3f9q6d13mdf99.amplifyapp.com").AllowAnyHeader().AllowAnyMethod().AllowCredentials());
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key")))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("admin", policy => policy.RequireRole("admin"));
    options.AddPolicy("user", policy => policy.RequireRole("user"));
});

var app = builder.Build();

app.UseCors("AllowSpecificOrigin");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<OrderHub>("/orderHub");



app.Run();
