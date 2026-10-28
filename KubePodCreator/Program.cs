using KubePodCreator.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<PodCreatorService>();

var app = builder.Build();
app.MapOpenApi();
app.MapScalarApiReference();
app.MapControllers();
app.Run();
