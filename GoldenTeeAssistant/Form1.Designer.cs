namespace GoldenTeeAssistant
{
	partial class Form1
	{
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			num_mouseSpeed = new NumericUpDown();
			label1 = new Label();
			timer1 = new System.Windows.Forms.Timer(components);
			btn_update = new Button();
			((System.ComponentModel.ISupportInitialize)num_mouseSpeed).BeginInit();
			SuspendLayout();
			// 
			// num_mouseSpeed
			// 
			num_mouseSpeed.Location = new Point(102, 12);
			num_mouseSpeed.Maximum = new decimal(new int[] { 19, 0, 0, 0 });
			num_mouseSpeed.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
			num_mouseSpeed.Name = "num_mouseSpeed";
			num_mouseSpeed.Size = new Size(75, 23);
			num_mouseSpeed.TabIndex = 1;
			num_mouseSpeed.Value = new decimal(new int[] { 1, 0, 0, 0 });
			// 
			// label1
			// 
			label1.AutoSize = true;
			label1.Location = new Point(14, 14);
			label1.Name = "label1";
			label1.Size = new Size(78, 15);
			label1.TabIndex = 3;
			label1.Text = "Mouse Speed";
			// 
			// timer1
			// 
			timer1.Tick += timer1_Tick;
			// 
			// btn_update
			// 
			btn_update.Location = new Point(183, 10);
			btn_update.Name = "btn_update";
			btn_update.Size = new Size(75, 23);
			btn_update.TabIndex = 4;
			btn_update.Text = "Update";
			btn_update.UseVisualStyleBackColor = true;
			btn_update.Click += btn_update_Click;
			// 
			// Form1
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(300, 53);
			Controls.Add(btn_update);
			Controls.Add(label1);
			Controls.Add(num_mouseSpeed);
			Name = "Form1";
			Text = "Form1";
			Load += Form1_Load;
			((System.ComponentModel.ISupportInitialize)num_mouseSpeed).EndInit();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion
		private NumericUpDown num_mouseSpeed;
		private Label label1;
		private System.Windows.Forms.Timer timer1;
		private Button btn_update;
	}
}
