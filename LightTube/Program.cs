using InnerTube;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews().AddNewtonsoftJson();

// ReSharper disable NotResolvedInText
InnerTubeAuthorization? auth = Environment.GetEnvironmentVariable("LIGHTTUBE_AUTH_TYPE")?.ToLower() switch
{
	"cookie" => InnerTubeAuthorization.SapisidAuthorization(
		Environment.GetEnvironmentVariable("LIGHTTUBE_AUTH_SAPISID") ?? 
		throw new ArgumentNullException("LIGHTTUBE_AUTH_SAPISID", "Authentication type set to 'cookie' but the 'LIGHTTUBE_AUTH_SAPISID' environment variable is not set."),
		Environment.GetEnvironmentVariable("LIGHTTUBE_AUTH_PSID") ?? 
		throw new ArgumentNullException("LIGHTTUBE_AUTH_PSID", "Authentication type set to 'cookie' but the 'LIGHTTUBE_AUTH_PSID' environment variable is not set.")),
	"oauth2" => InnerTubeAuthorization.RefreshTokenAuthorization(
		Environment.GetEnvironmentVariable("LIGHTTUBE_AUTH_REFRESH_TOKEN") ?? 
		throw new ArgumentNullException("LIGHTTUBE_AUTH_REFRESH_TOKEN", "Authentication type set to 'oauth2' but the 'LIGHTTUBE_AUTH_REFRESH_TOKEN' environment variable is not set.")),
	var _ => null
};
// ReSharper restore NotResolvedInText
builder.Services.AddSingleton(new InnerTube.InnerTube(new InnerTubeConfiguration
{
	Authorization = auth,
	CacheSize = int.Parse(Environment.GetEnvironmentVariable("LIGHTTUBE_CACHE_SIZE") ?? "50"),
	CacheExpirationPollingInterval = default
}));
WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
	app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();