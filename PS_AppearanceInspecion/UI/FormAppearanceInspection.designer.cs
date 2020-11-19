namespace PS_AppearanceInspecion.UI
{
    partial class FormAppearanceInspection
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.btnOK = new System.Windows.Forms.Button();
            this.tBModuleID = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tBPanelID = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.btnNG = new System.Windows.Forms.Button();
            this.pnlControlInspection = new System.Windows.Forms.Panel();
            this.tBSochiState = new System.Windows.Forms.TextBox();
            this.m_SignalView = new CommonUI.SignalView();
            this.m_flpInspection = new System.Windows.Forms.FlowLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.tBChassisID = new System.Windows.Forms.TextBox();
            this.tBInspectionProgramFile = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.tBOperatorID = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.m_stepView = new CommonUI.StepView();
            this.label4 = new System.Windows.Forms.Label();
            this.pnlControlInspection.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.Font = new System.Drawing.Font("MS UI Gothic", 22F, System.Drawing.FontStyle.Bold);
            this.btnOK.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            this.btnOK.Location = new System.Drawing.Point(3, 3);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(130, 75);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // tBModuleID
            // 
            this.tBModuleID.BackColor = System.Drawing.SystemColors.Info;
            this.tBModuleID.Font = new System.Drawing.Font("ＭＳ ゴシック", 21.75F);
            this.tBModuleID.Location = new System.Drawing.Point(190, 157);
            this.tBModuleID.Name = "tBModuleID";
            this.tBModuleID.ReadOnly = true;
            this.tBModuleID.Size = new System.Drawing.Size(242, 36);
            this.tBModuleID.TabIndex = 1;
            this.tBModuleID.TabStop = false;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("MS UI Gothic", 22F);
            this.label5.Location = new System.Drawing.Point(20, 160);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(164, 30);
            this.label5.TabIndex = 0;
            this.label5.Text = "モジュール ID";
            // 
            // tBPanelID
            // 
            this.tBPanelID.BackColor = System.Drawing.SystemColors.Info;
            this.tBPanelID.Font = new System.Drawing.Font("ＭＳ ゴシック", 21.75F);
            this.tBPanelID.Location = new System.Drawing.Point(190, 200);
            this.tBPanelID.Name = "tBPanelID";
            this.tBPanelID.ReadOnly = true;
            this.tBPanelID.Size = new System.Drawing.Size(242, 36);
            this.tBPanelID.TabIndex = 2;
            this.tBPanelID.TabStop = false;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Font = new System.Drawing.Font("MS UI Gothic", 22F);
            this.label15.Location = new System.Drawing.Point(23, 203);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(112, 30);
            this.label15.TabIndex = 0;
            this.label15.Text = "パネルID";
            // 
            // btnNG
            // 
            this.btnNG.Font = new System.Drawing.Font("MS UI Gothic", 22F, System.Drawing.FontStyle.Bold);
            this.btnNG.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            this.btnNG.Location = new System.Drawing.Point(3, 123);
            this.btnNG.Name = "btnNG";
            this.btnNG.Size = new System.Drawing.Size(130, 75);
            this.btnNG.TabIndex = 2;
            this.btnNG.Text = "NG";
            this.btnNG.UseVisualStyleBackColor = true;
            this.btnNG.Click += new System.EventHandler(this.btnNG_Click);
            // 
            // pnlControlInspection
            // 
            this.pnlControlInspection.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlControlInspection.Controls.Add(this.btnNG);
            this.pnlControlInspection.Controls.Add(this.btnOK);
            this.pnlControlInspection.Location = new System.Drawing.Point(1056, 258);
            this.pnlControlInspection.Name = "pnlControlInspection";
            this.pnlControlInspection.Size = new System.Drawing.Size(141, 216);
            this.pnlControlInspection.TabIndex = 49;
            // 
            // tBSochiState
            // 
            this.tBSochiState.BackColor = System.Drawing.Color.Honeydew;
            this.tBSochiState.Font = new System.Drawing.Font("MS UI Gothic", 22F);
            this.tBSochiState.Location = new System.Drawing.Point(825, 160);
            this.tBSochiState.Name = "tBSochiState";
            this.tBSochiState.ReadOnly = true;
            this.tBSochiState.Size = new System.Drawing.Size(320, 37);
            this.tBSochiState.TabIndex = 68;
            this.tBSochiState.TabStop = false;
            // 
            // m_SignalView
            // 
            this.m_SignalView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(147)))), ((int)(((byte)(147)))), ((int)(((byte)(147)))));
            this.m_SignalView.Location = new System.Drawing.Point(660, 160);
            this.m_SignalView.Name = "m_SignalView";
            this.m_SignalView.Size = new System.Drawing.Size(118, 76);
            this.m_SignalView.TabIndex = 69;
            // 
            // m_flpInspection
            // 
            this.m_flpInspection.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.m_flpInspection.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.m_flpInspection.Location = new System.Drawing.Point(12, 258);
            this.m_flpInspection.Name = "m_flpInspection";
            this.m_flpInspection.Size = new System.Drawing.Size(1038, 464);
            this.m_flpInspection.TabIndex = 70;
            this.m_flpInspection.AutoScroll = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("MS UI Gothic", 22F);
            this.label1.Location = new System.Drawing.Point(820, 120);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(73, 30);
            this.label1.TabIndex = 71;
            this.label1.Text = "装置";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("MS UI Gothic", 22F);
            this.label3.Location = new System.Drawing.Point(20, 114);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(131, 30);
            this.label3.TabIndex = 0;
            this.label3.Text = "シャーシID";
            // 
            // tBChassisID
            // 
            this.tBChassisID.BackColor = System.Drawing.SystemColors.Info;
            this.tBChassisID.Font = new System.Drawing.Font("ＭＳ ゴシック", 21.75F);
            this.tBChassisID.Location = new System.Drawing.Point(190, 114);
            this.tBChassisID.Name = "tBChassisID";
            this.tBChassisID.ReadOnly = true;
            this.tBChassisID.Size = new System.Drawing.Size(242, 36);
            this.tBChassisID.TabIndex = 2;
            this.tBChassisID.TabStop = false;
            // 
            // tBInspectionProgramFile
            // 
            this.tBInspectionProgramFile.BackColor = System.Drawing.SystemColors.Info;
            this.tBInspectionProgramFile.Font = new System.Drawing.Font("ＭＳ ゴシック", 21.75F);
            this.tBInspectionProgramFile.Location = new System.Drawing.Point(190, 15);
            this.tBInspectionProgramFile.Name = "tBInspectionProgramFile";
            this.tBInspectionProgramFile.ReadOnly = true;
            this.tBInspectionProgramFile.Size = new System.Drawing.Size(124, 36);
            this.tBInspectionProgramFile.TabIndex = 73;
            this.tBInspectionProgramFile.TabStop = false;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("MS UI Gothic", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label11.Location = new System.Drawing.Point(18, 18);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(80, 29);
            this.label11.TabIndex = 74;
            this.label11.Text = "レシピ";
            // 
            // tBOperatorID
            // 
            this.tBOperatorID.BackColor = System.Drawing.SystemColors.Info;
            this.tBOperatorID.Font = new System.Drawing.Font("ＭＳ ゴシック", 21.75F);
            this.tBOperatorID.Location = new System.Drawing.Point(190, 60);
            this.tBOperatorID.Name = "tBOperatorID";
            this.tBOperatorID.ReadOnly = true;
            this.tBOperatorID.Size = new System.Drawing.Size(242, 36);
            this.tBOperatorID.TabIndex = 75;
            this.tBOperatorID.TabStop = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("MS UI Gothic", 22F);
            this.label2.Location = new System.Drawing.Point(20, 63);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(129, 30);
            this.label2.TabIndex = 76;
            this.label2.Text = "作業者ID";
            // 
            // m_stepView
            // 
            this.m_stepView.AutoSize = true;
            this.m_stepView.BackColor = System.Drawing.SystemColors.Control;
            this.m_stepView.Location = new System.Drawing.Point(454, 15);
            this.m_stepView.Name = "m_stepView";
            this.m_stepView.Size = new System.Drawing.Size(75, 36);
            this.m_stepView.TabIndex = 77;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("MS UI Gothic", 22F);
            this.label4.Location = new System.Drawing.Point(655, 120);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(123, 30);
            this.label4.TabIndex = 78;
            this.label4.Text = "パトライト";
            // 
            // FormAppearanceInspection
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1209, 734);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.m_stepView);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.tBModuleID);
            this.Controls.Add(this.tBPanelID);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tBOperatorID);
            this.Controls.Add(this.tBChassisID);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tBInspectionProgramFile);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.m_flpInspection);
            this.Controls.Add(this.m_SignalView);
            this.Controls.Add(this.tBSochiState);
            this.Controls.Add(this.pnlControlInspection);
            this.Cursor = System.Windows.Forms.Cursors.Default;
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.MinimumSize = new System.Drawing.Size(1000, 600);
            this.Name = "FormAppearanceInspection";
            this.pnlControlInspection.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox tBModuleID;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.TextBox tBPanelID;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Button btnNG;
        private System.Windows.Forms.Panel pnlControlInspection;
        private System.Windows.Forms.TextBox tBSochiState;
        private CommonUI.SignalView m_SignalView;
        private System.Windows.Forms.FlowLayoutPanel m_flpInspection;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tBChassisID;
        private System.Windows.Forms.TextBox tBInspectionProgramFile;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox tBOperatorID;
        private System.Windows.Forms.Label label2;
        private CommonUI.StepView m_stepView;
        private System.Windows.Forms.Label label4;
    }
}

