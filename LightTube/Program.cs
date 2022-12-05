using InnerTube;
using LightTube;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews().AddNewtonsoftJson();

// ReSharper disable NotResolvedInText
InnerTubeAuthorization? auth = Configuration.GetInnerTubeAuthorization();
// ReSharper restore NotResolvedInText
builder.Services.AddSingleton(new InnerTube.InnerTube(new InnerTubeConfiguration
{
	Authorization = auth,
	CacheSize = int.Parse(Configuration.GetVariable("LIGHTTUBE_CACHE_SIZE", "50")!),
	CacheExpirationPollingInterval = default
}));
builder.Services.AddSingleton(new HttpClient());
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