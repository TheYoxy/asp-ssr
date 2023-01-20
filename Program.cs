using Jering.Javascript.NodeJS;
using Microsoft.AspNetCore.Http.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddNodeJS();
builder.Services.AddSpaStaticFiles(options => options.RootPath = "ClientApp/dist");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.MapFallback(async (HttpContext context, INodeJSService nodeJsService, ILogger<Program> logger, CancellationToken cancellationToken) => {
  logger.LogInformation("Reading index.html");
  var html = await File.ReadAllTextAsync("ClientApp/dist/index.html", cancellationToken);

  logger.LogInformation("Rendering client");
  var output = await nodeJsService.InvokeFromStringAsync<string>(
    // language=JavaScript
    """
module.exports = (callback, url) => {
    function requireModule(modulePath, exportName) {
        try {
            const imported = require(modulePath);
            return exportName ? imported[exportName] : imported;
        } catch (err) {
            return err.code;
        }
    }

    const render = requireModule('./ClientApp/dist/server/entry-server.cjs', 'render');
    const context = {};
    const html = render(url, context);
    callback(null, html);
}
""", "render", null, new[] { context.Request.GetEncodedPathAndQuery() }, cancellationToken);

  var htmlContent = html.Replace("<!--app-html-->", output);
  logger.LogInformation("Rendered output {Output}", htmlContent);
  return Results.Content(htmlContent, "text/html");
});

if (!app.Environment.IsDevelopment()) app.UseSpaStaticFiles();

app.UseSpa(spa => {
  spa.Options.SourcePath = "ClientApp";
  if (app.Environment.IsDevelopment())
    spa.UseProxyToSpaDevelopmentServer("http://localhost:5173");
});

await app.RunAsync();