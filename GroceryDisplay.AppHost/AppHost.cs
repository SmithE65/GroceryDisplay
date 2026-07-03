var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.GroceryDisplay_Api>("grocerydisplay-api");

builder.Build().Run();
