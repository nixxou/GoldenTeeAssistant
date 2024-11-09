using BsDiff;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Management;
using System.Runtime.InteropServices;
using System.Xml;
using System.Drawing;
using EnumerationOptions = System.IO.EnumerationOptions;

namespace GoldenTeeAssistant
{
	internal static class Program
	{
		private static NotifyIcon notifyIcon;
		private static ContextMenuStrip contextMenu;

		private static readonly string mutexName = "Global\\GTA_Launched";
		private static readonly Mutex Mutex = new Mutex();

		static Thread monitoringThread;
		static bool stopMonitoring = false;
		static string programDirectory = AppDomain.CurrentDomain.BaseDirectory.Trim('\\');
		static Process budgieLoaderProcess = null;

		public static string _postgreBinPath = Path.Combine(programDirectory, @"GoldenTeeAssistant\8.3\bin");
		public static string _postgreDataPath = Path.Combine(programDirectory, @"GoldenTeeAssistant\8.3\data");
		public static string _databaseSourceFile = @"";
		public static string _expectedMD5 = "";
		public static long _expectedSize = -1;
		public static string _tpProfile = "";
		public static string _databaseName = "";
		public static string _patchToUse = "";
		private static int? databaseProcessId = null;
		private static string newDatabaseFile = "";
		public static List<string> _possibleBddSourcePath = new List<string>();


		private static uint _mouseSpeedSaved = 0;
		private static bool _mousePrecisionSaved = false;

		// Constantes pour l'API Windows
		private const uint SPI_SETMOUSESPEED = 0x0071;
		private const uint SPI_GETMOUSESPEED = 0x0070; // Pour obtenir la vitesse de la souris
		private const uint SPIF_SENDCHANGE = 0x02; // Indique de mettre à jour les paramètres de la souris
		public const UInt32 SPI_SETMOUSE = 0x0004;
		public const UInt32 SPI_GETMOUSE = 0x0003;

		[DllImport("User32.dll")]
		static extern bool SystemParametersInfo(uint uiAction, uint uiParam, uint pvParam, uint fWinIni);
		[DllImport("user32.dll")]
		private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref uint pvParam, uint fWinIni);
		[DllImport("user32.dll", EntryPoint = "SystemParametersInfo", SetLastError = true)]
		public static extern bool SystemParametersInfoGet(uint action, uint param, IntPtr vparam, uint fWinIni);
		[DllImport("user32.dll", EntryPoint = "SystemParametersInfo", SetLastError = true)]
		public static extern bool SystemParametersInfoSet(uint action, uint param, IntPtr vparam, uint fWinIni);


		public static string gamePath = "";
		public static bool nocheck = false;
		public static bool install = false;
		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			if (args.Length > 0 && args[0] == "daemon")
			{
				ApplicationConfiguration.Initialize();
				Application.Run(new Form1());
				return;
			}

			if (args.Length > 0 && Directory.Exists(args.Last())) gamePath = args.Last();
			else gamePath = programDirectory;

			install = args.Any(arg => string.Equals(arg, "--install", StringComparison.OrdinalIgnoreCase));

			if (IsPortOpen("127.0.0&.1", 5433, 1))
			{
				if (!nocheck) MessageBox.Show("Database is still running");
				Environment.Exit(1);
			}


			if (!File.Exists(Path.Combine(gamePath, "game.bin")))
			{
				MessageBox.Show("Missing game.bin, the program must be in the same folder as the game");
				Environment.Exit(1);
			}
			else
			{
				string md5game = MD5Calc(Path.Combine(gamePath, "game.bin"));
				if(md5game == "51ED3A7313EB5A5B8D34E3715F5C7BC8")
				{
					_databaseSourceFile = @"1922-postgresql_database-GameDB-backup";
					_expectedMD5 = "A825B07F066CA3124F8D7FABE4F8CD5F";
					_expectedSize = 1583616;
					_tpProfile = "GoldenTeeLive2006.xml";
					_databaseName = "GameDB06";
					_patchToUse = "bddpatch.bdf";

					string bf_try1 = Path.GetFullPath(Path.Combine(gamePath, "pg_backup", "2017-12-18", _databaseSourceFile));
					string bf_try2 = Path.GetFullPath(Path.Combine(gamePath, "..", "6", "pg_backup", "2017-12-18", _databaseSourceFile));
					string bf_try3 = Path.GetFullPath(Path.Combine(gamePath, _databaseSourceFile));
					_possibleBddSourcePath.Add(bf_try1);
					_possibleBddSourcePath.Add(bf_try2);
					_possibleBddSourcePath.Add(bf_try3);
				}
				if (md5game == "2A3E5F85E78B6D7BC6C9F4AF6CB13FFD")
				{
					_databaseSourceFile = @"2043-postgresql_database-GameDB-backup";
					_expectedMD5 = "915802A03CF5C60B62E1F7B7AB7C3716";
					_expectedSize = 2157568;
					_tpProfile = "GoldenTeeLive2007.xml";
					_databaseName = "GameDB07";
					_patchToUse = "bddpatch2007.bdf";

					string bf_try1 = Path.GetFullPath(Path.Combine(gamePath, "pg_backup", "2017-12-18", _databaseSourceFile));
					string bf_try3 = Path.GetFullPath(Path.Combine(gamePath, _databaseSourceFile));
					_possibleBddSourcePath.Add(bf_try1);
					_possibleBddSourcePath.Add(bf_try3);
				}
				if (md5game == "3E183BEB550BDF16AD1CC44855872D80")
				{
					_databaseSourceFile = @"1433-postgresql_database-GameDB-backup";
					_expectedMD5 = "F9EE2C1ADBF5BEDD0EA9ABA7F5C0F3CF";
					_expectedSize = 6951424;
					_tpProfile = "PowerPuttLive2012.xml";
					_databaseName = "GameDBPP12";
					_patchToUse = "bddpatchpp12.bdf";

					string bf_try1 = Path.GetFullPath(Path.Combine(gamePath, "pg_backup", _databaseSourceFile));
					string bf_try3 = Path.GetFullPath(Path.Combine(gamePath, _databaseSourceFile));
					_possibleBddSourcePath.Add(bf_try1);
					_possibleBddSourcePath.Add(bf_try3);
				}
				if (md5game == "498FC82C13175499C4B3982DCF3EBB75")
				{
					_databaseSourceFile = @"2222-postgresql_database-GameDB-backup";
					_expectedMD5 = "92E17D665D8EF3DE08F36A8CC9466D47";
					_expectedSize = 5641728;
					_tpProfile = "SilverStrikeBowlingLive.xml";
					_databaseName = "GapeDBSSB";
					_patchToUse = "bddpatchssb.bdf";

					string bf_try1 = Path.GetFullPath(Path.Combine(gamePath, "pg_backup", "2024-10-12", _databaseSourceFile));
					string bf_try3 = Path.GetFullPath(Path.Combine(gamePath, _databaseSourceFile));
					_possibleBddSourcePath.Add(bf_try1);
					_possibleBddSourcePath.Add(bf_try3);
				}
				if (_databaseName == "")
				{
					MessageBox.Show("game.bin does not match any know md5");
					Environment.Exit(1);
				}
			}

			if(_tpProfile == "GoldenTeeLive2006.xml")
			{
				if (!File.Exists(Path.Combine(gamePath, "assetlist.txt")))
				{
					if (File.Exists(Path.Combine(programDirectory, "..", "4", "assetlist.txt")))
					{
						DialogResult result = MessageBox.Show("Do you want to setup the game folder", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
						if (result == DialogResult.Yes)
						{
							string sourceDir = Path.Combine(Directory.GetParent(gamePath).FullName, "4");
							string destDir = gamePath;
							MoveDirectory(sourceDir, destDir, true);
						}
						else
						{
							MessageBox.Show($"Exit");
							Environment.Exit(1);
						}
					}
					else
					{
						MessageBox.Show($"Some game files seems missing.");
						Environment.Exit(1);
					}
				}
			}

			if (!Directory.Exists(_postgreBinPath))
			{
				MessageBox.Show("Missing Postgre folder");
				Environment.Exit(1);
			}
			
			if (install)
			{
				var databaseExist = CheckDatabaseExistsOffline();
				if (databaseExist == 0)
				{
					string backupFile = "";

					foreach(var possiblePath in _possibleBddSourcePath)
					{
						if(File.Exists(possiblePath) && MD5Calc(possiblePath) == _expectedMD5)
						{
							backupFile = possiblePath;
							break;
						}
					}
					if (backupFile == "")
					{
						var filePaths = Directory.EnumerateFiles(gamePath, "*", new EnumerationOptions
						{
							IgnoreInaccessible = true,
							RecurseSubdirectories = true
						});
						foreach (var filePath in filePaths)
						{
							if (new FileInfo(filePath).Length == _expectedSize && MD5Calc(filePath) == _expectedMD5)
							{
								backupFile = filePath;
								break;
							}
						}
					}
					if (backupFile == "")
					{
						MessageBox.Show("Abord : Can't find " + _databaseSourceFile + " with MD5 : " + _expectedMD5);
						Environment.Exit(1);
					}
					newDatabaseFile = PatchDatabase(backupFile);
					if (newDatabaseFile == "")
					{
						MessageBox.Show("error processing database");
						Environment.Exit(1);
					}

					StartDatabase();
					bool serverOnline = IsPortOpen("127.0.0.1", 5433, 1);
					if (!serverOnline)
					{
						bool online = false;
						int nbtry = 0;
						while (!online && nbtry < 12)
						{
							Thread.Sleep(500);
							online = IsPortOpen("127.0.0.1", 5433, 1);
						}
						if (!online)
						{
							MessageBox.Show("Cant start database");
							Environment.Exit(1);
						}
						Thread.Sleep(2000);

					}


					InstallDatabase(newDatabaseFile);
					Thread.Sleep(1000);

					StopDatabase();
					serverOnline = IsPortOpen("127.0.0.1", 5433, 1);
					if (serverOnline)
					{
						bool online = true;
						int nbtry = 0;
						while (online && nbtry < 12)
						{
							Thread.Sleep(500);
							online = IsPortOpen("127.0.0.1", 5433, 1);
						}
						if (online)
						{
							MessageBox.Show("Cant stop database");
							Environment.Exit(1);
						}
					}
				}
				string selfExe = Process.GetCurrentProcess().MainModule.FileName;
				string exePath = selfExe;
				string exeDir = Path.GetDirectoryName(exePath);
				Process process = new Process();
				process.StartInfo.FileName = selfExe;
				process.StartInfo.Arguments = gamePath;
				process.StartInfo.WorkingDirectory = exeDir;
				process.StartInfo.UseShellExecute = true;
				process.Start();
				Environment.Exit(0);
				return;
			}

			using (Mutex mutex = new Mutex(false, mutexName, out bool createdNew))
			{

				if (!createdNew)
				{
					return;
				}

				try
				{
					_mouseSpeedSaved = GetMouseSpeed();
					_mousePrecisionSaved = GetPointerPrecision();
				}
				catch { }
				
				try
				{
					// Code principal de l'application
					bool serverOnline = IsPortOpen("127.0.0.1", 5433, 1);
					if (!serverOnline)
					{
						StartDatabase();

						bool online = false;
						int nbtry = 0;
						while (!online && nbtry < 6)
						{
							Thread.Sleep(500);
							online = IsPortOpen("127.0.0.1", 5433, 1);
						}
						Thread.Sleep(2000);
					}
					var databaseExist = CheckDatabaseExists();
					if (databaseExist == -1)
					{
						MessageBox.Show("Error checking database");
						return;
					}
					if (databaseExist == 0)
					{
						string backupFile = "";
						{
							foreach (var possiblePath in _possibleBddSourcePath)
							{
								if (File.Exists(possiblePath) && MD5Calc(possiblePath) == _expectedMD5)
								{
									backupFile = possiblePath;
									break;
								}
							}
							if (backupFile == "")
							{
								var filePaths = Directory.EnumerateFiles(gamePath, "*", new EnumerationOptions
								{
									IgnoreInaccessible = true,
									RecurseSubdirectories = true
								});
								foreach (var filePath in filePaths)
								{
									if (new FileInfo(filePath).Length == _expectedSize && MD5Calc(filePath) == _expectedMD5)
									{
										backupFile = filePath;
										break;
									}
								}
							}
							if(backupFile == "")
							{
								using (OpenFileDialog openFileDialog = new OpenFileDialog())
								{
									openFileDialog.Filter = $"{_databaseSourceFile}|{_databaseSourceFile}";
									openFileDialog.Title = $"Select {_databaseSourceFile}";

									string sourcefolder = programDirectory;
									openFileDialog.InitialDirectory = Path.GetFullPath(sourcefolder);

									// Show the dialog and get result
									if (openFileDialog.ShowDialog() == DialogResult.OK)
									{
										string md5Source = MD5Calc(openFileDialog.FileName);
										if (md5Source != _expectedMD5)
										{
											MessageBox.Show($"Invalid MD5 ({md5Source}), Expected : {_expectedMD5}");
											return;
										}
										backupFile = openFileDialog.FileName;
									}
									else
									{
										MessageBox.Show("No file selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
										return;
									}
								}
							}
							if(backupFile != "")
							{
								newDatabaseFile = PatchDatabase(backupFile);
								//MessageBox.Show($"debug : new database = {newDatabaseFile}");
								if (newDatabaseFile == "")
								{
									MessageBox.Show("error processing database");
									return;
								}
								InstallDatabase(newDatabaseFile);
							}
							if (newDatabaseFile != "")
							{
								DialogResult result = MessageBox.Show("Do you want to update your TP User Profile ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
								if (result == DialogResult.Yes)
								{
									using (OpenFileDialog openFileDialog = new OpenFileDialog())
									{
										openFileDialog.Filter = $"{_tpProfile}|{_tpProfile}";
										openFileDialog.Title = $"Select {_tpProfile}";
										openFileDialog.InitialDirectory = Path.GetFullPath(programDirectory);

										// Show the dialog and get result
										if (openFileDialog.ShowDialog() == DialogResult.OK)
										{
											string tpUserProfile = openFileDialog.FileName;
											string parentFolder = Path.GetFileName(Path.GetDirectoryName(tpUserProfile));


											if (parentFolder.ToLower() == "gameprofiles")
											{
												string newtpUserProfile = Path.Combine(Directory.GetParent(tpUserProfile).Parent.FullName, "UserProfiles", Path.GetFileName(tpUserProfile));
												if (File.Exists(newtpUserProfile))
												{
													tpUserProfile = newtpUserProfile;
													parentFolder = Path.GetFileName(Path.GetDirectoryName(tpUserProfile));
												}
											}

											if (parentFolder.ToLower() == "userprofiles")
											{
												XmlDocument xmlDoc = new XmlDocument();
												xmlDoc.Load(tpUserProfile);
												XmlNode gamePathNode = xmlDoc.SelectSingleNode("/GameProfile/GamePath");
												if (gamePathNode == null)
												{
													gamePathNode = xmlDoc.CreateElement("GamePath");
													xmlDoc.DocumentElement.AppendChild(gamePathNode);
												}
												gamePathNode.InnerText = Path.Combine(programDirectory, "game.bin");

												{
													XmlNode Node = xmlDoc.SelectSingleNode("/GameProfile/ConfigValues/FieldInformation[FieldName='Path']/FieldValue");
													if (Node != null) Node.InnerText = _postgreBinPath;
												}
												{
													XmlNode Node = xmlDoc.SelectSingleNode("/GameProfile/ConfigValues/FieldInformation[FieldName='Address']/FieldValue");
													if (Node != null) Node.InnerText = "127.0.0.1";
												}
												{
													XmlNode Node = xmlDoc.SelectSingleNode("/GameProfile/ConfigValues/FieldInformation[FieldName='Port']/FieldValue");
													if (Node != null) Node.InnerText = "5433";
												}
												{
													XmlNode Node = xmlDoc.SelectSingleNode("/GameProfile/ConfigValues/FieldInformation[FieldName='DbName']/FieldValue");
													if (Node != null) Node.InnerText = _databaseName;
												}
												{
													XmlNode Node = xmlDoc.SelectSingleNode("/GameProfile/ConfigValues/FieldInformation[FieldName='User']/FieldValue");
													if (Node != null) Node.InnerText = "postgres";
												}
												{
													XmlNode Node = xmlDoc.SelectSingleNode("/GameProfile/ConfigValues/FieldInformation[FieldName='Pass']/FieldValue");
													if (Node != null) Node.InnerText = "teknoparrot";
												}
												xmlDoc.Save(tpUserProfile);
											}
											else
											{
												MessageBox.Show($"You must select {_tpProfile} inside your teknoparrot UserProfiles folder");
											}

										}
									}
								}
							}
						}
					}

					
					try
					{
						Thread notifyIconThread = new Thread(() =>
						{
							// Création du menu contextuel
							contextMenu = new ContextMenuStrip();
							ToolStripMenuItem closeMenuItem = new ToolStripMenuItem("Close");
							closeMenuItem.Click += CloseMenuItem_Click;
							contextMenu.Items.Add(closeMenuItem);

							// Association du menu contextuel au NotifyIcon
							notifyIcon = new NotifyIcon
							{
								Icon = SystemIcons.Warning,
								Visible = true,
								Text = "Waiting for Teknoparrot",
								ContextMenuStrip = contextMenu // Associer le ContextMenuStrip ici
							};

							// Lancer la boucle d'événements pour gérer le NotifyIcon
							Application.Run();
						});
						notifyIconThread.IsBackground = true;
						notifyIconThread.Start();
					}
					catch { }
					
					StartProcessMonitoring();
					int nbtryMonitoring = 0;
					while (budgieLoaderProcess == null && nbtryMonitoring < 600 && !stopMonitoring)
					{
						Thread.Sleep(100);
						nbtryMonitoring++;
					}
					if(budgieLoaderProcess == null)
					{
						notifyIcon.Visible = false;
						notifyIcon.Dispose();
						StopProcessMonitoring();
						return;
					}
					else
					{

						notifyIcon.Visible = false;
						string selfExe = Process.GetCurrentProcess().MainModule.FileName;
						string exePath = selfExe;
						string exeDir = Path.GetDirectoryName(exePath);
						Process process = new Process();
						process.StartInfo.FileName = selfExe;
						process.StartInfo.Arguments = "daemon";
						process.StartInfo.WorkingDirectory = exeDir;
						process.StartInfo.UseShellExecute = true;
						process.Start();
						
						while (budgieLoaderProcess != null && !budgieLoaderProcess.HasExited)
						{
							Thread.Sleep(1000);
						}
						return;
					}
				}
				catch (Exception ex)
				{
					// Gérer les exceptions ici si nécessaire
					Console.WriteLine($"An error occurred: {ex.Message}");
				}
				finally
				 {
					if (createdNew)
					{
						try
						{
							SetMouseSpeed(_mouseSpeedSaved);
							SetPointerPrecision(_mousePrecisionSaved);
						}
						catch { }
						try
						{
							if (newDatabaseFile != "" && File.Exists(newDatabaseFile))
							{
								File.Delete(newDatabaseFile);
							}
							bool serverOnline = IsPortOpen("127.0.0.1", 5433, 1);
							if (serverOnline)
							{
								StopDatabase();
							}
						}
						catch { }

						try
						{
							mutex.ReleaseMutex();
						}
						catch { }
					}

				}

			}

		}

		static string MD5Calc(string filepath)
		{
			if (!File.Exists(filepath)) return "";
			using (var md5 = MD5.Create())
			{
				using (var stream = File.OpenRead(filepath))
				{
					var hash = md5.ComputeHash(stream);
					return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
				}
			}
		}

		static void MoveDirectory(string sourceDir, string destDir, bool overwrite)
		{
			// Créer le répertoire de destination s'il n'existe pas
			Directory.CreateDirectory(destDir);

			// Déplacer tous les fichiers du répertoire source vers le répertoire de destination
			foreach (var file in Directory.GetFiles(sourceDir))
			{
				string destFile = Path.Combine(destDir, Path.GetFileName(file));
				if (File.Exists(destFile) && overwrite)
				{
					File.Delete(destFile); // Supprimer le fichier de destination s'il existe déjà
				}
				File.Move(file, destFile);
			}

			// Déplacer récursivement les sous-répertoires
			foreach (var directory in Directory.GetDirectories(sourceDir))
			{
				string destSubDir = Path.Combine(destDir, Path.GetFileName(directory));
				MoveDirectory(directory, destSubDir, overwrite);
			}

			// Supprimer le répertoire source une fois que tout a été déplacé
			Directory.Delete(sourceDir, true);
		}

		private static void CloseMenuItem_Click(object? sender, EventArgs e)
		{
			stopMonitoring = true;
		}

		public static bool SetPointerPrecision(bool b)
		{
			int[] mouseParams = new int[3];
			// Get the current values.
			SystemParametersInfoGet(SPI_GETMOUSE, 0, GCHandle.Alloc(mouseParams, GCHandleType.Pinned).AddrOfPinnedObject(), 0);
			// Modify the acceleration value as directed.
			mouseParams[2] = b ? 1 : 0;
			// Update the system setting.
			return SystemParametersInfoSet(SPI_SETMOUSE, 0, GCHandle.Alloc(mouseParams, GCHandleType.Pinned).AddrOfPinnedObject(), SPIF_SENDCHANGE);
		}

		public static void SetMouseSpeed(uint speed)
		{
			if (speed < 1 || speed > 20)
				throw new ArgumentOutOfRangeException(nameof(speed), "La vitesse doit être comprise entre 1 et 20.");

			// Définir la vitesse de la souris
			bool success = SystemParametersInfo(SPI_SETMOUSESPEED, 0, speed, 0x01 | 0x02); // SPIF_UPDATEINIFILE | SPIF_SENDCHANGE

			if (!success)
			{
				throw new SystemException("Impossible de définir la vitesse de la souris.");
			}
		}


		public static uint GetMouseSpeed()
		{
			uint speed = 0;
			SystemParametersInfo(SPI_GETMOUSESPEED, 0, ref speed, 0);
			return speed;
		}


		public static bool GetPointerPrecision()
		{
			int[] mouseParams = new int[3];
			// Get the current values.
			SystemParametersInfoGet(SPI_GETMOUSE, 0, GCHandle.Alloc(mouseParams, GCHandleType.Pinned).AddrOfPinnedObject(), 0);
			return mouseParams[2] != 0; // Si precision est non nul, la précision est activée
		}

		static void StartProcessMonitoring()
		{
			monitoringThread = new Thread(() =>
			{
				while (!stopMonitoring)
				{
					try
					{
						// Obtenir la liste des processus BudgieLoader en cours d'exécution
						var processes = Process.GetProcessesByName("BudgieLoader");

						foreach (var process in processes)
						{
							try
							{
								// Récupérer les arguments du processus
								string commandLine = GetCommandLine(process);

								// Vérifier si un des arguments contient un fichier dans le même répertoire que le programme

								//if (!string.IsNullOrEmpty(commandLine) && commandLine.Contains("game.bin"))
								if (!string.IsNullOrEmpty(commandLine) && commandLine.Contains(gamePath))
								{
									//MessageBox.Show("Found !!!");
									Console.WriteLine($"Process trouvé : {process.ProcessName}, PID : {process.Id}");
									budgieLoaderProcess = process;
									stopMonitoring = true; // Arrêter la surveillance une fois le processus trouvé
									break;
								}
							}
							catch (Exception ex)
							{
								Console.WriteLine($"Erreur lors de la récupération des arguments du processus : {ex.Message}");
							}
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Erreur lors de la surveillance des processus : {ex.Message}");
					}

					Thread.Sleep(1000); // Pause d'une seconde avant de vérifier à nouveau
				}
			});

			monitoringThread.Start();
		}

		static void StopProcessMonitoring()
		{
			stopMonitoring = true;
			monitoringThread?.Join();
		}

		// Fonction pour récupérer les arguments d'un processus
		static string GetCommandLine(Process process)
		{
			string query = $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}";
			using (var searcher = new ManagementObjectSearcher(query))
			{
				foreach (var @object in searcher.Get())
				{
					return @object["CommandLine"]?.ToString();
				}
			}
			return null;
		}

		public static bool IsPortOpen(string host, int port, int timeout)
		{
			try
			{
				using (var client = new TcpClient())
				{
					var result = client.BeginConnect(host, port, null, null);
					var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeout));

					if (!success)
					{
						return false;
					}

					client.EndConnect(result);
					return true;
				}
			}
			catch
			{
				return false;
			}
		}

		private static bool IsPostgresRunning()
		{
			string expectedPath = Path.Combine(_postgreBinPath, "postgres.exe");
			var runningProcesses = Process.GetProcessesByName("postgres");
			foreach (var process in runningProcesses)
			{
				try
				{
					// Vérifier le chemin d'exécution du processus
					if (process.MainModule.FileName.Equals(expectedPath, StringComparison.OrdinalIgnoreCase))
					{
						return true; // Trouvé un processus avec le bon chemin
					}
				}
				catch (Exception)
				{
				}
			}
			return false; // Aucun processus correspondant n'a été trouvé
		}

		private static void StartDatabase()
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = Path.Combine(_postgreBinPath, "pg_ctl.exe"),
				Arguments = $"start -D \"{_postgreDataPath}\" -s",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true // Exécute le processus sans afficher de fenêtre
			};

			Process process = new Process
			{
				StartInfo = startInfo
			};

			process.Start();
			databaseProcessId = process.Id; // Sauvegarde du PID du processus
			Console.WriteLine($"Database started with PID: {databaseProcessId}");
		}

		private static void StopDatabase()
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = Path.Combine(_postgreBinPath, "pg_ctl.exe"),
				Arguments = $"stop -D \"{_postgreDataPath}\" -m smart",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true // Exécute le processus sans afficher de fenêtre
			};

			Process process = new Process
			{
				StartInfo = startInfo
			};

			process.Start();
		}

		public static int CheckDatabaseExists()
		{
			string command = $@"-U postgres -h 127.0.0.1 -p 5433 -d postgres -c ""SELECT 1 FROM pg_database WHERE datname = '{_databaseName}';""";
			string psqlPath = Path.Combine(_postgreBinPath, "psql.exe"); // Change this to your actual psql.exe path

			var zz = File.Exists(psqlPath);

			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = psqlPath,
				Arguments = command,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};
			startInfo.EnvironmentVariables["PGPASSWORD"] = "teknoparrot";

			try
			{
				using (Process process = Process.Start(startInfo))
				{
					process.WaitForExit();

					string output = process.StandardOutput.ReadToEnd();
					string error = process.StandardError.ReadToEnd();

					if (!string.IsNullOrEmpty(error))
					{
						MessageBox.Show($"Error: {error}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						return -1;
					}

					if (output.Contains("1"))
					{
						//MessageBox.Show("Database GameDB06 exists.");
						return 1;
					}
					else
					{
						//MessageBox.Show("Database GameDB06 does not exist.");
						return 0;
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return -1;
			}
		}

		public static int CheckDatabaseExistsOffline()
		{
			bool dbExists = false;
			string psqlPath = Path.Combine(_postgreBinPath, "postgres.exe"); // Change this to your actual psql.exe path

			Process process = new Process();
			process.StartInfo.FileName = psqlPath;
			process.StartInfo.Arguments = $"--single -D \"{_postgreDataPath}\" template1";
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.CreateNoWindow = true;

			try
			{
				process.Start();

				using (StreamWriter sw = process.StandardInput)
				{
					if (sw.BaseStream.CanWrite)
					{
						// Envoyer la commande SQL pour vérifier l'existence de la base de données
						sw.WriteLine($"SELECT datname FROM pg_database");
						sw.WriteLine("\\q");  // Quitter PostgreSQL après exécution
					}
				}

				// Lire la sortie standard pour vérifier si la base existe
				string output = process.StandardOutput.ReadToEnd();
				//MessageBox.Show(output);

				// Vérifier si la sortie contient un résultat
				if (output.Contains($"datname = \"{_databaseName}\""))
				{
					dbExists = true;
				}

				process.WaitForExit();
			}
			catch (Exception ex)
			{
				return -1;
			}

			if (dbExists) return 1;
			else return 0;
		}


		public static string PatchDatabase(string originalFile)
		{
			string tempDirectory = Path.GetTempPath(); // Obtenir le chemin du répertoire temporaire
			string fileName = $"tempfile_{Guid.NewGuid()}.tar"; // Générer un nom de fichier unique
			string expectedPatchFile = Path.Combine(tempDirectory, fileName); // Combiner le chemin

			if (originalFile != "" && File.Exists(originalFile))
			{


					var oldFile = originalFile;
					var newFile = expectedPatchFile;
					try
					{
						using (var input = new FileStream(oldFile, FileMode.Open, FileAccess.Read, FileShare.Read))
						using (var output = new FileStream(newFile, FileMode.Create))
						{
							if (_tpProfile == "GoldenTeeLive2006.xml")
							{
								byte[] bdfPatchData = Properties.Resources.bddpatch;
								using (var bdfPatchStream = new MemoryStream(bdfPatchData))
								{
									BinaryPatch.Apply(input, () => new MemoryStream(bdfPatchData), output);
								}
							}
							if (_tpProfile == "GoldenTeeLive2007.xml")
							{
								byte[] bdfPatchData = Properties.Resources.bddpatch2007;
								using (var bdfPatchStream = new MemoryStream(bdfPatchData))
								{
									BinaryPatch.Apply(input, () => new MemoryStream(bdfPatchData), output);
								}
							}
							if (_tpProfile == "PowerPuttLive2012.xml")
							{
								byte[] bdfPatchData = Properties.Resources.bddpatchpp12;
								using (var bdfPatchStream = new MemoryStream(bdfPatchData))
								{
									BinaryPatch.Apply(input, () => new MemoryStream(bdfPatchData), output);
								}
							}
							if (_tpProfile == "SilverStrikeBowlingLive.xml")
							{
								byte[] bdfPatchData = Properties.Resources.bddpatchssb;
								using (var bdfPatchStream = new MemoryStream(bdfPatchData))
								{
									BinaryPatch.Apply(input, () => new MemoryStream(bdfPatchData), output);
								}
							}

						}
					}
					catch (Exception ex) 
					{
						return "";
					}
			}
			return expectedPatchFile;
		}

		public static void InstallDatabase(string newDatabase)
		{
			string dropCommand = $@"-U postgres -h 127.0.0.1 -p 5433 -c ""DROP DATABASE IF EXISTS """"{_databaseName}"""";""";
			string createCommand = $@"-U postgres -h 127.0.0.1 -p 5433 -c ""CREATE DATABASE """"{_databaseName}"""";""";
			string restoreCommand = $@"-U postgres -h 127.0.0.1 -p 5433 -d ""{_databaseName}"" -F t --clean ""{newDatabase}""";
			string installCommand = $@"-U postgres -h 127.0.0.1 -p 5433 -d ""{_databaseName}"" -F t ""{newDatabase}""";

			string psqlPath = Path.Combine(_postgreBinPath, "psql.exe");
			string pgRestorePath = Path.Combine(_postgreBinPath, "pg_restore.exe");

			// Drop the existing database
			ExecuteCommand(psqlPath, dropCommand);
			Thread.Sleep(500);
			// Create the new database
			ExecuteCommand(psqlPath, createCommand);
			Thread.Sleep(500);
			// Restore the database from the tar file
			ExecuteCommand(pgRestorePath, restoreCommand);
			Thread.Sleep(1000);
			ExecuteCommand(pgRestorePath, installCommand);
			Thread.Sleep(1000);
		}

		private static void ExecuteCommand(string filePath, string arguments)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = filePath,
				Arguments = arguments,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};
			startInfo.EnvironmentVariables["PGPASSWORD"] = "teknoparrot";

			try
			{
				using (Process process = Process.Start(startInfo))
				{
					// Read output and error asynchronously to avoid blocking
					Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
					Task<string> errorTask = process.StandardError.ReadToEndAsync();

					process.WaitForExit();

					// Wait for both output and error streams to finish reading
					string output = outputTask.Result;
					string error = errorTask.Result;

					if (!string.IsNullOrEmpty(error))
					{
						// Log the error instead of showing a MessageBox to avoid blocking the process
						Console.WriteLine($"Warning: {error}");
					}

					if (process.ExitCode != 0)
					{
						return;
					}

					// Optionally, log the success output
					Console.WriteLine($"Output: {output}");
				}
			}
			catch (Exception ex)
			{

			}
		}

	}
}