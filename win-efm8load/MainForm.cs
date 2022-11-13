//
// This file is part of efm8load. efm8load is free software: you can
// redistribute it and/or modify it under the terms of the GNU General Public
// License as published by the Free Software Foundation, version 3.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more
// details.
//
// You should have received a copy of the GNU General Public License along with
// this program; if not, write to the Free Software Foundation, Inc., 51
// Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
//
// Copyright 2020 fishpepper.de
//

// v0.1
// 完成基本下载程序功能，尚未完成读取芯片程序功能

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Ports;
using System.Linq;
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
            Println("##############################################################");
            Println("#                        win-efm8load                        #");
            Println("#          https://github.com/YaoLiTao/win-efm8load          #");
            Println("#       @Thanks https://github.com/fishpepper/efm8load       #");
            Println("##############################################################");
            Println("");
        }

        private string ResponseToStr(int res)
        {
            switch (res)
            {
                case (int)RESPONSE.ACK:
                    return RESPONSE.ACK.ToString();
                case (int)RESPONSE.RANGE_ERROR:
                    return RESPONSE.RANGE_ERROR.ToString();
                case (int)RESPONSE.BAD_ID:
                    return RESPONSE.BAD_ID.ToString();
                case (int)RESPONSE.CRC_ERROR:
                    return RESPONSE.CRC_ERROR.ToString();
            }

            return "unknown response";
        }

        private void Println(string format, params object[] args)
        {
            Print(format + "\r\n", args);
        }

        /**
         * 打印文本（不换行），将文本刷新到底部
         */
        private void Print(string format, params object[] args)
        {
            infoTextBox.AppendText(string.Format(format, args));
            infoTextBox.ScrollToCaret();
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
            baudRateComboBox.Items.Add("115200");
            baudRateComboBox.Items.Add("120000");
            baudRateComboBox.SelectedIndex = 0;
        }

        /**
         * 打开串口
         */
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
                Println("> 打开串口[{0}] - 成功", serial.PortName);
            }
            catch (Exception)
            {
                Println("> 打开串口[{0}] - 失败", serial.PortName);
                throw;
            }
        }

        /**
         * 关闭串口
         */
        private void CloseSerialPort()
        {
            serial?.Close();
            Println("> 关闭串口[{0}]", serial?.PortName);
        }

        /**
         * 选择HEX文件
         */
        private void SelectHexFile()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.Title = "请选择程序HEX文件";
            dialog.Filter = "*.hex|*.HEX";
            if (dialog.ShowDialog() != DialogResult.OK) return;
            openFileTextBox.Text = dialog.FileName;
            Println("> 文件选择成功: {0}", openFileTextBox.Text);
        }

        /**
         * TODO 保存HEX文件
         */
        private string SaveHexFile()
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "请保存程序HEX文件";
            dialog.Filter = "*.hex|*.HEX";
            if (dialog.ShowDialog() != DialogResult.OK) return null;
            infoTextBox.Text += "文件选择成功：" + openFileTextBox.Text + "\r\n";
            Println("> 正在读取芯片HEX到: {0}", dialog.FileName);
            return dialog.FileName;
        }

        private void SetProgressBar(int min, int max)
        {
            progressBar.Minimum = min;
            progressBar.Maximum = max;
            progressBar.Value = 0;
        }


        /**************************** 串口下载函数 ****************************/


        /**
         * 下载程序到芯片
         */
        private void Upload()
        {
            Println("> uploading file '{0}'", openFileTextBox.Text);
            if (string.IsNullOrEmpty(openFileTextBox.Text))
            {
                Println("> 未选择程序文件，请先选择需要下载的程序文件");
                throw new NoNullAllowedException();
            }

            // read hex file
            var hexRecords = LoadHex(openFileTextBox.Text);
            SetProgressBar(0, hexRecords.Count * 2 + 3);

            // identify chip
            IdentifyChip();

            // send autoBaud training character
            SendAutoBaudTraining();
            progressBar.Value += 1;

            // enable flash access
            EnableFlashAccess();
            progressBar.Value += 1;

            // erase pages where we are going to write
            ErasePagesIh(hexRecords);
            progressBar.Value += 1;

            // write all data bytes
            WritePagesIh(hexRecords);

            // Verify all page
            VerifyPagesIh(hexRecords);
        }

        /**
         * 扫描芯片
         */
        private void IdentifyChip()
        {
            Println("> checking for device");

            // send autobaud training
            SendAutoBaudTraining();

            // enable flash access
            EnableFlashAccess();

            // we will now iterate through all known device ids
            foreach (var device in deviceList)
            {
                if (debug) Println("> checking for device {0}", device.DeviceName);
                foreach (var variant in device.Variant)
                {
                    if (CheckId(device.DeviceId, variant.VariantId))
                    {
                        Println("> success, detected {0} cpu (variant {1})", device.DeviceName, variant.VariantName);
                        // set up chip data
                        flashSize = variant.FlashSize;
                        flashPageSize = variant.PageSize;
                        flashSecurityPageSize = variant.SecurityPageSize;
                        Println("> detected {0} cpu (variant {1}, flash_size={2}, pagesize={3})",
                            device.DeviceName, variant.VariantName, flashSize, flashPageSize);
                        return;
                    }
                }
            }

            for (byte deviceId = 0; deviceId < 0xFF; deviceId++)
            {
                Println("\r\n> checking device_id 0x{0:X2}...", deviceId);
                for (byte variantId = 0; variantId < 24; variantId++)
                {
                    if (CheckId(deviceId, variantId))
                    {
                        Println("\n> ERROR: unknown device detected: id=0x{0:X2}, variant=0x{1:X2}\n" +
                                "         please add it to the deviceList. will exit now\n", deviceId, variantId);
                    }
                }
            }

            Println("> ERROR: could not find any device...");
        }

        /**
         * 重启芯片
         */
        private void SendReset()
        {
            Println("> send reset command");
            if (Send(COMMAND.RESET, new[] { (byte)255, (byte)255 }) == (int)RESPONSE.ACK)
            {
                Println("> success, device restarted...");
            }
        }

        /**
         * TODO 将芯片中的HEX导出到文件
         */
        private void Download()
        {
        }

        /**
         * 将HEX文件的页写入Flash
         */
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
                Println("> writing segment 0x{0:X4}-0x{1:X4}", start, end - 1);

                // fetch data
                var data = new List<byte>(hexRecord.Bytes);

                // write in 128byte blobs
                var dataPos = 0;

                // keep byte zero 0xFF in order to keep bootloader active (for now)
                if (start == 0)
                {
                    Println("> delaying write of flash[0] = 0x{0:X2} to the end", data[0]);
                    byteZero = data[0];
                    start += 1;
                    data.RemoveAt(0);
                }

                while ((dataPos + start) < end)
                {
                    var length = Math.Min(128, end - (dataPos + start));
                    Write(start + dataPos, data.GetRange(dataPos, length).ToArray());
                    dataPos += length;
                }

                // now verify this segment
                Print("> verifying segment... ");

                if (Verify(start, data.ToArray()) == (int)RESPONSE.ACK)
                {
                    Println("OK");
                }
                else
                {
                    Println("FAILURE");
                    throw new DataException();
                }

                progressBar.Value += 1;
            }

            // all bytes except byte zero were written, do this now
            if (byteZero != -1)
            {
                Println("> will now write flash[0] = 0x{0:X2}", byteZero);
                var res = Write(0, new[] { (byte)byteZero });
                if (res != (int)RESPONSE.ACK)
                {
                    Println("> ERROR, write of flash[0] failed (response = {0} {1})", res, ResponseToStr(res));
                    RestoreBootloaderAutostart();
                    throw new DataException();
                }

                res = Verify(0, new[] { (byte)byteZero });
                if (res != (int)RESPONSE.ACK)
                {
                    Println("> ERROR, write of flash[0] failed (response = {0} {1})", res, ResponseToStr(res));
                    RestoreBootloaderAutostart();
                    throw new DataException();
                }
            }
        }

        /**
         * 将页0擦除，下次芯片重启时自动进入串口下载模式
         */
        private void RestoreBootloaderAutostart()
        {
            // the bootloader will always start if flash[0] = 0xFF
            // in case something went wrong during programming,
            // call this in order to clear page 0 so that the bootloader
            // will always start
            Println("> will now erase page 0 in order to re-enable bootloader autorun");
            ErasePage(0);
        }

        /**
         * 向指定地址写入数据
         */
        private int Write(int address, byte[] data)
        {
            if (data.Length > 128)
            {
                Println("> ERROR: invalid chunksize, maximum allowed write is 128 bytes ({0})", data.Length);
                throw new DataException();
            }

            string dataExcerpt;
            if (data.Length > 8)
            {
                dataExcerpt = data.Take(4)
                                  .Select(x => $"0x{x:X2}")
                                  .Aggregate((a, b) => $"{a} {b}")
                              + " ... "
                              + data.Skip(data.Length - 4).Take(4)
                                  .Select(x => $"0x{x:X2}")
                                  .Aggregate((a, b) => $"{a} {b}");
            }
            else
            {
                dataExcerpt = data.Select(x => $"0x{x:X2}").Aggregate((a, b) => $"{a} {b}");
            }

            Println("> write at 0x{0:X4} ({1:D3}): {2}", address, data.Length, dataExcerpt);


            // send request
            var addressHi = (address >> 8) & 0xFF;
            var addressLo = address & 0xFF;
            var bytes = new[] { (byte)addressHi, (byte)addressLo }.ToList();
            bytes.AddRange(data);
            var res = Send(COMMAND.WRITE, bytes.ToArray());
            if (res == (int)RESPONSE.ACK) return res;
            Println("> ERROR: write failed at address 0x{0:X4} (response = {1} {2})", address, res, ResponseToStr(res));
            throw new DataException();
        }

        /**
         * CRC16校验指定地址的数据
         */
        private int Verify(int address, byte[] data)
        {
            var length = data.Length;
            var crc16 = data.CRC16(ExtensionForCRC16.CRC16Type.CCITTxModem);
            if (debug) Println("> verify address 0x{0:X4} (len={1}, crc16=0x{2:X4})", address, length, crc16);
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

        /**
         * CRC校验HEX文件与已写入Flash的页
         */
        private void VerifyPagesIh(List<IntelHexRecord> hexRecords)
        {
            // verify written data
            //
            // do a pagewise compare to find the position of
            // the mismatch
            foreach (var hexRecord in hexRecords)
            {
                var start = hexRecord.Address;
                var end = hexRecord.Address + hexRecord.ByteCount;
                Print("> verifying segment 0x{0:X4}-0x{1:X4}... ", start, end - 1);

                // calc crc16
                if (Verify(start, hexRecord.Bytes) == (int)RESPONSE.ACK)
                {
                    Println("OK");
                }
                else
                {
                    Println("FAILURE !");
                    throw new DataException();
                }

                progressBar.Value += 1;
            }
        }

        /**
         * 擦除HEX中需要的写入页
         */
        private void ErasePagesIh(List<IntelHexRecord> hexRecords)
        {
            // erase all pages that are occupied
            var lastAddress = hexRecords[hexRecords.Count - 1].Address;
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

        /**
         * 擦除整页
         */
        private int ErasePage(int page)
        {
            var start = page * flashPageSize;
            var end = start + flashPageSize - 1;
            var startHi = (start >> 8) & 0xFF;
            var startLo = start & 0xFF;
            Println("> will erase page {0} (0x{1:X4}-0x{2:X4})", page, start, end);
            return Send(COMMAND.ERASE, new[] { (byte)startHi, (byte)startLo });
        }

        /**
         * 解析HEX
         */
        private List<IntelHexRecord> LoadHex(string fileName)
        {
            var hexRecords = new List<IntelHexRecord>();
            var baseAddress = 0;
            foreach (var hexRecordLine in File.ReadLines(fileName))
            {
                var line = HexFileLineParser.ParseLine(hexRecordLine);
                switch (line.RecordType)
                {
                    case RecordType.Data:
                        var absoluteAddress = line.Address + baseAddress;
                        line.Address = absoluteAddress;
                        hexRecords.Add(line);
                        continue;
                    case RecordType.ExtendedSegmentAddress:
                        line.Assert(rec => rec.ByteCount == 2, "Byte count should be 2.");
                        baseAddress = (line.Bytes[0] << 8 | line.Bytes[1]) << 4;
                        continue;
                    case RecordType.ExtendedLinearAddress:
                        line.Assert(rec => rec.ByteCount == 2, "Byte count should be 2.");
                        baseAddress = (line.Bytes[0] << 8 | line.Bytes[1]) << 16;
                        continue;
                    case RecordType.EndOfFile:
                        line.Assert(rec => rec.Address == 0, "Address should equal zero in EOF.");
                        line.Assert(rec => rec.ByteCount == 0, "Byte count should be zero in EOF.");
                        line.Assert(rec => rec.Bytes.Length == 0, "Number of bytes should be zero for EOF.");
                        line.Assert(rec => rec.CheckSum == byte.MaxValue, "Checksum should be 0xff for EOF.");
                        continue;
                    default:
                        continue;
                }
            }

            // 连续地址归并到一起（合成一段）
            IntelHexRecord segmentHexRecord = null;
            var segmentHexRecords = new List<IntelHexRecord>();
            foreach (var hexRecord in hexRecords)
            {
                if (segmentHexRecord == null ||
                    hexRecord.Address != segmentHexRecord.Address + segmentHexRecord.ByteCount)
                {
                    segmentHexRecords.Add(segmentHexRecord = new IntelHexRecord
                    {
                        Address = hexRecord.Address,
                        Bytes = hexRecord.Bytes,
                        ByteCount = hexRecord.ByteCount
                    });
                }
                else
                {
                    segmentHexRecord.Bytes = segmentHexRecord.Bytes.Concat(hexRecord.Bytes).ToArray();
                    segmentHexRecord.ByteCount += hexRecord.ByteCount;
                }
            }

            return segmentHexRecords;
        }

        /**
         * 训练波特率
         */
        private void SendAutoBaudTraining()
        {
            if (debug) Println("> sending training char 0xFF");
            for (int i = 0; i < 2; i++)
            {
                SendByte(0xff);
            }
        }

        /**
         * 发送一个字节
         */
        private void SendByte(byte b)
        {
            try
            {
                serial.Write(new[] { b }, 0, 1);
            }
            catch (Exception)
            {
                Println("错误: 串口写入失败");
                throw;
            }
        }

        /**
         * 使能Flash访问
         */
        private void EnableFlashAccess()
        {
            var res = Send(COMMAND.SETUP, new byte[] { 0xA5, 0xF1, 0x00 });
            if (debug) Println("> reply 0x{0:X2} {1}", res, ResponseToStr(res));
            if (res == (int)RESPONSE.ACK) return;
            Println("> ERROR enabling flash access, error code 0x{0:X2} {1}", res, ResponseToStr(res));
            throw new DataException();
        }

        /**
         * 发送带命令的字节数组
         */
        private int Send(COMMAND cmd, byte[] data)
        {
            if (debug) Println("> send command: {0}", cmd);
            if (data.Length < 2 || data.Length > 130)
            {
                Println("> ERROR: invalid data length! allowed 2...130, got {0}", data.Length);
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
                if (debug) Println("> reply 0x{0:X2} {1}", resBytes[0], ResponseToStr(resBytes[0]));
                return resBytes[0];
            }
            catch (Exception e)
            {
                Println("> ERROR: failed to send data [{0}]", e.Message);
            }

            return -1;
        }

        /**
         * 判定芯片ID
         */
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
            try
            {
                Println("");
                Println("> 扫描芯片开始...");
                OpenSerialPort();
                IdentifyChip();
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                CloseSerialPort();
            }
        }

        private void resetMcuButton_Click(object sender, EventArgs e)
        {
            try
            {
                Println("");
                Println("> 重启芯片开始...");
                OpenSerialPort();
                SendReset();
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                CloseSerialPort();
            }
        }

        private void programButton_Click(object sender, EventArgs e)
        {
            try
            {
                Println("");
                Println("> 下载/编程开始...");
                OpenSerialPort();
                Upload();
                Println("> 下载程序成功！");
            }
            catch (Exception exception)
            {
                if (debug) Println("{0}", exception);
            }
            finally
            {
                CloseSerialPort();
            }
        }
    }
}