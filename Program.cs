using Jering.Javascript.NodeJS;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddNodeJS();
builder.Services.AddOutputCache();
builder.Host.UseSerilog((_, configuration) => { configuration.WriteTo.Console(theme: AnsiConsoleTheme.Code); });
builder.Services.AddHttpClient();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
  .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options => {
    options.Events.OnRedirectToLogin = context => {
      context.Response.StatusCode = 401;
      return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context => {
      context.Response.StatusCode = 403;
      return Task.CompletedTask;
    };
  });

builder.Services.AddAuthorization();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseOutputCache();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/login", async (HttpContext ctx) => {
  await ctx.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new("username", "test") }, CookieAuthenticationDefaults.AuthenticationScheme)));

  return Results.Ok("User authenticated");
});

app.MapGet("/api/todos", () => {
    app.Logger.LogInformation("GET /api/todos");
    return new Todo[] { new("Hello"), new("World") };
  })
  .RequireAuthorization();

app.UseEndpoints(_ => { });

if (app.Environment.IsDevelopment())
  app.UseSpa(spa => {
    spa.Options.SourcePath = "ClientApp";
    spa.UseProxyToSpaDevelopmentServer("http://localhost:3000");
  });
else {
  app.MapFallback(async (HttpContext context, HttpClient client) => {
    var targetUri = new Uri("http://localhost:3000");

    static void CopyFromOriginalRequestContentAndHeaders(HttpContext context, HttpRequestMessage requestMessage) {
      var requestMethod = context.Request.Method;

      if (!HttpMethods.IsGet(requestMethod) &&
          !HttpMethods.IsHead(requestMethod) &&
          !HttpMethods.IsDelete(requestMethod) &&
          !HttpMethods.IsTrace(requestMethod)) {
        var streamContent = new StreamContent(context.Request.Body);
        requestMessage.Content = streamContent;
      }

      foreach (var header in context.Request.Headers) {
        requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
      }
    }

    static void CopyFromTargetResponseHeaders(HttpContext context, HttpResponseMessage responseMessage) {
      foreach (var header in responseMessage.Headers) {
        context.Response.Headers[header.Key] = header.Value.ToArray();
      }

      foreach (var header in responseMessage.Content.Headers) {
        context.Response.Headers[header.Key] = header.Value.ToArray();
      }

      context.Response.Headers.Remove("transfer-encoding");
    }

    static HttpMethod GetMethod(string method) {
      if (HttpMethods.IsDelete(method)) return HttpMethod.Delete;
      if (HttpMethods.IsGet(method)) return HttpMethod.Get;
      if (HttpMethods.IsHead(method)) return HttpMethod.Head;
      if (HttpMethods.IsOptions(method)) return HttpMethod.Options;
      if (HttpMethods.IsPost(method)) return HttpMethod.Post;
      if (HttpMethods.IsPut(method)) return HttpMethod.Put;
      if (HttpMethods.IsTrace(method)) return HttpMethod.Trace;
      return new HttpMethod(method);
    }

    var requestMessage = new HttpRequestMessage();
    CopyFromOriginalRequestContentAndHeaders(context, requestMessage);

    requestMessage.RequestUri = targetUri;
    requestMessage.Headers.Host = targetUri.Host;
    requestMessage.Method = GetMethod(context.Request.Method);

    using var responseMessage = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
    context.Response.StatusCode = (int)responseMessage.StatusCode;
    CopyFromTargetResponseHeaders(context, responseMessage);
    await responseMessage.Content.CopyToAsync(context.Response.Body);
  });
}


await app.RunAsync();

internal sealed record Result(string Html, string State);

internal sealed record Todo(string Title);