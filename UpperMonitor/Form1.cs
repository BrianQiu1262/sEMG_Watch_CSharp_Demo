using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Runtime.InteropServices;


namespace UpperMonitor
{

    public partial class Form1 : Form
    {
       
        private List<byte> buffer = new List<byte>();//用于缓存数据的链表buffer
        private List<byte> msg_buf = new List<byte>();//用于缓存数据的链表buffer
        private byte[] buf;
        private byte[] data = new byte[93];//用于储存得到的有效单数据帧

        // 曲线绘制计数器
        int counter = 0;

        int count21 = 0;
        int count31 = 0;

        int count22 = 0;
        int count32 = 0;

        int count23 = 0;
        int count33 = 0;

        int count24 = 0;
        int count34 = 0;

        int count25 = 0;
        int count35 = 0;

        int count26 = 0;
        int count36 = 0;

        int count27 = 0;
        int count37 = 0;

        int count28 = 0;
        int count38 = 0;

        // 曲线比例缩放系数
        double ratio1 = 1;
        double ratio2 = 1;
        double ratio3 = 1;
        double ratio4 = 1;
        double ratio5 = 1;
        double ratio6 = 1;
        double ratio7 = 1;
        double ratio8 = 1;

        int start_point = 0;//数据记录起始断点

        int fresh_interval = 180;//刷新间隔

        //private static object lockObject = new Object();

        const Int16 QueeueLength = 500;//队列长度定义
        //用于存放sEMG数据的链表
        private List<double> list1 = new List<double>();
        private List<double> list2 = new List<double>();
        private List<double> list3 = new List<double>();
        private List<double> list4 = new List<double>();
        private List<double> list5 = new List<double>();
        private List<double> list6 = new List<double>();
        private List<double> list7 = new List<double>();
        private List<double> list8 = new List<double>();

        //多线程绘图委托对象
        public delegate void Fresh(); 
        public Fresh showdata;
        public Fresh plotdata;
        public Fresh multiplot1;
        public Fresh multiplot2;
        public Fresh multiplot3;
        public Fresh multiplot4;
        public Fresh multiplot5;
        public Fresh multiplot6;
        public Fresh multiplot7;
        public Fresh multiplot8;

        //多线程，串口读取及图形绘制
        private Thread ReadThread; 
        private Thread MultiShowData1;
        private Thread MultiShowData2;
        private Thread MultiShowData3;
        private Thread MultiShowData4;
        private Thread MultiShowData5;
        private Thread MultiShowData6;
        private Thread MultiShowData7;
        private Thread MultiShowData8;
        string path = Application.StartupPath;

        //sEMG
        List<double>[] GetList()//创建列表的索引数组
        {
            return new List<double>[] { list1, list2, list3, list4,
                                        list5, list6, list7, list8


            };//返回一个对象数组
        }
        private List<double>[] list;//列表索引对象

        //tcp 通讯
        TcpClient _tcpClient;
        NetworkStream _stream;

        // 键鼠控制
        // ---------- Win32 API 声明 ----------
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type; // 0: 鼠标, 1: 键盘
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;   // 滚轮
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        // 键盘虚拟键码
        private const ushort VK_UP = 0x26;
        private const ushort VK_DOWN = 0x28;
        private const ushort VK_CONTROL = 0x11;

        // 键盘标志
        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        // 鼠标标志
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint WHEEL_DELTA = 120;          // 标准一步滚动量
        private const int SCROLL_STEPS = 4;             // 拆分为几步
        private const int STEP_DELAY_MS = 10;           // 每步间隔

        private int start_hmi = 0;

        public Form1()
        {
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
            this.SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint |
               ControlStyles.AllPaintingInWmPaint,true);//开启双缓冲
            this.UpdateStyles();
            InitializeComponent();

            Directory.SetCurrentDirectory(Directory.GetParent(path).FullName);
            path = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Directory.GetParent(path).FullName);
            path = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Directory.GetParent(path).FullName);
            path = Directory.GetCurrentDirectory() + @"\DATA\";

            list = GetList();//创建列表索引对象实例
        }

        SerialPort com = new SerialPort();//定义串口com
        

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.Text = "5";//COM默认值
            comboBox2.Text = "1000000";//波特率默认值

            //多线程绘图委托实例
            multiplot1 = new Fresh(MultiChartShow1);
            multiplot2 = new Fresh(MultiChartShow2);
            multiplot3 = new Fresh(MultiChartShow3);
            multiplot4 = new Fresh(MultiChartShow4);
            multiplot5 = new Fresh(MultiChartShow5);
            multiplot6 = new Fresh(MultiChartShow6);
            multiplot7 = new Fresh(MultiChartShow7);
            multiplot8 = new Fresh(MultiChartShow8);

            //定义开关初始属性
            button1.Enabled = true;
            button2.Enabled = false;
            button8.Enabled = false;
        }

        double up_max = double.MaxValue;
        double down_max = -double.MaxValue;
        #region 多线程绘制波形图像
        private void MultiPlotData(UInt16 channel, Chart chart, ref int count2, ref int count3, ref double ratio)
        {
            //在chart上画图
            if (list[channel - 1].Count > 0)

            {
                for (int i = count2; i < list[channel - 1].Count; i++)
                {
                    count3++;
                    if (i % 1 == 0)
                    {
                        // 动态值域绘图
                        double y = (list[channel - 1].ElementAt(i));
                        if (y * ratio <= up_max && y * ratio >= down_max)
                        {
                            y = y * ratio;
                        }
                        if (y * ratio > up_max)
                        {
                            y = up_max;
                        }
                        if (y * ratio < down_max)
                        {
                            y = down_max;
                        }
                        chart.Series[0].Points.AddXY(count3, y);

                        //滚动更新
                        if (count3 > QueeueLength)
                        {
                            chart.Series[0].Points.RemoveAt(0);
                            chart.ChartAreas[0].AxisX.Maximum++;
                            chart.ChartAreas[0].AxisX.Minimum++;
                        }
                    }

                }
                count2 = list[channel - 1].Count - 1;
            }
        }

        void MultiChartShow1()
        {
            MultiPlotData(1, chart1, ref count21, ref count31, ref ratio1);
        }

        void MultiChartShow2()
        {
            MultiPlotData(2, chart2, ref count22, ref count32, ref ratio2);
        }

        void MultiChartShow3()
        {
            MultiPlotData(3, chart3, ref count23, ref count33, ref ratio3);
        }

        void MultiChartShow4()
        {
            MultiPlotData(4, chart4, ref count24, ref count34, ref ratio4);
        }

        void MultiChartShow5()
        {
            MultiPlotData(5, chart5, ref count25, ref count35, ref ratio5);
        }

        void MultiChartShow6()
        {
            MultiPlotData(6, chart6, ref count26, ref count36, ref ratio6);
        }

        void MultiChartShow7()
        {
            MultiPlotData(7, chart7, ref count27, ref count37, ref ratio7);
        }

        void MultiChartShow8()
        {
            MultiPlotData(8, chart8, ref count28, ref count38, ref ratio8);
        }

        void ChartShow1()
        {
            while (true)
            {
                try
                {
                    Invoke(multiplot1);
                }
                catch
                {
                    break;
                }
                Thread.Sleep(fresh_interval);
            }
         }

        void ChartShow2()
        {
            while (true)
            {
                try
                {
                    Invoke(multiplot2);
                }
                catch
                {
                    break;
                }
                Thread.Sleep(fresh_interval);
            }
        }

        void ChartShow3()
        {
            while (true)
            {
                try
                {
                    Invoke(multiplot3);
                }
                catch
                {
                    break;
                }
                Thread.Sleep(fresh_interval);
            }
        }

        void ChartShow4()
        {
            while (true)
            {
                try
                {
                    Invoke(multiplot4);
                }
                catch
                {
                    break;
                }
                Thread.Sleep(fresh_interval);
            }
        }

        void ChartShow5()
        {
            while (true)
            {
                try
                {
                    Invoke(multiplot5);
                }
                catch
                {
                    break;
                }
                Thread.Sleep(fresh_interval);
            }
        }

        void ChartShow6()
        {
            while (true)
            {
                try
                {
                    Invoke(multiplot6);
                }
                catch
                {
                    break;
                }
                Thread.Sleep(fresh_interval);
            }
        }

        void ChartShow7()
        {
            while (true)
            {
                try
                {
                    Invoke(multiplot7);
                }
                catch
                {
                    break;
                }
                Thread.Sleep(fresh_interval);
            }
        }

        void ChartShow8()
        {
            while (true)
            {
                try
                {
                    Invoke(multiplot8);
                }
                catch
                {
                    break;
                }
                Thread.Sleep(fresh_interval);
            }
        }

        #endregion

        #region 读数据线程
        void ReadData()
        {
            int msg_buf_counter = 0;

            while (true)
            {

                int n = com.BytesToRead;//待读字节个数
                buf = new byte[n];//创建n个字节的缓存
                com.Read(buf, 0, n);//读到在数据存储到buf


                buffer.AddRange(buf);//不断地将接收到的数据加入到buffer链表中

                while (buffer.Count >= 4) //0xAA 0xAA 0xF1 0x??(数据内容长度)
                {
                    byte sum = 0;

                    //2.1 查找数据头
                    if ((buffer[0] == 0xAA) && (buffer[1] == 0xAA) && (buffer[2] == 0xF1)) //传输数据有帧头，用于判断. 找到帧头  AA AA F1 
                    {
                        int len = buffer[3];//数据帧中数据内容总长度

                        if (buffer.Count < len + 5) //数据区尚未接收完整，4bytes帧头，1bytes帧尾校验位
                            break;//跳出接收函数后之后继续接收数据

                        counter++;
                        //将低八位byte和高八位byte合并成一个uint16
                        for (UInt16 i = 0; i < 8; i++)
                        {
                            byte b1 = buffer[i * 3 + 4];
                            byte b2 = buffer[i * 3 + 5];
                            byte b3 = buffer[i * 3 + 6];
                            byte[] bytes = new byte[] { 0x00, b3, b2, b1 };
                            Int32 i32 = BitConverter.ToInt32(bytes, 0);
                            double f = i32 / 256 * 0.0397;
                            (list[i]).Add(f);
                        }

                        // TCP将数据实时转发至Python端
                        msg_buf.AddRange(buffer.GetRange(0, len + 5));
                        msg_buf_counter++;
                        if (msg_buf_counter == 5)
                        {
                            byte[] msg = msg_buf.GetRange(0, (len + 5) * 5).ToArray();
                            tcp_send(msg);
                            msg_buf.Clear();
                            msg_buf_counter = 0;
                        }

                        buffer.RemoveRange(0, len +5);//从buffer中删除已读数据

                    }
                    else //帧头不正确时，记得清除
                    {
                        buffer.RemoveAt(0);//清除第一个字节，继续检测下一个。
                    }

                }

            }

        }
#endregion 

        #region close按钮
        //close按钮
        String s;
        private void button2_Click(object sender, EventArgs e)
        {
            //停止绘图线程
            MultiShowData1.Abort();
            MultiShowData2.Abort();
            MultiShowData3.Abort();
            MultiShowData4.Abort();
            MultiShowData5.Abort();
            MultiShowData6.Abort();
            MultiShowData7.Abort();
            MultiShowData8.Abort();

            ReadThread.Abort();//终止数据读取线程

            string strText = string.Empty;

            Form1.Show(out strText);
            s = strText;//文件名

            if (strText != string.Empty)
            {
                SaveToFile(strText);
            }
            
            try
            {
                com.Close();//关闭串口      
            }
            catch (Exception)
            {
                    
            }
            button1.Enabled = true;
            button2.Enabled = false;

        }
        #endregion

        #region open按钮
        private void button1_Click(object sender, EventArgs e)
        {

            try
            {
                //打开串口
                com.BaudRate = Convert.ToInt32(comboBox2.Text, 10);//设置波特率各值
                com.PortName = "COM" + textBox1.Text;
                com.DataBits = 8;
                com.ReadBufferSize = 4096;
                com.Open();  

                tcp_connect();

                ReadThread = new Thread(ReadData);//创建线程例程

                ReadThread.Start();//打开串口读取线程
                button1.Enabled = false;//打开串口按钮不可用
                button2.Enabled = true;//关闭串口

                InitChart(chart1);//绘图界面初始化1
                MultiShowData1 = new Thread(ChartShow1);
                MultiShowData1.Start();

                InitChart(chart2);//绘图界面初始化2
                MultiShowData2 = new Thread(ChartShow2);
                MultiShowData2.Start();

                InitChart(chart3);//绘图界面初始化3
                MultiShowData3 = new Thread(ChartShow3);
                MultiShowData3.Start();

                InitChart(chart4);//绘图界面初始化4
                MultiShowData4 = new Thread(ChartShow4);
                MultiShowData4.Start();

                InitChart(chart5);//绘图界面初始化5
                MultiShowData5 = new Thread(ChartShow5);
                MultiShowData5.Start();

                InitChart(chart6);//绘图界面初始化6
                MultiShowData6 = new Thread(ChartShow6);
                MultiShowData6.Start();

                InitChart(chart7);//绘图界面初始化7
                MultiShowData7 = new Thread(ChartShow7);
                MultiShowData7.Start();

                InitChart(chart8);//绘图界面初始化8
                MultiShowData8 = new Thread(ChartShow8);
                MultiShowData8.Start();

                button1.Enabled = false;
                button2.Enabled = false;
                button8.Enabled = true;

            }
            catch
            {
                MessageBox.Show("Port error, please check the port!", "Error");//报错
            }


        }
        #endregion

        #region 弹窗获得输入的信息字符
        public static DialogResult Show(out string strText)
        {
            string strTemp = string.Empty;

            Form2 inputDialog = new Form2();//创建新的弹窗窗体Form2
            inputDialog.TextHandler = (str) => { strTemp = str; };

            DialogResult result = inputDialog.ShowDialog();
            strText = strTemp;

            return result;
        }
        #endregion

        #region 初始化图表
        private void InitChart(Chart chart)
        {
            Series series = new Series("sEMG");
            series.ChartType = SeriesChartType.Line;
            chart.Series.Add(series);
            //设置图表显示样式
            chart.ChartAreas[0].AxisY.Minimum = -1000;
            chart.ChartAreas[0].AxisY.Maximum = 1000;
            chart.ChartAreas[0].AxisX.Interval = 200;
            chart.ChartAreas[0].AxisX.Maximum = QueeueLength;
            chart.ChartAreas[0].AxisX.Minimum = 0;
            chart.ChartAreas[0].AxisX.Enabled = AxisEnabled.False;
            chart.Series[0].Color = Color.Blue;
        }

        #endregion

        #region 保存数据到csv文件
        public void SaveToFile(string txt )
        {
           // 确定数据储存长度
            int totalnum = list[0].Count();
            for (short i = 0; i < 8; i++)
            {
                if (totalnum > list[i].Count())
                {
                    totalnum = list[i].Count();
                }
            }
            string file_path = path + txt + ".csv";
            if (totalnum > 0)
            {
                if (!File.Exists(file_path))
                {
                    File.Create(file_path).Close();
                }
                else
                {
                    File.Delete(file_path);
                }
                    
                StreamWriter sw = new StreamWriter(file_path, true, Encoding.UTF8);

                //写入表头
                sw.Write("Time" + ",");
                for (short i = 0; i < 8; i++)
                {             
                    sw.Write("sEMG" + i + ",");
                }
                sw.Write("\r\n");

                //写入数据
                for (int i = start_point; i < totalnum; i++)
                {
                    sw.Write((Double)(i-start_point)/250 + ",");
                    for (int j = 0; j < 8; j++)
                    {
                        sw.Write(list[j].ElementAt(i) + ",");
                    }

                    sw.Write("\r\n");

                }

                sw.Flush();
                sw.Close();

                MessageBox.Show("Data were saved to the csv file!", "Success");
            }
            
        }
        #endregion

        #region start按钮开始记录数据
        private void button8_Click(object sender, EventArgs e)
        {
            start_point = counter;
            button8.Enabled = false;
            button2.Enabled = true;
        }
        #endregion

        #region TCP
        // 连接服务器
        private async void tcp_connect()
        {
            string host = "127.0.0.1";
            int port = 9527;

            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(host, port);
                _stream = _tcpClient.GetStream();

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 已连接到服务器 {host}:{port}");

                Task.Run(() => ReceiveLoop());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 连接失败: {ex.Message}");
            }
        }

        // TCP发送
        private async void tcp_send(byte[] data)
        {
            if (_tcpClient == null || !_tcpClient.Connected)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 未连接到服务器");
                return;
            }

            try
            {
                await _stream.WriteAsync(data, 0, data.Length);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 发送: {data.Length}字节");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 发送失败: {ex.Message}");
            }
        }

        //TCP接收
        private void ReceiveLoop()
        {
            byte[] recvBuffer = new byte[1024];
            byte[] buf1;
            buf1 = new byte[1];
            string last = "0";
            bool hmi_flag = false;

            while (true)
            {
                try
                {
                    while (_tcpClient.Connected)
                    {
                        int bytesRead = _stream.Read(recvBuffer, 0, recvBuffer.Length);
                        if (bytesRead == 0) break;

                        string response = Encoding.UTF8.GetString(recvBuffer, 0, bytesRead);
                        label4.Text = response;

                        switch (response)
                        {   
                            case "0":
                                buf1[0] = 0;
                                com.Write(buf1, 0, 1); 
                                break;
                            case "1":
                                if (last == "0")
                                {
                                    hmi_flag = true;
                                }
                                buf1[0] = 1;
                                com.Write(buf1, 0, 1); ;
                                break;
                            case "2":
                                if (last == "0")
                                {
                                    hmi_flag = true;
                                }
                                buf1[0] = 2;
                                com.Write(buf1, 0, 1); ;
                                break;
                            case "3":
                                if (last == "0")
                                {
                                    hmi_flag = true;
                                }
                                buf1[0] = 3;
                                com.Write(buf1, 0, 1); ;
                                break;
                            case "4":
                                if (last == "0")
                                {
                                    hmi_flag = true;
                                }
                                buf1[0] = 4;
                                com.Write(buf1, 0, 1); ;
                                break;
                            default:
                                break;
                        }
                        last = response;
                        if (start_hmi == 1 && hmi_flag)
                        {
                            ExecuteAction(label4.Text);
                            hmi_flag = false;
                        }
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 收到: {response}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 接收异常: {ex.Message}");
                }
                finally
                {
                    _tcpClient?.Close();
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 连接已关闭");
                }
            }

        }

        #endregion

        #region 键鼠交互

        // 根据 action 执行对应的模拟操作

        // <param name="action">1:↑ 2:↓ 3:Ctrl+前滚 4:Ctrl+后滚</param>
        private void ExecuteAction(string action)
        {
            switch (action)
            {
                case "1":
                    SimulateKeyPress(VK_UP);
                    Console.WriteLine("Key Up");
                    break;
                case "2":
                    SimulateKeyPress(VK_DOWN);
                    Console.WriteLine("Key Down");
                    break;
                case "3":
                    SimulateCtrlWheel(false);  // 向前（正数）
                    Console.WriteLine("Zoom in");
                    break;
                case "4":
                    SimulateCtrlWheel(true); // 向后（负数）
                    Console.WriteLine("Zoom out");
                    break;
                default:
                    break;
            }
        }

        // 模拟单个按键（按下+松开）
        private void SimulateKeyPress(ushort vkCode)
        {
            INPUT[] inputs = new INPUT[2];

            // KeyDown
            inputs[0].type = 1; // keyboard
            inputs[0].u.ki.wVk = vkCode;
            inputs[0].u.ki.dwFlags = KEYEVENTF_KEYDOWN;

            // KeyUp
            inputs[1].type = 1;
            inputs[1].u.ki.wVk = vkCode;
            inputs[1].u.ki.dwFlags = KEYEVENTF_KEYUP;

            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }


        // 模拟 Ctrl + 鼠标滚轮（缓慢分步）
        // <param name="forward">true=向前(放大)，false=向后(缩小)</param>
        private void SimulateCtrlWheel(bool forward)
        {
            // 1. 按下 Ctrl 键
            PressKey(VK_CONTROL, true);

            // 2. 分步发送滚轮事件
            int totalDelta = (int)(forward ? WHEEL_DELTA : -WHEEL_DELTA);
            int stepDelta = totalDelta / SCROLL_STEPS;   // 每步滚动量

            for (int i = 0; i < SCROLL_STEPS; i++)
            {
                SendWheelEvent(stepDelta);
                Thread.Sleep(STEP_DELAY_MS);   // 微小延迟，模拟缓慢
            }

            // 3. 释放 Ctrl 键
            PressKey(VK_CONTROL, false);
        }


        // 按下或释放一个键（不自动弹起）
        private void PressKey(ushort vkCode, bool down)
        {
            INPUT input = new INPUT();
            input.type = 1;
            input.u.ki.wVk = vkCode;
            input.u.ki.dwFlags = down ? KEYEVENTF_KEYDOWN : KEYEVENTF_KEYUP;
            SendInput(1, new INPUT[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        //发送一次鼠标滚轮事件（相对当前位置）
        private void SendWheelEvent(int delta)
        {
            INPUT input = new INPUT();
            input.type = 0; // mouse
            input.u.mi.dwFlags = MOUSEEVENTF_WHEEL;
            input.u.mi.mouseData = (uint)delta;  // 正数向前，负数向后
            SendInput(1, new INPUT[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }
        #endregion

        // 是否启用HMI键鼠交互功能
        private void button3_Click(object sender, EventArgs e)
        {
            if (start_hmi == 1)
            {
                start_hmi = 0;
            }
            if (start_hmi == 0)
            {
                start_hmi =1;
            }
        }
    }



}
