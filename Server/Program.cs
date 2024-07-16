using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure;
using Azure.AI.OpenAI;
using Melon.ApiApp.Plugins.AddMemory;
using MelonChart.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
// builder.Services.AddSingleton<MelonService>();
builder.Services.AddScoped<OpenAIClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var endpoint = new Uri(config["OpenAI:Endpoint"]);
    var credential = new AzureKeyCredential(config["OpenAI:ApiKey"]);
    var client = new OpenAIClient(endpoint, credential);
    return client;
});
builder.Services.AddScoped<Kernel>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var kernel = Kernel.CreateBuilder()
                       .AddAzureOpenAIChatCompletion(
                           deploymentName: config["OpenAI:DeploymentName"],
                           endpoint: config["OpenAI:Endpoint"],
                           apiKey: config["OpenAI:ApiKey"])
                       .Build();

    kernel.ImportPluginFromType<AddMelonChartPlugin>();

    return kernel;
});

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0050

builder.Services.AddSingleton<ISemanticTextMemory>(sp=>{
    var config = sp.GetRequiredService<IConfiguration>();
    var memory = new MemoryBuilder()
        .WithAzureOpenAITextEmbeddingGeneration(
            deploymentName: "model-textembeddingada002-2",
            endpoint: config["OpenAI:Endpoint"],
            apiKey: config["OpenAI:ApiKey"])
        .WithMemoryStore(new VolatileMemoryStore())
        .Build();
    return memory;
});

#pragma warning restore SKEXP0050
#pragma warning restore SKEXP0010
#pragma warning restore SKEXP0001

var jso = new JsonSerializerOptions()
{
    WriteIndented = false,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
};
builder.Services.AddSingleton(jso);
builder.Services.AddHttpClient<MelonService>("memory");

var app = builder.Build();
app.MapDefaultEndpoints();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.MapPost("/melonchart", async (NewQuestion question, MelonService service) =>
{
    var answer = await service.AskBot(question);
    return answer;
}).WithOpenApi();

app.Run();

record NewQuestion(string Question);

#pragma warning disable SKEXP0001
internal class MelonService(HttpClient http, Kernel kernel, ISemanticTextMemory memory, JsonSerializerOptions jso)
{
    public async Task<string> AskBot(NewQuestion q)
    {
        var prompts = kernel.ImportPluginFromPromptDirectory("Prompts");
        var getIntent = prompts["GetIntent"];
        var refineQuestion = prompts["RefineQuestion"];
        var refineResult = prompts["RefineResult"];
        var intent = await kernel.InvokeAsync<string>(
            function: getIntent,
            arguments: new KernelArguments()
            {
                { "input", q.Question }
            }
        );
        Console.WriteLine($"Intent: {intent}");
        var refined = await kernel.InvokeAsync<string>(
            function: refineQuestion,
            arguments: new KernelArguments()
            {
                { "input", q.Question },
                { "intent", intent }
            }
        );
        Console.WriteLine($"Refined: {refined}");
        await kernel.InvokeAsync(
            pluginName: nameof(AddMelonChartPlugin),
            functionName: nameof(AddMelonChartPlugin.AddChart),
            arguments: new KernelArguments()
            {
                { "memory", memory },
                { "http", http },
                { "jso", jso },
            }
        );
        var results = await kernel.InvokeAsync(
            pluginName: nameof(AddMelonChartPlugin),
            functionName: nameof(AddMelonChartPlugin.FindSongs),
            arguments: new KernelArguments()
            {
                { "memory", memory },
                { "question", refined },
                { "jso", jso },
            }
        );
        var items = results.GetValue<List<ChartItem>>();
        if (items.Any()==false)
        {
            return "No results found";
        }
        var data = results.GetValue<List<ChartItem>>()?.Select(p=>JsonSerializer.Serialize(p, jso))
            .Aggregate((x,y)=>$"{x}\n{y}");
        Console.WriteLine(data);
        refined = await kernel.InvokeAsync<string>(
            function: refineResult,
            arguments: new KernelArguments()
            {
                { "input", data },
                { "intent", intent }
            }
        );
        Console.WriteLine($"Refined result: {refined}");
        return refined;
    }
}
#pragma warning restore SKEXP0001