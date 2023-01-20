using Jering.Javascript.NodeJS;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
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

app.UseRouting();
app.MapRazorPages();

if (!app.Environment.IsDevelopment()) app.UseSpaStaticFiles();

app.UseSpa(spa => {
  spa.Options.SourcePath = "ClientApp";
  if (app.Environment.IsDevelopment())
    spa.UseProxyToSpaDevelopmentServer("http://localhost:5173");
});

await app.RunAsync();