﻿namespace Nanni_ScreenConfigurator
{
    partial class NanniConfig_GUI
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NanniConfig_GUI));
            bt_Refresh = new Button();
            bt_Write = new Button();
            label1 = new Label();
            label2 = new Label();
            cb_Display = new ComboBox();
            cb_Engines = new ComboBox();
            tmr_WaitForDevices = new System.Windows.Forms.Timer(components);
            tmr_SendingStateMachine = new System.Windows.Forms.Timer(components);
            progressBar = new ProgressBar();
            tmr_GuiDelays = new System.Windows.Forms.Timer(components);
            lb_SendingStatus = new Label();
            label3 = new Label();
            pb_CustomerLogo = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pb_CustomerLogo).BeginInit();
            SuspendLayout();
            // 
            // bt_Refresh
            // 
            bt_Refresh.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            bt_Refresh.Location = new Point(255, 56);
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
            bt_Write.Location = new Point(265, 187);
            bt_Write.Name = "bt_Write";
            bt_Write.Size = new Size(174, 28);
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
            cb_Display.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_Display.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            cb_Display.FormattingEnabled = true;
            cb_Display.Location = new Point(17, 56);
            cb_Display.Name = "cb_Display";
            cb_Display.Size = new Size(232, 28);
            cb_Display.TabIndex = 4;
            // 
            // cb_Engines
            // 
            cb_Engines.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_Engines.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            cb_Engines.FormattingEnabled = true;
            cb_Engines.Location = new Point(17, 137);
            cb_Engines.Name = "cb_Engines";
            cb_Engines.Size = new Size(232, 28);
            cb_Engines.TabIndex = 5;
            // 
            // tmr_WaitForDevices
            // 
            tmr_WaitForDevices.Tick += tmr_WaitForDevices_Tick;
            // 
            // tmr_SendingStateMachine
            // 
            tmr_SendingStateMachine.Tick += tmr_SendingStateMachine_Tick;
            // 
            // progressBar
            // 
            progressBar.Location = new Point(17, 221);
            progressBar.Maximum = 32;
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(422, 10);
            progressBar.Step = 1;
            progressBar.TabIndex = 1;
            // 
            // tmr_GuiDelays
            // 
            tmr_GuiDelays.Interval = 1000;
            tmr_GuiDelays.Tick += tmr_GuiDelays_Tick;
            // 
            // lb_SendingStatus
            // 
            lb_SendingStatus.AutoSize = true;
            lb_SendingStatus.Location = new Point(17, 203);
            lb_SendingStatus.Name = "lb_SendingStatus";
            lb_SendingStatus.Size = new Size(39, 15);
            lb_SendingStatus.TabIndex = 6;
            lb_SendingStatus.Text = "Status";
            lb_SendingStatus.Visible = false;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.ForeColor = Color.Red;
            label3.Location = new Point(6, 5);
            label3.Name = "label3";
            label3.Size = new Size(60, 15);
            label3.TabIndex = 7;
            label3.Text = "V0.2 - Test";
            label3.Visible = false;
            // 
            // pb_CustomerLogo
            // 
            pb_CustomerLogo.Image = (Image)resources.GetObject("pb_CustomerLogo.Image");
            pb_CustomerLogo.InitialImage = null;
            pb_CustomerLogo.Location = new Point(324, 2);
            pb_CustomerLogo.Name = "pb_CustomerLogo";
            pb_CustomerLogo.Size = new Size(126, 40);
            pb_CustomerLogo.TabIndex = 8;
            pb_CustomerLogo.TabStop = false;
            // 
            // NanniConfig_GUI
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(451, 243);
            Controls.Add(pb_CustomerLogo);
            Controls.Add(label3);
            Controls.Add(lb_SendingStatus);
            Controls.Add(progressBar);
            Controls.Add(cb_Engines);
            Controls.Add(cb_Display);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(bt_Write);
            Controls.Add(bt_Refresh);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "NanniConfig_GUI";
            Text = "Nanni Screen Configurator V01.0";
            FormClosing += Form1_FormClosing;
            ((System.ComponentModel.ISupportInitialize)pb_CustomerLogo).EndInit();
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
        private System.Windows.Forms.Timer tmr_SendingStateMachine;
        private ProgressBar progressBar;
        private System.Windows.Forms.Timer tmr_GuiDelays;
        private Label lb_SendingStatus;
        private Label label3;
        private PictureBox pb_CustomerLogo;
    }
}