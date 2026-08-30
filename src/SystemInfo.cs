using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Net;

namespace ServerManager;

public class SystemInfo
{
	public static string GetLocalIPAddresses()
	{
		IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
		return string.Join(", ", localIPs);
	}

	public static string GetUFWStatus()
	{
		var process = new System.Diagnostics.Process
		{
			StartInfo = new System.Diagnostics.ProcessStartInfo
			{
				FileName = "sudo",
				Arguments = "/usr/sbin/ufw status",
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			}
		};
		process.Start();
		string output = process.StandardOutput.ReadToEnd();
		process.WaitForExit();
		switch (output)
		{
			case "Status: inactive":
				return "Disabled";
			case "Status: active":
				return "Enabled";
			default:
				return "Unknown";
		}
	}

	public static string GetCPU()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			var lines = File.ReadAllLines("/proc/cpuinfo");
			var modelNameLine = lines.FirstOrDefault(l => l.StartsWith("model name"));
			if (modelNameLine != null)
			{
				return modelNameLine.Split(':', 2)[1].Trim();
			}
		}
		return "Unknown";
	}

	public static string GetRAM()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			var lines = File.ReadAllLines("/proc/meminfo");
			var memTotalLine = lines.FirstOrDefault(l => l.StartsWith("MemTotal:"));
			if (memTotalLine != null)
			{
				var kb = long.Parse(memTotalLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).First());

				return FormatMemorySize(kb);
			}
		}
		return "Unknown";
	}

    public static string FormatMemorySize(long kb)
    {
        string[] sizes = { "KB", "MB", "GB", "TB" };
        double len = kb;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    public static string GetTotalStorageBytes(string path = "/")
	{
		try
		{
			var driveInfo = new DriveInfo(path);
            return FormatStorageSize(driveInfo.TotalSize);
        }
		catch
		{
			return "Unknown";
		}
	}

    public static string FormatStorageSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
