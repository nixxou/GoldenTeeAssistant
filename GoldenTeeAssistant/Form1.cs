using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace GoldenTeeAssistant
{
	public partial class Form1 : Form
	{
		private static NotifyIcon notifyIcon;
		private static ContextMenuStrip contextMenu;
		private bool _isDialog = false;
		private Process _parentProcess = null;





		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			_parentProcess = ParentProcessUtilities.GetParentProcess();

			if (_parentProcess != null)
			{
				timer1.Enabled = true;
			}

			//MessageBox.Show($"Vitesse actuelle de la souris : {currentSpeed}\nPrécision du pointeur : {(currentPrecision ? "Activée" : "Désactivée")}",
			//	"Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

			contextMenu = new ContextMenuStrip();
			ToolStripMenuItem closeMenuItem = new ToolStripMenuItem("Close");
			closeMenuItem.Click += CloseMenuItem_Click;
			Resize += VjoyControl_Resize;
			contextMenu.Items.Add(closeMenuItem);

			try
			{
				// Créer et afficher l'icône dans la barre des tâches
				notifyIcon = new NotifyIcon();
				notifyIcon.Icon = SystemIcons.Information;
				notifyIcon.ContextMenuStrip = contextMenu;
				notifyIcon.Visible = true;
				notifyIcon.Text = "Golden Tee Assistant Running";
				notifyIcon.DoubleClick += VjoyControl_DoubleClick;
				this.WindowState = FormWindowState.Minimized;
				this.Hide();

			}
			catch { }

			num_mouseSpeed.Value = ConfigurationManager.MainConfig.mouseSpeed;
			Program.SetMouseSpeed(uint.Parse(num_mouseSpeed.Value.ToString()));
			Program.SetPointerPrecision(false);


		}

		private void CloseMenuItem_Click(object? sender, EventArgs e)
		{
			this.Close();
		}

		private void VjoyControl_DoubleClick(object? sender, EventArgs e)
		{
			this.Show();
			this.WindowState = FormWindowState.Normal;
			notifyIcon.Visible = false;
		}

		private void VjoyControl_Resize(object sender, EventArgs e)
		{
			if (!_isDialog)
			{
				if (FormWindowState.Minimized == this.WindowState)
				{
					notifyIcon.Visible = true;

					this.Hide();
				}
				else if (FormWindowState.Normal == this.WindowState)
				{
					notifyIcon.Visible = false;
				}

			}

		}



		private void btn_install_Click(object sender, EventArgs e)
		{

		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			if (_parentProcess == null || _parentProcess.HasExited)
			{
				this.Close();
			}
		}

		private void btn_update_Click(object sender, EventArgs e)
		{
			Program.SetMouseSpeed(uint.Parse(num_mouseSpeed.Value.ToString()));
			ConfigurationManager.MainConfig.mouseSpeed = int.Parse(num_mouseSpeed.Value.ToString());
			ConfigurationManager.SaveConfig();

		}
	}
}
