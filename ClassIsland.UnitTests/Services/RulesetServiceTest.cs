using System.Text.Json;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Extensions.Registry;
using ClassIsland.Core.Models.Ruleset;
using ClassIsland.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsland.UnitTests.Services;

[TestClass]
[TestSubject(typeof(RulesetService))]
public class RulesetServiceTest
{
    public static IHost TestHost { get; } = Host.CreateDefaultBuilder().ConfigureServices((context, services) =>
    {
        services.AddSingleton<IRulesetService, RulesetService>();
        services.AddRule("test.true");
        services.AddRule("test.false");
        services.AddLogging(builder =>
        {
            builder.AddConsole();
        });
    }).Build();
    
    public static IRulesetService RulesetService { get; }

    static RulesetServiceTest()
    {
        var service = RulesetService = TestHost.Services.GetService<IRulesetService>()!;
        service.RegisterRuleHandler("test.true", settings => true);
        service.RegisterRuleHandler("test.false", settings => false);
    }
    
}