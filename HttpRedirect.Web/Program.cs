using HttpRedirect;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRedirectWithHeader("Ralay-Url");
var app = builder.Build();
app.UseRedirect();
app.Run();
