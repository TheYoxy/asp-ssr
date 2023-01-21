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
app.UseHttpLogging();
app.UseStaticFiles();

app.UseRouting();

app.MapGet("/api/todos", () => {
  app.Logger.LogInformation("GET /api/todos");
  return new Todo[] { new("Hello") };
});

if (app.Environment.IsProduction())
  app.MapFallback(async (HttpContext context, INodeJSService nodeJsService, ILogger<Program> logger, CancellationToken cancellationToken) => {
    logger.LogInformation("Reading index.html");
    var html = await File.ReadAllTextAsync("ClientApp/dist/index.html", cancellationToken);

    logger.LogInformation("Rendering client");
    var output = await nodeJsService.InvokeFromStringAsync<Result>(
      // language=JavaScript
      """
module.exports = async (url) => {
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
    const [html, state] = await render(url, context);
    return {html, state: JSON.stringify(state)};
}
""", "render", null, new[] { context.Request.GetEncodedPathAndQuery() }, cancellationToken);

    var htmlContent = html.Replace("<!--app-html-->", output.Html).Replace("<!--react-query-data-->", $"window.__REACT_QUERY_STATE__ = {output.State};");
    logger.LogInformation("Rendered output {Output}", htmlContent);
    return Results.Content(htmlContent, "text/html");
  });

app.UseEndpoints(_ => { });
if (!app.Environment.IsDevelopment()) app.UseSpaStaticFiles();

app.UseSpa(spa => {
  spa.Options.SourcePath = "ClientApp";
  if (app.Environment.IsDevelopment())
    spa.UseProxyToSpaDevelopmentServer("http://localhost:5173");
});

await app.RunAsync();

internal sealed record Result(string Html, string State);

internal sealed record Todo(string Title);