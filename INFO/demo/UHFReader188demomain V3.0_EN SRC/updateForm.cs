using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using ReaderB;

namespace UHFReader188demomain
{
    public partial class updateForm : Form
    {
        public updateForm()
        {
            InitializeComponent();
        }

        byte[] FlashByte = new byte[65536];
        public string fRecvString = "";
        byte[] fRecvData = new byte[48400];
        byte[] Data = new byte[50000];
        int nfilesize = 0;
        uint CRC32Value = 0;

        private uint GetCRC32(uint[] SourceData, int len)
        {
            uint dwPolynomial = 0x04c11db7;
            uint xbit;
            uint data;
            uint bits;
            uint CRC = 0xFFFFFFFF;    // init
            int index = 0;
            while ((len--) > 0)
            {
                xbit = 0x80000000;
                data = SourceData[index++];
                for (bits = 0; bits < 32; bits++)
                {
                    if ((CRC & 0x80000000) > 0)
                    {
                        CRC <<= 1;
                        CRC ^= dwPolynomial;
                    }
                    else
                        CRC <<= 1;
                    if ((data & xbit) > 0)
                    {
                        CRC ^= dwPolynomial;
                    }
                    xbit >>= 1;
                }
            }
            return CRC;
        }

        private uint[] GetData_32(byte[] FlashByte, int len)
        {
            int mSize = 0;
            if ((len % 4) == 0)
            {
                mSize = len / 4;
            }
            else
            {
                mSize = len / 4 + 1;
            }
            byte[] newdata = new byte[mSize * 4];
            for (int m = 0; m < newdata.Length; m++)
            {
                newdata[m] = 255;
            }

            uint[] data_32 = new uint[mSize];
            Array.Copy(FlashByte, newdata, len);
            for (int m = 0; m < newdata.Length / 4; m++)
            {
                byte[] daw = new byte[4];
                Array.Copy(newdata, m * 4, daw, 0, 4);
                data_32[m] = (uint)(daw[3] * 256 * 256 * 256 + daw[2] * 256 * 256 + daw[1] * 256 + daw[0]);
            }
            return data_32;
        }


        private void btOpenFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "二进制文件(*.bin)|*.bin";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                txtFileName.Text = openFileDialog1.FileName;
                //使用“打开”对话框中选择的文件名实例化FileStream对象
                FileStream aFile = new FileStream(openFileDialog1.FileName, FileMode.OpenOrCreate);
                nfilesize = Convert.ToInt32(aFile.Length);
                FlashByte = new byte[nfilesize];
                aFile.Seek(0, SeekOrigin.Begin);
                aFile.Read(FlashByte, 0, nfilesize);
                btStart.Enabled = true;
                //crc32 = GetCRC32(FlashByte, nfilesize);
                uint[] data_32 = GetData_32(FlashByte, nfilesize);
                CRC32Value = GetCRC32(data_32, data_32.Length);
             
            }
        }

        int fCmdRet = 0x30;
        volatile bool stopLoad = false;
        Thread readThread1 = null;
        private void btStart_Click(object sender, EventArgs e)
        {
            fCmdRet = StaticClassReaderB.ChangeToUpdateMode(ref Form1.fComAdr, Form1.frmcomportindex);
            fCmdRet=0;
            if (fCmdRet!=0)
            {
                StatusBar1.Panels[0].Text = "切换至升级模式失败";
                return;
            }

            btStart.Enabled = false;
            btStop.Enabled = true;
            stopLoad = false;
            readThread1 = new Thread(UpdateReader);
            readThread1.IsBackground = true;
            readThread1.Start();

        }

        private void UpdateReader()
        {
            this.Invoke((EventHandler)delegate
            {
                progressBar1.Value = 10;
                progressBar1.Visible = true;
                progressBar1.Update();
            });

            int ncount = 0;
            if (nfilesize % 128 != 0)
            {
                ncount = nfilesize / 128 + 1;
            }
            else
            {
                ncount = nfilesize / 128;
            }
            int result = 0x30;
            byte PageIndex = 0;
            for (int index = 0; index < ncount; index++)
            {
                this.Invoke((EventHandler)delegate
                {
                    progressBar1.Value += 10;
                    if (progressBar1.Value == 100)
                    {
                        progressBar1.Value = 0;
                    }
                    progressBar1.Update();
                });
                
                byte[] data = new byte[256];
                int nlen = 0;
                if ((index + 1) * 128 < nfilesize)
                {
                    Array.Copy(FlashByte, index * 128, data, 0, 128);
                    nlen = 128;
                }
                else
                {
                    Array.Copy(FlashByte, index * 128, data, 0, nfilesize - index * 128);
                    nlen = nfilesize - index * 128;
                }
               
                for (int p = 0; p < 5; p++)
                {
                    result = StaticClassReaderB.WriteBinData(ref Form1.fComAdr, PageIndex, (byte)nlen, data,Form1.frmcomportindex);
                    if (result == 0)
                    {
                        PageIndex++;
                        break;
                    }
                }
                if (result != 0)
                {
                    this.Invoke((EventHandler)delegate
                    {
                        StatusBar1.Panels[0].Text = "升级失败!!!";
                        progressBar1.Visible = false;
                        btStart.Enabled = true;
                        btStop.Enabled = false;
                    });
                    return;
                }

                if (stopLoad)
                {
                    this.Invoke((EventHandler)delegate
                    {
                        StatusBar1.Panels[0].Text = "升级失败!!!";
                        progressBar1.Visible = false;
                        btStart.Enabled = true;
                        btStop.Enabled = false;
                    });
                    return;
                }
            }

            bool isSuccess = false;
            for (int p = 0; p < 5; p++)
            {
                byte[]crc32=new byte[4];
                if (StaticClassReaderB.CheckCRC32(ref Form1.fComAdr,crc32,Form1.frmcomportindex)==0)
                {
                    uint data_32 = (uint)(crc32[3] * 256 * 256 * 256 + crc32[2] * 256 * 256 + crc32[1] * 256 + crc32[0]);
                    if (data_32 == CRC32Value)
                    {
                        isSuccess = true;
                        break;
                    }
                    else
                    {
                        isSuccess = false;
                    }
                }
                else
                {
                    isSuccess = false;
                }
            }
            if (isSuccess)
            {
                result = StaticClassReaderB.WriteMark(ref Form1.fComAdr, Form1.frmcomportindex);
                if (result==0)
                {
                    result = StaticClassReaderB.ResetReader(ref Form1.fComAdr, Form1.frmcomportindex);
                    this.Invoke((EventHandler)delegate {
                        progressBar1.Visible = false;
                        StatusBar1.Panels[0].Text = "升级成功!!!";
                        btStart.Enabled = true;
                        btStop.Enabled = false;
                    });
                    return;
                }
            }
            else
            {
                this.Invoke((EventHandler)delegate {
                    progressBar1.Visible = false;
                    StatusBar1.Panels[0].Text = "升级失败!!!";
                    btStart.Enabled = true;
                    btStop.Enabled = false;
                });
                return;
            }
            
            this.Invoke((EventHandler)delegate {
                btStart.Enabled = true;
                btStop.Enabled = false;
            });
        }

        private void btStop_Click(object sender, EventArgs e)
        {
            stopLoad = true;
        }
    }
}
