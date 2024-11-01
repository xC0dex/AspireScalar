using AspireScalar.Integration.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.AspireScalar_ApiService>("bookstore-api").WithScalarApiReference();

builder.Build().Run();
