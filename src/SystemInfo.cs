using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Net;

namespace ServerManager;

public class SystemInfo
{
    #region System Information
    public static string GetLocalIPAddresses()
	{
		IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
        var ipv4Addresses = localIPs
            .Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .ToList();
        var ipv6Addresses = localIPs
            .Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            .ToList();
        string ipv4Result = ipv4Addresses.Count switch
        {
            0 => IPAddress.Loopback.ToString(),
            1 => ipv4Addresses[0].ToString(),
            _ => "Multiple IP Addresses"
        };
        if (ipv6Addresses.Count > 0)
        {
            return string.Join(", ", ipv4Result) + ", Compatible IPv6";
        }
        else
        {
            return string.Join(", ", ipv4Result);
        }
	}

	// ! This implementation depends on the system having the following file:
	// ! /etc/sudoers.d/ufwstatus
	// ! To permit getting information without a password prompt.
	// ! If not present, program will bug out due to sudo blocking the process.
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

	public static string GetAppArmorStatus()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			var grubLSMLine = File.ReadAllLines("/etc/default/grub").FirstOrDefault(line => line.StartsWith("GRUB_CMDLINE_LINUX="));
			switch (grubLSMLine)
			{
				case string s when s.Contains("lsm=landlock,lockdown,yama,integrity,apparmor,bpf"):
					return "Enabled";
				default:
					return "Disabled";
			}
		}
		return "Unknown";
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

    public static string GetServices()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "systemctl",
                    Arguments = "list-units --type=service",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }
        return "Unknown";
    }
    #endregion
    #region Format
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
    #endregion
}
