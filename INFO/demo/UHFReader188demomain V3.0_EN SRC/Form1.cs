using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Resources;
using System.Reflection;
using ReaderB;
using System.IO.Ports;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace UHFReader188demomain
{
    public partial class Form1 : Form
    {
        [DllImport("User32.dll", EntryPoint = "PostMessage")]
        private static extern int PostMessage(
       IntPtr hWnd, // handle to destination window 
       uint Msg, // message 
       uint wParam, // first message parameter 
       uint lParam // second message parameter 
       );

        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, string lParam);

        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        public const int USER = 0x0400;
        public const int WM_SENDTAG = USER + 101;

        private bool fAppClosed; //在测试模式下响应关闭应用程序
        public static byte fComAdr=0xff; //当前操作的ComAdr
        private int ferrorcode;
        private byte fBaud;
        private double fdminfre;
        private double fdmaxfre;
        private byte Maskadr;
        private byte MaskLen;
        private byte MaskFlag;
        private int fCmdRet=30; //所有执行指令的返回值
        private int fOpenComIndex; //打开的串口索引号
        private bool fIsInventoryScan;
        private bool fisinventoryscan_6B;
        private byte[] fOperEPC=new byte[36];
        private byte[] fPassWord=new byte[4];
        private byte[] fOperID_6B=new byte[8];
        private int CardNum1 = 0;
        ArrayList list = new ArrayList();
        private bool fTimer_6B_ReadWrite;
        public static int frmcomportindex;
        private bool ComOpen=false;
        public DeviceClass SelectedDevice;
        private static List<DeviceClass> DevList;
        private static SearchCallBack searchCallBack = new SearchCallBack(searchCB);
        RFIDCallBack elegateRFIDCallBack;


        /// <summary>
        /// Device Search的回调函数;
        /// </summary>
        private static void searchCB(IntPtr dev, IntPtr data)
        {
            uint ipAddr = 0;
            StringBuilder devname = new StringBuilder(100);
            StringBuilder macAdd = new StringBuilder(100);
            //获取搜索到的设备信息；
            DevControl.tagErrorCode eCode = DevControl.DM_GetDeviceInfo(dev, ref ipAddr, macAdd, devname);
            if (eCode == DevControl.tagErrorCode.DM_ERR_OK)
            {
                //将搜索到的设备加入设备列表；
                DeviceClass device = new DeviceClass(dev, ipAddr, macAdd.ToString(), devname.ToString());
                DevList.Add(device);
            }
            else
            {
                //异常处理；
                string errMsg = ErrorHandling.GetErrorMsg(eCode);
                Log.WriteError(errMsg);
            }

        }

        private static IPAddress getIPAddress(uint interIP)
        {
            return new IPAddress((uint)IPAddress.HostToNetworkOrder((int)interIP));
        }

        public Form1()
        {
            InitializeComponent();
            elegateRFIDCallBack = new RFIDCallBack(GetUid);

            DevList = new List<DeviceClass>();

            //初始化设备控制模块；
            DevControl.tagErrorCode eCode = DevControl.DM_Init(searchCallBack, IntPtr.Zero);
            if (eCode != DevControl.tagErrorCode.DM_ERR_OK)
            {
                //如果初始化失败则关闭程序，并进行异常处理；
                string errMsg = ErrorHandling.HandleError(eCode);
                throw new Exception(errMsg);
            }

            
        }

       int total_tagnum=0;
        public void GetUid(IntPtr p, Int32 nEvt)
        {

            RFIDTag ce = (RFIDTag)Marshal.PtrToStructure(p, typeof(RFIDTag));
            this.Invoke((EventHandler)delegate
            {
                IntPtr ptrWnd = IntPtr.Zero;
                ptrWnd = FindWindow(null, "UHFReader188CSHarp V3.0");
                if (ptrWnd != IntPtr.Zero)         // 检查当前统计窗口是否打开
                {
                    string para = ce.LEN + ","+ ce.UID + "," + ce.RSSI.ToString() + " ";
                    SendMessage(ptrWnd, WM_SENDTAG, IntPtr.Zero, para);
                    total_tagnum++;
                }
            });
        }

        protected override void DefWndProc(ref Message m)
        {
            if (m.Msg == WM_SENDTAG)
            {
                string tagInfo = Marshal.PtrToStringAnsi(m.LParam);
                string[] temp = tagInfo.Split(',');
                string epclen = temp[0];
                string epc = temp[1];
                string rssi = temp[2];
                int index = mepclist.IndexOf(epc);
                if (index == -1)
                {
                    string[] btArray = new string[5];
                    btArray[0] = (mepclist.Count + 1) + "";
                    btArray[1] = epc;
                    btArray[2] = epclen;
                    btArray[3] = "1";
                    btArray[4] = rssi;
                    ListViewItem item = new ListViewItem(btArray);
                    ListView1_EPC.Items.Add(item);
                    ComboBox_EPC1.Items.Add(epc);
                    ComboBox_EPC2.Items.Add(epc);
                    ComboBox_EPC3.Items.Add(epc);
                    mepclist.Add(epc);
                    if (!CheckBox_TID.Checked)
                    {
                        ComboBox_EPC1.SelectedIndex = 0;
                        ComboBox_EPC2.SelectedIndex = 0;
                        ComboBox_EPC3.SelectedIndex = 0;
                    }
                    textBox10.Text = mepclist.Count + "";
                }
                else
                {
                    string strCount = ListView1_EPC.Items[index].SubItems[3].Text;
                    int Count = Convert.ToInt32(strCount) + 1;
                    ListView1_EPC.Items[index].SubItems[3].Text = Count + "";
                    ListView1_EPC.Items[index].SubItems[4].Text =rssi;
                }
            }
            else
                base.DefWndProc(ref m);
        }
        private void RefreshStatus()
        { 
              if(!(ComboBox_AlreadyOpenCOM.Items.Count != 0)) 
                StatusBar1.Panels[1].Text = "Communication Closed";
              else
                StatusBar1.Panels[1].Text = " COM" + Convert.ToString(frmcomportindex);
              StatusBar1.Panels[0].Text ="";
              StatusBar1.Panels[2].Text ="";
        }
        private string GetReturnCodeDesc(int cmdRet)
        {
            switch (cmdRet)
            {
                case 0x00:
                    return "Operation Successful";
                case 0x01:
                    return "Returned before query time expires";
                case 0x02:
                    return "Specified query time overflow";
                case 0x03:
                    return "There are more messages after this one";
                case 0x04:
                    return "Storage space of the read-write module is full";
                case 0x05:
                    return "Access password error";
                case 0x09:
                    return "Destruction password error";
                case 0x0a:
                    return "Destruction password cannot be all zeros";
                case 0x0b:
                    return "Electronic tag does not support this command";
                case 0x0c:
                    return "Access password cannot be all zeros for this command";
                case 0x0d:
                    return "Electronic tag has already been set to read-only protection; cannot set again";
                case 0x0e:
                    return "Electronic tag is not set to read-only protection; no need to unlock";
                case 0x10:
                    return "Some byte spaces are locked; write operation failed";
                case 0x11:
                    return "Cannot lock";
                case 0x12:
                    return "Already locked; cannot lock again";
                case 0x13:
                    return "Parameter saving failed, but the set value is valid before the read-write module is powered off";
                case 0x14:
                    return "Cannot adjust";
                case 0x15:
                    return "Returned before query time expires";
                case 0x16:
                    return "Specified query time overflow";
                case 0x17:
                    return "There are more messages after this one";
                case 0x18:
                    return "Storage space of the read-write module is full";
                case 0x19:
                    return "Electronic tag does not support this command or access password cannot be zero";
                case 0xFA:
                    return "Electronic tag exists, but communication is poor; cannot operate";
                case 0xFB:
                    return "No electronic tag available for operation";
                case 0xFC:
                    return "Electronic tag returns error code";
                case 0xFD:
                    return "Command length error";
                case 0xFE:
                    return "Invalid command";
                case 0xFF:
                    return "Parameter error";
                case 0x30:
                    return "Communitation Error";
                case 0x31:
                    return "CRC Check Error";
                case 0x32:
                    return "Returned data length error";
                case 0x33:
                    return "Communication busy; device is executing other commands";
                case 0x34:
                    return "Busy; command is being executed";
                case 0x35:
                    return "Port already open";
                case 0x36:
                    return "Port already closed";
                case 0x37: 
                    return "Invalid handle";
                case 0x38:
                    return "Invalid port";
                case 0xEE:
                    return "Return Command Error";
                default:
                    return "";
            }
        }
        private string GetErrorCodeDesc(int cmdRet)
        {
            switch (cmdRet)
            {
                case 0x00:
                    return "Other Errors";
                case 0x03:
                    return "Memory Overrun or Unsupported PC Value";
                case 0x04:
                    return "Memory Locked";
                case 0x0b:
                    return "Insufficient Power Supply";
                case 0x0f:
                    return "Non-Specific Error";
                default:
                    return "";
            }
        }
        private byte[] HexStringToByteArray(string s)
        {
            s = s.Replace(" ", "");
            byte[] buffer = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i += 2)
                buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
            return buffer;
        }

        private string ByteArrayToHexString(byte[] data)
        {
            StringBuilder sb = new StringBuilder(data.Length * 3);
            foreach (byte b in data)
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0'));
            return sb.ToString().ToUpper();

        }
        private void AddCmdLog(string CMD, string cmdStr, int cmdRet)
        {
            try
            {
                StatusBar1.Panels[0].Text = "";
                StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() + " " +
                                            cmdStr + ": " +
                                            GetReturnCodeDesc(cmdRet);
            }
            finally
            {
                ;
            }
        }
        private void AddCmdLog(string CMD, string cmdStr, int cmdRet,int errocode)
        {
            try
            {
                StatusBar1.Panels[0].Text = "";
                StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() + " " +
                                            cmdStr + ": " +
                                            GetReturnCodeDesc(cmdRet)+" "+"0x"+Convert.ToString(errocode,16).PadLeft(2,'0');
            }
            finally
            {
                ;
            }
        }
        private void ClearLastInfo()
        {    
              ComboBox_AlreadyOpenCOM.Refresh();
              RefreshStatus();
              Edit_Type.Text = "";
              Edit_Version.Text = "";
              ISO180006B.Checked=false;
              EPCC1G2.Checked=false;
              Edit_ComAdr.Text = "";
              Edit_powerdBm.Text = "";
              Edit_scantime.Text = "";
              Edit_dminfre.Text = "";
              Edit_dmaxfre.Text = "";
        }
        private void InitComList()
        {
            int i = 0;
            ComboBox_COM.Items.Clear();
              ComboBox_COM.Items.Add(" AUTO");
              for (i = 1; i < 13;i++ )
                  ComboBox_COM.Items.Add(" COM" + Convert.ToString(i));
              ComboBox_COM.SelectedIndex = 0;
              RefreshStatus();
        }
        private void InitReaderList()
        {
            int i=0;
           // ComboBox_PowerDbm.SelectedIndex = 0;
            ComboBox_baud.SelectedIndex =3;
             for (i=0 ;i< 63;i++)
             {
                ComboBox_dminfre.Items.Add(Convert.ToString(902.6+i*0.4)+" MHz");
                ComboBox_dmaxfre.Items.Add(Convert.ToString(902.6 + i * 0.4) + " MHz");
             }
              ComboBox_dmaxfre.SelectedIndex = 62;
              ComboBox_dminfre.SelectedIndex = 0;
              for (i=0x00;i<=0xff;i++)
                  ComboBox_scantime.Items.Add(Convert.ToString(i) + "*100ms");
              ComboBox_scantime.SelectedIndex = 60;
              i=40;
              while (i<=300)
              {
                  ComboBox_IntervalTime.Items.Add(Convert.ToString(i) + "ms");
                  i=i+10;
              }
              ComboBox_IntervalTime.SelectedIndex = 1;
              
              i=40;
              while (i<=300 )
              {
                  ComboBox_IntervalTime_6B.Items.Add(Convert.ToString(i) + "ms");
                  i=i+10;
              }
              ComboBox_IntervalTime_6B.SelectedIndex = 1;
              for (i = 0; i < 256; i++)
              {
                  comboBox1.Items.Add(Convert.ToString(i) + "*10ms");
                 cbb_relay1.Items.Add(Convert.ToString(i) + "*100ms");
                com_relay_time.Items.Add(Convert.ToString(i) + "*100ms");
            }
              comboBox1.SelectedIndex = 30;
              for (i = 1; i < 256; i++)
              {
                  comboBox3.Items.Add(Convert.ToString(i) + "*10us");
              }
              comboBox3.SelectedIndex = 9;
              for (i = 1; i < 256; i++)
              {
                  comboBox2.Items.Add(Convert.ToString(i) + "*100us");
                  
              }
              comboBox2.SelectedIndex = 14;
              for (i = 0; i < 256; i++)
              {
                  comboBox6.Items.Add(Convert.ToString(i) + "*1s");
              }
              comboBox6.SelectedIndex = 0;
              for (i = 1; i < 33; i++)
              {
                  comboBox5.Items.Add(Convert.ToString(i));
              }
              comboBox5.SelectedIndex = 0;
              comboBox4.SelectedIndex = 0;
              ComboBox_PowerDbm.SelectedIndex = 26;

              for (i = 0; i < 16; i++)
              {
                  comboBox7.Items.Add(Convert.ToString(i));
              }
              comboBox7.SelectedIndex = 4;

              for (i = 0; i < 4; i++)
              {
                  comboBox8.Items.Add(Convert.ToString(i));
              }
              comboBox8.SelectedIndex = 0;

              for (i = 0; i < 16; i++)
              {
                  com_Q.Items.Add(Convert.ToString(i));
              }
              com_Q.SelectedIndex = 4;

              for (i = 0; i < 4; i++)
              {
                  com_S.Items.Add(Convert.ToString(i));
              }
              com_S.SelectedIndex = 0;

              com_queryInter.SelectedIndex = 0;
              cbb_add.SelectedIndex = 4;
              for (i = 2; i < 256; i++)
              {
                  cbb_dwell.Items.Add(Convert.ToString(i) + "*100ms");
              }
              cbb_dwell.SelectedIndex = 48;

            for (i = 0; i < 255; i++)
                comboBox_tigtime.Items.Add(Convert.ToString(i) + "*1s");
            comboBox_tigtime.SelectedIndex = 0;   //

            com_contype.SelectedIndex = 0;
            cbb_Reconnect.SelectedIndex = 0;
            wifi_com_contype.SelectedIndex = 0;
            wifi_cbb_Reconnect.SelectedIndex = 0;
            cbb_wifi_keepalive_en.SelectedIndex = 1;
            cbb_keepalive_en.SelectedIndex = 1;

            cbb_output.SelectedIndex = 1;
            cbb_relay1.SelectedIndex = 30;
            com_relay_time.SelectedIndex = 30;
            cbb_matchlength.SelectedIndex = 6;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            tab_wifi.Hide();
            tab_wifi.Text = null;
            

            progressBar1.Visible = false;
            fOpenComIndex = -1;
            fComAdr = 0;
            ferrorcode= -1;
            fBaud =5;
            InitComList();
            InitReaderList();
             
            Byone_6B.Checked=true;
            Different_6B.Checked=true;

            P_EPC.Checked=true;
            C_EPC.Checked=true;
            DestroyCode.Checked=true;
            NoProect.Checked=true;
            NoProect2.Checked=true;
            fAppClosed = false;
            fIsInventoryScan = false;
            fisinventoryscan_6B = false;
            fTimer_6B_ReadWrite=false ;
            Timer_Test_.Enabled = false;
            Timer_G2_Read.Enabled = false;
            Timer_G2_Alarm.Enabled = false;
            timer1.Enabled = false;

            Button3.Enabled = false;
            button20.Enabled = false;
            Button5.Enabled = false;
            Button1.Enabled = false;
            button2.Enabled = false;
            Button_DestroyCard.Enabled = false;
            Button_WriteEPC_G2.Enabled = false;
            SpeedButton_Read_G2.Enabled = false;
            Button_DataWrite.Enabled = false;
            BlockWrite.Enabled = false;
            Button_BlockErase.Enabled = false;
            Button_SetProtectState.Enabled = false;
            SpeedButton_Query_6B.Enabled = false;
            SpeedButton_Read_6B.Enabled = false;
            SpeedButton_Write_6B.Enabled = false;
            Button14.Enabled = false;
            Button15.Enabled = false;
            btGetSerial.Enabled = false;
            DestroyCode.Enabled = false;
            AccessCode.Enabled = false;
            NoProect.Enabled = false;
            Proect.Enabled = false;
            Always.Enabled = false;
            AlwaysNot.Enabled = false;
            NoProect2.Enabled = false;
            Proect2.Enabled = false;
            Always2.Enabled = false;
            AlwaysNot2.Enabled = false;
            P_Reserve.Enabled = false;
            P_EPC.Enabled = false;
            P_TID.Enabled = false;
            P_User.Enabled = false;
            Same_6B.Enabled = false;
            Different_6B.Enabled = false;
            Less_6B.Enabled = false;
            Greater_6B.Enabled = false;

            radioButton1.Checked = true ;
            radioButton4.Checked = true ;
            radioButton5.Checked = true ;
            radioButton7.Checked = true ;
            radioButton10.Checked = true ;
            radioButton14.Checked = true ;
            button6.Enabled=false ;
            button8.Enabled = false ;
            button4.Enabled = false;
            button12.Enabled = false;
            button9.Enabled = false ;
            button10.Enabled = false ;
            button11.Enabled = false ;
            comboBox5.Enabled = false ;
            radioButton5.Enabled =false;
            radioButton7.Enabled =false;
            radioButton8.Enabled =false;
            radioButton9.Enabled =false;
            radioButton10.Enabled =false;
            radioButton11.Enabled =false;
            radioButton12.Enabled =false;
            radioButton13.Enabled =false;
            radioButton14.Enabled =false;
            radioButton15.Enabled =false;
            textBox3.Enabled = false;
            radioButton_band3.Checked = true;
            radioButton16.Enabled = false;
            radioButton17.Enabled = false;
            radioButton18.Enabled = false;
            radioButton19.Enabled = false;
            radioButton16.Checked=true;
            ComboBox_baud2.SelectedIndex = 3;
            com_relay_num.SelectedIndex = 0;
            com_relay_state.SelectedIndex = 0;
            radioButton22.Checked = true;
            panel1.Enabled = false;
            panel2.Enabled = false;
            panel3.Enabled = false;
            panel4.Enabled = false;
            panel5.Enabled = false;
        }

        private void OpenPort_Click(object sender, EventArgs e)
        {
            int port=0;
            int openresult,i;
            openresult = 30;
            string temp;
            Cursor = Cursors.WaitCursor;
              if  (Edit_CmdComAddr.Text=="")
              Edit_CmdComAddr.Text="FF";
              fComAdr = Convert.ToByte(Edit_CmdComAddr.Text,16); // $FF;
              try
              {
                  if (ComboBox_COM.SelectedIndex == 0)//Auto
                  {
                      fBaud=Convert.ToByte(ComboBox_baud2.SelectedIndex);
                      if (fBaud>2)
                          fBaud =Convert.ToByte(fBaud + 2);
                    openresult =StaticClassReaderB.AutoOpenComPort(ref port,ref fComAdr,fBaud,ref fOpenComIndex);
                    if (openresult == 0 )
                    {
                        ComOpen = true;
                        frmcomportindex = fOpenComIndex;
                        Button3_Click(sender, e); //自动执行读取写卡器信息
                        if (fBaud > 3)
                        {
                            ComboBox_baud.SelectedIndex = Convert.ToInt32(fBaud - 2);
                        }
                        else
                        {
                            ComboBox_baud.SelectedIndex = Convert.ToInt32(fBaud);
                        }
                        Button3_Click(sender, e); //自动执行读取写卡器信息
                        if((fCmdRet==0x35) |(fCmdRet==0x30))
                        {
                            MessageBox.Show("Serial Port Communication Error", "Message Prompt");
                            StaticClassReaderB.CloseSpecComPort(frmcomportindex);
                            ComOpen = false;
                        }
                     }          
                  }
                  else
                  {
                    temp = ComboBox_COM.SelectedItem.ToString();
                    temp = temp.Trim();
                    port = Convert.ToInt32(temp.Substring(3, temp.Length - 3));
                    for (i = 6; i >= 0; i--)
                    {
                        fBaud = Convert.ToByte(i);
                        if ((fBaud == 3)||(fBaud ==4))
                            continue;
                        openresult = StaticClassReaderB.OpenComPort(port, ref fComAdr, fBaud, ref fOpenComIndex);
                        
                        if (openresult == 0x35)
                        {
                            MessageBox.Show("The serial port is open", "Message Prompt");
                            return;
                        }
                        if (openresult == 0)
                        {
                            ComOpen = true;
                            frmcomportindex = fOpenComIndex;
                            Button3_Click(sender, e); //自动执行读取写卡器信息
                            
                            if (fBaud > 3)
                            {
                                ComboBox_baud.SelectedIndex = Convert.ToInt32(fBaud - 2);
                            }
                            else
                            {
                                ComboBox_baud.SelectedIndex = Convert.ToInt32(fBaud);
                            }
                            if ((fCmdRet == 0x35) || (fCmdRet == 0x30))
                            {
                                ComOpen = false;
                                MessageBox.Show("Serial Port Communication Error", "Message Prompt");
                                StaticClassReaderB.CloseSpecComPort(frmcomportindex);
                                return;
                            }
                            RefreshStatus();
                            break;
                        }

                    }
                  }
              }
              finally
              {
                  Cursor = Cursors.Default;
              }

              if ((fOpenComIndex != -1) &(openresult != 0X35)  &(openresult != 0X30))
              {
                frmcomportindex = fOpenComIndex;
                StaticClassReaderB.InitRFIDCallBack(elegateRFIDCallBack, false, frmcomportindex);
                ComboBox_AlreadyOpenCOM.Items.Add("COM"+Convert.ToString(port)) ;
                ComboBox_AlreadyOpenCOM.SelectedIndex = ComboBox_AlreadyOpenCOM.SelectedIndex + 1;
                Button3.Enabled = true ;
                button20.Enabled = true;
                Button5.Enabled = true;
                Button1.Enabled = true;
                button2.Enabled = true;
                Button_WriteEPC_G2.Enabled = true;
                SpeedButton_Query_6B.Enabled = true ;
                button6.Enabled = true;
                button8.Enabled = true;
                button9.Enabled = true;
                button4.Enabled = true;
                button12.Enabled = true;
                btGetSerial.Enabled = true;
                panel1.Enabled = true;
                panel2.Enabled = true;
                panel3.Enabled = true;
                panel4.Enabled = true;
                panel5.Enabled = true;
                ComOpen = true;
                button_settigtime.Enabled = true;
                button_gettigtime.Enabled = true;
            }
              if ((fOpenComIndex == -1) &&(openresult == 0x30))
                  MessageBox.Show("Serial Port Communication Error", "Message Prompt");
              RefreshStatus();
          }

        private void ClosePort_Click(object sender, EventArgs e)
        {
            int port;
            string temp;
            ClearLastInfo();
            fCmdRet = StaticClassReaderB.CloseSpecComPort(frmcomportindex);
            if (fCmdRet == 0)
            {
                ComboBox_AlreadyOpenCOM.Items.RemoveAt(0);
                if (ComboBox_AlreadyOpenCOM.Items.Count != 0)
                {
                    temp = ComboBox_AlreadyOpenCOM.SelectedItem.ToString();
                    port = Convert.ToInt32(temp.Substring(3, temp.Length - 3));
                    StaticClassReaderB.CloseSpecComPort(port);
                    fComAdr = 0xFF;
                    StaticClassReaderB.OpenComPort(port, ref fComAdr, fBaud, ref frmcomportindex);
                    fOpenComIndex = frmcomportindex;
                    RefreshStatus();
                    Button3_Click(sender, e); //自动执行读取写卡器信息
                }
                fOpenComIndex = -1;
                ComboBox_AlreadyOpenCOM.Items.Clear();
                ComboBox_AlreadyOpenCOM.Refresh();
                RefreshStatus();
                button20.Enabled = false;
                Button3.Enabled = false;
                Button5.Enabled = false;
                Button1.Enabled = false;
                button2.Enabled = false;
                Button_DestroyCard.Enabled = false;
                Button_WriteEPC_G2.Enabled = false;
                SpeedButton_Read_G2.Enabled = false;
                Button_DataWrite.Enabled = false;
                BlockWrite.Enabled = false;
                Button_BlockErase.Enabled = false;
                Button_SetProtectState.Enabled = false;
                SpeedButton_Query_6B.Enabled = false;
                SpeedButton_Read_6B.Enabled = false;
                SpeedButton_Write_6B.Enabled = false;
                Button14.Enabled = false;
                Button15.Enabled = false;
                btGetSerial.Enabled = false;
                DestroyCode.Enabled = false;
                AccessCode.Enabled = false;
                NoProect.Enabled = false;
                Proect.Enabled = false;
                Always.Enabled = false;
                AlwaysNot.Enabled = false;
                NoProect2.Enabled = false;
                Proect2.Enabled = false;
                Always2.Enabled = false;
                AlwaysNot2.Enabled = false;

                P_Reserve.Enabled = false;
                P_EPC.Enabled = false;
                P_TID.Enabled = false;
                P_User.Enabled = false;

                Same_6B.Enabled = false;
                Different_6B.Enabled = false;
                Less_6B.Enabled = false;
                Greater_6B.Enabled = false;
                button6.Enabled = false;
                button8.Enabled = false;
                button4.Enabled = false;
                button12.Enabled = false;
                button9.Enabled = false;

                DestroyCode.Enabled = false;
                AccessCode.Enabled = false;
                NoProect.Enabled = false;
                Proect.Enabled = false;
                Always.Enabled = false;
                AlwaysNot.Enabled = false;
                NoProect2.Enabled = false;
                Proect2.Enabled = false;
                Always2.Enabled = false;
                AlwaysNot2.Enabled = false;
                P_Reserve.Enabled = false;
                P_EPC.Enabled = false;
                P_TID.Enabled = false;
                P_User.Enabled = false;
                Button_WriteEPC_G2.Enabled = false;
                SpeedButton_Read_G2.Enabled = false;
                Button_DataWrite.Enabled = false;
                BlockWrite.Enabled = false;
                Button_BlockErase.Enabled = false;
                Button_SetProtectState.Enabled = false;
                ListView1_EPC.Items.Clear();
                ComboBox_EPC1.Items.Clear();
                ComboBox_EPC2.Items.Clear();
                ComboBox_EPC3.Items.Clear();
                button2.Text = "Query Tag";
                checkBox1.Enabled = false;

                SpeedButton_Read_6B.Enabled = false;
                SpeedButton_Write_6B.Enabled = false;
                Button14.Enabled = false;
                Button15.Enabled = false;
                ListView_ID_6B.Items.Clear();
                ComOpen = false;

                button10.Text = "Get";
                timer1.Enabled = false;
                button10.Enabled = false;
                button11.Enabled = false;
                panel1.Enabled = false;
                panel2.Enabled = false;
                panel3.Enabled = false;
                panel4.Enabled = false;
                panel5.Enabled = false;
                button_settigtime.Enabled = false;
                button_gettigtime.Enabled = false;
            }
         }
        private void Button3_Click(object sender, EventArgs e)
        {
              byte[] TrType=new byte[2];
              byte[] VersionInfo=new byte[2];
              byte ReaderType=0;
              byte ScanTime=0;
              byte dmaxfre=0;
              byte dminfre = 0;
              byte powerdBm=0;
              byte FreBand = 0;
              Edit_Version.Text = "";
              Edit_ComAdr.Text = "";
              Edit_scantime.Text = "";
              Edit_Type.Text = "";
              ISO180006B.Checked=false;
              EPCC1G2.Checked=false;
              Edit_powerdBm.Text = "";
              Edit_dminfre.Text = "";
              Edit_dmaxfre.Text = "";
              ComboBox_PowerDbm.Items.Clear();
              fCmdRet = StaticClassReaderB.GetReaderInformation(ref fComAdr, VersionInfo, ref ReaderType, TrType, ref dmaxfre, ref dminfre, ref powerdBm, ref ScanTime, frmcomportindex);
              if (fCmdRet == 0)
              {
                  Edit_Version.Text = Convert.ToString(VersionInfo[0], 10).PadLeft(2, '0') + "." + Convert.ToString(VersionInfo[1], 10).PadLeft(2, '0');
                  for (int i = 0; i < 34; i++)
                      ComboBox_PowerDbm.Items.Add(Convert.ToString(i));
                  if (powerdBm > 33)
                      ComboBox_PowerDbm.SelectedIndex = 33;
                  else
                      ComboBox_PowerDbm.SelectedIndex = powerdBm;
                  Edit_ComAdr.Text = Convert.ToString(fComAdr, 16).PadLeft(2, '0');
                  Edit_NewComAdr.Text = Convert.ToString(fComAdr, 16).PadLeft(2, '0');
                  Edit_scantime.Text = Convert.ToString(ScanTime, 10).PadLeft(2, '0') + "*100ms";
                  ComboBox_scantime.SelectedIndex = ScanTime;
                  Edit_powerdBm.Text = Convert.ToString(powerdBm, 10).PadLeft(2, '0');

                  FreBand= Convert.ToByte(((dmaxfre & 0xc0)>> 4)|(dminfre >> 6)) ;
                  switch (FreBand)
                  {
                      case 0:
                          {
                              radioButton_band0.Checked = true;
                              fdminfre = 840 + (dminfre & 0x3F) * 2;
                              fdmaxfre = 840 + (dmaxfre & 0x3F) * 2;
                          }
                          break;
                      case 1:
                          {
                              radioButton_band2.Checked = true;
                              fdminfre = 920.125 + (dminfre & 0x3F) * 0.25;
                              fdmaxfre = 920.125 + (dmaxfre & 0x3F) * 0.25;
                          }
                          break;
                      case 2:
                          {
                              radioButton_band3.Checked = true;
                              fdminfre = 902.75 + (dminfre & 0x3F) * 0.5;
                              fdmaxfre = 902.75 + (dmaxfre & 0x3F) * 0.5;
                          }
                          break;
                      case 3:
                          {
                              radioButton_band4.Checked = true;
                              fdminfre = 917.1 + (dminfre & 0x3F) * 0.2;
                              fdmaxfre = 917.1 + (dmaxfre & 0x3F) * 0.2;
                          }
                          break;
                      case 4:
                          {
                              radioButton_band5.Checked = true;
                              fdminfre = 865.1 + (dminfre & 0x3F) * 0.2;
                              fdmaxfre = 865.1 + (dmaxfre & 0x3F) * 0.2;
                          }
                          break;
                  }
                  Edit_dminfre.Text = Convert.ToString(fdminfre) + "MHz";
                  Edit_dmaxfre.Text = Convert.ToString(fdmaxfre) + "MHz";
                  if ((Convert.ToInt32(fdmaxfre) & 0x3F) != (Convert.ToInt32(fdminfre) & 0x3F))
                      CheckBox_SameFre.Checked = false;
                  if (ComboBox_dminfre.Items.Count>0)
                  ComboBox_dminfre.SelectedIndex = dminfre & 0x3F;
                if (ComboBox_dmaxfre.Items.Count > 0)
                    ComboBox_dmaxfre.SelectedIndex = dmaxfre & 0x3F;
                  if (ReaderType == 0x0d)
                      Edit_Type.Text = "UHFReader188";
                  else if(ReaderType == 0x0E)
                       Edit_Type.Text = "UHFReader7000";
                  else if (ReaderType == 0x89)
                    Edit_Type.Text = "UHFReader2001-A9393";

                if ((TrType[0] & 0x02) == 0x02) //bit1表示G2协议
                  {
                      EPCC1G2.Checked = true;
                  }
                  else
                  {
                      EPCC1G2.Checked = false;
                  }
                  if ((TrType[0] & 0x01) == 0x01) //bit0表示6B协议
                  {
                      ISO180006B.Checked = true;
                  }
                  else
                  {
                      ISO180006B.Checked = false;
                  }
              }
              AddCmdLog("GetReaderInformation", "Acquire Reader-Writer Information", fCmdRet);
        }

        private void Button5_Click(object sender, EventArgs e)
        {
              byte aNewComAdr, powerDbm, dminfre, dmaxfre, scantime, band=0;
              string returninfo="";
              string returninfoDlg="";
              string setinfo;
              if (radioButton_band0.Checked)
                  band = 0;
              if (radioButton_band2.Checked)
                  band = 1;
              if (radioButton_band3.Checked)
                  band = 2;
              if (radioButton_band4.Checked)
                  band = 3;
              if (radioButton_band5.Checked)
                  band = 4;
              if (Edit_NewComAdr.Text == "")
                  return;
              progressBar1.Visible = true;
              progressBar1.Minimum = 0;
              dminfre = Convert.ToByte(((band & 3) << 6) | (ComboBox_dminfre.SelectedIndex & 0x3F));
              dmaxfre = Convert.ToByte(((band & 0x0c) << 4) | (ComboBox_dmaxfre.SelectedIndex & 0x3F));
              aNewComAdr = Convert.ToByte(Edit_NewComAdr.Text);
              powerDbm = Convert.ToByte(ComboBox_PowerDbm.SelectedIndex);
              fBaud = Convert.ToByte(ComboBox_baud.SelectedIndex);
              if (fBaud > 2)
                  fBaud = Convert.ToByte(fBaud + 2);
              scantime = Convert.ToByte(ComboBox_scantime.SelectedIndex);
              setinfo = "Write";
              progressBar1.Value =10;     
              fCmdRet = StaticClassReaderB.WriteComAdr(ref fComAdr,ref aNewComAdr,frmcomportindex);
              if (fCmdRet==0x13)
              fComAdr = aNewComAdr;
              if (fCmdRet == 0)
              {
                fComAdr = aNewComAdr;
                returninfo = returninfo + setinfo + "The reader-writer address is successful";
              }
              else if (fCmdRet==0xEE )
                  returninfo = returninfo + setinfo + "The reader-writer address returns a command error";
              else
              {
                  returninfo = returninfo + setinfo + "Reader-Writer Address Failure";
                  returninfoDlg = returninfoDlg + setinfo + "Reader-Writer Address FailureCommand Return=0x"
                   + Convert.ToString(fCmdRet) + "(" + GetReturnCodeDesc(fCmdRet) + ")";
              }
              progressBar1.Value =25; 
              fCmdRet = StaticClassReaderB.SetPowerDbm(ref fComAdr,powerDbm,frmcomportindex);
              if (fCmdRet == 0)
                  returninfo = returninfo + ",Power Success";
              else if (fCmdRet==0xEE )
                  returninfo = returninfo + ",Power Return Command Error";
              else
              {
                  returninfo = returninfo + ",Power Failed";
                  returninfoDlg = returninfoDlg + " " + setinfo + "Power FailedCommand Return=0x"
                       +Convert.ToString(fCmdRet)+"("+GetReturnCodeDesc(fCmdRet)+")";
              }
              
              progressBar1.Value =40; 
              fCmdRet = StaticClassReaderB.Writedfre(ref fComAdr,ref dmaxfre,ref dminfre,frmcomportindex);
              if (fCmdRet == 0 )
                  returninfo = returninfo + ",Frequency Success";
              else if (fCmdRet==0xEE)
                  returninfo = returninfo + ",Frequency Return Command Error";
              else
              {
                  returninfo = returninfo + ",Frequency Failed";
                  returninfoDlg = returninfoDlg + " " + setinfo + "Frequency Failed Command Return=0x"
                   + Convert.ToString(fCmdRet) + "(" + GetReturnCodeDesc(fCmdRet) + ")";
              }

                    progressBar1.Value =55; 
                  fCmdRet = StaticClassReaderB.Writebaud(ref fComAdr,ref fBaud,frmcomportindex);
                  if (fCmdRet == 0)
                      returninfo = returninfo + ",Baud Rate Successful";
                  else if (fCmdRet==0xEE)
                      returninfo = returninfo + ",Baud Rate Return Command Error";
                  else
                  {
                      returninfo = returninfo + ",Baud Rate Failed";
                      returninfoDlg = returninfoDlg + " " + setinfo + "Baud Rate Failed Command Return=0x"
                       + Convert.ToString(fCmdRet) + "(" + GetReturnCodeDesc(fCmdRet) + ")";
                  }

             progressBar1.Value =70; 
              fCmdRet = StaticClassReaderB.WriteScanTime(ref fComAdr,ref scantime,frmcomportindex);
              if (fCmdRet == 0 )
                  returninfo = returninfo + ",Query time successful";
             else if (fCmdRet==0xEE)
                  returninfo = returninfo + ",Query Time Return Command Error";
              else
              {
                  returninfo = returninfo + ",Query time failed";
                  returninfoDlg = returninfoDlg + " " + setinfo + "Query time failed Command Return=0x"
                   + Convert.ToString(fCmdRet) + "(" + GetReturnCodeDesc(fCmdRet) + ")";
             }

              progressBar1.Value =100; 
              Button3_Click(sender,e);
              progressBar1.Visible=false;
              StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() + returninfo;
              if  (returninfoDlg!="")
                  MessageBox.Show(returninfoDlg, "Prompt");
            
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            byte aNewComAdr, powerDbm, dminfre, dmaxfre, scantime;
            string returninfo = "";
            string returninfoDlg = "";
            string setinfo;
            progressBar1.Visible = true;
            progressBar1.Minimum = 0;
            dminfre = 128;
            dmaxfre = 49;
            aNewComAdr =0x00;
            powerDbm = 26;
            fBaud=5;
            scantime=60;
            setinfo=" Restore ";
            ComboBox_baud.SelectedIndex = 3;
            progressBar1.Value = 10;
            fCmdRet = StaticClassReaderB.WriteComAdr(ref fComAdr, ref aNewComAdr, frmcomportindex);
            if (fCmdRet == 0x13)
                fComAdr = aNewComAdr;
            if (fCmdRet == 0)
            {
                fComAdr = aNewComAdr;
                returninfo = returninfo + setinfo + "Reader-Writer Address Successful";
            }
            else if (fCmdRet == 0xEE)
                returninfo = returninfo + setinfo + "Reader-Writer Address Returns Command Error";
            else
            {
                returninfo = returninfo + setinfo + "Reader-Writer Address Failure";
                returninfoDlg = returninfoDlg + setinfo + "Reader-Writer Address Failure Command Return=0x"
                 + Convert.ToString(fCmdRet) + "(" + GetReturnCodeDesc(fCmdRet) + ")";
            }

            progressBar1.Value = 25;
            fCmdRet = StaticClassReaderB.SetPowerDbm(ref fComAdr, powerDbm, frmcomportindex);
            if (fCmdRet == 0)
                returninfo = returninfo + ",Power Success";
            else if (fCmdRet == 0xEE)
                returninfo = returninfo + ",Power Return Command Error";
            else
            {
                returninfo = returninfo + ",Power Failed";
                returninfoDlg = returninfoDlg + " " + setinfo + "Power Failed Command Return=0x"
                     + Convert.ToString(fCmdRet) + "(" + GetReturnCodeDesc(fCmdRet) + ")";
            }

            progressBar1.Value = 40;
            fCmdRet = StaticClassReaderB.Writedfre(ref fComAdr, ref dmaxfre, ref dminfre, frmcomportindex);
            if (fCmdRet == 0)
                returninfo = returninfo + ",Frequency Success";
            else if (fCmdRet == 0xEE)
                returninfo = returninfo + ",Frequency Return Command Error";
            else
            {
                returninfo = returninfo + ",Frequency Failed";
                returninfoDlg = returninfoDlg + " " + setinfo + "Frequency Failed Command Return=0x"
                 + Convert.ToString(fCmdRet) + "(" + GetReturnCodeDesc(fCmdRet) + ")";
            }


            progressBar1.Value = 55;
            fCmdRet = StaticClassReaderB.Writebaud(ref fComAdr, ref fBaud, frmcomportindex);
            if (fCmdRet == 0)
                returninfo = returninfo + ",Baud Rate Successful";
            else if (fCmdRet == 0xEE)
                returninfo = returninfo + ",Baud Rate Return Command Error";
            else
            {
                returninfo = returninfo + ",Baud Rate Failed";
                returninfoDlg = returninfoDlg + " " + setinfo + "Baud Rate Failed Command Return=0x"
                 + Convert.ToString(fCmdRet) + "(" + GetReturnCodeDesc(fCmdRet) + ")";
            }

            progressBar1.Value = 70;
            fCmdRet = StaticClassReaderB.WriteScanTime(ref fComAdr, ref scantime, frmcomportindex);
            if (fCmdRet == 0)
                returninfo = returninfo + ",Query time successful";
            else if (fCmdRet == 0xEE)
                returninfo = returninfo + ",Query Time Return Command Error";
            else
            {
                returninfo = returninfo + ",Query time failed";
                returninfoDlg = returninfoDlg + " " + setinfo + "Query time failed Command Return=0x"
                 + Convert.ToString(fCmdRet) + "(" + GetReturnCodeDesc(fCmdRet) + ")";
            }

            progressBar1.Value = 100;
            Button3_Click(sender, e);
            progressBar1.Visible = false;
            StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() + returninfo;
            if (returninfoDlg != "")
                MessageBox.Show(returninfoDlg, "Prompt");
            
        }

        private void CheckBox_SameFre_CheckedChanged(object sender, EventArgs e)
        {
             if (CheckBox_SameFre.Checked)
              ComboBox_dmaxfre.SelectedIndex = ComboBox_dminfre.SelectedIndex;
        }


        private void ComboBox_dfreSelect(object sender, EventArgs e)
        {
             if (CheckBox_SameFre.Checked )
             {
                ComboBox_dminfre.SelectedIndex =ComboBox_dmaxfre.SelectedIndex;
             }
              else if  (ComboBox_dminfre.SelectedIndex> ComboBox_dmaxfre.SelectedIndex )
             {
                 ComboBox_dminfre.SelectedIndex = ComboBox_dmaxfre.SelectedIndex;
                 MessageBox.Show("The minimum frequency should be less than or equal to the maximum frequency", "Error Prompt");
              }
        }
        public void ChangeSubItem(ListViewItem ListItem, int subItemIndex, string ItemText,string RSSI)
        {
            if (subItemIndex == 1)
            {
                if (ItemText=="")
                {
                    ListItem.SubItems[subItemIndex].Text = ItemText;
                    if (ListItem.SubItems[subItemIndex + 2].Text == "")
                    {
                        ListItem.SubItems[subItemIndex + 2].Text = "1";
                    }
                    else
                    {
                        ListItem.SubItems[subItemIndex + 2].Text = Convert.ToString(Convert.ToInt32(ListItem.SubItems[subItemIndex + 2].Text) + 1);
                    }
                }
                else 
                if (ListItem.SubItems[subItemIndex].Text != ItemText)
                {
                    ListItem.SubItems[subItemIndex].Text = ItemText;
                    ListItem.SubItems[subItemIndex+2].Text = "1";
                }
                else
                {
                    ListItem.SubItems[subItemIndex + 2].Text = Convert.ToString(Convert.ToInt32(ListItem.SubItems[subItemIndex + 2].Text) + 1);
                    if( (Convert.ToUInt32(ListItem.SubItems[subItemIndex + 2].Text)>9999))
                        ListItem.SubItems[subItemIndex + 2].Text="1";
                }
                ListItem.SubItems[subItemIndex + 3].Text = RSSI;
            }
            if (subItemIndex == 2)
            {
                if (ListItem.SubItems[subItemIndex].Text != ItemText)
                {
                    ListItem.SubItems[subItemIndex].Text = ItemText;
                }
            }

        }
        List<string> mepclist = new List<string>();
        long beginTime=0;
        private volatile bool toStopThread = false;
        private Thread mythread = null;
        byte Qvalue = 6;
        byte Session = 0;
        private void button2_Click(object sender, EventArgs e)
        {
            if (button2.Text == "Query Tag")
            {
                Qvalue = Convert.ToByte(comboBox7.SelectedIndex);
                Session = Convert.ToByte(comboBox8.SelectedIndex);
                if (CheckBox_TID.Checked)
                {
                    if ((textBox4.Text.Length) != 2 || ((textBox5.Text.Length) != 2))
                    {
                        StatusBar1.Panels[0].Text = "TID Query Parameter Error!";
                        return;
                    }
                }
                toStopThread = false;
                mythread = new Thread(new ThreadStart(Inventory));
                mythread.IsBackground = true;
                mythread.Start();
                Timer_Test_.Enabled = true;
                textBox4.Enabled = false;
                textBox5.Enabled = false;
                CheckBox_TID.Enabled = false;
                comboBox7.Enabled = false;
                comboBox8.Enabled = false;
                DestroyCode.Enabled = false;
                AccessCode.Enabled = false;
                NoProect.Enabled = false;
                Proect.Enabled = false;
                Always.Enabled = false;
                AlwaysNot.Enabled = false;
                NoProect2.Enabled = false;
                Proect2.Enabled = false;
                Always2.Enabled = false;
                AlwaysNot2.Enabled = false;
                P_Reserve.Enabled = false;
                P_EPC.Enabled = false;
                P_TID.Enabled = false;
                P_User.Enabled = false;
                Button_WriteEPC_G2.Enabled = false;
                Button_DestroyCard.Enabled = false;
                SpeedButton_Read_G2.Enabled = false;
                Button_DataWrite.Enabled = false;
                BlockWrite.Enabled = false;
                Button_BlockErase.Enabled = false;
                Button_SetProtectState.Enabled = false;
                ListView1_EPC.Items.Clear();
                ComboBox_EPC1.Items.Clear();
                ComboBox_EPC2.Items.Clear();
                ComboBox_EPC3.Items.Clear();
                button2.Text = "Stop Query Tag";
                checkBox1.Enabled = false;
               // textBox6.Text = "";
                textBox10.Text = "0";
                mepclist.Clear();
                beginTime = System.Environment.TickCount;
              //  Timer_Test_.Enabled = true;

            }
            else
            {
                toStopThread = true;
                button2.Enabled = false;
                button2.Text = "Stopping";
            }
        }

        private void Inventory()
        {
            while (!toStopThread)
            {
                int CardNum = 0;
                int Totallen = 0;
                byte[] EPC = new byte[50000];
                byte AdrTID = 0;
                byte LenTID = 0;
                byte TIDFlag = 0;
                if (CheckBox_TID.Checked)
                {
                    AdrTID = Convert.ToByte(textBox4.Text, 16);
                    LenTID = Convert.ToByte(textBox5.Text, 16);
                    TIDFlag = 1;
                }
                else
                {
                    AdrTID = 0;
                    LenTID = 0;
                    TIDFlag = 0;
                }

               
                fCmdRet = StaticClassReaderB.Inventory_G2(ref fComAdr, Qvalue, Session, AdrTID, LenTID, TIDFlag, EPC, ref Totallen, ref CardNum, frmcomportindex);
            }
            
            this.Invoke((EventHandler)delegate
            {
                Timer_Test_.Enabled = false;
                textBox4.Enabled = true;
                textBox5.Enabled = true;
                CheckBox_TID.Enabled = true;
                comboBox7.Enabled = true;
                comboBox8.Enabled = true;
                if (ListView1_EPC.Items.Count != 0)
                {
                    DestroyCode.Enabled = false;
                    AccessCode.Enabled = false;
                    NoProect.Enabled = false;
                    Proect.Enabled = false;
                    Always.Enabled = false;
                    AlwaysNot.Enabled = false;
                    NoProect2.Enabled = true;
                    Proect2.Enabled = true;
                    Always2.Enabled = true;
                    AlwaysNot2.Enabled = true;
                    P_Reserve.Enabled = true;
                    P_EPC.Enabled = true;
                    P_TID.Enabled = true;
                    P_User.Enabled = true;
                    Button_DestroyCard.Enabled = true;
                    Button_WriteEPC_G2.Enabled = true;
                    SpeedButton_Read_G2.Enabled = true;
                    Button_SetProtectState.Enabled = true;
                    Button_DataWrite.Enabled = true;
                    BlockWrite.Enabled = true;
                    Button_BlockErase.Enabled = true;
                    checkBox1.Enabled = true;
                }
                if (ListView1_EPC.Items.Count == 0)
                {
                    DestroyCode.Enabled = false;
                    AccessCode.Enabled = false;
                    NoProect.Enabled = false;
                    Proect.Enabled = false;
                    Always.Enabled = false;
                    AlwaysNot.Enabled = false;
                    NoProect2.Enabled = false;
                    Proect2.Enabled = false;
                    Always2.Enabled = false;
                    AlwaysNot2.Enabled = false;
                    P_Reserve.Enabled = false;
                    P_EPC.Enabled = false;
                    P_TID.Enabled = false;
                    P_User.Enabled = false;
                    Button_DestroyCard.Enabled = false;

                    SpeedButton_Read_G2.Enabled = false;
                    Button_DataWrite.Enabled = false;
                    BlockWrite.Enabled = false;
                    Button_BlockErase.Enabled = false;
                    Button_WriteEPC_G2.Enabled = true;
                    Button_SetProtectState.Enabled = false;
                    checkBox1.Enabled = false;

                }
                button2.Enabled = true;
                AddCmdLog("Inventory", "Exit Query Tag", 0);
                button2.Text = "Query Tag";
            });
            mythread = null;
           
        }
        private void Timer_Test__Tick(object sender, EventArgs e)
        {
            /* if (fIsInventoryScan)
                 return;           
             Inventory();*/
            textBox11.Text = (System.Environment.TickCount - beginTime) + "";
        }

        private void SpeedButton_Read_G2_Click(object sender, EventArgs e)
        {
            if (Edit_WordPtr.Text == "")
            {
                MessageBox.Show("The start address is empty", "Information Prompt");
                return;
            }
            if (textBox1.Text == "")
            {
                MessageBox.Show("Read/Block Erase Lengh", "Information Prompt");
                return;
            }
            if (Edit_AccessCode2.Text == "")
            {
                MessageBox.Show("The password is empty", "Information Prompt");
                return;
            }
            if (Convert.ToInt32(Edit_WordPtr.Text,16) + Convert.ToInt32(textBox1.Text) > 120)
                return;
               Timer_G2_Read.Enabled =!Timer_G2_Read.Enabled;
               if (Timer_G2_Read.Enabled)
               {
                   DestroyCode.Enabled = false;
                   AccessCode.Enabled = false;
                   NoProect.Enabled = false;
                   Proect.Enabled = false;
                   Always.Enabled = false;
                   AlwaysNot.Enabled = false;
                   NoProect2.Enabled = false;
                   Proect2.Enabled = false;
                   Always2.Enabled = false;
                   AlwaysNot2.Enabled = false;
                   P_Reserve.Enabled = false;
                   P_EPC.Enabled = false;
                   P_TID.Enabled = false;
                   P_User.Enabled = false;
                   Button_WriteEPC_G2.Enabled = false;

                   Button_DestroyCard.Enabled = false;
                   button2.Enabled = false;
                   Button_DataWrite.Enabled = false;
                   BlockWrite.Enabled = false;
                   Button_BlockErase.Enabled = false;
                   Button_SetProtectState.Enabled = false;
                   SpeedButton_Read_G2.Text = "Stop";
               }
               else
               {
                   if (ListView1_EPC.Items.Count != 0)
                   {
                       DestroyCode.Enabled = false;
                       AccessCode.Enabled = false;
                       NoProect.Enabled = false;
                       Proect.Enabled = false;
                       Always.Enabled = false;
                       AlwaysNot.Enabled = false;
                       NoProect2.Enabled = true;
                       Proect2.Enabled = true;
                       Always2.Enabled = true;
                       AlwaysNot2.Enabled = true;
                       P_Reserve.Enabled = true;
                       P_EPC.Enabled = true;
                       P_TID.Enabled = true;
                       P_User.Enabled = true;
                       Button_DestroyCard.Enabled = true;
                       Button_WriteEPC_G2.Enabled = true;
                       button2.Enabled = true;
                       Button_SetProtectState.Enabled = true;
                   
                       Button_DataWrite.Enabled = true;
                       BlockWrite.Enabled = true;
                       Button_BlockErase.Enabled = true;
                   }
                   if (ListView1_EPC.Items.Count == 0)
                   {
                       DestroyCode.Enabled = false;
                       AccessCode.Enabled = false;
                       NoProect.Enabled = false;
                       Proect.Enabled = false;
                       Always.Enabled = false;
                       AlwaysNot.Enabled = false;
                       NoProect2.Enabled = false;
                       Proect2.Enabled = false;
                       Always2.Enabled = false;
                       AlwaysNot2.Enabled = false;
                       P_Reserve.Enabled = false;
                       P_EPC.Enabled = false;
                       P_TID.Enabled = false;
                       P_User.Enabled = false;
                       Button_DestroyCard.Enabled = false;
                       Button_SetProtectState.Enabled = false;
                       button2.Enabled = true;
                       Button_DataWrite.Enabled = false;
                       BlockWrite.Enabled = false;
                       Button_BlockErase.Enabled = false;
                       Button_WriteEPC_G2.Enabled = true;

                   }
                   SpeedButton_Read_G2.Text = "Read";
               }
        }

        private void Timer_G2_Read_Tick(object sender, EventArgs e)
        {
            if (fIsInventoryScan)
                return;
            fIsInventoryScan = true;
            byte WordPtr, ENum;
            byte Num = 0;
            byte Mem = 0;
            byte EPClength=0;
            string str;
            byte[] CardData=new  byte[320];
            if ((maskadr_textbox.Text=="")||(maskLen_textBox.Text=="") )            
            {
              fIsInventoryScan = false;
              return;
            }
            if (checkBox1.Checked)
                MaskFlag=1;
            else
                MaskFlag = 0;
            Maskadr = Convert.ToByte(maskadr_textbox.Text,16);
            MaskLen = Convert.ToByte(maskLen_textBox.Text,16);
            if (textBox1.Text == "")
            {
                fIsInventoryScan = false;
                return;
            }
            if (ComboBox_EPC2.Items.Count == 0)
            {
                fIsInventoryScan = false;
                return;
            }
            if (ComboBox_EPC2.SelectedItem == null)
            {
                fIsInventoryScan = false;
                return;
            }
            str = ComboBox_EPC2.SelectedItem.ToString();
            ENum = Convert.ToByte(str.Length / 4);
            EPClength = Convert.ToByte(str.Length / 2);
            byte[] EPC = new byte[ENum*2];
            EPC = HexStringToByteArray(str);
            if (C_Reserve.Checked)
                Mem = 0;
            if (C_EPC.Checked)
                Mem = 1;
            if (C_TID.Checked)
                Mem = 2;
            if (C_User.Checked)
                Mem = 3;
            if (Edit_AccessCode2.Text == "")
            {
                fIsInventoryScan = false;
                return;
            }
            if (Edit_WordPtr.Text == "")
            {
                fIsInventoryScan = false;
                return;
            }
            WordPtr = Convert.ToByte(Edit_WordPtr.Text, 16);
            Num = Convert.ToByte(textBox1.Text);
            if (Edit_AccessCode2.Text.Length != 8)
            {
                fIsInventoryScan = false;
                return;
            }
            fPassWord = HexStringToByteArray(Edit_AccessCode2.Text);
            fCmdRet = StaticClassReaderB.ReadCard_G2(ref fComAdr, EPC, Mem, WordPtr, Num, fPassWord,Maskadr,MaskLen,MaskFlag, CardData, EPClength, ref ferrorcode, frmcomportindex);
            if (fCmdRet == 0)
            {
                byte[] daw = new byte[Num*2];
                Array.Copy(CardData, daw, Num * 2);
                listBox1.Items.Add(ByteArrayToHexString(daw));
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
                AddCmdLog("ReadData", "Read", fCmdRet);
            }
            if (ferrorcode != -1)
             {
                  StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() +
                   " 'Read' Return Error=0x" + Convert.ToString(ferrorcode, 2) +
                   "(" + GetErrorCodeDesc(ferrorcode) + ")";
                    ferrorcode=-1;
             }
             fIsInventoryScan = false;
              if (fAppClosed)
                    Close();
        }

        private void Button_DataWrite_Click(object sender, EventArgs e)
        {
            byte WordPtr, ENum;
            byte Num = 0;
            byte Mem = 0;
            byte WNum = 0;
            byte EPClength = 0;
            byte Writedatalen = 0;
            int  WrittenDataNum = 0;
            string s2, str;
            byte[] CardData = new byte[320];
            byte[] writedata = new byte[230];
            if ((maskadr_textbox.Text == "") || (maskLen_textBox.Text == ""))
            {
                return;
            }
            if (checkBox1.Checked)
                MaskFlag = 1;
            else
                MaskFlag = 0;
            Maskadr = Convert.ToByte(maskadr_textbox.Text, 16);
            MaskLen = Convert.ToByte(maskLen_textBox.Text, 16);
            if (ComboBox_EPC2.Items.Count == 0)
                return;
            if (ComboBox_EPC2.SelectedItem == null)
                return;
            str = ComboBox_EPC2.SelectedItem.ToString();
            ENum = Convert.ToByte(str.Length / 4);
            EPClength = Convert.ToByte(ENum * 2);
            byte[] EPC = new byte[ENum];
            EPC = HexStringToByteArray(str);
            if (C_Reserve.Checked)
                Mem = 0;
            if (C_EPC.Checked)
                Mem = 1;
            if (C_TID.Checked)
                Mem = 2;
            if (C_User.Checked)
                Mem = 3;
            if (Edit_WordPtr.Text == "")
            {
                MessageBox.Show("The start address is empty", "Information Prompt");
                return;
            }
            if (textBox1.Text == "")
            {
                MessageBox.Show("Read/Block Erase Length", "Information Prompt");
                return;
            }
            if (Convert.ToInt32(Edit_WordPtr.Text,16) + Convert.ToInt32(textBox1.Text) > 120)
                return;
            if (Edit_AccessCode2.Text == "")
            {
                return;
            }
            WordPtr = Convert.ToByte(Edit_WordPtr.Text, 16);
            Num = Convert.ToByte(textBox1.Text);
            if (Edit_AccessCode2.Text.Length != 8)
            {
                return;
            }
            fPassWord = HexStringToByteArray(Edit_AccessCode2.Text);
            if (Edit_WriteData.Text == "")
                return;
            s2 = Edit_WriteData.Text;
            if (s2.Length % 4 != 0)
            {
                MessageBox.Show("Input in word units.", "Write");
                return;
            }
            WNum = Convert.ToByte(s2.Length / 4);
            byte[] Writedata = new byte[WNum * 2];
            Writedata = HexStringToByteArray(s2);
            Writedatalen = Convert.ToByte(WNum * 2);
             if((checkBox_pc.Checked)&&(C_EPC.Checked))
             {
                 WordPtr = 1;
                 Writedatalen =Convert.ToByte(Edit_WriteData.Text.Length /2 + 2);
                 Writedata = HexStringToByteArray(textBox_pc.Text + Edit_WriteData.Text);
             }
            fCmdRet = StaticClassReaderB.WriteCard_G2(ref fComAdr, EPC, Mem, WordPtr, Writedatalen, Writedata, fPassWord,Maskadr,MaskLen,MaskFlag, WrittenDataNum, EPClength, ref ferrorcode, frmcomportindex);
            AddCmdLog("Write data", "Write", fCmdRet);
            if (fCmdRet == 0)
            {
                StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() + "‘Write EPC”Command Return=0x00" +
                  "(Write EPC Successful)";
            }    
        }

        private void Button_BlockErase_Click(object sender, EventArgs e)
        {
            byte WordPtr, ENum;
            byte Num = 0;
            byte Mem = 0;
            byte EPClength = 0;
            string str;
            byte[] CardData = new byte[320];
            if ((maskadr_textbox.Text == "") || (maskLen_textBox.Text == ""))
            {
                fIsInventoryScan = false;
                return;
            }
            if (checkBox1.Checked)
                MaskFlag = 1;
            else
                MaskFlag = 0;
            Maskadr = Convert.ToByte(maskadr_textbox.Text,16);
            MaskLen = Convert.ToByte(maskLen_textBox.Text,16);
            if (ComboBox_EPC2.Items.Count == 0)
                return;
            if (ComboBox_EPC2.SelectedItem == null)
                return;
            str = ComboBox_EPC2.SelectedItem.ToString();
            if (str == "")
                return;
            ENum = Convert.ToByte(str.Length / 4);
            EPClength = Convert.ToByte(str.Length / 2);
            byte[] EPC = new byte[ENum];
            EPC = HexStringToByteArray(str);
            if (C_Reserve.Checked)
                Mem = 0;
            if (C_EPC.Checked)
                Mem = 1;
            if (C_TID.Checked)
                Mem = 2;
            if (C_User.Checked)
                Mem = 3;
            if (Edit_WordPtr.Text == "")
            {
                MessageBox.Show("The start address is empty", "Information Prompt");
                return;
            }
            if (textBox1.Text == "")
            {
                MessageBox.Show("Read/Block Erase Length", "Information Prompt");
                return;
            }
            if (Convert.ToInt32(Edit_WordPtr.Text,16) + Convert.ToInt32(textBox1.Text) > 120)
                return;
            if (Edit_AccessCode2.Text == "")
                return;
            WordPtr = Convert.ToByte(Edit_WordPtr.Text, 16);
            if ((Mem == 1) & (WordPtr < 2))
            {
                MessageBox.Show("The start address length for erasing the EPC area must be greater than or equal to 0x01! Please re-enter!", "Information Prompt");
                return;
            }
            Num = Convert.ToByte(textBox1.Text);
            if (Edit_AccessCode2.Text.Length != 8)
            {
                return;
            }
            fPassWord = HexStringToByteArray(Edit_AccessCode2.Text);
            fCmdRet = StaticClassReaderB.EraseCard_G2(ref fComAdr, EPC, Mem, WordPtr, Num, fPassWord,Maskadr,MaskLen,MaskFlag,EPClength, ref ferrorcode, frmcomportindex);
            AddCmdLog("EraseCard", "Block Erase", fCmdRet);
            if (fCmdRet == 0)
            {
                StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() + "“Erase Data”Command Return=0x00" +
                     "((Data Erasure Successful)";
            }       
        }

        private void button7_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
        }

        private void Button_SetProtectState_Click(object sender, EventArgs e)
        {
              byte select=0;
              byte setprotect=0;
              byte EPClength;
              string str;
              byte ENum;
              if ((maskadr_textbox.Text == "") || (maskLen_textBox.Text == ""))
              {
                  fIsInventoryScan = false;
                  return;
              }
              if (checkBox1.Checked)
                  MaskFlag = 1;
              else
                  MaskFlag = 0;
              Maskadr = Convert.ToByte(maskadr_textbox.Text,16);
              MaskLen = Convert.ToByte(maskLen_textBox.Text,16);
              if (ComboBox_EPC1.Items.Count == 0)
                  return;
              if (ComboBox_EPC1.SelectedItem == null)
                  return;
              str = ComboBox_EPC1.SelectedItem.ToString();
              if (str == "")
                  return;
              ENum = Convert.ToByte(str.Length / 4);             
              EPClength = Convert.ToByte(str.Length / 2);
              byte[] EPC = new byte[ENum];
              EPC = HexStringToByteArray(str);
              if (textBox2.Text.Length != 8)
              {
                  MessageBox.Show("The access password must be at least 8 characters long. Please re-enter!", "Information Prompt");
                  return;
              }
              fPassWord = HexStringToByteArray(textBox2.Text);
              if ((P_Reserve.Checked) & (DestroyCode.Checked))
                  select = 0x00;
              else if ((P_Reserve.Checked) & (AccessCode.Checked))
                  select = 0x01;
              else if (P_EPC.Checked)
                  select = 0x02;
              else if (P_TID.Checked)
                  select = 0x03;
              else if (P_User.Checked)
                  select = 0x04;
              if (P_Reserve.Checked)
              {
                  if (NoProect.Checked )
                   setprotect=0x00;
                  else if (Proect.Checked)
                   setprotect=0x02;
                  else if (Always.Checked )
                  {
                   setprotect=0x01;
                   if (MessageBox.Show(this, "Are you sure you want to set it to always readable and writable?", "Information Prompt", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                         return;
                  }
                  else if (AlwaysNot.Checked )
                  {
                   setprotect=0x03;
                   if (MessageBox.Show(this, "Are you sure you want to set it to always unreadable and unwritable?", "Information Prompt", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                         return;
                  }
        }
        else
              {
                  if (NoProect2.Checked)
                   setprotect=0x00;
                  else if (Proect2.Checked)
                   setprotect=0x02;
                  else if (Always2.Checked)
                  {
                   setprotect=0x01;
                   if (MessageBox.Show(this, "Are you sure you want to set it to always writable?", "Information Prompt", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                         return;
                  }
                  else if (AlwaysNot2.Checked )
                  {
                   setprotect=0x03;
                   if (MessageBox.Show(this, "Are you sure you want to set it to always unwritable?", "Information Prompt", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                         return;
                  }
              }

              fCmdRet = StaticClassReaderB.SetCardProtect_G2(ref fComAdr, EPC, select, setprotect, fPassWord,Maskadr,MaskLen,MaskFlag, EPClength, ref ferrorcode, frmcomportindex); ;
              AddCmdLog("SetCardProtect", "Set Protection", fCmdRet);
        }

        private void Button_DestroyCard_Click(object sender, EventArgs e)
        {
            byte EPClength;
            string str;
            byte ENum;
            if ((maskadr_textbox.Text == "") || (maskLen_textBox.Text == ""))
            {
                fIsInventoryScan = false;
                return;
            }
            if (checkBox1.Checked)
                MaskFlag = 1;
            else
                MaskFlag = 0;
            Maskadr = Convert.ToByte(maskadr_textbox.Text, 16);
            MaskLen = Convert.ToByte(maskLen_textBox.Text, 16);
            StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() + "";
            if (MessageBox.Show(this, "Are you sure you want to destroy this tag?", "Information Prompt", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                return;
            if (Edit_DestroyCode.Text.Length != 8)
            {
                MessageBox.Show("The destruction password must be 8 characters long! Please re-enter!", "Information Prompt");
                return;
            }
            if (ComboBox_EPC3.Items.Count == 0)
                return;
            if (ComboBox_EPC3.SelectedItem == null)
                return;
            str = ComboBox_EPC3.SelectedItem.ToString();
            if (str == "")
                return;
            ENum = Convert.ToByte(str.Length / 4);
            EPClength = Convert.ToByte(str.Length / 2);
            byte[] EPC = new byte[ENum];
            EPC = HexStringToByteArray(str);
            fPassWord = HexStringToByteArray(Edit_DestroyCode.Text);
            fCmdRet = StaticClassReaderB.DestroyCard_G2(ref fComAdr, EPC, fPassWord,Maskadr,MaskLen,MaskFlag, EPClength, ref ferrorcode, frmcomportindex);
            AddCmdLog("DestroyCard", "Destroy Tag", fCmdRet);
            if (fCmdRet == 0)
                StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() + " “Destroy Tag”Command Return=0x00" +
                          "(Destruction Successful)";
        }

        private void Button_WriteEPC_G2_Click(object sender, EventArgs e)
        {
              byte[] WriteEPC =new byte[100];
              byte WriteEPClen;
              byte ENum;
              if (Edit_AccessCode3.Text.Length < 8)
              {
                  MessageBox.Show("The access password must be 8 characters long! Please re-enter!", "Information Prompt");
                  return;
              }
             if ((Edit_WriteEPC.Text.Length%4) !=0) 
            {
                MessageBox.Show("Please enter a hexadecimal number in word units!'+#13+#10+'example：1234、12345678!", "Information Prompt");
                    return;
            }
            WriteEPClen=Convert.ToByte(Edit_WriteEPC.Text.Length/ 2) ;
            ENum = Convert.ToByte(Edit_WriteEPC.Text.Length / 4);
            byte[] EPC = new byte[ENum];
            EPC = HexStringToByteArray(Edit_WriteEPC.Text);
            fPassWord = HexStringToByteArray(Edit_AccessCode3.Text);
            fCmdRet = StaticClassReaderB.WriteEPC_G2(ref fComAdr, fPassWord, EPC, WriteEPClen, ref ferrorcode, frmcomportindex);
            AddCmdLog("WriteEPC_G2", "WriteEPC", fCmdRet);
              if (fCmdRet == 0)
                  StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() + "“WriteEPC”Command Return=0x00" +
                            "(Write EPC Successful)";
        }

       

        private void Button_LockUserBlock_G2_Click(object sender, EventArgs e)
        {
           
           
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Timer_Test_.Enabled = false;
            Timer_G2_Read.Enabled = false;
            Timer_G2_Alarm.Enabled = false;
            fAppClosed = true;
            StaticClassReaderB.CloseComPort();

            DevControl.tagErrorCode eCode = DevControl.DM_DeInit();
            if (eCode != DevControl.tagErrorCode.DM_ERR_OK)
            {
                ErrorHandling.HandleError(eCode);
            }
        }

        private void ComboBox_IntervalTime_SelectedIndexChanged(object sender, EventArgs e)
        {
              if   (ComboBox_IntervalTime.SelectedIndex <6)
              Timer_Test_.Interval =100;
              else
              Timer_Test_.Interval =(ComboBox_IntervalTime.SelectedIndex+4)*10;
        }

        private void SpeedButton_Query_6B_Click(object sender, EventArgs e)
        {
            Timer_Test_6B.Enabled = !Timer_Test_6B.Enabled;
            if (!Timer_Test_6B.Enabled)
            {
                if (ListView_ID_6B.Items.Count != 0)
                {
                    SpeedButton_Read_6B.Enabled = true;
                    SpeedButton_Write_6B.Enabled = true;
                    Button14.Enabled = true;
                    Button15.Enabled = true;
                    if (Bycondition_6B.Checked)
                    {
                        Same_6B.Enabled = true;
                        Different_6B.Enabled = true;
                        Less_6B.Enabled = true;
                        Greater_6B.Enabled = true;
                    }
                }
                if (ListView_ID_6B.Items.Count == 0)
                {
                    SpeedButton_Read_6B.Enabled = false;
                    SpeedButton_Write_6B.Enabled = false;
                    Button14.Enabled = false;
                    Button15.Enabled = false;
                    if (Bycondition_6B.Checked)
                    {
                        Same_6B.Enabled = true ;
                        Different_6B.Enabled = true;
                        Less_6B.Enabled = true;
                        Greater_6B.Enabled = true;
                    }
                }
                AddCmdLog("Inventory", "Exit Inquiry", 0);
                SpeedButton_Query_6B.Text = "Single Record Query ";
            }
            else
            {
                SpeedButton_Read_6B.Enabled = false;
                SpeedButton_Write_6B.Enabled = false;
                Button14.Enabled = false;
                Button15.Enabled = false;
                Same_6B.Enabled = false;
                Different_6B.Enabled = false;
                Less_6B.Enabled = false;
                Greater_6B.Enabled = false;
                ListView_ID_6B.Items.Clear();
                ComboBox_ID1_6B.Items.Clear();
                CardNum1 = 0;
                list.Clear();
                SpeedButton_Query_6B.Text = "Stop";
            }
        }
        public void ChangeSubItem1(ListViewItem ListItem, int subItemIndex, string ItemText)
        {
            if (subItemIndex == 1)
            {
                if (ListItem.SubItems[subItemIndex].Text != ItemText)
                {
                    ListItem.SubItems[subItemIndex].Text = ItemText;
                    ListItem.SubItems[subItemIndex + 1].Text = "1";
                }
                else
                {
                    ListItem.SubItems[subItemIndex + 1].Text = Convert.ToString(Convert.ToUInt32(ListItem.SubItems[subItemIndex + 1].Text) + 1);
                    if ((Convert.ToUInt32(ListItem.SubItems[subItemIndex + 1].Text) > 9999))
                        ListItem.SubItems[subItemIndex + 1].Text = "1";
                }

            }
        }
        private void Inventory_6B()
        {
            int CardNum = 0 ;
             byte[] ID_6B=new byte[2000];
             byte[] ID2_6B=new byte[5000] ;
             bool isonlistview;
             string temps;
             string s,ss, sID;
             ListViewItem aListItem = new ListViewItem();
             int i, j;
             byte Condition=0;
             byte StartAddress;
             byte mask = 0;
             byte[] ConditionContent =new byte[300];
             byte Contentlen;
            if (Byone_6B.Checked)
            {
                fCmdRet = StaticClassReaderB.Inventory_6B(ref fComAdr, ID_6B, frmcomportindex);
                if (fCmdRet == 0)
                {
                    byte[] daw = new byte[8];
                    Array.Copy(ID_6B, daw, 8);
                    temps = ByteArrayToHexString(daw);                    
                    if (!list.Contains(temps))
                    {
                        CardNum1 = CardNum1 + 1;
                        list.Add(temps);
                    }
                    while (ListView_ID_6B.Items.Count < CardNum1)
                    {
                        aListItem = ListView_ID_6B.Items.Add((ListView_ID_6B.Items.Count + 1).ToString());
                        aListItem.SubItems.Add("");
                        aListItem.SubItems.Add("");
                        aListItem.SubItems.Add("");
                    }
                    isonlistview = false;
                    for (i = 0; i < CardNum1; i++)     //判断是否在Listview列表内
                    {        
                        if (temps==ListView_ID_6B.Items[i].SubItems[1].Text)
                        {
                            aListItem = ListView_ID_6B.Items[i];
                            ChangeSubItem1(aListItem, 1, temps);
                            isonlistview=true;
                        }
                    }
                    if (!isonlistview)
                    {
                        aListItem = ListView_ID_6B.Items[CardNum1-1];
                        s = temps;
                        ChangeSubItem1(aListItem, 1, s);                        
                        if (ComboBox_EPC1.Items.IndexOf(s) == -1)
                        {                   
                            ComboBox_ID1_6B.Items.Add(temps);
                        }
                    }
                }
                 if (ComboBox_ID1_6B.Items.Count != 0)
                     ComboBox_ID1_6B.SelectedIndex = 0;
            }
            if (Bycondition_6B.Checked)
            {
                if (Same_6B.Checked)
                    Condition = 0;
                else if (Different_6B.Checked)
                    Condition = 1;
                else if (Greater_6B.Checked)
                    Condition = 2;
                else if (Less_6B.Checked)
                    Condition = 3;
                if (Edit_ConditionContent_6B.Text == "")
                    return;
                ss = Edit_ConditionContent_6B.Text;
                Contentlen = Convert.ToByte((Edit_ConditionContent_6B.Text).Length);
                for (i = 0; i < 16 - Contentlen; i++)
                    ss = ss + "0";
                int Nlen = (ss.Length) / 2;
                byte[] daw = new byte[Nlen];
                daw = HexStringToByteArray(ss);
                switch (Contentlen / 2)
                {
                    case 1:                                                                                                                                                                                           
                        mask = 0x80;
                        break;
                    case 2:
                        mask = 0xC0;
                        break;
                    case 3:
                        mask = 0xE0;
                        break;
                    case 4:
                        mask = 0XF0;
                        break;
                    case 5:
                        mask = 0XF8;
                        break;
                    case 6:
                        mask = 0XFC;
                        break;
                    case 7:
                        mask = 0XFE;
                        break;
                    case 8:
                        mask = 0XFF;
                        break;
                }
                if (Edit_Query_StartAddress_6B.Text == "")
                    return;
                StartAddress = Convert.ToByte(Edit_Query_StartAddress_6B.Text);
                fCmdRet = StaticClassReaderB.inventory2_6B(ref fComAdr, Condition, StartAddress, mask, daw, ID2_6B, ref CardNum, frmcomportindex);
                if ((fCmdRet == 0x15) | (fCmdRet == 0x16) | (fCmdRet == 0x17) | (fCmdRet == 0x18) | (fCmdRet == 0xFB))
                {
                    byte[] daw1 = new byte[CardNum * 8];
                    Array.Copy(ID2_6B, daw1, CardNum * 8);
                    temps = ByteArrayToHexString(daw1);
                    for (i = 0; i < CardNum; i++)
                    {
                        sID = temps.Substring(16*i,16);
                        if ((sID.Length) != 16)
                            return;
                        if (CardNum == 0)
                            return;
                        while (ListView_ID_6B.Items.Count < CardNum)
                        {
                            aListItem = ListView_ID_6B.Items.Add((ListView_ID_6B.Items.Count + 1).ToString());
                            aListItem.SubItems.Add("");
                            aListItem.SubItems.Add("");
                            aListItem.SubItems.Add("");
                        }
                        isonlistview = false;
                        for (j = 0; j < ListView_ID_6B.Items.Count; j++)     //判断是否在Listview列表内
                        {
                            if (sID == ListView_ID_6B.Items[j].SubItems[1].Text)
                            {
                                aListItem = ListView_ID_6B.Items[j];
                                ChangeSubItem1(aListItem, 1, sID);
                                isonlistview = true;
                            }
                        }
                        if (!isonlistview)
                        {
                            // CardNum1 = Convert.ToByte(ListView_ID_6B.Items.Count+1);
                            aListItem = ListView_ID_6B.Items[i];
                            s = sID;
                            ChangeSubItem1(aListItem, 1, s);
                            if (ComboBox_EPC1.Items.IndexOf(s) == -1)
                            {
                                ComboBox_ID1_6B.Items.Add(sID);
                            }
                        }
                    }
                    if (ComboBox_ID1_6B.Items.Count != 0)
                        ComboBox_ID1_6B.SelectedIndex = 0;
                }
            }
             if (Timer_Test_6B.Enabled)
             {
                  if (Bycondition_6B.Checked)
                  {
                    if  (fCmdRet!=0 )
                    AddCmdLog("Inventory", "Query Tag", fCmdRet);
                  }
                  else if (fCmdRet == 0XFB) //说明还未将所有卡读取完
                  {

                      StatusBar1.Panels[0].Text =  DateTime.Now.ToLongTimeString() + " “Query Tag”Command Return=0xFB" +
                           "(No electronic tag available for operation)";
                  }
                  else if (fCmdRet == 0)
                      StatusBar1.Panels[0].Text =  DateTime.Now.ToLongTimeString() + " “Query Tag”Command Return=0x00" +
                           "(One electronic tag found)";
                  else
                     AddCmdLog("Inventory", "Query Tag", fCmdRet);
                  if (fCmdRet==0xEE)
                  StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() + "Query Tag”Command Return=0xee" +
                                "(Return Command Error)" ;
             }
             if (fAppClosed)
                 Close();
        }
        private void Timer_Test_6B_Tick(object sender, EventArgs e)
        {
            if (fisinventoryscan_6B)
                return;
            fisinventoryscan_6B = true;
            Inventory_6B();
            fisinventoryscan_6B = false;
        }

        private void SpeedButton_Read_6B_Click(object sender, EventArgs e)
        {
             if (( Edit_StartAddress_6B.Text=="" )|( Edit_Len_6B.Text==""))
             {
                MessageBox.Show("The start address is empty!", "Information Prompt");
                return;
             }
             Timer_6B_Read.Enabled = !Timer_6B_Read.Enabled;
             if (!Timer_6B_Read.Enabled)
             {
                 AddCmdLog("Read", "Exit", 0);
                 SpeedButton_Read_6B.Text = "Read";
                 SpeedButton_Query_6B.Enabled = true;
                 SpeedButton_Write_6B.Enabled = true;
                 Button14.Enabled = true;
                 Button15.Enabled = true;
                 if (Bycondition_6B.Checked)
                 {
                     Same_6B.Enabled = true;
                     Different_6B.Enabled = true;
                     Less_6B.Enabled = true;
                     Greater_6B.Enabled = true;
                 }
             }
             else
             {
                 SpeedButton_Query_6B.Enabled = false ;
                 SpeedButton_Write_6B.Enabled = false ;
                 Button14.Enabled = false;
                 Button15.Enabled = false;
                 if (Bycondition_6B.Checked)
                 {
                     Same_6B.Enabled = false;
                     Different_6B.Enabled = false;
                     Less_6B.Enabled = false;
                     Greater_6B.Enabled = false;
                 }
                 SpeedButton_Read_6B.Text = "Stop";
             }
        }
        private void Read_6B()
        {
            string temp, temps;
            byte[] CardData = new byte[320];
            byte[] ID_6B = new byte[8];
            byte  Num, StartAddress;
            if (ComboBox_ID1_6B.Items.Count == 0)
                return;
            if (ComboBox_ID1_6B.SelectedItem == null)
                return;
            temp = ComboBox_ID1_6B.SelectedItem.ToString();
            if (temp == "")
                return;
            ID_6B = HexStringToByteArray(temp);
            if (Edit_StartAddress_6B.Text == "")
                return;
            StartAddress = Convert.ToByte(Edit_StartAddress_6B.Text,16);
            if (Edit_Len_6B.Text == "")
                return;
            Num = Convert.ToByte(Edit_Len_6B.Text);
            fCmdRet = StaticClassReaderB.ReadCard_6B(ref fComAdr, ID_6B, StartAddress, Num, CardData, ref ferrorcode, frmcomportindex);
            if (fCmdRet == 0)
            {
                byte[] data = new byte[Num];
                Array.Copy(CardData, data, Num);
                temps = ByteArrayToHexString(data);
                listBox2.Items.Add(temps);
            }
            if(fAppClosed )
                Close();
        }

        private void Timer_6B_Read_Tick(object sender, EventArgs e)
        {
            if (fTimer_6B_ReadWrite)
                return;
            fTimer_6B_ReadWrite = true;
            Read_6B();
            fTimer_6B_ReadWrite = false;
        }

        private void SpeedButton_Write_6B_Click(object sender, EventArgs e)
        {
            if (( Edit_WriteData_6B.Text=="" )| ((Edit_WriteData_6B.Text.Length% 2)!=0))
            {
                MessageBox.Show("Please enter hexadecimal data!", "Information Prompt");
                return;
            }
            if ((Edit_StartAddress_6B.Text == "") | (Edit_Len_6B.Text == ""))
            {
                MessageBox.Show("The start address is empty", "Information Prompt");
                return;
            }
            Timer_6B_Write.Enabled = !Timer_6B_Write.Enabled;
            if (!Timer_6B_Write.Enabled)
            {
                AddCmdLog("Wtite", "Exit", 0);
                SpeedButton_Write_6B.Text = "Write ";
            }
            else
            {
                SpeedButton_Write_6B.Text = "Stop";
            }
        }
        private void Write_6B()
        {
            string temp;
            byte[] CardData = new byte[320];
            byte[] ID_6B = new byte[8];
            byte  StartAddress;       
            byte Writedatalen;
            int writtenbyte=0;
            if (ComboBox_ID1_6B.Items.Count == 0)
                return;
            if (ComboBox_ID1_6B.SelectedItem == null)
                return;
            temp = ComboBox_ID1_6B.SelectedItem.ToString();
            if (temp == "")
                return;
            ID_6B = HexStringToByteArray(temp);
            if (Edit_StartAddress_6B.Text == "")
                return;
            StartAddress = Convert.ToByte(Edit_StartAddress_6B.Text);
            if ((Edit_WriteData_6B.Text == "") | (Edit_WriteData_6B.Text.Length%2)!=0)
                return;
            Writedatalen =Convert.ToByte(Edit_WriteData_6B.Text.Length / 2);
            byte[] Writedata = new byte[Writedatalen];
            Writedata = HexStringToByteArray(Edit_WriteData_6B.Text);
            fCmdRet=StaticClassReaderB.WriteCard_6B(ref fComAdr,ID_6B,StartAddress,Writedata,Writedatalen,ref writtenbyte,ref ferrorcode,frmcomportindex);
              AddCmdLog("WriteCard", "Write", fCmdRet);
              if (fAppClosed)
                  Close();
        }

        private void Timer_6B_Write_Tick(object sender, EventArgs e)
        {
            if (fTimer_6B_ReadWrite)
                return;
            fTimer_6B_ReadWrite = true;
            Write_6B();
            fTimer_6B_ReadWrite = false;
        }

        private void Button14_Click(object sender, EventArgs e)
        {
               byte Address;
               string temps;
               byte[] ID_6B = new byte[8];
               if (ComboBox_ID1_6B.Items.Count == 0)
                   return;
               if (ComboBox_ID1_6B.SelectedItem == null)
                   return;
               temps = ComboBox_ID1_6B.SelectedItem.ToString();
               if (temps == "")
                   return;
               ID_6B = HexStringToByteArray(temps);
               if (Edit_StartAddress_6B.Text == "")
                   return;
               Address = Convert.ToByte(Edit_StartAddress_6B.Text);
               if (MessageBox.Show(this, "Are you sure you want to permanently lock this address?", "Information Prompt", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                   return;
                fCmdRet=StaticClassReaderB.LockByte_6B(ref fComAdr,ID_6B,Address,ref ferrorcode,frmcomportindex);
                AddCmdLog("LockByte_6B", "Lock", fCmdRet);
        }

        private void Button15_Click(object sender, EventArgs e)
        {
           byte Address,ReLockState=2;
           string temps;
           byte[] ID_6B = new byte[8];
           if (ComboBox_ID1_6B.Items.Count == 0)
               return;
           if (ComboBox_ID1_6B.SelectedItem == null)
               return;
           temps = ComboBox_ID1_6B.SelectedItem.ToString();
           if (temps == "")
               return;
           ID_6B = HexStringToByteArray(temps);
           if (Edit_StartAddress_6B.Text == "")
               return;
           Address = Convert.ToByte(Edit_StartAddress_6B.Text);
           fCmdRet=StaticClassReaderB.CheckLock_6B(ref fComAdr,ID_6B,Address,ref ReLockState,ref ferrorcode,frmcomportindex);
           AddCmdLog("CheckLock_6B", "Check Lock Status", fCmdRet);
           if (fCmdRet==0)
           {
               if  (ReLockState==0)
               StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() +  " “Check Lock Status”Command Return=0x00" +
                         "(This byte is not locked)";
               if  (ReLockState==1)
               StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() +  "  “Check Lock Status”Command Return=0x01" +
                       "(This byte is already locked)";

           }
        }

        private void Button22_Click(object sender, EventArgs e)
        {
            listBox2.Items.Clear();
        }

        private void P_Reserve_CheckedChanged(object sender, EventArgs e)
        {
            if (ListView1_EPC.Items.Count != 0)
            {
                DestroyCode.Enabled = true;
                AccessCode.Enabled = true;
                NoProect.Enabled = true;
                Proect.Enabled = true;
                Always.Enabled = true;
                AlwaysNot.Enabled = true;
                NoProect2.Enabled = false;
                Proect2.Enabled = false;
                Always2.Enabled = false;
                AlwaysNot2.Enabled = false;
            }
        }

        private void P_EPC_CheckedChanged(object sender, EventArgs e)
        {
            if (ListView1_EPC.Items.Count != 0)
            {
                DestroyCode.Enabled = false;
                AccessCode.Enabled = false;
                NoProect.Enabled = false;
                Proect.Enabled = false;
                Always.Enabled = false;
                AlwaysNot.Enabled = false;
                NoProect2.Enabled = true;
                Proect2.Enabled = true;
                Always2.Enabled = true;
                AlwaysNot2.Enabled = true;
            }
        }

        private void P_TID_CheckedChanged(object sender, EventArgs e)
        {
            if (ListView1_EPC.Items.Count != 0)
            {
                DestroyCode.Enabled = false;
                AccessCode.Enabled = false;
                NoProect.Enabled = false;
                Proect.Enabled = false;
                Always.Enabled = false;
                AlwaysNot.Enabled = false;
                NoProect2.Enabled = true;
                Proect2.Enabled = true;
                Always2.Enabled = true;
                AlwaysNot2.Enabled = true;
            }
        }

        private void P_User_CheckedChanged(object sender, EventArgs e)
        {
            if (ListView1_EPC.Items.Count!=0)
            {
                DestroyCode.Enabled = false;
                AccessCode.Enabled = false;
                NoProect.Enabled = false;
                Proect.Enabled = false;
                Always.Enabled = false;
                AlwaysNot.Enabled = false;
                NoProect2.Enabled = true;
                Proect2.Enabled = true;
                Always2.Enabled = true;
                AlwaysNot2.Enabled = true;
            }
        }

        private void Byone_6B_CheckedChanged(object sender, EventArgs e)
        {
            if ((!Timer_6B_Read.Enabled) & (!Timer_6B_Write.Enabled) & (!Timer_Test_6B.Enabled))
            {
                Same_6B.Enabled = false;
                Different_6B.Enabled = false;
                Less_6B.Enabled = false;
                Greater_6B.Enabled = false;
            }
        }

        private void Bycondition_6B_CheckedChanged(object sender, EventArgs e)
        {
            if ((!Timer_6B_Read.Enabled) &(!Timer_6B_Write.Enabled)&(!Timer_Test_6B.Enabled))
            {
                Same_6B.Enabled = true;
                Different_6B.Enabled = true;
                Less_6B.Enabled = true;
                Greater_6B.Enabled = true;
            }
        }

        private void C_EPC_CheckedChanged(object sender, EventArgs e)
        {
            if ((!Timer_Test_.Enabled) & (!Timer_G2_Alarm.Enabled) &(!Timer_G2_Read.Enabled))
            {
            //  Button_DataWrite.Enabled = false;
            }
            if (checkBox_pc.Checked)
            {
                Edit_WordPtr.Text = "02";
                Edit_WordPtr.ReadOnly = true;
            }
            else
            {
                Edit_WordPtr.ReadOnly = false;
            }
        }

        private void C_TID_CheckedChanged(object sender, EventArgs e)
        {
            if ((!Timer_Test_.Enabled) & (!Timer_G2_Alarm.Enabled) &(!Timer_G2_Read.Enabled))
            {
                if (ListView1_EPC.Items.Count != 0)
                    Button_DataWrite.Enabled = true;
            }
            Edit_WordPtr.ReadOnly = false;
        }

        private void C_User_CheckedChanged(object sender, EventArgs e)
        {
            if ((!Timer_Test_.Enabled) & (!Timer_G2_Alarm.Enabled) & (!Timer_G2_Read.Enabled))
            {
                if (ListView1_EPC.Items.Count != 0)
                    Button_DataWrite.Enabled = true;
            }
            Edit_WordPtr.ReadOnly = false;
        }

        private void C_Reserve_CheckedChanged(object sender, EventArgs e)
        {
            if ((!Timer_Test_.Enabled) & (!Timer_G2_Alarm.Enabled) &(!Timer_G2_Read.Enabled))
            {
                if (ListView1_EPC.Items.Count != 0)
                    Button_DataWrite.Enabled = true;
            }
            Edit_WordPtr.ReadOnly = false;
        }

        private void tabControl1_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (mythread != null || mthread!=null)
            {
                e.Cancel = true;
                MessageBox.Show("Please stop the worker thread first");
                return;
            }
                timer1.Enabled = false;
                button10.Text = "Get";

                Timer_G2_Alarm.Enabled = false;
                Timer_G2_Read.Enabled = false;
                Timer_Test_.Enabled = false;
                SpeedButton_Read_G2.Text = "Read";
                button2.Text = "Query Tag";
                if ((ListView1_EPC.Items.Count != 0)&&(ComOpen))
                {
                    button2.Enabled = true;
                    DestroyCode.Enabled = false;
                    AccessCode.Enabled = false;
                    NoProect.Enabled = false;
                    Proect.Enabled = false;
                    Always.Enabled = false;
                    AlwaysNot.Enabled = false;
                    NoProect2.Enabled = true;
                    Proect2.Enabled = true;
                    Always2.Enabled = true;
                    AlwaysNot2.Enabled = true;
                    P_Reserve.Enabled = true;
                    P_EPC.Enabled = true;
                    P_TID.Enabled = true;
                    P_User.Enabled = true;
                    Button_DestroyCard.Enabled = true;
                    Button_WriteEPC_G2.Enabled = true;
                    SpeedButton_Read_G2.Enabled = true;
                    Button_SetProtectState.Enabled = true;
                  //  if (C_EPC.Checked)
                  //      Button_DataWrite.Enabled = false;
                  //  else
                        Button_DataWrite.Enabled = true;
                        BlockWrite.Enabled = true;
                    Button_BlockErase.Enabled = true;
                    checkBox1.Enabled = true;
                }
                if ((ListView1_EPC.Items.Count == 0)&&(ComOpen))
                {
                    button2.Enabled = true;
                    DestroyCode.Enabled = false;
                    AccessCode.Enabled = false;
                    NoProect.Enabled = false;
                    Proect.Enabled = false;
                    Always.Enabled = false;
                    AlwaysNot.Enabled = false;
                    NoProect2.Enabled = false;
                    Proect2.Enabled = false;
                    Always2.Enabled = false;
                    AlwaysNot2.Enabled = false;
                    P_Reserve.Enabled = false;
                    P_EPC.Enabled = false;
                    P_TID.Enabled = false;
                    P_User.Enabled = false;
                    Button_DestroyCard.Enabled = false;
                    SpeedButton_Read_G2.Enabled = false;
                    Button_DataWrite.Enabled = false;
                    BlockWrite.Enabled = false;
                    Button_BlockErase.Enabled = false;
                    Button_WriteEPC_G2.Enabled = true;
                    Button_SetProtectState.Enabled = false;
                    checkBox1.Enabled = false;
                }

                Timer_Test_6B.Enabled = false;
                Timer_6B_Read.Enabled = false;
                Timer_6B_Write.Enabled = false;
                SpeedButton_Query_6B.Text = "Single Card Query";
                SpeedButton_Read_6B.Text = "Read";
                SpeedButton_Write_6B.Text ="Write";
                if ((ListView_ID_6B.Items.Count != 0)&&(ComOpen))
                {
                    SpeedButton_Query_6B.Enabled = true;
                    SpeedButton_Read_6B.Enabled = true;
                    SpeedButton_Write_6B.Enabled = true;
                    Button14.Enabled = true;
                    Button15.Enabled = true;
                    if (Bycondition_6B.Checked)
                    {
                        Same_6B.Enabled = true;
                        Different_6B.Enabled = true;
                        Less_6B.Enabled = true;
                        Greater_6B.Enabled = true;
                    }
                }
                if ((ListView_ID_6B.Items.Count == 0)&&(ComOpen))
                {
                    SpeedButton_Query_6B.Enabled = true;
                    SpeedButton_Read_6B.Enabled = false;
                    SpeedButton_Write_6B.Enabled = false;
                    Button14.Enabled = false;
                    Button15.Enabled = false;
                    if (Bycondition_6B.Checked)
                    {
                        Same_6B.Enabled = true;
                        Different_6B.Enabled = true;
                        Less_6B.Enabled = true;
                        Greater_6B.Enabled = true;
                    }
                }
            
        }

        private void Edit_CmdComAddr_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = ("0123456789ABCDEF".IndexOf(Char.ToUpper(e.KeyChar)) < 0);
        }

        private void Edit_Len_6B_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = ("0123456789".IndexOf(Char.ToUpper(e.KeyChar)) < 0);
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox4.SelectedIndex == 0)
            {
                radioButton5.Enabled = false;
                radioButton7.Enabled = false;
                radioButton8.Enabled = false;
                radioButton9.Enabled = false;
                radioButton10.Enabled = false;
                radioButton11.Enabled = false;
                radioButton12.Enabled = false;
                radioButton13.Enabled = false;
                radioButton14.Enabled = false;
                radioButton15.Enabled = false;
                radioButton16.Enabled = false;
                radioButton17.Enabled = false;
                radioButton18.Enabled = false;
                radioButton19.Enabled = false;
                textBox3.Enabled = false;
                comboBox5.Enabled = false;
                comboBox6.Enabled = false;
            }
            if ((comboBox4.SelectedIndex == 1) | (comboBox4.SelectedIndex == 2) | (comboBox4.SelectedIndex == 3))
            {
                radioButton5.Enabled = true;
                radioButton7.Enabled = true;
                radioButton8.Enabled = true;
                comboBox5.Items.Clear();
                for (int i = 1; i < 33; i++)
                    comboBox5.Items.Add(Convert.ToString(i));
                comboBox5.SelectedIndex = 0;
                label42.Text = "Number of Words to Read:";

                if (radioButton7.Checked)
                {
                    radioButton16.Enabled = true;
                    radioButton17.Enabled = true;
                }
                else
                {
                    radioButton16.Enabled = false;
                    radioButton17.Enabled = false;
                }
                if (radioButton5.Checked)
                {
                    radioButton9.Enabled = true;
                    radioButton10.Enabled = true;
                    radioButton11.Enabled = true;
                    radioButton12.Enabled = true;
                    radioButton18.Enabled = true;
                    radioButton19.Enabled = true;
                    radioButton13.Enabled = true;
                    if ((radioButton13.Checked))
                        comboBox6.Enabled = false;
                    else
                        comboBox6.Enabled = true;
                }
                else
                    comboBox6.Enabled = true;
                radioButton14.Enabled = true;
                radioButton15.Enabled = true;
                textBox3.Enabled = true;
                if (radioButton7.Checked)
                    comboBox5.Enabled = false;
                else
                    comboBox5.Enabled =true;
            }
            
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton5.Checked)
            {
                if ((comboBox4.SelectedIndex >0))
                {
                    radioButton9.Enabled = true;
                    radioButton10.Enabled = true;
                    radioButton11.Enabled = true;
                    radioButton12.Enabled = true;
                    radioButton13.Enabled = true;
                    radioButton18.Enabled = true;
                    radioButton19.Enabled = true;
                    if (radioButton16.Checked)
                        label41.Text = "Start Word Address (Hex):";
                    else
                        label41.Text = "Start Byte Address (Hex):";
                    radioButton13.Enabled=true;
                    if (radioButton7.Checked)
                    {
                        radioButton16.Enabled = true;
                        radioButton17.Enabled = true;
                        if (radioButton13.Checked)
                        {
                            comboBox6.Enabled = false;
                        }
                        else
                        {
                            comboBox6.Enabled = true;
                        }
                       
                    }
                    else
                    {
                        radioButton16.Enabled = false;
                        radioButton17.Enabled = false;
                        if (radioButton13.Checked) 
                            comboBox6.Enabled = false;
                        else
                            comboBox6.Enabled = true;
                   
                          label41.Text= "Start Word Address (Hex):";
                    }
                }
            }
            else
            {
                radioButton9.Enabled = false;
                radioButton10.Enabled = false;
                radioButton11.Enabled = false;
                radioButton12.Enabled = false;
                radioButton13.Enabled = false;
                radioButton18.Enabled = false;
                radioButton19.Enabled = false;
                radioButton16.Enabled = false;
                radioButton17.Enabled = false;
                comboBox6.Enabled = true;
                label41.Text = "Start Byte Address(Hex)";
            }

        }

        private void radioButton7_CheckedChanged(object sender, EventArgs e)
        {
              if((radioButton5.Checked)&&(comboBox4.SelectedIndex>0))
              {
                radioButton16.Enabled=true;
                radioButton17.Enabled=true;
                radioButton13.Enabled=true;
                if(radioButton16.Checked)
                label41.Text= "Start Word Address (Hex):";
                else
                label41.Text= "Start Byte Address (Hex):";
                label42.Text="Number of Words to Read:";
              }
               comboBox5.Enabled=false;
        }

        private void radioButton8_CheckedChanged(object sender, EventArgs e)
        {
             if(comboBox4.SelectedIndex>0)
             {
                  if(radioButton8.Checked)
                    comboBox5.Enabled=true;
                   comboBox5.Items.Clear();
                      for (int i=1;i<33;i++)
                      comboBox5.Items.Add(Convert.ToString(i));
                      comboBox5.SelectedIndex=0;
                      label42.Text="Number of Words to Read:";
                      label41.Text= "Start Word Address (Hex):";
                if(radioButton5.Checked)
                {
                   radioButton16.Enabled=false;
                   radioButton17.Enabled=false;
                   radioButton13.Enabled=true;
                }
                else
                {
                  label41.Text= "Start Byte Address (Hex):";
                  radioButton13.Enabled=false;
                }


             }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            byte Wg_mode=0;
            byte Wg_Data_Inteval;
            byte Wg_Pulse_Width;
            byte Wg_Pulse_Inteval;
            if(radioButton1.Checked)
            {
            if(radioButton3.Checked)
            Wg_mode=2;
            else
            Wg_mode= 0;
            }
            if(radioButton2.Checked)
            {
            if(radioButton3.Checked) 
            Wg_mode=3;
            else
            Wg_mode= 1;
            }
            Wg_Data_Inteval=Convert.ToByte(comboBox1.SelectedIndex);
            Wg_Pulse_Width=Convert.ToByte(comboBox3.SelectedIndex+1);
            Wg_Pulse_Inteval = Convert.ToByte(comboBox2.SelectedIndex + 1);
            fCmdRet = StaticClassReaderB.SetWGParameter(ref fComAdr, Wg_mode, Wg_Data_Inteval, Wg_Pulse_Width, Wg_Pulse_Inteval,frmcomportindex);
            AddCmdLog("SetWGParameter", "Wiegand Settings", fCmdRet);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            int Reader_bit0;
            int Reader_bit1;
            int Reader_bit2;
            int Reader_bit3;
            byte[] Parameter = new byte[6];
            Parameter[0] = Convert.ToByte(comboBox4.SelectedIndex);
            if (radioButton5.Checked)
                Reader_bit0 = 0;
            else
                Reader_bit0 = 0;
            if (radioButton7.Checked)
                Reader_bit1 = 0;
            else
                Reader_bit1 = 1;
            if (radioButton14.Checked)
                Reader_bit2 = 0;
            else
                Reader_bit2 = 1;
            if (radioButton16.Checked)
                Reader_bit3 = 0;
            else
                Reader_bit3 = 1;
            
            Parameter[1] = Convert.ToByte(Reader_bit0 * 1 + Reader_bit1 * 2 + Reader_bit2 * 4 + Reader_bit3 * 8);
            if (radioButton9.Checked)
                Parameter[2] = 0;
            if (radioButton10.Checked)
                Parameter[2] = 1;
            if (radioButton11.Checked)
                Parameter[2] = 2;
            if (radioButton12.Checked)
                Parameter[2] = 3;
            if (radioButton13.Checked)
                Parameter[2] = 4;
            if (radioButton18.Checked)
                Parameter[2] = 5;
            if (radioButton19.Checked)
                Parameter[2] = 6;
            if (textBox3.Text == "")
            {
                MessageBox.Show("Address cannot be empty!", "Prompt");
                return;
            }
            Parameter[3] = Convert.ToByte(textBox3.Text, 16);
            Parameter[4] = Convert.ToByte(comboBox5.SelectedIndex + 1);
            Parameter[5] = Convert.ToByte(comboBox6.SelectedIndex); ;
            fCmdRet = StaticClassReaderB.SetWorkMode(ref fComAdr, Parameter, frmcomportindex);
            if (fCmdRet == 0)
            {
                if ((comboBox4.SelectedIndex == 1) | (comboBox4.SelectedIndex == 2) | (comboBox4.SelectedIndex == 3))
                {

                    button10.Enabled = true;
                    button11.Enabled = true;
                }
                if (comboBox4.SelectedIndex == 0)
                {
                    button10.Enabled = false;
                    button11.Enabled = false;
                    button10.Text = "Get";
                    timer1.Enabled = false;
                }
            }
            AddCmdLog("SetWorkMode", "Set", fCmdRet);
        }


        private void button10_Click(object sender, EventArgs e)
        {
            timer1.Enabled = !timer1.Enabled;
            if (!timer1.Enabled)
            {
                button10.Text = "Get";
            }
            else
            {
                button10.Text = "Stop";
            }
        }
        private void GetData()
        {
            byte[] ScanModeData = new byte[40960];
          int ValidDatalength,i;
          string temp, temps;
          ValidDatalength = 0;
          fCmdRet = StaticClassReaderB.ReadActiveModeData(ScanModeData, ref ValidDatalength, frmcomportindex);
          if (fCmdRet == 0)
          { 
            temp="";
            temps=ByteArrayToHexString(ScanModeData);
            for(i=0;i<ValidDatalength;i++)
            {
                temp = temp + temps.Substring(i * 2, 2) + " ";
            }
            listBox3.Items.Add(temp);
            listBox3.SelectedIndex = listBox3.Items.Count-1;
          }
         // AddCmdLog("Get", "Get", fCmdRet);
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (fIsInventoryScan)
                fIsInventoryScan = true;
            GetData();
            if (fAppClosed)
                Close();
            fIsInventoryScan = false;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            listBox3.Items.Clear();
        }

        private void radioButton_band1_CheckedChanged(object sender, EventArgs e)
        {
            int i;
            ComboBox_dmaxfre.Items.Clear();
            ComboBox_dminfre.Items.Clear();
            for (i = 0; i < 63; i++)
            {
                ComboBox_dminfre.Items.Add(Convert.ToString(902.6 + i * 0.4) + " MHz");
                ComboBox_dmaxfre.Items.Add(Convert.ToString(902.6 + i * 0.4) + " MHz");
            }
            ComboBox_dmaxfre.SelectedIndex = 62;
            ComboBox_dminfre.SelectedIndex = 0;
        }

        private void radioButton_band2_CheckedChanged(object sender, EventArgs e)
        {
            int i;
            ComboBox_dmaxfre.Items.Clear();
            ComboBox_dminfre.Items.Clear();
            for (i = 0; i < 20; i++)
            {
                ComboBox_dminfre.Items.Add(Convert.ToString(920.125 + i * 0.25) + " MHz");
                ComboBox_dmaxfre.Items.Add(Convert.ToString(920.125 + i * 0.25) + " MHz");
            }
            ComboBox_dmaxfre.SelectedIndex = 19;
            ComboBox_dminfre.SelectedIndex = 0;
        }

        private void radioButton_band3_CheckedChanged(object sender, EventArgs e)
        {
            int i;
            ComboBox_dmaxfre.Items.Clear();
            ComboBox_dminfre.Items.Clear();
            for (i = 0; i < 50; i++)
            {
                ComboBox_dminfre.Items.Add(Convert.ToString(902.75 + i * 0.5) + " MHz");
                ComboBox_dmaxfre.Items.Add(Convert.ToString(902.75 + i * 0.5) + " MHz");
            }
            ComboBox_dmaxfre.SelectedIndex = 49;
            ComboBox_dminfre.SelectedIndex = 0;
        }

        private void radioButton_band4_CheckedChanged(object sender, EventArgs e)
        {
            int i;
            ComboBox_dmaxfre.Items.Clear();
            ComboBox_dminfre.Items.Clear();
            for (i = 0; i < 32; i++)
            {
                ComboBox_dminfre.Items.Add(Convert.ToString(917.1 + i * 0.2) + " MHz");
                ComboBox_dmaxfre.Items.Add(Convert.ToString(917.1 + i * 0.2) + " MHz");
            }
            ComboBox_dmaxfre.SelectedIndex = 31;
            ComboBox_dminfre.SelectedIndex = 0;
        }



        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                maskadr_textbox.Enabled = true;
                maskLen_textBox.Enabled = true;
            }
            else
            {
                maskadr_textbox.Enabled = false;
                maskLen_textBox.Enabled = false;
            }
        }

        private void groupBox30_Enter(object sender, EventArgs e)
        {

        }

        private void radioButton16_CheckedChanged(object sender, EventArgs e)
        {
            label41.Text = "Start Word Address(Hex):";
        }

        private void radioButton17_CheckedChanged(object sender, EventArgs e)
        {
            label41.Text = "Start Byte Address(Hex):";
        }

        private void radioButton9_CheckedChanged(object sender, EventArgs e)
        {
            comboBox6.Enabled = true;
        }

        private void radioButton10_CheckedChanged(object sender, EventArgs e)
        {
            comboBox6.Enabled = true;
        }

        private void radioButton11_CheckedChanged(object sender, EventArgs e)
        {
            comboBox6.Enabled = true;
        }

        private void radioButton12_CheckedChanged(object sender, EventArgs e)
        {
            comboBox6.Enabled = true;
        }

        private void radioButton18_CheckedChanged(object sender, EventArgs e)
        {
            comboBox6.Enabled = true;
        }

        private void radioButton13_CheckedChanged(object sender, EventArgs e)
        {
            comboBox6.Enabled = true;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            byte[] Parameter = new byte[12];

            fCmdRet = StaticClassReaderB.GetWorkModeParameter(ref fComAdr, Parameter, frmcomportindex);
            if (fCmdRet == 0)
            {
                if (Parameter[0] == 0)
                {
                    radioButton1.Checked = true;
                    radioButton4.Checked = true;
                }
                if (Parameter[0] == 1)
                {
                    radioButton2.Checked = true;
                    radioButton4.Checked = true;
                }
                if (Parameter[0] == 2)
                {
                    radioButton1.Checked = true;
                    radioButton3.Checked = true;
                }
                if (Parameter[0] == 3)
                {
                    radioButton2.Checked = true;
                    radioButton3.Checked = true;
                }
                comboBox1.SelectedIndex = Convert.ToInt32(Parameter[1]);
                comboBox2.SelectedIndex = Convert.ToInt32(Parameter[3] - 1);
                comboBox3.SelectedIndex = Convert.ToInt32(Parameter[2] - 1);
                comboBox4.SelectedIndex = Convert.ToInt32(Parameter[4]);
                if (Parameter[4] >0) 
                {
                    button10.Enabled = true;
                    button11.Enabled = true;
                    radioButton5.Enabled = true;
                    radioButton7.Enabled = true;
                    radioButton8.Enabled = true;
                   
                    if (radioButton5.Checked)
                    {
                        if (radioButton7.Checked)
                        {
                            radioButton16.Enabled = true;
                            radioButton17.Enabled = true;
                        }
                        else
                        {
                            radioButton16.Enabled = false;
                            radioButton17.Enabled = false;
                        }
                        radioButton9.Enabled = true;
                        radioButton10.Enabled = true;
                        radioButton11.Enabled = true;
                        radioButton12.Enabled = true;
                        radioButton18.Enabled = true;
                        radioButton19.Enabled = true;
                        if (Convert.ToInt32((Parameter[5] & 0x10)) == 0x10) 
                        {
                          radioButton13.Enabled =false;
                        }
                         else
                        {
                            radioButton13.Enabled = true;
                        }
                        comboBox6.Enabled = true;

                    }
                    else
                        comboBox6.Enabled = true;
                    radioButton14.Enabled = true;
                    radioButton15.Enabled = true;
                    textBox3.Enabled = true;
                    if (radioButton8.Checked)
                        comboBox5.Enabled = true;
                }
                if (Parameter[4] == 0)
                {
                    button10.Enabled = false;
                    button11.Enabled = false;
                    radioButton5.Enabled = false;
                    radioButton7.Enabled = false;
                    radioButton8.Enabled = false;
                    radioButton9.Enabled = false;
                    radioButton10.Enabled = false;
                    radioButton11.Enabled = false;
                    radioButton12.Enabled = false;
                    radioButton13.Enabled = false;
                    radioButton14.Enabled = false;
                    radioButton15.Enabled = false;
                    radioButton16.Enabled = false;
                    radioButton17.Enabled = false;
                    radioButton18.Enabled = false;
                    radioButton19.Enabled = false;
                    textBox3.Enabled = false;
                    comboBox5.Enabled = false;
                    comboBox6.Enabled = false;
                }
                if (Convert.ToInt32((Parameter[5]) & 0x01) == 0)
                    radioButton5.Checked = true;

                if (Convert.ToInt32((Parameter[5]) & 0x02) == 0)
                    radioButton7.Checked = true;
                else
                {
                    if (Convert.ToInt32((Parameter[5] & 0x10)) == 0) 
                    radioButton8.Checked=true;
                }
                if (Convert.ToInt32((Parameter[5]) & 0x04) == 0)
                    radioButton14.Checked = true;
                else
                    radioButton15.Checked = true;
                if (Convert.ToInt32((Parameter[5]) & 0x08) == 0)
                    radioButton16.Checked = true;
                else
                    radioButton17.Checked = true;
                switch (Parameter[6])
                {
                    case 0:
                        radioButton9.Checked = true;
                        break;
                    case 1:
                        radioButton10.Checked = true;
                        break;
                    case 2:
                        radioButton11.Checked = true;
                        break;
                    case 3:
                        radioButton12.Checked = true;
                        break;
                    case 4:
                        radioButton13.Checked = true;
                        break;
                    case 5:
                        radioButton18.Checked = true;
                        break;
                    case 6:
                        radioButton19.Checked = true;
                        break;
                    default:
                        break;
                }
                textBox3.Text = Convert.ToString(Parameter[7], 16).PadLeft(2, '0');
                comboBox5.SelectedIndex = Convert.ToInt32(Parameter[8] - 1);
                comboBox6.SelectedIndex = Convert.ToInt32(Parameter[9]);
            }
            AddCmdLog("GetWorkModeParameter", "Get Working Mode Parameters", fCmdRet);
        }

        private void radioButton19_CheckedChanged(object sender, EventArgs e)
        {
            comboBox6.Enabled = false;
        }



        private void ComboBox_COM_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox_baud2.Items.Clear();
              if(ComboBox_COM.SelectedIndex==0)
              {
              ComboBox_baud2.Items.Add("9600bps");
              ComboBox_baud2.Items.Add("19200bps");
              ComboBox_baud2.Items.Add("38400bps");
              ComboBox_baud2.Items.Add("57600bps");
              ComboBox_baud2.Items.Add("115200bps");
              ComboBox_baud2.SelectedIndex=3;
              }
            else
              {
              ComboBox_baud2.Items.Add("Auto");
              ComboBox_baud2.SelectedIndex=0;
              }
        }


        private void BlockWrite_Click(object sender, EventArgs e)
        {
            byte WordPtr, ENum;
            byte Num = 0;
            byte Mem = 0;
            byte WNum = 0;
            byte EPClength = 0;
            byte Writedatalen = 0;
            int WrittenDataNum = 0;
            string s2, str;
            byte[] CardData = new byte[320];
            byte[] writedata = new byte[230];
            if ((maskadr_textbox.Text == "") || (maskLen_textBox.Text == ""))
            {
                fIsInventoryScan = false;
                return;
            }
            if (checkBox1.Checked)
                MaskFlag = 1;
            else
                MaskFlag = 0;
            Maskadr = Convert.ToByte(maskadr_textbox.Text,16);
            MaskLen = Convert.ToByte(maskLen_textBox.Text,16);
            if (ComboBox_EPC2.Items.Count == 0)
                return;
            if (ComboBox_EPC2.SelectedItem == null)
                return;
            str = ComboBox_EPC2.SelectedItem.ToString();
            if (str == "")
                return;
            ENum = Convert.ToByte(str.Length / 4);
            EPClength = Convert.ToByte(ENum * 2);
            byte[] EPC = new byte[ENum];
            EPC = HexStringToByteArray(str);
            if (C_Reserve.Checked)
                Mem = 0;
            if (C_EPC.Checked)
                Mem = 1;
            if (C_TID.Checked)
                Mem = 2;
            if (C_User.Checked)
                Mem = 3;
            if (Edit_WordPtr.Text == "")
            {
                MessageBox.Show("The start address is empty", "Information Prompt");
                return;
            }
            if (textBox1.Text == "")
            {
                MessageBox.Show("Read/Block Erase Length", "Information Prompt");
                return;
            }
            if (Convert.ToInt32(Edit_WordPtr.Text,16) + Convert.ToInt32(textBox1.Text) > 120)
                return;
            if (Edit_AccessCode2.Text == "")
            {
                return;
            }
            WordPtr = Convert.ToByte(Edit_WordPtr.Text, 16);
            Num = Convert.ToByte(textBox1.Text);
            if (Edit_AccessCode2.Text.Length != 8)
            {
                return;
            }
            fPassWord = HexStringToByteArray(Edit_AccessCode2.Text);
            if (Edit_WriteData.Text == "")
                return;
            s2 = Edit_WriteData.Text;
            if (s2.Length % 4 != 0)
            {
                MessageBox.Show("以字为单位输入.", "Block Write");
                return;
            }
            WNum = Convert.ToByte(s2.Length / 4);
            byte[] Writedata = new byte[WNum * 2];
            Writedata = HexStringToByteArray(s2);
            Writedatalen = Convert.ToByte(WNum * 2);
            if ((checkBox_pc.Checked) && (C_EPC.Checked))
            {
                WordPtr = 1;
                Writedatalen = Convert.ToByte(Edit_WriteData.Text.Length / 2 + 2);
                Writedata = HexStringToByteArray(textBox_pc.Text + Edit_WriteData.Text);
            }
            fCmdRet = StaticClassReaderB.WriteBlock_G2(ref fComAdr, EPC, Mem, WordPtr, Writedatalen, Writedata, fPassWord, Maskadr, MaskLen, MaskFlag, WrittenDataNum, EPClength, ref ferrorcode, frmcomportindex);
            AddCmdLog("Write Block", "Block Write", fCmdRet, ferrorcode);
            if (fCmdRet == 0)
            {
                StatusBar1.Panels[0].Text = DateTime.Now.ToLongTimeString() + "'Block Write'Commad Return=0x00" +
                     "(Block Write Successful)";
            }    
        }
        
        private void button18_Click(object sender, EventArgs e)
        {

        }

        private void button19_Click(object sender, EventArgs e)
        {

        }

        private void radioButton21_CheckedChanged(object sender, EventArgs e)
        {
             int i;
             ComboBox_dminfre.Items.Clear();
             ComboBox_dmaxfre.Items.Clear();
             for (i=0;i<15;i++)
             {
                 ComboBox_dminfre.Items.Add(Convert.ToString(865.1 + i * 0.2) + " MHz");
                 ComboBox_dmaxfre.Items.Add(Convert.ToString(865.1 + i * 0.2) + " MHz");
             }
             ComboBox_dmaxfre.SelectedIndex = 14;
             ComboBox_dminfre.SelectedIndex=0;
        }

        private void checkBox_pc_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_pc.Checked)
            {
                Edit_WordPtr.Text = "02";
                Edit_WordPtr.ReadOnly = true;
                int m, n;
                n = Edit_WriteData.Text.Length;
                if ((checkBox_pc.Checked) && (n % 4 == 0) && (C_EPC.Checked))
                {
                    m = n / 4;
                    m = (m & 0x3F) << 3;
                    textBox_pc.Text = Convert.ToString(m, 16).PadLeft(2, '0') + "00";
                }
            }
            else
            {
                Edit_WordPtr.ReadOnly = false;
            }
        }

        private void Edit_WriteData_TextChanged(object sender, EventArgs e)
        {
            int m,n;
            n= Edit_WriteData.Text.Length;
            if((checkBox_pc.Checked)&&(n % 4==0)&&(C_EPC.Checked))
            {
                m = n / 4;
                m = (m & 0x3F) << 3;
                textBox_pc.Text = Convert.ToString(m, 16).PadLeft(2, '0') + "00";
            }
        }

        private void CheckBox_TID_CheckedChanged(object sender, EventArgs e)
        {
           if (CheckBox_TID.Checked) 
            {
                 groupBox33.Enabled = true;
                 textBox4.Enabled = true;
                 textBox4.Enabled = true;
            }
            else      
            {
                groupBox33.Enabled = false;
                textBox4.Enabled = false;
                textBox4.Enabled = false;
            }
        }

        private void button20_Click(object sender, EventArgs e)
        {
            byte RelayStatus = 0;
            if (com_relay_num.SelectedIndex == 0)
                RelayStatus = Convert.ToByte(RelayStatus | 0);
            else
                RelayStatus = Convert.ToByte(RelayStatus | 1);
            if (com_relay_state.SelectedIndex == 0)
                RelayStatus = Convert.ToByte(RelayStatus | 0);
            else
                RelayStatus = Convert.ToByte(RelayStatus | 2);
            fCmdRet = StaticClassReaderB.SetRelay(ref fComAdr, RelayStatus, frmcomportindex);
            AddCmdLog("SetRelay", "Set", fCmdRet);
        }

      
     
        private void OpenNetPort_Click(object sender, EventArgs e)
        {
            int port, openresult = 0;
            string IPAddr;
            if (textBox9.Text == "")
                Edit_CmdComAddr.Text = "FF";
            fComAdr = Convert.ToByte(textBox9.Text, 16); // $FF;
            if ((textBox7.Text == "") || (textBox8.Text == ""))
                MessageBox.Show("Network Port，IP Cannot be empty !", "Prompt");
            port = Convert.ToInt32(textBox7.Text);
            IPAddr = textBox8.Text;
            openresult = StaticClassReaderB.OpenNetPort(port, IPAddr, ref fComAdr, ref frmcomportindex);
            fOpenComIndex = frmcomportindex;
            if (openresult == 0)
            {
                ComOpen = true;
                Button3_Click(sender, e); //自动执行读取写卡器Information

            }
            if ((openresult == 0x35) || (openresult == 0x30))
            {
                MessageBox.Show("TCPIPCommunitation Error", "Information");
                StaticClassReaderB.CloseNetPort(frmcomportindex);
                ComOpen = false;
                return;
            }
            if ((fOpenComIndex != -1) && (openresult != 0X35) && (openresult != 0X30))
            {
                StaticClassReaderB.InitRFIDCallBack(elegateRFIDCallBack, false, frmcomportindex);
                Button3.Enabled = true;
                button20.Enabled = true;
                Button5.Enabled = true;
                Button1.Enabled = true;
                button2.Enabled = true;
                Button_WriteEPC_G2.Enabled = true;
                SpeedButton_Query_6B.Enabled = true;
                button6.Enabled = true;
                button8.Enabled = true;
                button9.Enabled = true;
                button4.Enabled = true;
                button12.Enabled = true;
                btGetSerial.Enabled = true;
                panel1.Enabled = true;
                panel2.Enabled = true;
                panel3.Enabled = true;
                panel4.Enabled = true;
                panel5.Enabled = true;
                ComOpen = true;
                button_settigtime.Enabled = true;
                button_gettigtime.Enabled = true;
            }
            if ((fOpenComIndex == -1) && (openresult == 0x30))
                MessageBox.Show("TCPIP Communitation Error", "Information");
            RefreshStatus();
        }

        private void CloseNetPort_Click(object sender, EventArgs e)
        {
            ClearLastInfo();
            fCmdRet = StaticClassReaderB.CloseNetPort(frmcomportindex);
            if (fCmdRet == 0)
            {
                fOpenComIndex = -1;
                RefreshStatus();
                Button3.Enabled = false;
                button20.Enabled = false;
                Button5.Enabled = false;
                Button1.Enabled = false;
                button2.Enabled = false;
                Button_DestroyCard.Enabled = false;
                Button_WriteEPC_G2.Enabled = false;
                SpeedButton_Read_G2.Enabled = false;
                Button_DataWrite.Enabled = false;
                BlockWrite.Enabled = false;
                Button_BlockErase.Enabled = false;
                Button_SetProtectState.Enabled = false;
                SpeedButton_Query_6B.Enabled = false;
                SpeedButton_Read_6B.Enabled = false;
                SpeedButton_Write_6B.Enabled = false;
                Button14.Enabled = false;
                Button15.Enabled = false;
                btGetSerial.Enabled = false;

                DestroyCode.Enabled = false;
                AccessCode.Enabled = false;
                NoProect.Enabled = false;
                Proect.Enabled = false;
                Always.Enabled = false;
                AlwaysNot.Enabled = false;
                NoProect2.Enabled = false;
                Proect2.Enabled = false;
                Always2.Enabled = false;
                AlwaysNot2.Enabled = false;

                P_Reserve.Enabled = false;
                P_EPC.Enabled = false;
                P_TID.Enabled = false;
                P_User.Enabled = false;
                
                Same_6B.Enabled = false;
                Different_6B.Enabled = false;
                Less_6B.Enabled = false;
                Greater_6B.Enabled = false;
                button6.Enabled = false;
                button8.Enabled = false;
                button4.Enabled = false;
                button12.Enabled = false;
                button9.Enabled = false;
                
                DestroyCode.Enabled = false;
                AccessCode.Enabled = false;
                NoProect.Enabled = false;
                Proect.Enabled = false;
                Always.Enabled = false;
                AlwaysNot.Enabled = false;
                NoProect2.Enabled = false;
                Proect2.Enabled = false;
                Always2.Enabled = false;
                AlwaysNot2.Enabled = false;
                P_Reserve.Enabled = false;
                P_EPC.Enabled = false;
                P_TID.Enabled = false;
                P_User.Enabled = false;
                Button_WriteEPC_G2.Enabled = false;
                Button_DestroyCard.Enabled = false;
                SpeedButton_Read_G2.Enabled = false;
                Button_DataWrite.Enabled = false;
                BlockWrite.Enabled = false;
                Button_BlockErase.Enabled = false;
                Button_SetProtectState.Enabled = false;
                ListView1_EPC.Items.Clear();
                ComboBox_EPC1.Items.Clear();
                ComboBox_EPC2.Items.Clear();
                ComboBox_EPC3.Items.Clear();
                button2.Text = "Stop";
                checkBox1.Enabled = false;
                
                SpeedButton_Read_6B.Enabled = false;
                SpeedButton_Write_6B.Enabled = false;
                Button14.Enabled = false;
                Button15.Enabled = false;
                ListView_ID_6B.Items.Clear();
                ComOpen = false;
                button10.Text = "Get";
                button10.Enabled = false;
                button11.Enabled = false;
                timer1.Enabled = false;
                comboBox4.SelectedIndex = 0;
                panel1.Enabled = false;
                panel2.Enabled = false;
                panel3.Enabled = false;
                panel4.Enabled = false;
                panel5.Enabled = false;
                button_settigtime.Enabled = false;
                button_gettigtime.Enabled = false;
            }
        }

        private void radioButton22_CheckedChanged(object sender, EventArgs e)
        {
            OpenPort.Enabled = true;
            ClosePort.Enabled = true;
            OpenNetPort.Enabled = false;
            CloseNetPort.Enabled = false;
            CloseNetPort_Click(sender, e);
        }
        
        private void radioButton21_CheckedChanged_1(object sender, EventArgs e)
        {
            if (ComboBox_AlreadyOpenCOM.Items.Count > 0)
                ClosePort_Click(sender, e);
            OpenPort.Enabled = false;
            ClosePort.Enabled = false;
            OpenNetPort.Enabled = true;
            CloseNetPort.Enabled = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            byte Qvalue = 0;
            byte Session = 0;
            Qvalue = Convert.ToByte(com_Q.SelectedIndex);
            Session = Convert.ToByte(com_S.SelectedIndex);
            fCmdRet = StaticClassReaderB.SetQS(ref fComAdr,Qvalue,Session, frmcomportindex);
            AddCmdLog("SetQS", "Set", fCmdRet);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            byte Qvalue = 0;
            byte Session = 0;
            fCmdRet = StaticClassReaderB.GetQS(ref fComAdr,ref Qvalue,ref Session, frmcomportindex);
            if (fCmdRet==0)
            {
                com_Q.SelectedIndex = Convert.ToInt32(Qvalue);
                com_S.SelectedIndex = Convert.ToInt32(Session);
            }
            AddCmdLog("GetQS", "Get", fCmdRet);
        }

        private void btGetSerial_Click(object sender, EventArgs e)
        {
            byte[] serialnum=new byte[4];
            text_serial.Text = "";
            fCmdRet = StaticClassReaderB.GetSerialNo(ref fComAdr, serialnum, frmcomportindex);
            if (fCmdRet == 0)
            {
                text_serial.Text = ByteArrayToHexString(serialnum);
            }
            AddCmdLog("GetSerialNo", "Get", fCmdRet);
        }

        private void radioButton_band0_CheckedChanged(object sender, EventArgs e)
        {
            int i;
            ComboBox_dmaxfre.Items.Clear();
            ComboBox_dminfre.Items.Clear();
            for (i = 0; i < 61; i++)
            {
                ComboBox_dminfre.Items.Add(Convert.ToString(840 + i * 2) + " MHz");
                ComboBox_dmaxfre.Items.Add(Convert.ToString(840 + i * 2) + " MHz");
            }
            ComboBox_dmaxfre.SelectedIndex = 60;
            ComboBox_dminfre.SelectedIndex = 0;
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button18_Click_1(object sender, EventArgs e)
        {
            byte cfgNo = 7;
            byte[] cfgData = new byte[256];
            int len = 3;
            cfgData[0] = (byte)com_queryInter.SelectedIndex;
            cfgData[1] = (byte)(cbb_dwell.SelectedIndex + 2);
            cfgData[2] = (byte)cbb_add.SelectedIndex;
            int fCmdRet = StaticClassReaderB.SetCfgParameter(ref fComAdr, 0, cfgNo, cfgData, len, frmcomportindex);
            AddCmdLog("SetCfgParameter", "Set", fCmdRet);
        }

        private void button17_Click(object sender, EventArgs e)
        {
            byte cfgNo = 7;
            byte[] cfgData = new byte[256];
            int len = 0;
            int fCmdRet = StaticClassReaderB.GetCfgParameter(ref fComAdr, cfgNo, cfgData, ref len, frmcomportindex);
            if (fCmdRet == 0)
            {
                if (len == 3)
                {
                    com_queryInter.SelectedIndex = cfgData[0];
                    cbb_dwell.SelectedIndex = cfgData[1] - 2;
                    cbb_add.SelectedIndex = cfgData[2];
                }
            }
            AddCmdLog("GetCfgParameter", "Get", fCmdRet);
        }

        private void button16_Click(object sender, EventArgs e)
        {
            byte enFlag = 1;
            if (rb_enable.Checked) enFlag = 1;
            else
                enFlag = 0;
            byte cfgNo = 8;
            byte[] cfgData = new byte[256];
            int len = 1;
            cfgData[0] = (byte)enFlag;
            int fCmdRet = StaticClassReaderB.SetCfgParameter(ref fComAdr, 0, cfgNo, cfgData, len, frmcomportindex);
            AddCmdLog("SetCfgParameter", "Set", fCmdRet);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            byte cfgNo = 8;
            byte[] cfgData = new byte[256];
            int len = 0;
            int fCmdRet = StaticClassReaderB.GetCfgParameter(ref fComAdr, cfgNo, cfgData, ref len, frmcomportindex);
            if (fCmdRet == 0)
            {
                if (len == 1)
                {
                    if (cfgData[0] == 1)
                    {
                        rb_enable.Checked = true;
                    }
                    else
                    {
                        rb_disable.Checked = true;
                    }
                }
            }
            AddCmdLog("GetCfgParameter", "Get", fCmdRet);
        }

        private void button_settigtime_Click(object sender, EventArgs e)
        {
            byte TriggerTime;
            TriggerTime = Convert.ToByte(comboBox_tigtime.SelectedIndex);
            fCmdRet = StaticClassReaderB.SetTriggerTime(ref fComAdr, ref TriggerTime, frmcomportindex);
            AddCmdLog("SetTriggerTime", "Set Trigger Time", fCmdRet);
        }

        private void button_gettigtime_Click(object sender, EventArgs e)
        {
            byte TriggerTime;
            TriggerTime = 255;
            fCmdRet = StaticClassReaderB.SetTriggerTime(ref fComAdr, ref TriggerTime, frmcomportindex);
            if (fCmdRet == 0)
            {
                comboBox_tigtime.SelectedIndex = TriggerTime;
            }
            AddCmdLog("SetTriggerTime", "Read Trigger Time", fCmdRet);
        }

        private void com_contype_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (com_contype.SelectedIndex == 0)
            {
                txtSvrIp.Enabled = false;
                txtSvrPort.Enabled = false;
                txtLoclPort.Enabled = true;
            }
            else
            {
                txtSvrIp.Enabled = true;
                txtSvrPort.Enabled = true;
                txtLoclPort.Enabled = false;
            }
        }

        private void wifi_com_contype_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (wifi_com_contype.SelectedIndex == 0)
            {
                wifi_txtSvrIp.Enabled = false;
                wifi_txtSvrPort.Enabled = false;
                wifi_txtLoclPort.Enabled = true;
            }
            else
            {
                wifi_txtSvrIp.Enabled = true;
                wifi_txtSvrPort.Enabled = true;
                wifi_txtLoclPort.Enabled = false;
            }
        }

        private void btSetIp_Click(object sender, EventArgs e)
        {
            try
            {
                if ((txtIpaddr.Text == "") || (txtSubnet.Text == "") || (txtGateway.Text == ""))
                {
                    return;
                }
                string[] temp1 = txtIpaddr.Text.Split('.');
                string[] temp2 = txtSubnet.Text.Split('.');
                string[] temp3 = txtGateway.Text.Split('.');
                if ((temp1.Length != 4) || (temp2.Length != 4) || (temp3.Length != 4))
                {
                    return;
                }
                byte[] ipAddr = new byte[4];
                byte[] SubnetMask = new byte[4];
                byte[] wgAddr = new byte[4];
                byte dhcp = 0;
                for (int i = 0; i < 4; i++)
                {
                    ipAddr[i] = Convert.ToByte(temp1[i], 10);
                    SubnetMask[i] = Convert.ToByte(temp2[i], 10);
                    wgAddr[i] = Convert.ToByte(temp3[i], 10);
                }
                if (rb_dhcp.Checked) dhcp = 1;
                fCmdRet = StaticClassReaderB.SetNetWorkIP(ref fComAdr, ipAddr, SubnetMask, wgAddr, dhcp,0, frmcomportindex);
                AddCmdLog("SetNetWorkIP", "Set Ethernet IP", fCmdRet);

            }
            catch (System.Exception ex)
            {
                ex.ToString();
            }
        }

        private void btGetIp_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] ipAddr = new byte[4];
                byte[] SubnetMask = new byte[4];
                byte[] wgAddr = new byte[4];
                byte dhcp = 0;
                txtIpaddr.Text = "";
                txtSubnet.Text = "";
                txtGateway.Text = "";
                fCmdRet = StaticClassReaderB.GetNetWorkIP(ref fComAdr, ipAddr, SubnetMask, wgAddr, ref dhcp,0, frmcomportindex);
                if (fCmdRet == 0)
                {
                    txtIpaddr.Text = ipAddr[0].ToString() + "." + ipAddr[1].ToString() + "." + ipAddr[2].ToString() + "." + ipAddr[3].ToString();
                    txtSubnet.Text = SubnetMask[0].ToString() + "." + SubnetMask[1].ToString() + "." + SubnetMask[2].ToString() + "." + SubnetMask[3].ToString();
                    txtGateway.Text = wgAddr[0].ToString() + "." + wgAddr[1].ToString() + "." + wgAddr[2].ToString() + "." + wgAddr[3].ToString();
                    if (dhcp == 1)
                        rb_dhcp.Checked = true;
                    else
                        rb_static.Checked = true;
                }
                AddCmdLog("GetNetWorkIP", "Read Ethernet IP", fCmdRet);

            }
            catch (System.Exception ex)
            {
                ex.ToString();
            }
        }

        private void btSetconnect_Click(object sender, EventArgs e)
        {
            try
            {
                if ((txtSvrIp.Text == "") || (txtSvrPort.Text == "") || (txtLoclPort.Text == ""))
                {
                    return;
                }
                string[] temp1 = txtSvrIp.Text.Split('.');
                if (temp1.Length != 4)
                {
                    return;
                }
                byte[] SvripAddr = new byte[4];
                int SvrPOrt = 0;
                byte ConnectionMode = 0;
                int ClientPort = 0;

                for (int i = 0; i < 4; i++)
                {
                    SvripAddr[i] = Convert.ToByte(temp1[i], 10);
                }
                SvrPOrt = Convert.ToInt32(txtSvrPort.Text, 10);
                ClientPort = Convert.ToInt32(txtLoclPort.Text, 10);
                ConnectionMode = (byte)com_contype.SelectedIndex;
                fCmdRet = StaticClassReaderB.SetNetWorkConnection(ref fComAdr, SvripAddr, SvrPOrt, ConnectionMode, ClientPort, 0,frmcomportindex);
                AddCmdLog("SetNetWorkConnection", "Ethernet Connection Set", fCmdRet);
            }
            catch (System.Exception ex)
            {
                ex.ToString();
            }
        }

        private void btGetconnect_Click(object sender, EventArgs e)
        {
            try
            {
                txtSvrIp.Text = "";
                txtSvrPort.Text = "";
                txtLoclPort.Text = "";
                byte[] SvripAddr = new byte[4];
                int SvrPOrt = 0;
                byte ConnectionMode = 0;
                int ClientPort = 0;
                fCmdRet = StaticClassReaderB.GetNetWorkConnection(ref fComAdr, SvripAddr, ref SvrPOrt, ref ConnectionMode, ref ClientPort,0, frmcomportindex);
                if (fCmdRet == 0)
                {
                    txtSvrIp.Text = SvripAddr[0].ToString() + "." + SvripAddr[1].ToString() + "." + SvripAddr[2].ToString() + "." + SvripAddr[3].ToString();
                    txtSvrPort.Text = Convert.ToString(SvrPOrt, 10);
                    txtLoclPort.Text = Convert.ToString(ClientPort, 10);
                    com_contype.SelectedIndex = ConnectionMode;
                }
                AddCmdLog("GetNetWorkConnection", "Ethernet Connection Read", fCmdRet);
            }
            catch (System.Exception ex)
            {
                ex.ToString();
            }
        }

        private void btSetKeepAlive_Click(object sender, EventArgs e)
        {
            try
            {
                if (txt_keepalive_time.Text == "")
                {
                    return;
                }

                byte keepaliveen = (byte)cbb_keepalive_en.SelectedIndex;
                int keepalivetime = Convert.ToInt32(txt_keepalive_time.Text,10);

                fCmdRet = StaticClassReaderB.SetKeepAlive(ref fComAdr, keepaliveen, keepalivetime, 0, frmcomportindex);
                AddCmdLog("SetKeepAlive", "Keep Alive Set", fCmdRet);
            }
            catch (System.Exception ex)
            {
                ex.ToString();
            }
        }

        private void btGetKeepAlive_Click(object sender, EventArgs e)
        {
            try
            {
                txt_keepalive_time.Text = "";
                byte keepaliveen = 0;
                int keepalivetime = 0;
                fCmdRet = StaticClassReaderB.GetKeepAlive(ref fComAdr,ref keepaliveen,ref keepalivetime, 0, frmcomportindex);
                if (fCmdRet == 0)
                {
                    cbb_keepalive_en.SelectedIndex = keepaliveen;
                    txt_keepalive_time.Text = keepalivetime + "";
                }
                AddCmdLog("GetKeepAlive", "Keep Alive Get", fCmdRet);
            }
            catch (System.Exception ex)
            {
                ex.ToString();
            }
        }

        private void btSetTimeout_Click(object sender, EventArgs e)
        {
            try
            {
                byte TCPReconnectMode = (byte)cbb_Reconnect.SelectedIndex;
                fCmdRet = StaticClassReaderB.SetReconnect(ref fComAdr, TCPReconnectMode,0, frmcomportindex);
                AddCmdLog("SetReconnect", "Network Reconnection Set", fCmdRet);
            }
            catch (System.Exception ex)
            {
                ex.ToString();
            }
        }

        private void btGetTimeout_Click(object sender, EventArgs e)
        {
            try
            {
                byte TCPReconnectMode = 0;
                fCmdRet = StaticClassReaderB.GetReconnect(ref fComAdr, ref TCPReconnectMode,0, frmcomportindex);
                if (fCmdRet == 0)
                {
                    cbb_Reconnect.SelectedIndex = TCPReconnectMode;
                }
                AddCmdLog("GetReconnect", "Network Reconnection Read", fCmdRet);
            }
            catch (System.Exception ex)
            {
                ex.ToString();
            }
        }

        private void wifi_btSetIp_Click(object sender, EventArgs e)
        {
            try
            {
                if ((wifi_txtIpaddr.Text == "") || (wifi_txtSubnet.Text == "") || (wifi_txtGateway.Text == ""))
                {
                    return;
                }
                string[] temp1 = wifi_txtIpaddr.Text.Split('.');
                string[] temp2 = wifi_txtSubnet.Text.Split('.');
                string[] temp3 = wifi_txtGateway.Text.Split('.');
                if ((temp1.Length != 4) || (temp2.Length != 4) || (temp3.Length != 4))
                {
                    return;
                }
                byte[] ipAddr = new byte[4];
                byte[] SubnetMask = new byte[4];
                byte[] wgAddr = new byte[4];
                byte dhcp = 0;
                for (int i = 0; i < 4; i++)
                {
                    ipAddr[i] = Convert.ToByte(temp1[i], 10);
                    SubnetMask[i] = Convert.ToByte(temp2[i], 10);
                    wgAddr[i] = Convert.ToByte(temp3[i], 10);
                }
                if (rb_wifi_dhcp.Checked) dhcp = 1;
                fCmdRet = StaticClassReaderB.SetNetWorkIP(ref fComAdr, ipAddr, SubnetMask, wgAddr, dhcp, 1, frmcomportindex);
                AddCmdLog("SetNetWorkIP", "Set WIFI Network IP", fCmdRet);

            }
            catch (System.Exception ex)
            {
                ex.ToString();
            }
        }

        private void wifi_btGetIp_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] ipAddr = new byte[4];
                byte[] SubnetMask = new byte[4];
                byte[] wgAddr = new byte[4];
                byte dhcp = 0;
                wifi_txtIpaddr.Text = "";
                wifi_txtSubnet.Text = "";
                wifi_txtGateway.Text = "";
                fCmdRet = StaticClassReaderB.GetNetWorkIP(ref fComAdr, ipAddr, SubnetMask, wgAddr, ref dhcp, 1, frmcomportindex);
                if (fCmdRet == 0)
                {
                    wifi_txtIpaddr.Text = ipAddr[0].ToString() + "." + ipAddr[1].ToString() + "." + ipAddr[2].ToString() + "." + ipAddr[3].ToString();
                    wifi_txtSubnet.Text = SubnetMask[0].ToString() + "." + SubnetMask[1].ToString() + "." + SubnetMask[2].ToString() + "." + SubnetMask[3].ToString();
                    wifi_txtGateway.Text = wgAddr[0].ToString() + "." + wgAddr[1].ToString() + "." + wgAddr[2].ToString() + "." + wgAddr[3].ToString();
                    if (dhcp == 1)
                        rb_wifi_dhcp.Checked = true;
                    else
                        rb_wifi_static.Checked = true;
                }
                AddCmdLog("GetNetWorkIP", "Read WIFI Network IP", fCmdRet);

            }
            catch (System.Exception ex)
            {
                ex.ToString();
            }
        }

        private void wifi_btSetconnect_Click(object sender, EventArgs e)
        {
            try
            {
                if ((wifi_txtSvrIp.Text == "") || (wifi_txtSvrPort.Text == "") || (wifi_txtLoclPort.Text == ""))
                {
                    return;
                }
                string[] temp1 = wifi_txtSvrIp.Text.Split('.');
                if (temp1.Length != 4)
                {
                    return;
                }
                byte[] SvripAddr = new byte[4];
                int SvrPOrt = 0;
                byte ConnectionMode = 0;
                int ClientPort = 0;

                for (int i = 0; i < 4; i++)
                {
                    SvripAddr[i] = Convert.ToByte(temp1[i], 10);
                }
                SvrPOrt = Convert.ToInt32(wifi_txtSvrPort.Text, 10);
                ClientPort = Convert.ToInt32(wifi_txtLoclPort.Text, 10);
                ConnectionMode = (byte)wifi_com_contype.SelectedIndex;
                fCmdRet = StaticClassReaderB.SetNetWorkConnection(ref fComAdr, SvripAddr, SvrPOrt, ConnectionMode, ClientPort, 1, frmcomportindex);
                AddCmdLog("SetNetWorkConnection", "WIFI Connection Set", fCmdRet);
            }
            catch (System.Exception ex)
            {
                ex.ToString();
            }
        }

        private void wifi_btGetconnect_Click(object sender, EventArgs e)
        {
            try
            {
                wifi_txtSvrIp.Text = "";
                wifi_txtSvrPort.Text = "";
                wifi_txtLoclPort.Text = "";
                byte[] SvripAddr = new byte[4];
                int SvrPOrt = 0;
                byte ConnectionMode = 0;
                int ClientPort = 0;
                fCmdRet = StaticClassReaderB.GetNetWorkConnection(ref fComAdr, SvripAddr, ref SvrPOrt, ref ConnectionMode, ref ClientPort, 1, frmcomportindex);
                if (fCmdRet == 0)
                {
                    wifi_txtSvrIp.Text = SvripAddr[0].ToString() + "." + SvripAddr[1].ToString() + "." + SvripAddr[2].ToString() + "." + SvripAddr[3].ToString();
                    wifi_txtSvrPort.Text = Convert.ToString(SvrPOrt, 10);
                    wifi_txtLoclPort.Text = Convert.ToString(ClientPort, 10);
                    wifi_com_contype.SelectedIndex = ConnectionMode;
                }
                AddCmdLog("GetNetWorkConnection", "WIFI Connection Read", fCmdRet);
            }
            catch (System.Exception ex)
            {
                ex.ToString();
            }
        }

        private void wifi_btSetKeepAlive_Click(object sender, EventArgs e)
        {
            try
            {
                if (txt_wifi_keepalive_time.Text == "")
                {
                    return;
                }

                byte keepaliveen = (byte)cbb_wifi_keepalive_en.SelectedIndex;
                int keepalivetime = Convert.ToInt32(txt_wifi_keepalive_time.Text, 10);

                fCmdRet = StaticClassReaderB.SetKeepAlive(ref fComAdr, keepaliveen, keepalivetime, 1, frmcomportindex);
                AddCmdLog("SetKeepAlive", "KeepAliveSet", fCmdRet);
            }
            catch (System.Exception ex)
            {
                ex.ToString();
            }
        }

        private void wifi_btGetKeepAlive_Click(object sender, EventArgs e)
        {
            try
            {
                txt_wifi_keepalive_time.Text = "";
                byte keepaliveen = 0;
                int keepalivetime = 0;
                fCmdRet = StaticClassReaderB.GetKeepAlive(ref fComAdr, ref keepaliveen, ref keepalivetime, 1, frmcomportindex);
                if (fCmdRet == 0)
                {
                    cbb_wifi_keepalive_en.SelectedIndex = keepaliveen;
                    txt_wifi_keepalive_time.Text = keepalivetime + "";
                }
                AddCmdLog("GetKeepAlive", "KeepAliveGet", fCmdRet);
            }
            catch (System.Exception ex)
            {
                ex.ToString();
            }
        }

        private void wifi_btSetTimeout_Click(object sender, EventArgs e)
        {
            try
            {
                byte TCPReconnectMode = (byte)wifi_cbb_Reconnect.SelectedIndex;
                fCmdRet = StaticClassReaderB.SetReconnect(ref fComAdr, TCPReconnectMode, 1, frmcomportindex);
                AddCmdLog("SetReconnect", "Network Reconnection Set", fCmdRet);
            }
            catch (System.Exception ex)
            {
                ex.ToString();
            }
        }

        private void wifi_btGetTimeout_Click(object sender, EventArgs e)
        {
            try
            {
                byte TCPReconnectMode = 0;
                fCmdRet = StaticClassReaderB.GetReconnect(ref fComAdr, ref TCPReconnectMode, 1, frmcomportindex);
                if (fCmdRet == 0)
                {
                    wifi_cbb_Reconnect.SelectedIndex = TCPReconnectMode;
                }
                AddCmdLog("GetReconnect", "Network Reconnection Read", fCmdRet);
            }
            catch (System.Exception ex)
            {
                ex.ToString();
            }
        }

        private void txtIpaddr_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = ("0123456789.".IndexOf(Char.ToUpper(e.KeyChar)) < 0);
        }

        private void button29_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
        }

        private void wifi_setApConnect_Click(object sender, EventArgs e)
        {
            if (txt_ssid.Text.Trim().Length == 0 
                || txt_apmac.Text.Trim().Length!=17)
                return;
            byte ssidLen = (byte)txt_ssid.Text.Trim().Length;
            byte[] ssid = new byte[256];
            byte pwdLen = (byte)txt_apsw.Text.Trim().Length;
            byte[] Pwd = new byte[256];
            byte[] MAC = new byte[6];
            ssid = ASCIIEncoding.UTF8.GetBytes(txt_ssid.Text.Trim());
            if(pwdLen>0)
                Pwd = ASCIIEncoding.ASCII.GetBytes(txt_apsw.Text.Trim());
            MAC = HexStringToByteArray(txt_apmac.Text.Trim().Replace(":",""));
            int fCmdRet = StaticClassReaderB.SetAPConnectName_Pwd(ref fComAdr,ssidLen,ssid, pwdLen,Pwd, MAC, frmcomportindex);
            AddCmdLog("SetAPConnectName_Pwd", "WIFI Connection", fCmdRet);
        }

        private void wifi_getApConnect_Click(object sender, EventArgs e)
        {
            byte ssidLen = (byte)txt_ssid.Text.Trim().Length;
            byte[] ssid = new byte[256];
            byte pwdLen = (byte)txt_apsw.Text.Trim().Length;
            byte[] Pwd = new byte[256];
            byte[] MAC = new byte[6];
            int fCmdRet = StaticClassReaderB.GetAPConnectName_Pwd(ref fComAdr,ref ssidLen, ssid,ref pwdLen, Pwd, MAC, frmcomportindex);
            if (fCmdRet==0)
            {
                if (ssidLen > 0)
                {
                    byte[] daw = new byte[ssidLen];
                    Array.Copy(ssid, 0, daw, 0, ssidLen);
                    txt_ssid.Text = ASCIIEncoding.ASCII.GetString(daw);
                }
                if (pwdLen>0)
                {
                    byte[] daw = new byte[pwdLen];
                    Array.Copy(Pwd, 0, daw, 0, pwdLen);
                    txt_apsw.Text = ASCIIEncoding.UTF8.GetString(daw);
                }
                txt_apmac.Text = Convert.ToString(MAC[0],16).PadLeft(2,'0').ToUpper()+":"
                    + Convert.ToString(MAC[1], 16).PadLeft(2, '0').ToUpper() + ":"
                    + Convert.ToString(MAC[2], 16).PadLeft(2, '0').ToUpper() + ":"
                    + Convert.ToString(MAC[3], 16).PadLeft(2, '0').ToUpper() + ":"
                    + Convert.ToString(MAC[4], 16).PadLeft(2, '0').ToUpper() + ":"
                    + Convert.ToString(MAC[5], 16).PadLeft(2, '0').ToUpper();
            }
            AddCmdLog("SetAPConnectName_Pwd", "WIFI Connection", fCmdRet);
        }

        public static Thread mthread = null;
        private void wifi_search_Click(object sender, EventArgs e)
        {
            if (mthread == null)
            {
                dataGridView1.Rows.Clear();
                mthread = new Thread(new ThreadStart(searchwifi));
                mthread.IsBackground = true;
                mthread.Start();
                SearchNotityForm mform = new SearchNotityForm();
                mform.ShowDialog();
            }
        }

        private void searchwifi()
        {
            byte[] APInfo = new byte[25600];
            int length = 0;
            int Count = 0;
            fCmdRet = StaticClassReaderB.SearchWIFI(ref fComAdr, APInfo,ref length,ref Count, frmcomportindex);
            if (fCmdRet!=0x30 && Count>0)
            {
                int pos = 0;
                for (int m=0;m< Count;m++)
                {
                    byte ssidLen = APInfo[pos];
                    pos++;
                    byte[] ssid = new byte[ssidLen];
                    Array.Copy(APInfo, pos, ssid, 0, ssidLen);
                    pos += ssidLen;
                    byte rssi = APInfo[pos];
                    pos++;
                    byte[] MAC = new byte[6];
                    Array.Copy(APInfo, pos, MAC, 0, 6);
                    pos += 6;
                    byte Channel = APInfo[pos];
                    pos++;
                    string[] temp = new string[5];
                    temp[0] = dataGridView1.RowCount + 1 + "";
                    temp[1] = ASCIIEncoding.UTF8.GetString(ssid);
                    temp[2] = Convert.ToString(MAC[0], 16).PadLeft(2, '0').ToUpper() + ":"
                    + Convert.ToString(MAC[1], 16).PadLeft(2, '0').ToUpper() + ":"
                    + Convert.ToString(MAC[2], 16).PadLeft(2, '0').ToUpper() + ":"
                    + Convert.ToString(MAC[3], 16).PadLeft(2, '0').ToUpper() + ":"
                    + Convert.ToString(MAC[4], 16).PadLeft(2, '0').ToUpper() + ":"
                    + Convert.ToString(MAC[5], 16).PadLeft(2, '0').ToUpper();
                    temp[3] = Channel+"";
                    temp[4] = (rssi-256)+"";
                    this.Invoke((EventHandler)delegate
                    {
                        dataGridView1.Rows.Add(temp);
                    });
                }
            }
            mthread = null;
        }

        private void button27_Click(object sender, EventArgs e)
        {
            fCmdRet = StaticClassReaderB.ResetReader(ref fComAdr, frmcomportindex);
            AddCmdLog("ResetReader", "Restart Reader", fCmdRet);
        }

        private void bluetooth_mac_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = ("0123456789ABCDEF:".IndexOf(Char.ToUpper(e.KeyChar)) < 0);
        }


        private void set_match_Click(object sender, EventArgs e)
        {
            byte matchen = 0;
            byte matchopt = 0;
            byte matchAddr = 0;
            byte matchLen = 0;
            byte matchEPCLen = 0;
            byte[] matchData = new byte[256];
            if (chk_match_en.Checked)
                matchen |= 0x01;
            if (checkBox2.Checked)
                matchen |= 0x02;
            matchEPCLen = (byte)cbb_matchlength.SelectedIndex;
            if (txt_match_addr.Text.Length == 0 || chk_match_len.Text.Length == 0 || txt_match_data.Text.Length == 0) return;

            matchopt = (byte)cbb_output.SelectedIndex;
            matchAddr = Convert.ToByte(txt_match_addr.Text, 10);
            matchLen = Convert.ToByte(chk_match_len.Text, 10);
            int length = (txt_match_data.Text.Length + 1) / 2;
            string temp = txt_match_data.Text.PadRight(length * 2, '0');
            matchData = HexStringToByteArray(temp);
            if (matchLen > matchData.Length * 8) return;
            fCmdRet = StaticClassReaderB.SetMatchRule(ref fComAdr, matchen, matchopt, matchEPCLen, matchAddr, matchLen, matchData, frmcomportindex);
            AddCmdLog("SetMatchRule", "Set Matching Rule", fCmdRet);
        }

        private void get_match_Click(object sender, EventArgs e)
        {
            byte matchen = 0;
            byte matchopt = 0;
            byte matchAddr = 0;
            byte matchLen = 0;
            byte matchEPCLen = 0;
            byte[] matchData = new byte[256];
            fCmdRet = StaticClassReaderB.GetMatchRule(ref fComAdr,ref matchen, ref matchopt,ref matchEPCLen, ref matchAddr, ref matchLen, matchData, frmcomportindex);
            if (fCmdRet==0)
            {
                if ((matchen&0x01) == 0x01)
                    chk_match_en.Checked = true;
                else
                    chk_match_en.Checked = false;

                if ((matchen & 0x02) == 0x02)
                    checkBox2.Checked = true;
                else
                    checkBox2.Checked = false;
                cbb_matchlength.SelectedIndex = matchEPCLen;
                cbb_output.SelectedIndex = matchopt;
                txt_match_addr.Text = matchAddr + "";
                chk_match_len.Text = matchLen + "";
                int maskbyte = (matchLen + 7) / 8;
                byte[] daw = new byte[maskbyte];
                Array.Copy(matchData, 0, daw, 0, maskbyte);
                txt_match_data.Text = ByteArrayToHexString(daw);

            }
            AddCmdLog("GetMatchRule", "Read Matching Rule", fCmdRet);
        }

        private void set_relay_Click(object sender, EventArgs e)
        {
            byte RelayEn = 0;
            byte ActionTime = 0;
            if (chk_relay1.Checked)
                RelayEn |= 0x01;
            if (chk_relay2.Checked)
                RelayEn |= 0x02;
            if (chk_relay3.Checked)
                RelayEn |= 0x04;
            if (chk_relay4.Checked)
                RelayEn |= 0x08;
            ActionTime = (byte)cbb_relay1.SelectedIndex;
            fCmdRet = StaticClassReaderB.SetRelayAction(ref fComAdr, RelayEn, ActionTime, frmcomportindex);
            AddCmdLog("SetRelayAction", "Configure Relay Action", fCmdRet);
        }

        private void get_relay_Click(object sender, EventArgs e)
        {
            byte RelayEn = 0;
            byte ActionTime = 0;
            fCmdRet = StaticClassReaderB.GetRelayAction(ref fComAdr,ref RelayEn,ref ActionTime, frmcomportindex);
            if (fCmdRet == 0)
            {
                if ((RelayEn & 0x01) == 0x01)
                    chk_relay1.Checked = true;
                else
                    chk_relay1.Checked = false;

                if ((RelayEn & 0x02) == 0x02)
                    chk_relay2.Checked = true;
                else
                    chk_relay2.Checked = false;

                if ((RelayEn & 0x04) == 0x04)
                    chk_relay3.Checked = true;
                else
                    chk_relay3.Checked = false;

                if ((RelayEn & 0x08) == 0x08)
                    chk_relay4.Checked = true;
                else
                    chk_relay4.Checked = false;

                cbb_relay1.SelectedIndex = ActionTime;
            }
            AddCmdLog("GetRelayAction", "Read Relay Action", fCmdRet);
        }


        private void rj45_get_mac_Click(object sender, EventArgs e)
        {
            byte[] MAC = new byte[6];
            rj45_mac.Text = "";
            fCmdRet = StaticClassReaderB.GetRJ45MacAddr(ref fComAdr, MAC, frmcomportindex);
            if (fCmdRet ==0 )
            {
                rj45_mac.Text = Convert.ToString(MAC[0], 16).PadLeft(2, '0') + ":"
                              + Convert.ToString(MAC[1], 16).PadLeft(2, '0') + ":"
                              + Convert.ToString(MAC[2], 16).PadLeft(2, '0') + ":"
                              + Convert.ToString(MAC[3], 16).PadLeft(2, '0') + ":"
                              + Convert.ToString(MAC[4], 16).PadLeft(2, '0') + ":"
                              + Convert.ToString(MAC[5], 16).PadLeft(2, '0');
            }
            AddCmdLog("GetRJ45MacAddr", "Read MAC Address", fCmdRet);
        }

        private void button28_Click(object sender, EventArgs e)
        {
            byte rj45state = 0;
            byte[] localIP = new byte[4];
            byte[] subnet = new byte[4];
            byte[] gatewayIp = new byte[4];
            txt_rj45_status.Text = "";
            fCmdRet = StaticClassReaderB.ReadRJ45ConnectStatus(ref fComAdr,ref rj45state, localIP, subnet, gatewayIp, frmcomportindex);
            if (fCmdRet==0)
            {
                //txt_rj45_status
                string temp = "";
                if (rj45state == 0)
                    temp = "Ethernet cable connected";
                else if (rj45state == 1)
                    temp = "Ethernet cable disconnected";
                else if (rj45state == 255)
                    temp = "Hardware error or Ethernet not supported";
                int length = temp.Length;
                string ip = localIP[0] + "." + localIP[1] + "." + localIP[2] + "." + localIP[3];
                string net = subnet[0] + "." + subnet[1] + "." + subnet[2] + "." + subnet[3];
                string wayip = gatewayIp[0] + "." + gatewayIp[1] + "." + gatewayIp[2] + "." + gatewayIp[3];
                temp += "\t";

                temp += "Current IP:" + ip + "\r\n";
                temp += "\t\tCurrent Subnet Mask:" + net + "\r\n";
                temp += "\t\tCurrent Gateway:" + wayip;
                txt_rj45_status.Text = temp;
            }
            AddCmdLog("ReadRJ45ConnectStatus", "Read Ethernet connection status", fCmdRet);

        }

        private void read_wifi_mac_Click(object sender, EventArgs e)
        {
            byte[] MAC = new byte[6];
            wifi_mac.Text = "";
            fCmdRet = StaticClassReaderB.GetWIFIMacAddr(ref fComAdr, MAC, frmcomportindex);
            if (fCmdRet == 0)
            {
                wifi_mac.Text = Convert.ToString(MAC[0], 16).PadLeft(2, '0') + ":"
                              + Convert.ToString(MAC[1], 16).PadLeft(2, '0') + ":"
                              + Convert.ToString(MAC[2], 16).PadLeft(2, '0') + ":"
                              + Convert.ToString(MAC[3], 16).PadLeft(2, '0') + ":"
                              + Convert.ToString(MAC[4], 16).PadLeft(2, '0') + ":"
                              + Convert.ToString(MAC[5], 16).PadLeft(2, '0');
            }
            AddCmdLog("GetWIFIMacAddr", "Read MAC Address", fCmdRet);
        }

        private void wifi_read_status_Click(object sender, EventArgs e)
        {
            byte wifistate = 0;
            byte[] localIP = new byte[4];
            byte[] subnet = new byte[4];
            byte[] gatewayIp = new byte[4];
            txt_wifi_state.Text = "";
            fCmdRet = StaticClassReaderB.ReadWIFIConnectStatus(ref fComAdr, ref wifistate, localIP, subnet, gatewayIp, frmcomportindex);
            if (fCmdRet == 0)
            {
                //txt_rj45_status
                string temp = "";
                if (wifistate == 0)
                    temp = "Connected to AP";
                else if (wifistate == 1)
                    temp = "Not connected to AP";
                else if (wifistate == 255)
                    temp = "Hardware error or WIFI not supported";
                int length = temp.Length;
                string ip = localIP[0] + "." + localIP[1] + "." + localIP[2] + "." + localIP[3];
                string net = subnet[0] + "." + subnet[1] + "." + subnet[2] + "." + subnet[3];
                string wayip = gatewayIp[0] + "." + gatewayIp[1] + "." + gatewayIp[2] + "." + gatewayIp[3];
                temp += "\t";
                
                temp += "Current IP:" + ip + "\r\n";
                temp += "\t\tCurrent Subnet Mask:" + net + "\r\n";
                temp += "\t\tCurrent Gateway:" + wayip;
                txt_wifi_state.Text = temp;
            }
            AddCmdLog("ReadWIFIConnectStatus", "Read WIFI connection status", fCmdRet);
        }

        private void read_blue_mac_Click(object sender, EventArgs e)
        {
            byte[] MAC = new byte[6];
            bluetooth_mac.Text = "";
            fCmdRet = StaticClassReaderB.GetBluetoothMacAddr(ref fComAdr, MAC, frmcomportindex);
            if (fCmdRet == 0)
            {
                bluetooth_mac.Text = Convert.ToString(MAC[0], 16).PadLeft(2, '0') + ":"
                              + Convert.ToString(MAC[1], 16).PadLeft(2, '0') + ":"
                              + Convert.ToString(MAC[2], 16).PadLeft(2, '0') + ":"
                              + Convert.ToString(MAC[3], 16).PadLeft(2, '0') + ":"
                              + Convert.ToString(MAC[4], 16).PadLeft(2, '0') + ":"
                              + Convert.ToString(MAC[5], 16).PadLeft(2, '0');
            }
            AddCmdLog("GetWIFIMacAddr", "Read MAC Address", fCmdRet);
        }

        private void read_ble_mac_Click(object sender, EventArgs e)
        {
            byte[] MAC = new byte[6];
            ble_mac.Text = "";
            fCmdRet = StaticClassReaderB.GetBleMacAddr(ref fComAdr, MAC, frmcomportindex);
            if (fCmdRet == 0)
            {
                ble_mac.Text = Convert.ToString(MAC[0], 16).PadLeft(2, '0') + ":"
                              + Convert.ToString(MAC[1], 16).PadLeft(2, '0') + ":"
                              + Convert.ToString(MAC[2], 16).PadLeft(2, '0') + ":"
                              + Convert.ToString(MAC[3], 16).PadLeft(2, '0') + ":"
                              + Convert.ToString(MAC[4], 16).PadLeft(2, '0') + ":"
                              + Convert.ToString(MAC[5], 16).PadLeft(2, '0');
            }
            AddCmdLog("GetWIFIMacAddr", "Read MAC Address", fCmdRet);
        }

        private void set_blue_name_Click(object sender, EventArgs e)
        {
            byte namelen = 0;
            byte[] BTName = null;
            if (bluetooth_name.Text.Trim().Length == 0) return;
            BTName = Encoding.ASCII.GetBytes(bluetooth_name.Text.Trim());
            namelen = (byte)BTName.Length;
            fCmdRet = StaticClassReaderB.SetBluetoothNme(ref fComAdr, namelen, BTName,frmcomportindex);
            AddCmdLog("SetBluetoothNme", "Set BLE Name", fCmdRet);
        }

        private void get_blue_name_Click(object sender, EventArgs e)
        {
            byte namelen = 0;
            byte[] BTName = new byte[256];
            bluetooth_name.Text = "";
            fCmdRet = StaticClassReaderB.GetBluetoothNme(ref fComAdr,ref namelen, BTName, frmcomportindex);
            if (fCmdRet == 0)
            {
                bluetooth_name.Text = Encoding.ASCII.GetString(BTName, 0, namelen);
            }
            AddCmdLog("GetBluetoothNme", "Get BLE Name", fCmdRet);
        }

        private void set_ble_name_Click(object sender, EventArgs e)
        {
            byte namelen = 0;
            byte[] BTName = null;
            if (ble_name.Text.Trim().Length == 0) return;
            BTName = Encoding.ASCII.GetBytes(ble_name.Text.Trim());
            namelen = (byte)BTName.Length;
            fCmdRet = StaticClassReaderB.SetBleNme(ref fComAdr, namelen, BTName, frmcomportindex);
            AddCmdLog("SetBleNme", "Set BLE Name", fCmdRet);
        }

        private void get_ble_name_Click(object sender, EventArgs e)
        {
            byte namelen = 0;
            byte[] BTName = new byte[256];
            ble_name.Text = "";
            fCmdRet = StaticClassReaderB.GetBleNme(ref fComAdr, ref namelen, BTName, frmcomportindex);
            if (fCmdRet == 0)
            {
                ble_name.Text = Encoding.ASCII.GetString(BTName, 0, namelen);
            }
            AddCmdLog("GetBleNme", "Get BLE Name", fCmdRet);
        }

        private void set_interface_en_Click(object sender, EventArgs e)
        {
            int connIntf = 0x0000;
            if (chk_wifi.Checked)
                connIntf |= 0x0001;
            if(chk_bluetooth.Checked)
                connIntf |= 0x0004;
            if (chk_ble.Checked)
                connIntf |= 0x0008;
            fCmdRet = StaticClassReaderB.SetCommunicationSwitch(ref fComAdr, connIntf,frmcomportindex);
            AddCmdLog("SetCommunicationSwitch", "Set", fCmdRet);
        }

        private void get_interface_en_Click(object sender, EventArgs e)
        {
            int connIntf = 0x0000;
            fCmdRet = StaticClassReaderB.GetCommunicationSwitch(ref fComAdr,ref connIntf, frmcomportindex);
            if (fCmdRet==0)
            {
                if ((connIntf & 0x0001) == 0x0001)
                    chk_wifi.Checked = true;
                else
                    chk_wifi.Checked = false;

                if ((connIntf & 0x0004) == 0x0004)
                    chk_bluetooth.Checked = true;
                else
                    chk_bluetooth.Checked = false;

                if ((connIntf & 0x0008) == 0x0008)
                    chk_ble.Checked = true;
                else
                    chk_ble.Checked = false;
            }
            AddCmdLog("GetCommunicationSwitch", "Get", fCmdRet);
        }

        /// <summary>
        /// 将Device List中所记录设备显示至DeviceListView控件;
        /// </summary>
        private void ReflashDeviceListView(List<DeviceClass> deviceList)
        {
            this.DeviceListView.Items.Clear();
            foreach (DeviceClass device in deviceList)
            {
                IPAddress ipAddr = getIPAddress(device.DeviceIP);
                ListViewItem deviceListViewItem = new ListViewItem(new string[] { device.DeviceName, ipAddr.ToString(), device.DeviceMac });
                deviceListViewItem.ImageIndex = 0;
                this.DeviceListView.Items.Add(deviceListViewItem);
            }
        }

        /// <summary>
        /// 将Device List中所记录设备显示至DeviceListView控件;
        /// </summary>
        private void ClearDeviceListView()
        {
            DevControl.tagErrorCode eCode;
            List<DeviceClass> deviceList = DevList;

            foreach (DeviceClass device in deviceList)
            {
                eCode = DevControl.DM_FreeDevice(device.DevHandle);
                Debug.Assert(eCode == DevControl.tagErrorCode.DM_ERR_OK);
            }
            //清空设备列表，并清空对应显示控件；
            DevList.Clear();
            ReflashDeviceListView(DevList);
        }

        /// <summary>
        /// 搜索设备，然后将记录搜索结果的DevList显示至DeviceListView控件;
        /// </summary>
        private bool SearchDevice(uint targetIP)
        {
            ClearDeviceListView();
            DevControl.tagErrorCode eCode = DevControl.DM_SearchDevice(targetIP, 1500);
            if (eCode == DevControl.tagErrorCode.DM_ERR_OK)
            {
                ReflashDeviceListView(DevList);
                return true;
            }
            else
            {
                //异常处理；
                string errMsg = ErrorHandling.HandleError(eCode);
                System.Windows.Forms.MessageBox.Show(errMsg);
                return false;
            }
        }

        
        private void searchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //使用广播搜索设备；
            SearchDevice(DeviceClass.Broadcast);
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearDeviceListView();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //关闭主窗体并退出程序；
            this.Close();
        }

        private void iEToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //开启IE访问目标设备；
            try
            {
                if (DeviceListView.SelectedIndices.Count > 0
                    && DeviceListView.SelectedIndices[0] != -1)
                {
                    DeviceClass currentDevice = DevList[DeviceListView.SelectedIndices[0]];
                    System.Diagnostics.Process.Start("iexplore.exe", "HTTP://" + getIPAddress(currentDevice.DeviceIP).ToString());
                }
            }
            catch (Exception ex)
            {
                Log.WriteException(ex);
            }
        }

     

        private void button20_Click_1(object sender, EventArgs e)
        {
            byte num = (byte)com_relay_num.SelectedIndex;
            byte status = (byte)com_relay_state.SelectedIndex;
            byte ntime = (byte)com_relay_time.SelectedIndex;
            fCmdRet = StaticClassReaderB.RelayControll(ref fComAdr, num, status, ntime, frmcomportindex);
            AddCmdLog("SetSerialNo", "Set", fCmdRet);
        }

        private void btOutputRep_Click(object sender, EventArgs e)
        {
            byte OutputPin = 0;
            fCmdRet = StaticClassReaderB.GetGPIOStatus(ref fComAdr, ref OutputPin, frmcomportindex);
            if (fCmdRet == 0)
            {
                if ((OutputPin & 0x01) == 0x01)
                    check_IN1.Checked = true;
                else
                    check_IN1.Checked = false;

                if ((OutputPin & 0x02) == 0x02)
                    check_IN2.Checked = true;
                else
                    check_IN2.Checked = false;

                if ((OutputPin & 0x04) == 0x04)
                    check_IN3.Checked = true;
                else
                    check_IN3.Checked = false;

                if ((OutputPin & 0x08) == 0x08)
                    check_IN4.Checked = true;
                else
                    check_IN4.Checked = false;
            }
            AddCmdLog("GetGPIOStatus", "Get", fCmdRet);

        }

        private void btUpdate_Click(object sender, EventArgs e)
        {
            updateForm up = new updateForm();
            up.ShowDialog();
        }

        private void button21_Click(object sender, EventArgs e)
        {
            try
            {
                if (ListView1_EPC.Items.Count == 0)
                {
                    MessageBox.Show("No information to export");
                    return;
                }

                string path = System.Windows.Forms.Application.StartupPath;
                Application.DoEvents();
                saveFileDialog1.Title = "Label Export";
                saveFileDialog1.Filter = "Excel(*.xls)|*.xls";
                saveFileDialog1.FileName = string.Format("Label info--{0}", DateTime.Now.ToString("yyyyMMddhhmmss"));
                DialogResult result = saveFileDialog1.ShowDialog();
                if (result == DialogResult.OK)
                {
                    Application.DoEvents();
                    Thread.Sleep(1000);
                    Microsoft.Office.Interop.Excel._Application xlapp = new Microsoft.Office.Interop.Excel.Application();
                    Microsoft.Office.Interop.Excel.Workbook xlbook = xlapp.Workbooks.Add(true);
                    Microsoft.Office.Interop.Excel.Worksheet xlsheet = (Microsoft.Office.Interop.Excel.Worksheet)xlbook.Worksheets[1];
                    int colIndex = 0;
                    int RowIndex = 1;
                    string headInfo = "No., EPC, Length, Times, RSSI";
                    //开始写入每列的标题
                    foreach (string s in headInfo.Split(','))
                    {
                        colIndex++;
                        xlsheet.Cells[RowIndex, colIndex] = s;
                    }

                    xlsheet.Cells.NumberFormatLocal = "@";
                    //开始写入内容 
                    int RowCount = ListView1_EPC.Items.Count;//行数
                    // int count = 0;
                    for (int i = 0; i < RowCount; i++)
                    {
                        RowIndex++;
                        xlsheet.Cells[RowIndex, 1] = ListView1_EPC.Items[i].SubItems[0].Text;
                        xlsheet.Cells[RowIndex, 2] = ListView1_EPC.Items[i].SubItems[1].Text;
                        xlsheet.Cells[RowIndex, 3] = ListView1_EPC.Items[i].SubItems[2].Text;
                        xlsheet.Cells[RowIndex, 4] = ListView1_EPC.Items[i].SubItems[3].Text;
                        xlsheet.Cells[RowIndex, 5] = ListView1_EPC.Items[i].SubItems[4].Text;
                        Application.DoEvents();
                    }
                    xlbook.Saved = true;
                    xlbook.SaveCopyAs(saveFileDialog1.FileName);
                    xlapp.Quit();
                    GC.Collect();
                    MessageBox.Show("Export Successful！", "Message Prompt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch
            {
                MessageBox.Show("Export Failed！", "Message Prompt", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void label49_Click(object sender, EventArgs e)
        {

        }

        private void btSetPwd_Click(object sender, EventArgs e)
        {
            byte[] PSW = new byte[4];
            if (txt_password.Text.Length != 8) return;
            PSW = HexStringToByteArray(txt_password.Text);
            fCmdRet = StaticClassReaderB.SetPSW(ref fComAdr, PSW, frmcomportindex);
            AddCmdLog("SetPSW", "Set Access Password for Active - mode Tag", fCmdRet);
        }

        private void btGetPwd_Click(object sender, EventArgs e)
        {
            byte[] PSW = new byte[4];
            txt_password.Text = "";
            fCmdRet = StaticClassReaderB.GetPSW(ref fComAdr, PSW, frmcomportindex);
            if (fCmdRet==0)
            {
                txt_password.Text = ByteArrayToHexString(PSW);
            }
            AddCmdLog("SetPSW", "Read Access Password for Active-mode Tag", fCmdRet);
        }
    }
}