using System.Diagnostics;

namespace TestPlaywright;

public class RunProgramAutomatically
{
    private Process? _serverProcess;

    public async Task StartServer()
    {
        //Starts server
        _serverProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments =
                    "run --project \"../../../../Presentation/Presentation/Presentation.csproj\" --urls https://localhost:5001",
                UseShellExecute = true,
                CreateNoWindow = false,
            }
        };
        try
        {
            _serverProcess.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error starting server: " + ex.Message);
        }

        //Waits till server is finished building
        var client = new HttpClient();
        var isServerReady = false;
        while (!isServerReady)
        {
            try
            {
                var response = await client.GetAsync("https://localhost:5001/health"); // Replace with your server's health check endpoint
                if (response.IsSuccessStatusCode)
                {
                    isServerReady = true;
                }
            }
            catch (Exception)
            {
                // Ignore exceptions
            }
            if (!isServerReady)
            {
                // Wait before trying again
                await Task.Delay(1000);
            }
        }
    }

    public void StopServer()
    {
        _serverProcess?.Kill();
        _serverProcess = null;
    }
}
