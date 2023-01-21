using Jering.Javascript.NodeJS;
using Microsoft.AspNetCore.Http.Extensions;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddNodeJS();
builder.Services.AddSpaStaticFiles(options => options.RootPath = "ClientApp/dist");
builder.Services.AddOutputCache();
builder.Host.UseSerilog((context, configuration) => { configuration.MinimumLevel.Warning().WriteTo.Console(theme: AnsiConsoleTheme.Code); });

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
app.UseOutputCache();
app.MapGet("/api/todos", () => {
  app.Logger.LogInformation("GET /api/todos");
  return new Todo[] { new("Hello") };
});

if (app.Environment.IsProduction())
  app.MapFallback(async (HttpContext context, INodeJSService nodeJsService, ILogger<Program> logger, CancellationToken cancellationToken) => {
    logger.LogInformation("Reading index.html");
    var html = await File.ReadAllTextAsync("ClientApp/dist/index.html", cancellationToken);

    logger.LogInformation("Rendering client");

    Result? result;
    var fromCache = await nodeJsService.TryInvokeFromCacheAsync<Result>("render", null, new[] { context.Request.GetEncodedPathAndQuery() }, cancellationToken);

    if (!fromCache.Item1) {
      logger.LogWarning("Cache miss");
      result = await nodeJsService.InvokeFromStringAsync<Result>(
        // language=JavaScript
        """
module.exports = async (url) => {
    const render = require('./ClientApp/dist/server/entry-server.cjs').render;
    const context = {};
    const [html, state] = await render(url, context);
    return {html, state: JSON.stringify(state)};
}
""", "render", null, new[] { context.Request.GetEncodedPathAndQuery() }, cancellationToken);
    }
    else {
      result = fromCache.Item2;
    }

    var htmlContent = html.Replace("<!--app-html-->", result.Html).Replace("<!--react-query-data-->", $"window.__REACT_QUERY_STATE__ = {result.State};");
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