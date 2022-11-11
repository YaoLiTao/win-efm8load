using System;
using System.IO.Ports;
using System.Windows.Forms;

namespace win_efm8load
{
    public partial class MainForm : Form
    {
        private bool debug = false;
        private SerialPort serial;
        private int flash_size;
        private int flash_page_size;
        private int flash_security_page_size;

        /**
         * 初始化芯片列表
         */
        private Device[] deviceList =
        {
            new Device()
            {
                DeviceId = 0x16,
                DeviceName = "EFM8SB2",
                Variant = Array.Empty<Variant>()
            },

            new Device()
            {
                DeviceId = 0x25,
                DeviceName = "EFM8SB1",
                Variant = new[]
                {
                    new Variant(0x01, "EFM8SB10F8G_QFN24", 8 * 1024, 512, 512),
                    new Variant(0x02, "EFM8SB10F8G_QSOP24", 8 * 1024, 512, 512),
                    new Variant(0x03, "EFM8SB10F8G_QFN20", 8 * 1024, 512, 512),
                    new Variant(0x06, "EFM8SB10F4G_QFN20", 4 * 1024, 512, 512),
                    new Variant(0x09, "EFM8SB10F2G_QFN20", 2 * 1024, 512, 512),
                }
            },

            new Device()
            {
                DeviceId = 0x30,
                DeviceName = "EFM8BB1",
                Variant = new[]
                {
                    new Variant(0x01, "EFM8BB10F8G_QSOP24", 8 * 1024, 512, 512),
                    new Variant(0x02, "EFM8BB10F8G_QFN20", 8 * 1024, 512, 512),
                    new Variant(0x03, "EFM8BB10F8G_SOIC16", 8 * 1024, 512, 512),
                    new Variant(0x05, "EFM8BB10F4G_QFN20", 4 * 1024, 512, 512),
                    new Variant(0x08, "EFM8BB10F2G_QFN20", 2 * 1024, 512, 512),
                    new Variant(0x12, "EFM8BB10F8I_QFN20", 8 * 1024, 512, 521),
                }
            },

            new Device()
            {
                DeviceId = 0x32,
                DeviceName = "EFM8BB2",
                Variant = new[]
                {
                    new Variant(0x01, "EFM8BB22F16G_QFN28", 16 * 1024, 512, 512),
                    new Variant(0x02, "EFM8BB21F16G_QSOP24", 16 * 1024, 512, 512),
                    new Variant(0x03, "EFM8BB21F16G_QFN20", 16 * 1024, 512, 512),
                }
            },

            new Device()
            {
                DeviceId = 0x34,
                DeviceName = "EFM8BB3",
                Variant = new[]
                {
                    new Variant(0x01, "EFM8BB31F64G-QFN32", 64 * 1024, 512, 512),
                }
            },
        };

        class Device
        {
            public byte DeviceId { set; get; }
            public string DeviceName { set; get; }
            public Variant[] Variant { set; get; }
        }

        class Variant
        {
            public Variant(byte variantId, string variantName, int flashSize, int pageSize, int securityPageSize)
            {
                VariantId = variantId;
                VariantName = variantName;
                FlashSize = flashSize;
                PageSize = pageSize;
                SecurityPageSize = securityPageSize;
            }

            public byte VariantId { set; get; }
            public string VariantName { set; get; }
            public int FlashSize { set; get; }
            public int PageSize { set; get; }
            public int SecurityPageSize { set; get; }
        }


        enum COMMAND
        {
            IDENTIFY = 0x30,
            SETUP = 0x31,
            ERASE = 0x32,
            WRITE = 0x33,
            VERIFY = 0x34,
            RESET = 0x36,
        }

        enum RESPONSE
        {
            ACK = 0x40,
            RANGE_ERROR = 0x41,
            BAD_ID = 0x42,
            CRC_ERROR = 0x43,
        }

        public MainForm()
        {
            InitializeComponent();
            InitBaudRate();
            ScanCom();
        }

        private void print(string format, params object[] args)
        {
            infoTextBox.Text += string.Format(format + "\r\n", args);
            RefreshInfoTextBox();
        }

        /**
         * 扫描电脑串口
         */
        private void ScanCom()
        {
            foreach (var portName in SerialPort.GetPortNames())
            {
                scanComComboBox.Items.Add(portName);
            }

            if (scanComComboBox.Items.Count > 0)
            {
                scanComComboBox.SelectedIndex = scanComComboBox.Items.Count - 1;
            }
        }

        /**
         * 初始化波特率
         */
        private void InitBaudRate()
        {
            baudRateComboBox.Items.Add("38400");
            baudRateComboBox.Items.Add("57600");
            baudRateComboBox.Items.Add("115200");
            baudRateComboBox.Items.Add("230400");
            baudRateComboBox.SelectedIndex = 2;
        }

        private void OpenSerialPort()
        {
            try
            {
                serial = new SerialPort()
                {
                    PortName = scanComComboBox.Text,
                    BaudRate = int.Parse(baudRateComboBox.Text),
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    ReadTimeout = 1000 // 1000ms
                };

                if (serial.IsOpen)
                {
                    serial.Close();
                }

                serial.Open();
                print("打开串口成功: {0}", serial.PortName);
            }
            catch (Exception e)
            {
                print("打开串口失败: {0}", e);
            }
        }

        private void CloseSerialPort()
        {
            serial?.Close();
            print("关闭串口成功: {0}", serial?.PortName);
        }

        /**
         * 将文本框刷新到最下面
         */
        private void RefreshInfoTextBox()
        {
            infoTextBox.SelectionStart = infoTextBox.TextLength;
            infoTextBox.ScrollToCaret();
        }

        /**
         * 开始下载程序
         */
        private void StartProgram()
        {
            IdentifyChip();
        }

        /**
         * 选择文件
         */
        private void SelectHexFile()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.Title = "请选择程序HEX文件";
            dialog.Filter = "*.hex|*.HEX";
            if (dialog.ShowDialog() != DialogResult.OK) return;
            openFileTextBox.Text = dialog.FileName;
            infoTextBox.Text += "文件选择成功：" + openFileTextBox.Text + "\r\n";
        }

        /**
         * 扫描芯片
         */
        private int IdentifyChip()
        {
            print("> checking for device");

            // send autobaud training
            SendAutoBaudTraining();

            // enable flash access
            EnableFlashAccess();

            // we will now iterate through all known device ids
            foreach (var device in deviceList)
            {
                if (debug) print("> checking for device %s", device.DeviceName);
                foreach (var variant in device.Variant)
                {
                    if (CheckId(device.DeviceId, variant.VariantId))
                    {
                        print("> success, detected {0} cpu (variant {1})", device.DeviceName, variant.VariantName);
                        // set up chip data
                        flash_size = variant.FlashSize;
                        flash_page_size = variant.PageSize;
                        flash_security_page_size = variant.SecurityPageSize;
                        print("> detected {0} cpu (variant {1}, flash_size={2}, pagesize={3})",
                            device.DeviceName, variant.VariantName, flash_size, flash_page_size);
                        return 1;
                    }
                }
            }

            for (byte deviceId = 0; deviceId < 0xFF; deviceId++)
            {
                print("\r\n> checking device_id 0x{0:X2}...", deviceId);
                for (byte variantId = 0; variantId < 24; variantId++)
                {
                    if (CheckId(deviceId, variantId))
                    {
                        print("\n> ERROR: unknown device detected: id=0x{0:X2}, variant=0x{1:X2}\n" +
                              "         please add it to the deviceList. will exit now\n", deviceId, variantId);
                    }
                }
            }

            print("> ERROR: could not find any device...");
            return -1;
        }

        /**
         * 训练波特率
         */
        private void SendAutoBaudTraining()
        {
            if (debug) print("> sending training char 0xFF");
            for (int i = 0; i < 2; i++)
            {
                SendByte(0xff);
            }
        }

        private void SendByte(byte b)
        {
            try
            {
                serial.Write(new[] { b }, 0, 1);
            }
            catch (Exception e)
            {
                print("ERROR: failed to send byte to serial port");
            }
        }

        private void EnableFlashAccess()
        {
            var res = Send(COMMAND.SETUP, new byte[] { 0xA5, 0xF1, 0x00 });
            if (res != (int)RESPONSE.ACK) print("> ERROR enabling flash access, error code 0x{0:X2}", res);
        }

        private int Send(COMMAND cmd, byte[] data)
        {
            print("> send command: {0}", cmd);
            if (data.Length < 2 || data.Length > 130)
            {
                print("> ERROR: invalid data length! allowed 2...130, got {0}", data.Length);
                return -1;
            }

            try
            {
                serial.Write(new[] { '$' }, 0, 1);
                serial.Write(new[] { (byte)(data.Length + 1) }, 0, 1);
                serial.Write(new[] { (byte)cmd }, 0, 1);
                serial.Write(data, 0, data.Length);

                // read back reply
                var resBytes = new byte[1];
                serial.Read(resBytes, 0, 1);

                // res_bytes = b"\x40"
                if (debug) print("> reply 0x{0:X2}", resBytes[0]);
                return resBytes[0];
            }
            catch (Exception e)
            {
                print("ERROR: failed to send data: {0}", e);
            }

            return -1;
        }

        private bool CheckId(byte deviceId, byte variantId)
        {
            return Send(COMMAND.IDENTIFY, new[] { deviceId, variantId }) == (int)RESPONSE.ACK;
        }

        /**************************** 点击事件 ****************************/

        private void scanComButton_Click(object sender, EventArgs e)
        {
            ScanCom();
        }

        private void openFileButton_Click(object sender, EventArgs e)
        {
            SelectHexFile();
        }

        private void scanMcuButton_Click(object sender, EventArgs e)
        {
            OpenSerialPort();
            IdentifyChip();
            CloseSerialPort();
        }

        private void readMcuButton_Click(object sender, EventArgs e)
        {
        }

        private void programButton_Click(object sender, EventArgs e)
        {
            OpenSerialPort();
            StartProgram();
            CloseSerialPort();
        }
    }
}