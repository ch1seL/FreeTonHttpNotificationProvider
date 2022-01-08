using System;
using System.Net.Http;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using ch1seL.TonNet.Client.Models;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Notifon.Client.MessageSender;
using Notifon.Client.Storage;

namespace Notifon.Client;

public class Program {
    public static async Task Main(string[] args) {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");

        builder.Services
               .AddMudServices();

        builder.Services
               .AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
               .AddTransient<IApiStatusClient, ApiStatusClient>()
               .AddScoped<AppConfigProvider>()
               .AddTonClient(options => options.Network = new NetworkConfig {
                   Endpoints = new[] { "net1.ton.dev", "net5.ton.dev" }
               })
               .AddTransient<IEverscaleMessageSender, EverscaleMessageSender>();

        builder.Services
               .AddBlazoredLocalStorage()
               .AddScoped<IMessageInfoStorage, MessageInfoStorage>();

        await builder.Build().RunAsync();
    }
}