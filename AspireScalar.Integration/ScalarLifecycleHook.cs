using System.Collections.Immutable;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using AspireScalar.Integration.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Forwarder;

namespace AspireScalar.Integration;

internal sealed class ScalarLifecycleHook(ResourceNotificationService resourceNotificationService) : IDistributedApplicationLifecycleHook
{
    public async Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        var scalarResource = appModel.Resources.OfType<ScalarResource>().FirstOrDefault();
        if (scalarResource is null) return;

        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddHttpForwarder();

        var app = builder.Build();

        app.MapScalarApiReference();

        var resourceToEndpoint = new Dictionary<string, (string, string)>();
        var portToResourceMap = new Dictionary<int, (string, List<string>)>();

        foreach (var resource in appModel.Resources)
        {
            if (!resource.TryGetLastAnnotation<ScalarAnnotation>(out var annotation)) continue;
            resourceToEndpoint[resource.Name] = (annotation.EndpointReference.Url, annotation.Route);
            List<string> paths = [$"scalar/{resource.Name}/{annotation.DocumentName}"];
            portToResourceMap[app.Urls.Count] = (annotation.EndpointReference.Url, paths);
            app.Urls.Add("http://127.0.0.1:0");
        }

        app.Map("/openapi/{resourceName}/{documentName}.json", async (string resourceName, string documentName, IHttpForwarder forwarder, HttpContext context) =>
        {
            var (endpoint, path) = resourceToEndpoint[resourceName];
            using var client = new HttpMessageInvoker(new SocketsHttpHandler());
            await forwarder.SendAsync(context, endpoint, client, (c, requestMessage) =>
            {
                requestMessage.RequestUri = new Uri($"{endpoint}/{path}");
                return ValueTask.CompletedTask;
            });
        });

        app.Map("{*path}", async (HttpContext context, IHttpForwarder forwarder, string? path) =>
        {
            var (endpoint, _) = portToResourceMap[context.Connection.LocalPort];
            using var client = new HttpMessageInvoker(new SocketsHttpHandler());
            await forwarder.SendAsync(context, endpoint, client, (c, r) =>
            {
                r.RequestUri = path is null ? new Uri(endpoint) : new Uri($"{endpoint}/{path}");
                return ValueTask.CompletedTask;
            });
        });


        await app.StartAsync(cancellationToken);

        var addresses = app.Services.GetRequiredService<IServer>().Features.GetRequiredFeature<IServerAddressesFeature>().Addresses;

        var urls = ImmutableArray.CreateBuilder<UrlSnapshot>();
        
        var index = 0;
        foreach (var rawAddress in addresses)
        {
            var address = BindingAddress.Parse(rawAddress);
            
            var (_, paths) = portToResourceMap[address.Port] = portToResourceMap[index++];
            
            foreach (var p in paths) urls.Add(new UrlSnapshot(rawAddress, $"{rawAddress}/{p}", false));
        }

        await resourceNotificationService.PublishUpdateAsync(scalarResource, state => state with
        {
            State = "Running",
            Urls = urls.ToImmutable()
        });
    }
}