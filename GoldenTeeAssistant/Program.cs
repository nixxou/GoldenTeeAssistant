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
		public static string _databaseSourceFile = @"1922-postgresql_database-GameDB-backup";
		public static string _expectedMD5 = "A825B07F066CA3124F8D7FABE4F8CD5F";
		public static string _tpProfile = "GoldenTeeLive2006.xml";
		private static int? databaseProcessId = null;
		private static string newDatabaseFile = "";

		private static uint _mouseSpeedSaved = 0;
		private static bool _mousePrecisionSaved = false;

		// Constantes pour l'API Windows
		private const uint SPI_SETMOUSESPEED = 0x0071;
		private const uint SPI_GETMOUSESPEED = 0x0070; // Pour obtenir la vitesse de la souris
		private const uint SPIF_SENDCHANGE = 0x02; // Indique de mettre � jour les param�tres de la souris
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
		
		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{

			
			if (!File.Exists("game.bin"))
			{
				MessageBox.Show("Missing game.bin, the program must be in the same folder as the game");
				return;
			}
			else
			{
				if (!File.Exists(Path.Combine(programDirectory, "assetlist.txt")))
				{
					if (File.Exists(Path.Combine(programDirectory, "..", "4", "assetlist.txt")))
					{
						DialogResult result = MessageBox.Show("Do you want to setup the game folder", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
						if (result == DialogResult.Yes)
						{
							string sourceDir = Path.Combine(Directory.GetParent(programDirectory).FullName, "4");
							string destDir = programDirectory;
							MoveDirectory(sourceDir, destDir, true);
						}
						else
						{
							MessageBox.Show($"Exit");
							return;
						}
					}
					else
					{
						MessageBox.Show($"Some game files seems missing.");
						return;
					}
				}
			}
			if (!Directory.Exists(_postgreBinPath))
			{
				MessageBox.Show("Missing Postgre folder");
				return;
			}

			if (args.Length > 0 && args[0] == "daemon")
			{
				ApplicationConfiguration.Initialize();
				Application.Run(new Form1());
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
						throw new Exception("Error checking database");
					}
					if (databaseExist == 0)
					{
						
						string backupFile = "";
						using (OpenFileDialog openFileDialog = new OpenFileDialog())
						{
							openFileDialog.Filter = $"{_databaseSourceFile}|{_databaseSourceFile}";
							openFileDialog.Title = $"Select {_databaseSourceFile}";

							string sourcefolder = programDirectory;
							if (Directory.Exists(Path.Combine(programDirectory, "pg_backup", "2017-12-18"))) sourcefolder = Path.Combine(programDirectory, "pg_backup", "2017-12-18");
							if (Directory.Exists(Path.Combine(programDirectory, "..", "6", "pg_backup", "2017-12-18"))) sourcefolder = Path.Combine(programDirectory, "..", "6", "pg_backup", "2017-12-18");
							openFileDialog.InitialDirectory = Path.GetFullPath(sourcefolder);

							// Show the dialog and get result
							if (openFileDialog.ShowDialog() == DialogResult.OK)
							{
								// Get the path and file name of the selected file
								backupFile = openFileDialog.FileName;
								string fileName = Path.GetFileNameWithoutExtension(backupFile);
								string md5Source = "";
								using (var md5 = MD5.Create())
								{
									using (var stream = File.OpenRead(backupFile))
									{
										var hash = md5.ComputeHash(stream);
										md5Source = BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
									}
								}
								if (md5Source != _expectedMD5)
								{
									MessageBox.Show($"Invalid MD5 ({md5Source}), Expected : {_expectedMD5}");
									return;
								}

								newDatabaseFile = PatchDatabase(backupFile);
								if (newDatabaseFile == "")
								{
									MessageBox.Show("error processing database");
									return;
								}
								InstallDatabase(newDatabaseFile);


							}
							else
							{
								MessageBox.Show("No file selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
								return;
							}
						}
						
						if(newDatabaseFile != "")
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


										if(parentFolder.ToLower() == "gameprofiles")
										{
											string newtpUserProfile = Path.Combine(Directory.GetParent(tpUserProfile).Parent.FullName, "UserProfiles", Path.GetFileName(tpUserProfile));
											if (File.Exists(newtpUserProfile))
											{
												tpUserProfile = newtpUserProfile;
												parentFolder = Path.GetFileName(Path.GetDirectoryName(tpUserProfile));
											}
										}

										if(parentFolder.ToLower() == "userprofiles")
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
												if (Node != null) Node.InnerText = "GameDB06";
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

					
					try
					{
						Thread notifyIconThread = new Thread(() =>
						{
							// Cr�ation du menu contextuel
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

							// Lancer la boucle d'�v�nements pour g�rer le NotifyIcon
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
					// G�rer les exceptions ici si n�cessaire
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

		static void MoveDirectory(string sourceDir, string destDir, bool overwrite)
		{
			// Cr�er le r�pertoire de destination s'il n'existe pas
			Directory.CreateDirectory(destDir);

			// D�placer tous les fichiers du r�pertoire source vers le r�pertoire de destination
			foreach (var file in Directory.GetFiles(sourceDir))
			{
				string destFile = Path.Combine(destDir, Path.GetFileName(file));
				if (File.Exists(destFile) && overwrite)
				{
					File.Delete(destFile); // Supprimer le fichier de destination s'il existe d�j�
				}
				File.Move(file, destFile);
			}

			// D�placer r�cursivement les sous-r�pertoires
			foreach (var directory in Directory.GetDirectories(sourceDir))
			{
				string destSubDir = Path.Combine(destDir, Path.GetFileName(directory));
				MoveDirectory(directory, destSubDir, overwrite);
			}

			// Supprimer le r�pertoire source une fois que tout a �t� d�plac�
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
				throw new ArgumentOutOfRangeException(nameof(speed), "La vitesse doit �tre comprise entre 1 et 20.");

			// D�finir la vitesse de la souris
			bool success = SystemParametersInfo(SPI_SETMOUSESPEED, 0, speed, 0x01 | 0x02); // SPIF_UPDATEINIFILE | SPIF_SENDCHANGE

			if (!success)
			{
				throw new SystemException("Impossible de d�finir la vitesse de la souris.");
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
			return mouseParams[2] != 0; // Si precision est non nul, la pr�cision est activ�e
		}

		static void StartProcessMonitoring()
		{
			monitoringThread = new Thread(() =>
			{
				while (!stopMonitoring)
				{
					try
					{
						// Obtenir la liste des processus BudgieLoader en cours d'ex�cution
						var processes = Process.GetProcessesByName("BudgieLoader");

						foreach (var process in processes)
						{
							try
							{
								// R�cup�rer les arguments du processus
								string commandLine = GetCommandLine(process);

								// V�rifier si un des arguments contient un fichier dans le m�me r�pertoire que le programme

								//if (!string.IsNullOrEmpty(commandLine) && commandLine.Contains("game.bin"))
								if (!string.IsNullOrEmpty(commandLine) && commandLine.Contains(programDirectory))
								{
									//MessageBox.Show("Found !!!");
									Console.WriteLine($"Process trouv� : {process.ProcessName}, PID : {process.Id}");
									budgieLoaderProcess = process;
									stopMonitoring = true; // Arr�ter la surveillance une fois le processus trouv�
									break;
								}
							}
							catch (Exception ex)
							{
								Console.WriteLine($"Erreur lors de la r�cup�ration des arguments du processus : {ex.Message}");
							}
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Erreur lors de la surveillance des processus : {ex.Message}");
					}

					Thread.Sleep(1000); // Pause d'une seconde avant de v�rifier � nouveau
				}
			});

			monitoringThread.Start();
		}

		static void StopProcessMonitoring()
		{
			stopMonitoring = true;
			monitoringThread?.Join();
		}

		// Fonction pour r�cup�rer les arguments d'un processus
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

		private static void StartDatabase()
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = Path.Combine(_postgreBinPath, "pg_ctl.exe"),
				Arguments = $"start -D \"{_postgreDataPath}\" -s",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true // Ex�cute le processus sans afficher de fen�tre
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
				CreateNoWindow = true // Ex�cute le processus sans afficher de fen�tre
			};

			Process process = new Process
			{
				StartInfo = startInfo
			};

			process.Start();
		}

		public static int CheckDatabaseExists()
		{
			string command = @"-U postgres -h 127.0.0.1 -p 5433 -d postgres -c ""SELECT 1 FROM pg_database WHERE datname = 'GameDB06';""";
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

		public static string PatchDatabase(string originalFile)
		{
			string tempDirectory = Path.GetTempPath(); // Obtenir le chemin du r�pertoire temporaire
			string fileName = $"tempfile_{Guid.NewGuid()}.tar"; // G�n�rer un nom de fichier unique
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
							// Charger la ressource bddpatch dans un tableau de bytes
							byte[] bdfPatchData = Properties.Resources.bddpatch; // Assuming bddpatch is the name in the .resx

							// Cr�er un MemoryStream � partir des donn�es
							using (var bdfPatchStream = new MemoryStream(bdfPatchData))
							{
								BinaryPatch.Apply(input, () => new MemoryStream(bdfPatchData), output);
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
			string dropCommand = @"-U postgres -h 127.0.0.1 -p 5433 -c ""DROP DATABASE IF EXISTS """"GameDB06"""";""";
			string createCommand = @"-U postgres -h 127.0.0.1 -p 5433 -c ""CREATE DATABASE """"GameDB06"""";""";
			string restoreCommand = $@"-U postgres -h 127.0.0.1 -p 5433 -d ""GameDB06"" -F t --clean ""{newDatabase}""";
			string installCommand = $@"-U postgres -h 127.0.0.1 -p 5433 -d ""GameDB06"" -F t ""{newDatabase}""";

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