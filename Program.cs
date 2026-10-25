using System.Text;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using DocsData;
using DocsData.ExcelConverter.Abstractions;
using DocsData.ExcelConverter.Export;
using DocsData.ExcelConverter.Ingestion;
using DocsData.ExcelConverter.Parsing;
using DocsData.ExcelConverter.Pipeline;
using DocsData.ExcelConverter.Transform;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IExcelFileValidator, ExcelFileValidator>();
builder.Services.AddScoped<IFileIngestor, BrowserFileIngestor>();
builder.Services.AddScoped<IWorkbookParser, ExcelWorkbookParser>();
builder.Services.AddScoped<IWorkbookTransformer, WorkbookTransformer>();
builder.Services.AddScoped<IWorkbookExporter, ParquetWorkbookExporter>();
builder.Services.AddScoped<IExcelPipeline, ExcelPipeline>();

await builder.Build().RunAsync();
