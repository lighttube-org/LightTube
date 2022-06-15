using System;
using System.IO;
using YamlDotNet.Serialization;

namespace LightTube
{
	public class Configuration
	{
		public static Configuration Instance { get; private set; }

		public InterfaceConfig Interface = new();
		public CredentialsConfig Credentials = new();
		public DatabaseConfig Database = new();

		public static void LoadConfiguration()
		{
			string path = null;
			string cwdConfigPath = Path.Join(Environment.CurrentDirectory, "lighttube.yml");
			if (File.Exists(cwdConfigPath))
			{
				path = cwdConfigPath;
			}
			else if (OperatingSystem.IsLinux())
			{
				path = "/etc/lighttube.yml";
			}
			else if (OperatingSystem.IsWindows())
			{
				string appdataFolder = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
					"lighttube", "config.yml");
				path = appdataFolder;
			}
			else
			{
				Console.WriteLine($"Unknown operating system {Environment.OSVersion}, using the current working dir for the configuration ({cwdConfigPath}).");
			}

			path ??= cwdConfigPath;
			if (!File.Exists(path))
			{
				CreateConfigurationFile(path);
			}
			
			Console.WriteLine($"Reading configuration from {path}");
			Instance = new Deserializer().Deserialize<Configuration>(File.ReadAllText(path));
		}

		private static void CreateConfigurationFile(string path)
		{
			Console.WriteLine($"Creating configuration file at {path}");
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			using FileStream stream = File.Create(path);
			using TextWriter writer = new StreamWriter(stream);
			writer.WriteLine("# see https://gitlab.com/kuylar/lighttube/-/blob/master/CONFIGURATION.md");
			writer.WriteLine();
			new Serializer().Serialize(writer, new Configuration());
			writer.Close();
			stream.Close();
			Console.WriteLine("Created configuration file");
		}
	}

	public class InterfaceConfig
	{
		public string MessageOfTheDay = "Search something to get started!";
	}

	public class CredentialsConfig
	{
		public bool UseCredentials = false;
		public string Sapisid = null;
		public string Psid = null;

		public bool CanUseAuthorizedEndpoints() => UseCredentials && Sapisid != null && Psid != null;
	}

	public class DatabaseConfig
	{
		public string MongoConnectionString = null;
	}
}