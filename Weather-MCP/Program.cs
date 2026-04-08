using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using McpDotNet;
using McpDotNet.Protocol.Types;

var builder = Host.CreateEmptyApplicationBuilder(settings: null);

builder.Logging.AddConsole(options => 
{
    // Important: Route all console logs to StandardError so it doesn't corrupt MCP's StandardOutput
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddMcpServer(options => 
{
    options.ServerInfo = new Implementation { Name = "WeatherMCP", Version = "1.0.0" };
    
    // Safely update Capabilities without overwriting the internally populated Tool handlers
    options.Capabilities = (options.Capabilities ?? new ServerCapabilities()) with 
    {
        Tools = (options.Capabilities?.Tools ?? new ToolsCapability()) with 
        {
            ListChanged = true
        }
    };
})
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Weather MCP Server started and running.");

await app.RunAsync();