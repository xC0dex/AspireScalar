var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.AspireScalar_ApiService>("api");

builder.Build().Run();
