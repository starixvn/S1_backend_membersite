using MemberSite;
using MemberSite.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseSentry();

var environmentName = builder.Configuration.GetValue<string>("Environment");
if (!string.IsNullOrEmpty(environmentName))
{
    builder.Environment.EnvironmentName = environmentName;
}

var remoteUrls = GetRemoteUrls();
remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, args, remoteUrls);
builder.Configuration.AddRemoteJsonConfig(remoteUrls);

var environmentName = builder.Configuration.GetValue<string>("Environment");
if (!string.IsNullOrEmpty(environmentName))
{
    builder.Environment.EnvironmentName = environmentName;
}

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();
startup.Configure(app, builder.Environment);

var minWorkers = 96;
var minIOCPs = 4;
ThreadPool.SetMinThreads(minWorkers, minIOCPs);
SentrySdk.CaptureMessage("Member_site_api started");
app.Run();
return;

string[] GetRemoteUrls()
{
    return builder.Configuration.GetSection("ConfigService").Get<string[]>();
}
