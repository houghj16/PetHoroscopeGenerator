var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.PetHoroscopeGenerator_ApiService>("apiservice");

builder.AddProject<Projects.PetHoroscopeGenerator_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
