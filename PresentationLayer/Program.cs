using DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PresentationLayer.Hubs;
using Repository.Implements;
using Repository.Interfaces;
using Service.Implements;
using Service.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// === Kestrel: 500MB limit ===
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 500 * 1024 * 1024;
});

// === Database ===
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// === CORS ===
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Đổi port này theo port Frontend của bạn
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Bắt buộc khi dùng SignalR
    });
});

// === Dependency Injection ===
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IUserService, UserService>();

// === SignalR ===
builder.Services.AddSignalR();

// === Controllers ===
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize enum dưới dạng string thay vì số trong JSON response
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// === Swagger / OpenAPI ===
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ChatBox Web API",
        Version = "v1",
        Description = "API cho ứng dụng chat - hỗ trợ gửi tin nhắn text, ảnh và file giữa các users"
    });
});

var app = builder.Build();

// === Middleware Pipeline ===

// Swagger UI (chỉ trong Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ChatBox Web API v1");
        options.DocumentTitle = "ChatBox API - Swagger UI";
    });
}

app.UseCors("AllowFrontend");

app.UseStaticFiles(); // Serve uploaded files từ wwwroot/uploads/

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHub<ChatHub>("/chathub");

app.Run();
