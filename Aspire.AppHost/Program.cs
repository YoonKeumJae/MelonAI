var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var apiapp = builder.AddProject<Projects.Server>("server");

builder.AddProject<Projects.Client>("client")
        .WithReference(cache)
        .WithReference(apiapp);


builder.Build().Run();
