var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var cartrackDb = postgres.AddDatabase("cartrack");

var server = builder.AddProject<Projects.CarTrack_Server>("server")
    .WithReference(cache)
    .WithReference(cartrackDb)
    .WaitFor(cache)
    .WaitFor(cartrackDb)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithHttpEndpoint(port: 51705)
    .WithReference(server)
    .WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
