using System.Globalization;
using InnerTube;
using LightTube;
using LightTube.Chores;
using LightTube.Database;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews().AddNewtonsoftJson();

InnerTubeAuthorization? auth = Configuration.GetInnerTubeAuthorization();
builder.Services.AddSingleton(new InnerTube.InnerTube(new InnerTubeConfiguration
{
	Authorization = auth,
	CacheSize = int.Parse(Configuration.GetVariable("LIGHTTUBE_CACHE_SIZE", "50")!),
	CacheExpirationPollingInterval = default
}));
builder.Services.AddSingleton(new HttpClient());

await JsCache.DownloadLibraries();
ChoreManager.RegisterChores();
DatabaseManager.Init(Configuration.GetVariable("LIGHTTUBE_MONGODB_CONNSTR"));

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
	app.UseHttpsRedirection();
}

app.UseStaticFiles(new StaticFileOptions {
	OnPrepareResponse = ctx =>
	{
		// Cache static files for 3 days
		ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=259200");
		ctx.Context.Response.Headers.Append("Expires", DateTime.UtcNow.AddDays(3).ToString("R", CultureInfo.InvariantCulture));
	}
});

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();