using APIWeaver;
using AspireScalar.ApiService.Books;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<BookStore>();
builder.Services.AddProblemDetails();

builder.Services.AddHttpContextAccessor();
builder.Services.AddOpenApi(options => options.AddServerFromRequest());

var app = builder.Build();

app.UseExceptionHandler();

app.MapOpenApi();
app.MapBookEndpoints();

app.Run();

