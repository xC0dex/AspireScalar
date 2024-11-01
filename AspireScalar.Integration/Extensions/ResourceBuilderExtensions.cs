using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;

namespace AspireScalar.Integration.Extensions;

public static class ResourceBuilderExtensions
{
    public static IResourceBuilder<ProjectResource> WithScalarApiReference(this IResourceBuilder<ProjectResource> builder)
    {
        var scalarResource = new ScalarResource();
        builder.ApplicationBuilder
            .AddResource(scalarResource)
            .WithInitialState(new CustomResourceSnapshot
            {
                State = "Starting",
                ResourceType = "scalar-api-reference",
                Properties = []
            })
            .ExcludeFromManifest();
        builder.ApplicationBuilder.Services.TryAddLifecycleHook<ScalarLifecycleHook>();
        builder.WithAnnotation(new ScalarAnnotation("v1", "openapi/v1.json", builder.GetEndpoint("http")));
        return builder;
    }
}