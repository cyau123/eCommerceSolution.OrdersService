using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelet.json", optional: false,
    reloadOnChange: true);
;
builder.Services.AddOcelot().AddPolly();

var app = builder.Build();
await app.UseOcelot();

app.Run();