using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOutputCache();
builder.Host.UseSerilog((_, configuration) => { configuration.WriteTo.Console(theme: AnsiConsoleTheme.Code); });
builder.Services.AddHttpClient();

builder.Services
  .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
  .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options => {
    options.Events = new JwtBearerEvents {
      OnMessageReceived = ctx => {
        var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        // logger.LogInformation("Message received: {Token}", ctx.Token);
        return Task.CompletedTask;
      },
    };
  });

builder.Services.AddAuthorization();
builder.Services.AddSpaStaticFiles(options => options.RootPath = "ClientApp/public");
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
app.UseSpaStaticFiles();

app.MapGet("/api/login", async (HttpContext ctx) => {
  await ctx.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new("username", "test") }, CookieAuthenticationDefaults.AuthenticationScheme)));

  return Results.Ok("User authenticated");
});

app.MapGet("/api/todos", () => {
    app.Logger.LogInformation("GET /api/todos");
    return new Todo[] { new("Hello"), new("World") };
  })
  .RequireAuthorization();

app.MapFallback(async (HttpContext context, HttpClient client) => {
  var targetUri = new Uri("http://localhost:3000");

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

  var handler = new JwtSecurityTokenHandler();
  var key = "secretqsjfhdsqjfhlqsjhflkqsjhfdlkjqshl"u8.ToArray();
  var tokenDescriptor = new SecurityTokenDescriptor {
    Subject = new ClaimsIdentity(new Claim[] { new("username", "test") }),
    Expires = DateTime.UtcNow.AddMinutes(1),
    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
  };
  var token = handler.CreateToken(tokenDescriptor);
  var tokenString = handler.WriteToken(token);
  requestMessage.Headers.ProxyAuthorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, tokenString);
  requestMessage.RequestUri = new Uri(targetUri, context.Request.Path + context.Request.QueryString);
  requestMessage.Headers.Host = targetUri.Host;
  requestMessage.Method = GetMethod(context.Request.Method);

  var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
  logger.LogWarning("Headers: {@Request}", requestMessage.Headers);

  using var responseMessage = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

  context.Response.StatusCode = (int)responseMessage.StatusCode;
  foreach (var header1 in responseMessage.Headers) {
    context.Response.Headers[header1.Key] = header1.Value.ToArray();
  }

  foreach (var header2 in responseMessage.Content.Headers) {
    context.Response.Headers[header2.Key] = header2.Value.ToArray();
  }

  context.Response.Headers.Remove("transfer-encoding");
  await responseMessage.Content.CopyToAsync(context.Response.Body);
});

app.UseEndpoints(_ => { });

await app.RunAsync();

internal sealed record Result(string Html, string State);

internal sealed record Todo(string Title);