using System;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using PVZ104;

namespace RZDemo
{
    public partial class AxisConfigForm : Form
    {
        private readonly FileConfiguration fileConfiguration = new FileConfiguration();
        private readonly bool isConnected;
        private AxisMechanicalConfig[] configs;

        public AxisConfigForm(bool isConnected = false)
        {
            this.isConnected = isConnected;
            InitializeComponent();
            ApplyConnectionState();
            LoadConfigs();
        }

        private void LoadConfigs()
        {
            configs = fileConfiguration.GetAxisMechanicalConfigs(4);
            axisConfigGrid.Rows.Clear();
            foreach (AxisMechanicalConfig config in configs)
            {
                axisConfigGrid.Rows.Add(
                    config.AxisName,
                    config.Scale.ToString(CultureInfo.InvariantCulture),
                    config.IsPosLmtDown,
                    config.IsNegLmtDown);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (isConnected)
                {
                    throw new InvalidOperationException("请先断开连接后再保存机械参数。");
                }

                axisConfigGrid.EndEdit();
                AxisMechanicalConfig[] editedConfigs = ReadGridConfigs();
                fileConfiguration.SaveAxisMechanicalConfigs(editedConfigs);
                MessageBox.Show("轴参数已保存，下次 Init/ServoReset 后生效。", "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                configs = editedConfigs;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "配置错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private AxisMechanicalConfig[] ReadGridConfigs()
        {
            AxisMechanicalConfig[] editedConfigs = new AxisMechanicalConfig[4];
            for (int i = 0; i < editedConfigs.Length; i++)
            {
                DataGridViewRow row = axisConfigGrid.Rows[i];
                string scaleText = Convert.ToString(row.Cells[colScale.Name].Value);
                if (!double.TryParse(scaleText, NumberStyles.Float, CultureInfo.InvariantCulture, out double scale) &&
                    !double.TryParse(scaleText, NumberStyles.Float, CultureInfo.CurrentCulture, out scale))
                {
                    throw new FormatException(string.Format("{0} 脉冲当量格式不正确。", configs[i].AxisName));
                }

                AxisMechanicalConfig config = new AxisMechanicalConfig
                {
                    AxisIndex = i,
                    Scale = scale,
                    IsPosLmtDown = Convert.ToBoolean(row.Cells[colPosLmtDown.Name].Value),
                    IsNegLmtDown = Convert.ToBoolean(row.Cells[colNegLmtDown.Name].Value),
                };

                config.Validate();
                editedConfigs[i] = config;
            }

            return editedConfigs;
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            LoadConfigs();
        }

        private void btnDefaults_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确认恢复四轴机械参数为现场默认值？恢复后仍需点击保存才会写入配置文件。",
                "恢复默认参数",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            configs = fileConfiguration.GetDefaultAxisMechanicalConfigs().Take(4).ToArray();
            axisConfigGrid.Rows.Clear();
            foreach (AxisMechanicalConfig config in configs)
            {
                axisConfigGrid.Rows.Add(
                    config.AxisName,
                    config.Scale.ToString(CultureInfo.InvariantCulture),
                    config.IsPosLmtDown,
                    config.IsNegLmtDown);
            }
        }

        private void axisConfigGrid_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.ColumnIndex != colScale.Index || e.RowIndex < 0)
            {
                return;
            }

            string scaleText = Convert.ToString(e.FormattedValue);
            if (!double.TryParse(scaleText, NumberStyles.Float, CultureInfo.InvariantCulture, out double scale) &&
                !double.TryParse(scaleText, NumberStyles.Float, CultureInfo.CurrentCulture, out scale))
            {
                axisConfigGrid.Rows[e.RowIndex].ErrorText = "脉冲当量格式不正确。";
                e.Cancel = true;
                return;
            }

            if (e.RowIndex == 0 &&
                (scale < AxisMechanicalConfig.Axis01MinScale || scale > AxisMechanicalConfig.Axis01MaxScale))
            {
                axisConfigGrid.Rows[e.RowIndex].ErrorText = "Axis01 脉冲当量必须在 8000-20000 范围内。";
                e.Cancel = true;
            }
        }

        private void axisConfigGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                axisConfigGrid.Rows[e.RowIndex].ErrorText = string.Empty;
            }
        }

        private void ApplyConnectionState()
        {
            axisConfigGrid.ReadOnly = isConnected;
            btnSave.Enabled = !isConnected;
            btnDefaults.Enabled = !isConnected;
            labelConnectionWarning.Visible = isConnected;
        }
    }
}
