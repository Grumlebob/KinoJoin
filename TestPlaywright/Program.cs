// See https://aka.ms/new-console-template for more information

//Global settings

using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
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
    await using var browser = await playwright.Chromium.LaunchAsync(
        new BrowserTypeLaunchOptions { Headless = isHeadless, SlowMo = slowDown }
    );
    var context = await browser.NewContextAsync();

    var page = await context.NewPageAsync();

    //Get Connection to KinoContext
    var builder = new ConfigurationBuilder();
    builder.AddUserSecrets<Program>();
    IConfiguration configuration = builder.Build();
    var optionsBuilder = new DbContextOptionsBuilder<KinoContext>();
    optionsBuilder.UseNpgsql(configuration["TestDatabaseConnection"]);
    using var kinoContext = new KinoContext(optionsBuilder.Options);

    //Get all cinemas and print name
    var cinemas = await kinoContext.Cinemas.ToListAsync();
    Console.WriteLine("PRINTER CINEMAS");
    foreach (var cinema in cinemas)
    {
        Console.WriteLine(cinema.Name);
    }

    //Endpoints
    await page.GotoAsync("https://localhost:5001/joinCreate/cinemas=53&sort=most_purchased");
    //Var førsteShowtimeId
    await page.Locator("[id=\"\\33 61923\"]").ClickAsync();
    await page.GetByRole(AriaRole.Button, new() { Name = "Opret Event" }).ClickAsync();
    await page.GetByPlaceholder("Titel").FillAsync("Min seje bingoaften");
    await page.GetByPlaceholder("Beskrivelse (valgfrit)").ClickAsync();
    await page.GetByPlaceholder("Beskrivelse (valgfrit)").ClickAsync();
    await page.GetByPlaceholder("Beskrivelse (valgfrit)")
        .FillAsync("Bingopladerne er rækkerne, brikkerne er sekundet folk nyser");
    await page.GetByRole(AriaRole.Button, new() { Name = "Opret", Exact = true }).ClickAsync();
    await page.Locator("#eventDialog")
        .GetByRole(AriaRole.Button, new() { Name = "Opret" })
        .ClickAsync();
    await page.GetByRole(AriaRole.Link, new() { Name = "https://localhost:5001/" }).ClickAsync();
    await page.GetByRole(AriaRole.Button, new() { Name = "Uden login" }).ClickAsync();
    await page.GetByPlaceholder("Indtast dit navn").ClickAsync();
    await page.GetByPlaceholder("Indtast dit navn").FillAsync("Ny deltager");
    await page.GetByPlaceholder("Note (valgfrit)").ClickAsync();
    await page.GetByPlaceholder("Note (valgfrit)").FillAsync("elsker bingoaften i biografen");
    await page.GetByRole(AriaRole.Button, new() { Name = "Send svar" }).ClickAsync();
    await page.GetByRole(AriaRole.Button, new() { Name = "Ja" }).ClickAsync();

    return true;
}
