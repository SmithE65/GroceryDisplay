var builder = DistributedApplication.CreateBuilder(args);

var groceryPostgres = builder.AddPostgres("grocerydb-instance")
    .WithPgAdmin();

var groceryDb = groceryPostgres.AddDatabase("grocerydb");

builder.AddProject<Projects.GroceryDisplay_Api>("grocerydisplay-api")
    .WithReference(groceryDb); ;

builder.Build().Run();
