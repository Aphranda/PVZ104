namespace RZDemo
{
    partial class AxisConfigForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.axisConfigGrid = new System.Windows.Forms.DataGridView();
            this.colAxis = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colScale = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPosLmtDown = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colNegLmtDown = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnReload = new System.Windows.Forms.Button();
            this.btnDefaults = new System.Windows.Forms.Button();
            this.labelTip = new System.Windows.Forms.Label();
            this.labelConnectionWarning = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.axisConfigGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // axisConfigGrid
            // 
            this.axisConfigGrid.AllowUserToAddRows = false;
            this.axisConfigGrid.AllowUserToDeleteRows = false;
            this.axisConfigGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.axisConfigGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colAxis,
            this.colScale,
            this.colPosLmtDown,
            this.colNegLmtDown});
            this.axisConfigGrid.Location = new System.Drawing.Point(12, 12);
            this.axisConfigGrid.MultiSelect = false;
            this.axisConfigGrid.Name = "axisConfigGrid";
            this.axisConfigGrid.RowHeadersVisible = false;
            this.axisConfigGrid.RowTemplate.Height = 23;
            this.axisConfigGrid.Size = new System.Drawing.Size(556, 135);
            this.axisConfigGrid.TabIndex = 0;
            this.axisConfigGrid.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.axisConfigGrid_CellEndEdit);
            this.axisConfigGrid.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this.axisConfigGrid_CellValidating);
            // 
            // colAxis
            // 
            this.colAxis.HeaderText = "轴";
            this.colAxis.Name = "colAxis";
            this.colAxis.ReadOnly = true;
            this.colAxis.Width = 90;
            // 
            // colScale
            // 
            this.colScale.HeaderText = "脉冲当量";
            this.colScale.Name = "colScale";
            this.colScale.Width = 150;
            // 
            // colPosLmtDown
            // 
            this.colPosLmtDown.HeaderText = "正限位低电平";
            this.colPosLmtDown.Name = "colPosLmtDown";
            this.colPosLmtDown.Width = 150;
            // 
            // colNegLmtDown
            // 
            this.colNegLmtDown.HeaderText = "负限位低电平";
            this.colNegLmtDown.Name = "colNegLmtDown";
            this.colNegLmtDown.Width = 150;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(371, 192);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(90, 32);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = "保存";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnReload
            // 
            this.btnReload.Location = new System.Drawing.Point(467, 192);
            this.btnReload.Name = "btnReload";
            this.btnReload.Size = new System.Drawing.Size(101, 32);
            this.btnReload.TabIndex = 2;
            this.btnReload.Text = "重新读取";
            this.btnReload.UseVisualStyleBackColor = true;
            this.btnReload.Click += new System.EventHandler(this.btnReload_Click);
            // 
            // btnDefaults
            // 
            this.btnDefaults.Location = new System.Drawing.Point(12, 192);
            this.btnDefaults.Name = "btnDefaults";
            this.btnDefaults.Size = new System.Drawing.Size(112, 32);
            this.btnDefaults.TabIndex = 3;
            this.btnDefaults.Text = "恢复默认";
            this.btnDefaults.UseVisualStyleBackColor = true;
            this.btnDefaults.Click += new System.EventHandler(this.btnDefaults_Click);
            // 
            // labelTip
            // 
            this.labelTip.AutoSize = true;
            this.labelTip.Location = new System.Drawing.Point(12, 158);
            this.labelTip.Name = "labelTip";
            this.labelTip.Size = new System.Drawing.Size(389, 12);
            this.labelTip.TabIndex = 4;
            this.labelTip.Text = "Axis01 脉冲当量范围：8000-20000。保存后下次 Init/ServoReset 生效。";
            // 
            // labelConnectionWarning
            // 
            this.labelConnectionWarning.AutoSize = true;
            this.labelConnectionWarning.ForeColor = System.Drawing.Color.Firebrick;
            this.labelConnectionWarning.Location = new System.Drawing.Point(12, 177);
            this.labelConnectionWarning.Name = "labelConnectionWarning";
            this.labelConnectionWarning.Size = new System.Drawing.Size(293, 12);
            this.labelConnectionWarning.TabIndex = 5;
            this.labelConnectionWarning.Text = "当前已连接设备。为避免机械风险，请断开连接后再编辑保存。";
            this.labelConnectionWarning.Visible = false;
            // 
            // AxisConfigForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(580, 236);
            this.Controls.Add(this.labelConnectionWarning);
            this.Controls.Add(this.labelTip);
            this.Controls.Add(this.btnDefaults);
            this.Controls.Add(this.btnReload);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.axisConfigGrid);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AxisConfigForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "轴机械参数配置";
            ((System.ComponentModel.ISupportInitialize)(this.axisConfigGrid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.DataGridView axisConfigGrid;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAxis;
        private System.Windows.Forms.DataGridViewTextBoxColumn colScale;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colPosLmtDown;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colNegLmtDown;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnReload;
        private System.Windows.Forms.Button btnDefaults;
        private System.Windows.Forms.Label labelTip;
        private System.Windows.Forms.Label labelConnectionWarning;
    }
}
