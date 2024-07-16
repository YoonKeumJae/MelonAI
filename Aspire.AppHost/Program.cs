var builder = DistributedApplication.CreateBuilder(args);
var apiapp = builder.AddProject<Projects.Server>("server");
builder.AddProject<Projects.Client>("client")
       .WithReference(apiapp);
builder.Build().Run();
