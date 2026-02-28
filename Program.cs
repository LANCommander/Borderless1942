using Borderless1942;
using System.Diagnostics;

// Start Process
var processStartInfo = new ProcessStartInfo
{
	FileName = @"BF1942.exe"
};
foreach (var arg in args)
{
	processStartInfo.ArgumentList.Add(arg);
}
var process = Process.Start(processStartInfo)!;
var keepAlive = true;
Console.WriteLine($"[Borderless1942]: [{DateTime.Now:yyyy-MM-dd - hh:mm:ss tt}] [BF1942 Process Has Started] [{process.Id}]");

// Wait for initial window handle
var window = await process.WaitForMainWindowAsync();
window.RemoveBorders();

// Keep itself alive until detected otherwise
while (keepAlive)
{
MainLoop:
	UpdateWindowPosition(window, width, height);
	await Task.Delay(100);

	if (process.HasExited)
	{
		var oldProcessId = process.Id;
		var retryCount = 0;
		while (retryCount < 3)
		{
			var matchingProcesses = Process.GetProcessesByName("BF1942");
			if (matchingProcesses.Length == 1)
			{
				process = matchingProcesses[0];
				if (process.HasExited)
				{
					break;
				}
				Console.WriteLine($"[Borderless1942]: [{DateTime.Now:yyyy-MM-dd - hh:mm:ss tt}] [BF1942 Process Has Changed] [{oldProcessId} -> {process.Id}]");
				window = await process.WaitForMainWindowAsync();
				window.RemoveBorders();
				goto MainLoop;
			}
			retryCount++;
			await Task.Delay(TimeSpan.FromSeconds(1));
		}
		keepAlive = false;
		Console.WriteLine($"[Borderless1942]: [{DateTime.Now:yyyy-MM-dd - hh:mm:ss tt}] [BF1942 Process Has Exited]");
    }
}

static void UpdateWindowPosition(Window window, int width, int height)
{
	var monitorBounds = window.GetCurrentMonitor().GetBounds();
	var x = monitorBounds.Left + (monitorBounds.Width - width) / 2;
	var y = monitorBounds.Top + (monitorBounds.Height - height) / 2;
	window.SetPosition(x, y, width, height);
}
