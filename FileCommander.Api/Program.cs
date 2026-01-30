var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddScoped<FileCommander.Api.Services.IFileSystemService, FileCommander.Api.Services.FileSystemService>();

var app = builder.Build();

// Middleware
app.UseCors();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.MapControllers();

// Fallback to SPA-like static index (optional)
app.MapFallbackToFile("index.html");

app.Run();
