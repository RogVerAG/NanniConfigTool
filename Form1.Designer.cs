namespace Nanni_ScreenConfigurator
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
            bt_Refresh = new Button();
            bt_Write = new Button();
            label1 = new Label();
            label2 = new Label();
            cb_Display = new ComboBox();
            cb_Engines = new ComboBox();
            tmr_WaitForDevices = new System.Windows.Forms.Timer(components);
            tmr_SendConfigStateMachine = new System.Windows.Forms.Timer(components);
            SuspendLayout();
            // 
            // bt_Refresh
            // 
            bt_Refresh.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            bt_Refresh.Location = new Point(241, 55);
            bt_Refresh.Name = "bt_Refresh";
            bt_Refresh.Size = new Size(75, 29);
            bt_Refresh.TabIndex = 0;
            bt_Refresh.Text = "Refresh";
            bt_Refresh.UseVisualStyleBackColor = true;
            bt_Refresh.Click += bt_Refresh_Click;
            // 
            // bt_Write
            // 
            bt_Write.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            bt_Write.Location = new Point(241, 223);
            bt_Write.Name = "bt_Write";
            bt_Write.Size = new Size(188, 28);
            bt_Write.TabIndex = 1;
            bt_Write.Text = "Write Config to Display";
            bt_Write.UseVisualStyleBackColor = true;
            bt_Write.Click += bt_Write_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            label1.Location = new Point(17, 32);
            label1.Name = "label1";
            label1.Size = new Size(58, 20);
            label1.TabIndex = 2;
            label1.Text = "Display";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            label2.Location = new Point(17, 114);
            label2.Name = "label2";
            label2.Size = new Size(54, 20);
            label2.TabIndex = 3;
            label2.Text = "Engine";
            // 
            // cb_Display
            // 
            cb_Display.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            cb_Display.FormattingEnabled = true;
            cb_Display.Location = new Point(17, 56);
            cb_Display.Name = "cb_Display";
            cb_Display.Size = new Size(210, 28);
            cb_Display.TabIndex = 4;
            // 
            // cb_Engines
            // 
            cb_Engines.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            cb_Engines.FormattingEnabled = true;
            cb_Engines.Items.AddRange(new object[] { "Configuration 01", "Configuration 02", "Configuration 03", "Configuration 04", "Configuration 05", "Configuration 06", "Configuration 07", "Configuration 08", "Configuration 09", "Configuration 10", "Configuration 11", "Configuration 12" });
            cb_Engines.Location = new Point(17, 137);
            cb_Engines.Name = "cb_Engines";
            cb_Engines.Size = new Size(210, 28);
            cb_Engines.TabIndex = 5;
            // 
            // tmr_WaitForDevices
            // 
            tmr_WaitForDevices.Tick += tmr_WaitForDevices_Tick;
            // 
            // tmr_SendConfigStateMachine
            // 
            tmr_SendConfigStateMachine.Tick += tmr_SendConfigStateMachine_Tick;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(437, 259);
            Controls.Add(cb_Engines);
            Controls.Add(cb_Display);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(bt_Write);
            Controls.Add(bt_Refresh);
            Name = "Form1";
            Text = "Nanni Screen Configurator";
            FormClosing += Form1_FormClosing;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button bt_Refresh;
        private Button bt_Write;
        private Label label1;
        private Label label2;
        private ComboBox cb_Display;
        private ComboBox cb_Engines;
        private System.Windows.Forms.Timer tmr_WaitForDevices;
        private System.Windows.Forms.Timer tmr_SendConfigStateMachine;
    }
}