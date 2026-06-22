using Telerik.Blazor.Services;
using PragmaticScaffolder.Application.Abstractions;
using PragmaticScaffolder.Application.Services;
using PragmaticScaffolder.Application.Services.Generators;
using PragmaticScaffolder.Infrastructure.Database;
using PragmaticScaffolder.Infrastructure.FileSystem;
using PragmaticScaffolder.Infrastructure.Templates;
using PragmaticScaffolder.Web;
using PragmaticScaffolder.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddTelerikBlazor();

// Infrastructure (composition root — the only place these concrete types are referenced)
builder.Services.AddScoped<ISchemaReader, SqlServerSchemaReader>();
builder.Services.AddScoped<ITemplateRenderer, ScribanTemplateRenderer>();
builder.Services.AddScoped<IGeneratedFileWriter, FileSystemGeneratedFileWriter>();

// Code generators — registration order is the order they run in
builder.Services.AddScoped<ICodeGenerator, EntityGenerator>();
builder.Services.AddScoped<ICodeGenerator, DbContextGenerator>();
builder.Services.AddScoped<ICodeGenerator, DtoGenerator>();
builder.Services.AddScoped<ICodeGenerator, ServiceGenerator>();
builder.Services.AddScoped<ICodeGenerator, EndpointsGenerator>();
builder.Services.AddScoped<ICodeGenerator, BlazorApiClientGenerator>();
builder.Services.AddScoped<ICodeGenerator, BlazorPageGenerator>();
builder.Services.AddScoped<ICodeGenerator, TestsGenerator>();
builder.Services.AddScoped<ICodeGenerator, ProjectFilesGenerator>();
builder.Services.AddScoped<ICodeGenerator, StoredProcedureGenerator>();

builder.Services.AddScoped<GenerationEngine>();

// Session-scoped state shared across wizard pages
builder.Services.AddScoped<ScaffolderState>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
