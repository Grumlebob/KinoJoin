// See https://aka.ms/new-console-template for more information

//Global settings

using Microsoft.Playwright;
using TestPlaywright;

bool isHeadless = false;
int slowDown = 1000;

//SurveyCraft.Server has to be running for playwright to work, this does the setup
var serverHandler = new RunProgramAutomatically();
try
{
    await serverHandler.StartServer();
    var finishedPace = await KinoDemo();
    if (finishedPace)
    {
        Console.WriteLine("Made it until the end of Kino playwright demo");
    }
    else
    {
        Console.WriteLine("Failed Playwright");
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
finally
{
    serverHandler.StopServer();
}

async Task<bool> KinoDemo()
{
    using var playwright = await Playwright.CreateAsync();
    await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
    {
        Headless = isHeadless,
        SlowMo = slowDown
    });
    var context = await browser.NewContextAsync();

    var KinoJoin = await context.NewPageAsync();

    await KinoJoin.GotoAsync("https://localhost:5001/");
    await KinoJoin.GetByRole(AriaRole.Button, new() { Name = "Go to Monkey" }).ClickAsync();
    await KinoJoin.GetByRole(AriaRole.Button, new() { Name = "Create Monkey" }).ClickAsync();
    await KinoJoin.GetByRole(AriaRole.Button, new() { Name = "Create Monkey" }).ClickAsync();
    await KinoJoin.GetByRole(AriaRole.Button, new() { Name = "Create Monkey" }).ClickAsync();
    await KinoJoin.GetByRole(AriaRole.Button, new() { Name = "Get first monkey" }).ClickAsync();

    return true;
}