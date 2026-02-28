using Borderless1942;
using System.Diagnostics;

var monitorBounds = Win32Extensions.GetPrimaryMonitor().GetBounds();
var defaultWidth = monitorBounds.Width;
var defaultHeight = monitorBounds.Height;
var width = defaultWidth;
var height = defaultHeight;
var skipConfigEdits = false;

List<string> additionalArgs = new();

for (int i = 0; i < args.Length; i++)
{
	if (args[i].Equals("-width", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
	{
		if (int.TryParse(args[i + 1], out int parsedWidth))
		{
			width = parsedWidth;
		}
	}
	else if (args[i].Equals("-height", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
	{
		if (int.TryParse(args[i + 1], out int parsedHeight))
		{
			height = parsedHeight;
		}
	}
	else if (args[i].Equals("-noedit", StringComparison.OrdinalIgnoreCase))
	{
		skipConfigEdits = true;
	}
	else
		additionalArgs.Add(args[i]);
}

if (!skipConfigEdits)
{
	// Update Video.con files if they exist
	var modsPath = Path.Combine(Environment.CurrentDirectory, "Mods", "bf1942", "Settings");
	if (Directory.Exists(modsPath))
	{
		// Update resolution in profile Video.con files
		var videoConFiles = Directory.GetFiles(Path.Combine(modsPath, "Profiles"), "Video.con", SearchOption.AllDirectories);
		foreach (var videoConFile in videoConFiles)
		{
			var lines = File.ReadAllLines(videoConFile);
			for (int i = 0; i < lines.Length; i++)
			{
				if (lines[i].StartsWith("game.setGameDisplayMode"))
				{
					lines[i] = $"game.setGameDisplayMode {width} {height} 32 60";
				}
			}
			File.WriteAllLines(videoConFile, lines);
			Console.WriteLine($"[Borderless1942]: [{DateTime.Now:yyyy-MM-dd - hh:mm:ss tt}] [Updated resolution in {videoConFile} to {width}x{height}]");
		}

		// Update fullscreen setting in VideoDefault.con
		var videoDefaultPath = Path.Combine(modsPath, "VideoDefault.con");
		if (File.Exists(videoDefaultPath))
		{
			var lines = File.ReadAllLines(videoDefaultPath);
			for (int i = 0; i < lines.Length; i++)
			{
				if (lines[i].StartsWith("renderer.setFullScreen"))
				{
					lines[i] = "renderer.setFullScreen 0";
				}
			}
			File.WriteAllLines(videoDefaultPath, lines);
			Console.WriteLine($"[Borderless1942]: [{DateTime.Now:yyyy-MM-dd - hh:mm:ss tt}] [Updated fullscreen setting in VideoDefault.con]");
		}
	}
}

// Start Process
var processStartInfo = new ProcessStartInfo
{
	FileName = @"BF1942.exe",
};

foreach (var arg in additionalArgs)
	processStartInfo.ArgumentList.Add(arg);

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
