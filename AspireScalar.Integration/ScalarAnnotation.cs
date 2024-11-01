using Aspire.Hosting.ApplicationModel;

namespace AspireScalar.Integration;

internal sealed record ScalarAnnotation(string DocumentName, string Route, EndpointReference EndpointReference) : IResourceAnnotation;