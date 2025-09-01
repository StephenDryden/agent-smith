var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Root placeholder
app.MapGet("/", () => Results.Ok(new { status = "ok", service = "agent-smith" }));

// Health endpoint
app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));

app.Run();
