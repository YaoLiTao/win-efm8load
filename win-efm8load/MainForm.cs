using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Security.Policy;
using System.Windows.Forms;
using Algorithm.Check;
using IntelHexFormatReader;
using IntelHexFormatReader.Model;

namespace win_efm8load
{
    public partial class MainForm : Form
    {
        private const int WM_DEVICECHANGE = 0x219; //设备改变
        private const int DBT_DEVICEARRIVAL = 0x8000; //检测到新设备
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004; //移除设备

        private bool debug = false;
        private SerialPort serial;
        private int flashSize;
        private int flashPageSize;
        private int flashSecurityPageSize;

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
            Print("################################################");
            Print("#                 win-efm8load                 #");
            Print("################################################");
        }

        private void Print(string format, params object[] args)
        {
            infoTextBox.Text += string.Format(format + "\r\n", args);
            RefreshInfoTextBox();
        }

        /**
         * 监听USB端口拔插
         */
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m); //调用父类方法，以确保其他功能正常
            switch (m.Msg)
            {
                case WM_DEVICECHANGE: //设备改变事件
                    //刷新串口设备
                    ScanCom();
                    break;
            }
        }

        /**
         * 扫描电脑串口
         */
        private void ScanCom()
        {
            scanComComboBox.Items.Clear();
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
                    ReadTimeout = 1000, // 1000ms
                };

                if (serial.IsOpen)
                {
                    serial.Close();
                }

                serial.Open();
                Print("打开串口[{0}]成功", serial.PortName);
            }
            catch (Exception e)
            {
                Print("打开串口[{0}]失败: {1}", serial.PortName, e);
            }
        }

        private void CloseSerialPort()
        {
            serial?.Close();
            Print("关闭串口[{0}]成功", serial?.PortName);
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
         * 下载程序到芯片
         */
        private void Upload()
        {
            Print("> uploading file '{0}'", openFileTextBox.Text);
            if (string.IsNullOrEmpty(openFileTextBox.Text))
            {
                Print("> 未选择程序文件，请先选择需要下载的程序文件！");
                return;
            }

            // identify chip
            IdentifyChip();

            // read hex file
            var hexRecords = LoadHex(openFileTextBox.Text);

            // send autoBaud training character
            SendAutoBaudTraining();

            // enable flash access
            EnableFlashAccess();

            // erase pages where we are going to write
            ErasePagesIh(hexRecords);

            // write all data bytes
            WritePagesIh(hexRecords);
            VerifyPVerifyPagesIh(hexRecords);
        }

        private void WritePagesIh(List<IntelHexRecord> hexRecords)
        {
            // write all segments from this ihex to flash
            //
            // NOTE:
            // it is important to keep flash location 0
            // equal to 0xFF until we are almost finished...
            // therefore the bootloader will still be functional in case
            // something goes wrong in the process.
            // (the bootloader will be executed as long the first flash
            // content equals 0xFF)

            var byteZero = -1;
            foreach (var hexRecord in hexRecords)
            {
                var start = hexRecord.Address;
                var end = hexRecord.Address + hexRecord.ByteCount;
                Print("> writing segment 0x{0:X4}-0x{1:X4}", start, end - 1);

                // fetch data
                var data = new List<byte>(hexRecord.Bytes);

                // write in 128byte blobs
                var dataPos = 0;

                // keep byte zero 0xFF in order to keep bootloader active (for now)
                if (start == 0)
                {
                    Print("> delaying write of flash[0] = 0x{0:X2} to the end", data[0]);
                    byteZero = data[0];
                    start = start + 1;
                    data.RemoveAt(0);
                }

                while ((dataPos + start) < end)
                {
                    var length = Math.Min(128, end - (dataPos + start));
                    Write(start + dataPos, data.GetRange(dataPos, dataPos + length).ToArray());
                    dataPos += length;
                }

                // now verify this segment
                Print("> verifying segment... ");

                if (Verify(start, data.ToArray()) == (int)RESPONSE.ACK)
                {
                    Print("OK");
                }
                else
                {
                    Print("FAILURE !");
                    throw new DataException();
                }
            }

            // all bytes except byte zero were written, do this now
            if (byteZero != -1)
            {
                Print("> will now write flash[0] = 0x{0:X2}", byteZero);
                var res = Write(0, new[] { (byte)byteZero });
                if (res != (int)RESPONSE.ACK)
                {
                    Print("> ERROR, write of flash[0] failed (response = {0})", res);
                    RestoreBootloaderAutostart();
                    throw new DataException();
                }

                res = Verify(0, new[] { (byte)byteZero });
                if (res != (int)RESPONSE.ACK)
                {
                    Print("> ERROR, write of flash[0] failed (response = {0})", res);
                    RestoreBootloaderAutostart();
                    throw new DataException();
                }
            }
        }

        private void RestoreBootloaderAutostart()
        {
            // the bootloader will always start if flash[0] = 0xFF
            // in case something went wrong during programming,
            // call this in order to clear page 0 so that the bootloader
            // will always start
            Print("> will now erase page 0 in order to re-enable bootloader autorun");
            ErasePage(0);
        }

        private int Write(int address, byte[] data)
        {
            if (data.Length > 128)
            {
                Print("ERROR: invalid chunksize, maximum allowed write is 128 bytes ({})", data.Length);
                throw new DataException();
            }

            // send request
            var addressHi = (address >> 8) & 0xFF;
            var addressLo = address & 0xFF;
            var bytes = new[] { (byte)addressHi, (byte)addressLo }.ToList();
            bytes.AddRange(data);
            var res = Send(COMMAND.WRITE, bytes.ToArray());
            if (res != (int)RESPONSE.ACK) return res;
            Print("ERROR: write failed at address 0x{0:X3} (response = {1})", address, res);
            throw new DataException();
        }

        private int Verify(int address, byte[] data)
        {
            var length = data.Length;
            var crc16 = ExtensionForCRC16.CRC16(data, ExtensionForCRC16.CRC16Type.CCITTxModem);
            if (debug) Print("> verify address 0x{0:X4} (len={1}, crc16=0x{0:X4})", address, length, crc16);
            var startHi = (address >> 8) & 0xFF;
            var startLo = address & 0xFF;
            var end = address + length - 1;
            var endHi = (end >> 8) & 0xFF;
            var endLo = end & 0xFF;
            var crcHi = (crc16 >> 8) & 0xFF;
            var crcLo = crc16 & 0xFF;
            return Send(COMMAND.VERIFY,
                new[] { (byte)startHi, (byte)startLo, (byte)endHi, (byte)endLo, (byte)crcHi, (byte)crcLo });
        }

        private void VerifyPVerifyPagesIh(List<IntelHexRecord> hexRecords)
        {
            // verify written data
            //
            // do a pagewise compare to find the position of
            // the mismatch

            foreach (var hexRecord in hexRecords)
            {
                var start = hexRecord.Address;
                var end = hexRecord.Address + hexRecord.ByteCount;
                Print("> verifying segment 0x{0:X4}-0x{0:X4}... ", start, end - 1);

                // calc crc16
                if (Verify(start, hexRecord.Bytes) == (int)RESPONSE.ACK)
                {
                    Print("OK");
                }
                else
                {
                    Print("FAILURE !");
                    throw new DataException();
                }
            }
        }

        private void ErasePagesIh(List<IntelHexRecord> hexRecords)
        {
            // erase all pages that are occupied
            var lastAddress = hexRecords[-1].Address;
            var lastPage = (lastAddress / flashPageSize);
            for (var page = 0; page < lastPage + 1; page++)
            {
                var start = page * flashPageSize;
                var end = start + flashPageSize - 1;
                var pageUsed = false;
                foreach (var hexRecord in hexRecords)
                {
                    if (hexRecord.Address >= start && hexRecord.Address <= end)
                    {
                        pageUsed = true;
                        break;
                    }
                }

                // always erase page 0 to retain bootloader access
                if (page == 0 || pageUsed)
                {
                    ErasePage(page);
                }
            }
        }

        private int ErasePage(int page)
        {
            var start = page * flashPageSize;
            var end = start + flashPageSize - 1;
            var startHi = (start >> 8) & 0xFF;
            var startLo = start & 0xFF;
            Print("> will erase page {0} (0x{1:X4}-0x{2:X4})", page, start, end);
            return Send(COMMAND.ERASE, new[] { (byte)startHi, (byte)startLo });
        }

        private List<IntelHexRecord> LoadHex(string fileName)
        {
            var hexRecords = new List<IntelHexRecord>();
            foreach (var line in File.ReadLines(fileName))
            {
                var hexRecord = HexFileLineParser.ParseLine(line);
                hexRecords.Add(hexRecord);
            }

            return hexRecords;
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
        private void IdentifyChip()
        {
            Print("> checking for device");

            // send autobaud training
            SendAutoBaudTraining();

            // enable flash access
            EnableFlashAccess();

            // we will now iterate through all known device ids
            foreach (var device in deviceList)
            {
                if (debug) Print("> checking for device %s", device.DeviceName);
                foreach (var variant in device.Variant)
                {
                    if (CheckId(device.DeviceId, variant.VariantId))
                    {
                        Print("> success, detected {0} cpu (variant {1})", device.DeviceName, variant.VariantName);
                        // set up chip data
                        flashSize = variant.FlashSize;
                        flashPageSize = variant.PageSize;
                        flashSecurityPageSize = variant.SecurityPageSize;
                        Print("> detected {0} cpu (variant {1}, flash_size={2}, pagesize={3})",
                            device.DeviceName, variant.VariantName, flashSize, flashPageSize);
                    }
                }
            }

            for (byte deviceId = 0; deviceId < 0xFF; deviceId++)
            {
                Print("\r\n> checking device_id 0x{0:X2}...", deviceId);
                for (byte variantId = 0; variantId < 24; variantId++)
                {
                    if (CheckId(deviceId, variantId))
                    {
                        Print("\n> ERROR: unknown device detected: id=0x{0:X2}, variant=0x{1:X2}\n" +
                              "         please add it to the deviceList. will exit now\n", deviceId, variantId);
                    }
                }
            }

            Print("> ERROR: could not find any device...");
        }

        /**
         * 训练波特率
         */
        private void SendAutoBaudTraining()
        {
            if (debug) Print("> sending training char 0xFF");
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
                Print("ERROR: failed to send byte to serial port");
                throw;
            }
        }

        private void EnableFlashAccess()
        {
            var res = Send(COMMAND.SETUP, new byte[] { 0xA5, 0xF1, 0x00 });
            if (res == (int)RESPONSE.ACK) return;
            Print("> ERROR enabling flash access, error code 0x{0:X2}", res);
            throw new DataException();
        }

        private int Send(COMMAND cmd, byte[] data)
        {
            Print("> send command: {0}", cmd);
            if (data.Length < 2 || data.Length > 130)
            {
                Print("> ERROR: invalid data length! allowed 2...130, got {0}", data.Length);
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
                if (debug) Print("> reply 0x{0:X2}", resBytes[0]);
                return resBytes[0];
            }
            catch (Exception e)
            {
                Print("ERROR: failed to send data: {0}", e);
                throw;
            }
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
            Upload();
            CloseSerialPort();
        }
    }
}