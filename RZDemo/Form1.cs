using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Timers;
using PVZ104;


namespace RZDemo
{
    public partial class Form1 : Form
    {



        public Form1()
        {
            InitializeComponent();
        }



        private void Form1_Load(object sender, EventArgs e)
        {


        }


        readonly AutoGCApi AutoGC = new AutoGCApi();
        bool Test_flag = true;
        bool connect_flag = false;
        Dimension dimension = Dimension.Axis01;

        private delegate void MovingDelegate();

        private delegate void ConnectDelegate();

        public delegate void SetControlValue(long value);


 
        private void btn_connect_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] ipv4 = Array.ConvertAll(textBox_ip.Text.Split('.'), byte.Parse);
                foreach (var item in ipv4)
                {
                    Debug.WriteLine(item);
                }

                E_Result e_Result = AutoGC.Connect(ipv4);



                AutoGC.Init(dimension, false, 5000);

                if (e_Result == E_Result.E_SUCCESS)
                {
                    btn_signal.Text = "Conected";
                    btn_signal.BackColor = Color.DarkGreen;
                    TopMessage.AppendText("Connected\n");
                    connect_flag = true;
                }
                else
                {
                    btn_signal.Text = "Disconnect";
                    btn_signal.BackColor = Color.DarkRed;
                    connect_flag = false;
                }
                
                Getps(dimension);
            }
            catch (Exception)
            {

                TopMessage.AppendText("Connect Error\n");
            }

        }

        private void comboBox_dim_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboBox_dim.Text)
            {
                case "Axis01":
                    dimension = Dimension.Axis01;
                    break;
                case "Axis02":
                    dimension = Dimension.Axis02;
                    break;
                case "Axis03":
                    dimension = Dimension.Axis03;
                    break;
                case "Axis04":
                    dimension = Dimension.Axis04;
                    break;
                default:
                    break;
            }
        }

        private void button_ServoReset(object sender, EventArgs e)
        {
            try
            {
                UInt32 timeDalay = UInt32.Parse(textBox_Time.Text);
                AutoGC.Init(dimension, true, timeDalay * 1000);
                TopMessage.AppendText("Servo Init\n");
            }
            catch (Exception)
            {
                TopMessage.AppendText("Servo Error\n");
            }
        }

        private void btn_disconnect_Click(object sender, EventArgs e)
        {
            try
            {
                E_Result e_Result = AutoGC.Disconnect();
                if (e_Result == E_Result.E_SUCCESS)
                {
                    btn_signal.Text = "Disconnect";
                    btn_signal.BackColor = Color.DarkRed;
                    TopMessage.AppendText("Disconnected\n");
                    connect_flag = false;
                }
                else
                {
                    btn_signal.Text = "Connect";
                    btn_signal.BackColor = Color.DarkGreen;
                }
            }
            catch (Exception)
            {

                TopMessage.AppendText("Disconnect Error\n");
            }
        }

        private async void button_home_Click(object sender, EventArgs e)
        {
            if (alarm_jugement())
            {
                TopMessage.AppendText("请在排除异常后运行ServoReset 清除报错\n");
            }
            else
            {
                await Task.Run(() =>
                {
                    try
                    {
                        float speed = float.Parse(textBox_homeSpeed.Text);
                        float offset = float.Parse(textBox_homeOffset.Text);
                        int timeout = int.Parse(textBox_homeTime.Text);
                        AutoGC.Home(dimension, speed, offset, timeout);
                        Invoke(new MovingDelegate(() =>
                        {
                            TopMessage.AppendText("原点复位完成\n");
                        }));

                    }
                    catch (Exception)
                    {
                        Invoke(new MovingDelegate(() =>
                        {
                            TopMessage.AppendText("Home Error\n");
                        }));
                    }
                });
            }
        }

        private async void btn_reset_Click(object sender, EventArgs e)
        {
            if (alarm_jugement())
            {
                TopMessage.AppendText("请在排除异常后运行ServoReset 清除报错\n");
            }
            else
            {
                await Task.Run(() =>
                {
                    try
                    {
                        float hightSpeed = float.Parse(textBox_resetSpeedH.Text);
                        float lowSpeed = float.Parse(textBox_resetSpeedL.Text);
                        float offset = float.Parse(textBox_resetOffset.Text);
                        int timeout = int.Parse(textBox_resetTime.Text);
                        AutoGC.Reset(dimension, hightSpeed, lowSpeed, offset, timeout);
                        Invoke(new MovingDelegate(() =>
                        {
                            TopMessage.AppendText("原点复位完成\n");
                        }));

                    }
                    catch (Exception)
                    {
                        Invoke(new MovingDelegate(() =>
                        {
                            TopMessage.AppendText("Reset Error\n");
                        }));

                    }
                });
            }
        }


        private async void button_relative_Click(object sender, EventArgs e)
        {
            if (alarm_jugement())
            {
                TopMessage.AppendText("请在排除异常后运行ServoReset 清除报错\n");
            }
            else
            {
                await Task.Run(() =>
                {
                    try
                    {
                        float speed = float.Parse(textBox_relSpeed.Text);
                        float position = float.Parse(textBox_relPos.Text);
                        int timeout = int.Parse(textBox_relTime.Text);
                        AutoGC.MoveRelative(dimension, speed, position, timeout);
                        Invoke(new MovingDelegate(() =>
                        {
                            TopMessage.AppendText("相对定位完成\n");
                        }));
                    }
                    catch (Exception)
                    {
                        Invoke(new MovingDelegate(() =>
                        {
                            TopMessage.AppendText("Relative Error\n");
                        }));
                    }
                });
            }
        }

        private async void button_absolute_Click(object sender, EventArgs e)
        {
            if (alarm_jugement())
            {
                TopMessage.AppendText("请在排除异常后运行ServoReset 清除报错\n");
            }
            else
            {
                await Task.Run(() =>
                {
                    try
                    {
                        E_Result data;
                        float speed = float.Parse(textBox_absSpeed.Text);
                        float position = float.Parse(textBox_absPos.Text);
                        int timeout = int.Parse(textBox_absTime.Text);
                        data = AutoGC.MoveAbsolute(dimension, speed, position, timeout);

                        TopMessage.AppendText(data.ToString());
                        Invoke(new MovingDelegate(() =>
                        {
                            TopMessage.AppendText("绝对定位完成\n");
                        }));

                    }
                    catch (Exception)
                    {
                        Invoke(new MovingDelegate(() =>
                        {
                            TopMessage.AppendText("Absolute Error\n");
                        }));
                    }
                });
            }

        }

        private void btn_jog_Click(object sender, EventArgs e)
        {

        }

        private void btn_jog_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                bool direction_flag = false;
                string direction = cob_jogDir.Text;
                if (direction == "顺时针")
                    direction_flag = false;
                if (direction == "逆时针")
                    direction_flag = true;
                float speed = float.Parse(textBox_jogSpeed.Text);
                int timeout = int.Parse(textBox_jogTime.Text);
                AutoGC.Jog(dimension, speed, direction_flag, timeout);
                TopMessage.AppendText(direction + "JOG\n");
            }
            catch (Exception)
            {

                TopMessage.AppendText("JOG Error\n");
            }
        }

        private void btn_jog_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                AutoGC.Stop(dimension);
            }
            catch (Exception)
            {

                TopMessage.AppendText("Stop Error\n");
            }
        }

        private void btn_stop_Click(object sender, EventArgs e)
        {
            try
            {
                AutoGC.Stop(dimension);
            }
            catch (Exception)
            {

                TopMessage.AppendText("Stop Error\n");
            }

        }

        private delegate void AddDateDelegate(double[] item);
        private async void Getps(Dimension dimension)
        {
            await Task.Run(() =>
            {
                double position = 0;
                double speed = 0;
                int alarmId;
                E_Turntable_Status status;
                while (connect_flag)
                {
                    AutoGC.GetPosition(dimension, out position);
                    AutoGC.GetSpeed(dimension, out speed);
                    AutoGC.GetStatus(dimension, out status, out alarmId);

                    byte[] ipv4 = AutoGC.ipv4;
                    string ipv4Show = "";
                    foreach (var item in ipv4)
                    {
                        ipv4Show = ipv4Show + "." + item.ToString();
                    }


                    double[] data = new double[2] { position, speed };
                    Invoke(new AddDateDelegate((item) =>
                    {
                        textBox_staticPos.Text = data[0].ToString();
                        textBox_staticSpeed.Text = data[1].ToString();
                        textBox_status.Text = status.ToString() + " [" + alarmId.ToString() + "] ";
                        textBox_ip.Text = ipv4Show.Substring(1);
                        if (status == E_Turntable_Status.alarm)
                        {
                            textBox_status.BackColor = Color.Coral;

                        }
                        else
                        {
                            textBox_status.BackColor = Color.LightGreen;

                        }
                        if (alarmId == 128 && status == E_Turntable_Status.alarm)
                        {
                            btn_signal.Text = "Disconnect";
                            btn_signal.BackColor = Color.DarkRed;
                            connect_flag = false;
                        }
                        panel5.Enabled = connect_flag;
                    }), data);
                    Task.Delay(100);
                }
            });
        }

        /// <summary>
        /// 开始测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button_startTest_Click(object sender, EventArgs e)
        {

            if (connect_flag != true && alarm_jugement())
            {
                TopMessage.AppendText("请连接转台,清除错误\n");
            }

            Test_flag = true;
            int numSet = int.Parse(textBox_numSet.Text);
            float speed = float.Parse(textBox_testSpeed.Text);
            float step = float.Parse(textBox_testStep.Text);
            int timedelay = int.Parse(textBox_testDelay.Text);
            float travel = float.Parse(textBox_testTravel.Text);
            float position_now = float.Parse(textBox_staticPos.Text);
            int numFlag = 0;
            textBox_testSpeed.Enabled = false;
            textBox_testDelay.Enabled = false;
            textBox_testStep.Enabled = false;
            textBox_testTravel.Enabled = false;
            button_startTest.Enabled = false;
            panel2.Enabled = false;
            await Task.Run(() =>
            {
                while (numFlag < numSet && Test_flag && connect_flag)
                {
                    if (alarm_jugement())
                    {
                        Invoke(new MovingDelegate(() =>
                        {
                            panel2.Enabled = true;
                            TopMessage.AppendText("Step Error\n");
                        }));
                        break;
                    }
                    if (position_now == travel)
                    {
                        Invoke(new MovingDelegate(() =>
                        {
                            panel2.Enabled = true;
                            TopMessage.AppendText("请先回原点\n");
                        }));
                        break;
                    }
                    double position = 0;
                    int stepFlag = 1;

                    while (position < travel && position >= 0 && connect_flag && Test_flag)
                    {

                        if (alarm_jugement())
                            break;
                        try
                        {
                            AutoGC.MoveRelative(dimension, speed, Math.Pow(-1, numFlag) * step, 10);
                            Invoke(new MovingDelegate(async () =>
                            {
                                TopMessage.AppendText(String.Format("第{0}次步进完成\n", stepFlag));
                                await Task.Delay(timedelay);
                                textBox_numNow.Text = numFlag.ToString();
                            }));
                        }
                        catch (Exception)
                        {
                            Invoke(new MovingDelegate(() =>
                            {
                                panel2.Enabled = true;
                                TopMessage.AppendText("Step Error\n");
                            }));
                            connect_flag = false;
                            break;
                        }
                        AutoGC.GetPosition(dimension, out position);
                        E_Turntable_Status status = E_Turntable_Status.moving;
                        int alarmId = 0;
                        AutoGC.GetStatus(dimension, out status, out alarmId);
                        if (status == E_Turntable_Status.ready)
                        {
                            stepFlag++;
                        }
                        if (position <= 0 | position >= travel)
                        {
                            Debug.WriteLine("X");
                            break;
                        }
                    }
                    numFlag++;
                }
            });

        }

        /// <summary>
        /// 关闭测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_StopTest_Click(object sender, EventArgs e)
        {
            textBox_testSpeed.Enabled = true;
            textBox_testDelay.Enabled = true;
            textBox_testStep.Enabled = true;
            textBox_testTravel.Enabled = true;
            panel2.Enabled = true;
            Test_flag = false;
            button_startTest.Enabled = true;
        }

        private void button_TriggerStart_Click(object sender, EventArgs e)
        {
            double trigerStart = double.Parse(textBox_TriggerStart.Text);
            double trigerStop = double.Parse(textBox_TriggerStop.Text);
            double triggerStep = double.Parse(textBox_TriggerStep.Text);
            int triggerWidth = int.Parse(textBox_TriggerWidth.Text);
            Dimension dimension = DimensionSelect();
            AutoGC.Trigger(dimension, trigerStart, trigerStop, triggerStep, triggerWidth);
        }

        private void button_TriggerStop_Click(object sender, EventArgs e)
        {
            Dimension dimension = DimensionSelect();
            AutoGC.TriggerStop(dimension);
        }

        private Dimension DimensionSelect()
        {
            switch (comboBox_dim.Text)
            {
                case "Axis01":
                    dimension = Dimension.Axis01;
                    break;
                case "Axis02":
                    dimension = Dimension.Axis02;
                    break;
                case "Axis03":
                    dimension = Dimension.Axis03;
                    break;
                case "Axis04":
                    dimension = Dimension.Axis04;
                    break;
                default:
                    break;
            }
            return dimension;
        }

        private void TopMessage_TextChanged(object sender, EventArgs e)
        {
            TopMessage.SelectionStart = TopMessage.Text.Length;
            TopMessage.ScrollToCaret();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        /// <summary>
        /// 判断状态
        /// </summary>
        /// <returns></returns>
        private bool alarm_jugement()
        {
            string alarm_flag = textBox_status.Text;
            if (alarm_flag.Contains("alarm"))
                return true;
            return false;
        }

        private void keyPressActionF(KeyPressEventArgs e)
        {
            if ((e.KeyChar >= '0' && e.KeyChar <= '9') || (e.KeyChar == 8) || (e.KeyChar == '.') || (e.KeyChar == '-'))
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }
        private void keyPressActionI(KeyPressEventArgs e)
        {
            if ((e.KeyChar >= '0' && e.KeyChar <= '9') || (e.KeyChar == 8) || (e.KeyChar == '-'))
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }
        private void keyPressActionIP(KeyPressEventArgs e)
        {
            if ((e.KeyChar >= '0' && e.KeyChar <= '9') || (e.KeyChar == 8) || (e.KeyChar == '.'))
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void textBox_homePos_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionF(e);
        }

        private void textBox_homeSpeed_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionF(e);
        }

        private void textBox_homeOffset_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionF(e);
        }

        private void textBox_resetPos_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionF(e);
        }

        private void textBox_resetSpeedH_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionF(e);
        }

        private void textBox_resetSpeedL_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionF(e);
        }

        private void textBox_resetOffset_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionF(e);
        }

        private void textBox_relPos_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionF(e);
        }

        private void textBox_relSpeed_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionF(e);
        }

        private void textBox_absPos_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionF(e);
        }

        private void textBox_absSpeed_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionF(e);
        }

        private void textBox_jogSpeed_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionF(e);
        }

        private void textBox_homeTime_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionI(e);
        }

        private void textBox_resetTime_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionI(e);
        }

        private void textBox_relTime_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionI(e);
        }

        private void textBox_absTime_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionI(e);
        }

        private void textBox_jogTime_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionI(e);
        }


        private void textBox_Time_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionI(e);
        }

        private void textBox_numSet_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionI(e);
        }

        private void textBox_testSpeed_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionF(e);
        }

        private void textBox_testStep_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionF(e);
        }

        private void textBox_testDelay_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionI(e);
        }

        private void textBox_testTravel_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionI(e);
        }

        private void textBox_change_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionIP(e);
        }

        private void textBox_ip_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyPressActionIP(e);
        }


    }
}
