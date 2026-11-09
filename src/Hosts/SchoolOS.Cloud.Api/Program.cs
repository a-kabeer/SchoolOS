var builder = WebApplication.CreateBuilder(args);

// Cloud Control Plane Services Configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Platform Health Check
app.MapGet("/health", () => Results.Ok(new 
{ 
    service = "SchoolOS.Cloud.Api", 
    status = "Healthy", 
    plane = "CloudControlPlane",
    timestamp = DateTime.UtcNow 
}));

app.Run();
