var builder = WebApplication.CreateBuilder(args);

// Local Data Plane Services Configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Local Node Health Check
app.MapGet("/health", () => Results.Ok(new 
{ 
    service = "SchoolOS.Local.Api", 
    status = "Healthy", 
    plane = "CampusLocalDataPlane",
    timestamp = DateTime.UtcNow 
}));

app.Run();
