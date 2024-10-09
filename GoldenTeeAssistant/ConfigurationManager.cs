using Newtonsoft.Json;
using System.Diagnostics;

namespace GoldenTeeAssistant
{

	public static class ConfigurationManager
	{
		public static Configuration MainConfig { get; set; } = new Configuration();

		public static void LoadConfig()
		{
			string exePath = Process.GetCurrentProcess().MainModule.FileName;
			string exeDir = Path.GetDirectoryName(exePath);
			string TeknoparrotAutoXinputConfigFile = Path.Combine(exeDir, "GoldenTeeAssistant.json");
			if (File.Exists(TeknoparrotAutoXinputConfigFile))
			{
				MainConfig = new Configuration(File.ReadAllText(TeknoparrotAutoXinputConfigFile));
			}
		}

		public static void SaveConfig()
		{
			string exePath = Process.GetCurrentProcess().MainModule.FileName;
			string exeDir = Path.GetDirectoryName(exePath);
			string TeknoparrotAutoXinputConfigFile = Path.Combine(exeDir, "GoldenTeeAssistant.json");
			File.WriteAllText(TeknoparrotAutoXinputConfigFile, MainConfig.Serialize());
		}
	}
	public class Configuration
	{
		public int mouseSpeed { get; set; } = 4;

		public Configuration()
		{

		}

		public Configuration(string json)
		{
			try
			{
				Configuration DeserializeData = JsonConvert.DeserializeObject<Configuration>(json);
				this.mouseSpeed = DeserializeData.mouseSpeed;

			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		public string Serialize()
		{
			string json = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
			return json;
		}
	}


}
