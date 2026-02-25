using TabularModelDeployer.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ✅ ADD CORS SERVICE (NEW)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("https://id-preview--1115fb10-6ea8-4052-8d1b-31238016c02e.lovable.app")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<TabularDeploymentService>();

var app = builder.Build();

// ✅ ENABLE CORS (NEW)
app.UseCors("AllowFrontend");

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.MapControllers();

app.Run();
