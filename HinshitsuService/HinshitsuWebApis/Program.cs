using HinshitsuWebApis.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using HinshitsuService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<TaskService>();
builder.Services.AddSingleton<AnalysisJobService>();
builder.Services.AddSingleton(sp => new QueueService(builder.Configuration["AzureStorage:ConnectionString"]!));
builder.Services.AddSingleton(sp => new BlobStorageService(
    builder.Configuration["AzureStorage:ConnectionString"]!,
    sp.GetRequiredService<ILogger<BlobStorageService>>()));
builder.Services.AddScoped<ResultsSummaryService>();
builder.Services.AddScoped<PdfReportService>();

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "JwtBearer";
    options.DefaultChallengeScheme = "JwtBearer";
})
.AddJwtBearer("JwtBearer", options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR hub for real-time notifications
app.MapHub<HinshitsuWebApis.Hubs.NotificationHub>("/hubs/notifications");

await app.RunAsync();
