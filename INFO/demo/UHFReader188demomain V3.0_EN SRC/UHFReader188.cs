using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
namespace ReaderB
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RFIDTag
    {
        public byte PacketParam;
        public byte LEN;
        public string UID;
        public int phase_begin;
        public int phase_end;
        public byte RSSI;
        public byte ANT;
        public Int32 Handles;
    }

    public delegate void RFIDCallBack(IntPtr p, Int32 nEvt);


    public static class StaticClassReaderB
    {
        private const string DLLNAME = @"UHFReader188.dll";

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern void InitRFIDCallBack(RFIDCallBack t, bool uidBack, int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int OpenNetPort(int Port,
                                             string IPaddr,
                                             ref byte ComAddr,
                                             ref int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CloseNetPort(int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int OpenComPort(int Port,
                                                 ref byte ComAddr,
                                                 byte Baud,
                                                 ref int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CloseComPort();

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int AutoOpenComPort(ref int Port,
                                                 ref byte ComAddr,
                                                 byte Baud,
                                                 ref int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CloseSpecComPort(int Port);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetReaderInformation(ref byte ConAddr,
                                                      byte[] VersionInfo,
                                                      ref byte ReaderType,
                                                      byte[] TrType,
                                                      ref byte dmaxfre,
                                                      ref byte dminfre,
                                                      ref byte powerdBm,
                                                      ref byte ScanTime,                                                 
                                                      int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int WriteComAdr(ref byte ConAddr,
                                                      ref byte ComAdrData,
                                                      int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetPowerDbm(ref byte ConAddr,
                                             byte powerDbm,
                                             int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int Writedfre(ref byte ConAddr,
                                           ref byte dmaxfre,
                                           ref byte dminfre,
                                             int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int Writebaud(ref byte ConAddr,
                                           ref byte baud,
                                           int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int WriteScanTime(ref byte ConAddr,
                                               ref byte ScanTime,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int InSelfTestMode(ref byte ConAddr,
                                                bool IsSelfTestMode,
                                                int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int RfOutput(ref byte ConAddr,
                                          byte onoff,
                                                int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetPWM(ref byte ConAddr,
                                          byte PWM,
                                                int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ReadPWM(ref byte ConAddr,
                                         ref byte PWM,
                                                int PortHandle);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetPSW(ref byte ConAddr,
                                        byte[] PSW,
                                        int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetPSW(ref byte ConAddr,
                                        byte[] PSW,
                                        int PortHandle);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetPowerParameter(ref byte ConAddr,
                                                   ref byte power,
                                                   int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int Getpower(ref byte ConAddr,
                                          ref byte power,
                                          int PortHandle);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CheckPowerParameter(ref byte ConAddr,
                                                     ref int code,
                                                     int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetStartInformation(ref byte ConAddr,
                                                     ref byte ADF7020E,
                                                     ref byte FreE,
                                                     ref byte addrE,
                                                     ref byte scnE,
                                                     ref byte xpwrE,
                                                     ref byte pwmE,
                                                     int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SolidifyPWMandPowerlist(ref byte ConAddr,
                                                         byte[] dBm_list,
                                                         ref int code,
                                                         int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int Inventory_G2(ref byte ConAddr,
                                              byte Qvalue,
							                  byte Session,
                                              byte AdrTID,
							                  byte LenTID,
							                  byte TIDFlag,
                                              byte[] EPClenandEPC,
                                              ref int Totallen,
                                              ref int CardNum,
                                              int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ReadCard_G2(ref byte ConAddr,
                                              byte[] EPC,
                                              byte Mem,
                                              byte WordPtr,
                                              byte Num,
                                              byte[] Password,
                                              byte maskadr,
                                              byte maskLen,
                                              byte maskFlag,
                                              byte[] Data,
                                              byte EPClength,
                                              ref int errorcode,
                                              int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int WriteCard_G2(ref byte ConAddr,
                                              byte[] EPC,
                                              byte Mem,
                                              byte WordPtr,
                                              byte Writedatalen,
                                              byte[] Writedata,
                                              byte[] Password,
                                              byte maskadr,
                                              byte maskLen,
                                              byte maskFlag,
                                              int WrittenDataNum,
                                              byte EPClength,
                                              ref int errorcode,
                                              int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int WriteBlock_G2(ref byte ConAddr,
                                              byte[] EPC,
                                              byte Mem,
                                              byte WordPtr,
                                              byte Writedatalen,
                                              byte[] Writedata,
                                              byte[] Password,
                                              byte maskadr,
                                              byte maskLen,
                                              byte maskFlag,
                                              int WrittenDataNum,
                                              byte EPClength,
                                              ref int errorcode,
                                              int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int EraseCard_G2(ref byte ConAddr,
                                              byte[] EPC,
                                              byte Mem,
                                              byte WordPtr,
                                              byte Num,
                                              byte[] Password,
                                                byte maskadr,
                                                  byte maskLen,
                                                  byte maskFlag,
                                              byte EPClength,
                                              ref int errorcode,
                                              int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetCardProtect_G2(ref byte ConAddr,
                                              byte[] EPC,
                                              byte select,
                                              byte setprotect,
                                              byte[] Password,
                                                byte maskadr,
                                                  byte maskLen,
                                                  byte maskFlag,
                                              byte EPClength,
                                              ref int errorcode,
                                              int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int DestroyCard_G2(ref byte ConAddr,
                                              byte[] EPC,
                                              byte[] Password,
                                                byte maskadr,
                                                  byte maskLen,
                                                  byte maskFlag,
                                              byte EPClength,
                                              ref int errorcode,
                                              int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int WriteEPC_G2(ref byte ConAddr,
                                              byte[] Password,
                                              byte[] WriteEPC,
                                              byte WriteEPClen,
                                              ref int errorcode,
                                              int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetReadProtect_G2(ref byte ConAddr,
                                              byte[] EPC,
                                              byte[] Password,
                                                 byte maskadr,
                                                  byte maskLen,
                                                  byte maskFlag,
                                              byte EPClength,
                                              ref int errorcode,
                                              int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetMultiReadProtect_G2(ref byte ConAddr,
                                              byte[] Password,
                                              ref int errorcode,
                                              int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int RemoveReadProtect_G2(ref byte ConAddr,
                                              byte[] Password,
                                              ref int errorcode,
                                              int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CheckReadProtected_G2(ref byte ConAddr,
                                              ref byte readpro,
                                              ref int errorcode,
                                              int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetEASAlarm_G2(ref byte ConAddr,
                                               byte[] EPC,
                                               byte[] Password,
                                                byte maskadr,
                                                  byte maskLen,
                                                  byte maskFlag,
                                               byte EAS,
                                               byte EPClength,
                                              ref int errorcode,
                                              int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CheckEASAlarm_G2(ref byte ConAddr,
                                              ref int errorcode,
                                              int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int LockUserBlock_G2(ref byte ConAddr,
                                                  byte[] EPC,
                                                  byte[] Password,
                                                     byte maskadr,
                                                  byte maskLen,
                                                  byte maskFlag,
                                                  byte BlockNum,
                                                  byte EPClength,
                                                  ref int errorcode,
                                                  int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int Inventory_6B(ref byte ConAddr,
                                                  byte[] ID_6B,
                                                  int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int inventory2_6B(ref byte ConAddr,
                                               byte Condition,
                                               byte StartAddress,
                                               byte mask,
                                               byte[] ConditionContent,
                                               byte[] ID_6B,
                                               ref int Cardnum,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ReadCard_6B(ref byte ConAddr,
                                               byte[] ID_6B,
                                               byte StartAddress,
                                               byte Num,
                                               byte[] Data,
                                               ref int errorcode,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int WriteCard_6B(ref byte ConAddr,
                                               byte[] ID_6B,
                                               byte StartAddress,
                                               byte[] Writedata,
                                               byte Writedatalen,
                                               ref int writtenbyte,
                                               ref int errorcode,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int LockByte_6B(ref byte ConAddr,
                                               byte[] ID_6B,
                                               byte Address,
                                               ref int errorcode,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CheckLock_6B(ref byte ConAddr,
                                               byte[] ID_6B,
                                               byte Address,
                                               ref byte ReLockState,
                                               ref int errorcode,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetWGParameter(ref byte ConAddr,
                                               byte Wg_mode,
                                               byte Wg_Data_Inteval,
                                               byte Wg_Pulse_Width,
                                               byte Wg_Pulse_Inteval,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetWorkMode(ref byte ConAddr,
                                             byte[] Parameter,                                            
                                             int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetWorkModeParameter(ref byte ConAddr,
                                             byte[] Parameter,
                                             int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ReadActiveModeData(byte[] ModeData,
                                                     ref int Datalength,
                                                     int PortHandle);

        /* [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int SetAccuracy(ref byte ConAddr,
                                                     byte Accuracy,
                                                      int PortHandle);

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int SetOffsetTime(ref byte ConAddr,
                                                     byte OffsetTime,
                                                      int PortHandle);

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int SetFhssMode(ref byte ConAddr,
                                              byte FhssMode,
                                              int PortHandle);

         [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
         public static extern int GetFhssMode(ref byte ConAddr,
                                              ref byte FhssMode,
                                              int PortHandle);*/

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetTriggerTime(ref byte ConAddr,
                                                ref byte TriggerTime,
                                                int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int BuzzerAndLEDControl(ref byte ConAddr,
                                                    byte AvtiveTime,
                                                    byte SilentTime,
                                                    byte Times,
                                                    int FrmHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetRelay(ref byte ConAddr,
                                                byte RelayStatus,
                                                int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetAntenna(ref byte ConAddr,
                                                byte Ant_Mode,
                                                byte Ant_SWTcnt,
                                                byte AntInfoEn,
                                                int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetQvalue(ref byte ConAddr,
                                                byte Qvalue,
                                                int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetAntenna(ref byte ConAddr,
                                            ref byte Ant_No,
                                            int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetQandAntenna(ref byte ConAddr,
                                                ref byte Qvalue,
                                                ref byte Ant_Mode,
                                                ref byte Ant_SWTcnt,
                                                ref byte AntInfoEn,
                                                int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetQS(ref byte ConAddr,
                                        byte Qvalue,
                                        byte Session,
                                        int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetQS(ref byte ConAddr,
                                       ref byte Qvalue,
                                       ref byte Session,
                                       int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetSerialNo(ref byte ConAddr,
                                        byte[] SerialNo,
                                       int PortHandle);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetTagCustomFunction(ref byte ConAddr,
                                               ref byte InlayType,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetMonza4QTWorkParamter_G2(ref byte ComAdr,
                                             byte[] EPC,
                                             byte ENum,
                                             byte[] Password,
                                             byte MaskMem,
                                             byte[] MaskAdr,
                                             byte MaskLen,
                                             byte[] MaskData,
                                             ref byte QTcontrol,
                                             ref int errorcode,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetMonza4QTWorkParamter_G2(ref byte ComAdr,
                                              byte[] EPC,
                                              byte ENum,
                                              byte QTcontrol,
                                              byte[] Password,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              ref int errorcode,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetUserPwd(ref byte ConAddr,
                                               byte[] UserPwd,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetUserPwd(ref byte ConAddr,
                                               byte[] UserPwd,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetMacAddr(ref byte ConAddr,
                                               byte[] MacAddr,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetMacAddr(ref byte ConAddr,
                                               byte[] MacAddr,
                                               int PortHandle);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ReadData_G2(ref byte ComAdr,
                                             byte[] EPC,
                                             byte ENum,
                                             byte Mem,
                                             byte WordPtr,
                                             byte Num,
                                             byte[] Password,
                                             byte MaskMem,
                                             byte[] MaskAdr,
                                             byte MaskLen,
                                             byte[] MaskData,
                                             byte[] Data,
                                             ref int errorcode,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int WriteData_G2(ref byte ComAdr,
                                              byte[] EPC,
                                              byte WNum,
                                              byte ENum,
                                              byte Mem,
                                              byte WordPtr,
                                              byte[] Wdt,
                                              byte[] Password,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              ref int errorcode,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetRelayTime(ref byte ConAddr,
                                               byte RelayTime,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetRelayTime(ref byte ConAddr,
                                               ref byte RelayTime,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetCfgParameter(ref byte ComAdr,
                                             byte opt,
                                             byte cfgNo, byte[] cfgData, int len,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetCfgParameter(ref byte ComAdr,
                                             byte cfgNo, byte[] cfgData, ref int len,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SearchWIFI(ref byte ComAdr,
                                              byte[] APInfo, ref int length,ref int Count,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ResetReader(ref byte ComAdr,int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int RelayControll(ref byte ComAdr,byte num,byte status,byte ntime, int frmComPortindex);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetRJ45MacAddr(ref byte ComAdr, byte[] mac, int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetWIFIMacAddr(ref byte ComAdr, byte[] mac, int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetBluetoothMacAddr(ref byte ComAdr, byte[] mac, int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetBleMacAddr(ref byte ComAdr, byte[] mac, int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ReadRJ45ConnectStatus(ref byte ComAdr, ref byte rj45state,byte[] localIP,byte[] subnet,byte[] gatewayIp, int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ReadWIFIConnectStatus(ref byte ComAdr, ref byte WIFIstate, byte[] localIP, byte[] subnet, byte[] gatewayIp, int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetGPIOStatus(ref byte ComAdr, ref byte pinstate, int frmComPortindex);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ChangeToUpdateMode(ref byte ComAdr, int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int WriteBinData(ref byte ComAdr,
                                              byte Index,
                                              byte len,
                                              byte[] data,
                                             int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CheckCRC32(ref byte ComAdr,
                                            byte[] crc32,
                                             int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int WriteMark(ref byte ComAdr,
                                             int PortHandle);


        public static int SetNetWorkIP(ref byte ComAddr, byte[] ipAddr, byte[] SubnetMask, byte[] wgAddr, byte dhcp,int cfgType, int frmComPortindex)
        {
            byte CFGNo = 50;
            if (cfgType == 1) CFGNo = 54;
            byte CFGLen = 16;
            byte opt = 0;
            byte[] CFGData = new byte[CFGLen];
            Array.Copy(ipAddr, 0, CFGData, 0, 4);
            Array.Copy(SubnetMask, 0, CFGData, 4, 4);
            Array.Copy(wgAddr, 0, CFGData, 8, 4);
            CFGData[12] = dhcp;
            return SetCfgParameter(ref ComAddr, opt, CFGNo, CFGData, CFGLen, frmComPortindex);
        }

        public static int GetNetWorkIP(ref byte ComAddr, byte[] ipAddr, byte[] SubnetMask, byte[] wgAddr, ref byte dhcp, int cfgType, int frmComPortindex)
        {
            byte CFGNo = 50;
            if (cfgType == 1) CFGNo = 54;
            int CFGLen = 255;
            byte[] CFGData = new byte[CFGLen];
            int result = GetCfgParameter(ref ComAddr, CFGNo, CFGData, ref CFGLen, frmComPortindex);
            if (result == 0)
            {
                Array.Copy(CFGData, 0, ipAddr, 0, 4);
                Array.Copy(CFGData, 4, SubnetMask, 0, 4);
                Array.Copy(CFGData, 8, wgAddr, 0, 4);
                dhcp = CFGData[12];
            }
            return result;
        }

        public static int SetNetWorkConnection(ref byte ComAddr, byte[] SvripAddr, int SvrPOrt, byte ConnectionMode, int ClientPort, int cfgType, int frmComPortindex)
        {
            byte CFGNo = 51;
            if (cfgType == 1) CFGNo = 55;
            byte CFGLen = 9;
            byte opt = 0;
            byte[] CFGData = new byte[CFGLen];
            Array.Copy(SvripAddr, 0, CFGData, 0, 4);
            CFGData[4] = (byte)(SvrPOrt >> 8);
            CFGData[5] = (byte)(SvrPOrt & 0x00FF);
            CFGData[6] = (byte)(ClientPort >> 8);
            CFGData[7] = (byte)(ClientPort & 0x00FF);
            CFGData[8] = ConnectionMode;
            return SetCfgParameter(ref ComAddr, opt, CFGNo, CFGData, CFGLen, frmComPortindex);
        }

        public static int GetNetWorkConnection(ref byte ComAddr, byte[] SvripAddr, ref int SvrPOrt, ref byte ConnectionMode, ref int ClientPort, int cfgType, int frmComPortindex)
        {
            byte CFGNo = 51;
            if (cfgType == 1) CFGNo = 55;
            int CFGLen = 255;
            byte[] CFGData = new byte[CFGLen];
            int result = GetCfgParameter(ref ComAddr, CFGNo, CFGData, ref CFGLen, frmComPortindex);
            if (result == 0)
            {
                Array.Copy(CFGData, 0, SvripAddr, 0, 4);
                SvrPOrt = CFGData[4] * 256 + CFGData[5];
                ClientPort = CFGData[6] * 256 + CFGData[7];
                ConnectionMode = CFGData[8];
            }
            return result;
        }

        public static int SetReconnect(ref byte ComAddr, byte TCPReconnectMode, int cfgType, int frmComPortindex)
        {
            byte CFGNo = 52;
            if (cfgType == 1) CFGNo = 56;
            byte CFGLen = 4;
            byte opt = 0;
            byte[] CFGData = new byte[CFGLen];
            CFGData[0] = (byte)0;
            CFGData[1] = (byte)0;
            CFGData[2] = TCPReconnectMode;
            CFGData[3] = 0;
            return SetCfgParameter(ref ComAddr, opt, CFGNo, CFGData, CFGLen, frmComPortindex);
        }

        public static int GetReconnect(ref byte ComAddr, ref byte TCPReconnectMode, int cfgType, int frmComPortindex)
        {
            byte CFGNo = 52;
            if (cfgType == 1) CFGNo = 56;
            int CFGLen = 255;
            byte[] CFGData = new byte[CFGLen];
            int result = GetCfgParameter(ref ComAddr, CFGNo, CFGData, ref CFGLen, frmComPortindex);
            if (result == 0)
            {
                TCPReconnectMode = CFGData[2];
            }
            return result;
        }

        public static int SetKeepAlive(ref byte ComAddr, byte keepaliveen, int keepalivetime, int cfgType, int frmComPortindex)
        {
            byte CFGNo = 53;
            if (cfgType == 1) CFGNo = 57;
            byte CFGLen = 8;
            byte opt = 0;
            byte[] CFGData = new byte[CFGLen];
            CFGData[0] = (byte)0;
            CFGData[1] = (byte)0;
            CFGData[2] = 0;
            CFGData[3] = 0;
            CFGData[4] = keepaliveen;
            CFGData[5] = 0;
            CFGData[6] = (byte)(keepalivetime>>8);
            CFGData[7] = (byte)(keepalivetime&255);
            return SetCfgParameter(ref ComAddr, opt, CFGNo, CFGData, CFGLen, frmComPortindex);
        }

        public static int GetKeepAlive(ref byte ComAddr,ref byte keepaliveen,ref int keepalivetime, int cfgType, int frmComPortindex)
        {
            byte CFGNo = 53;
            if (cfgType == 1) CFGNo = 57;
            int CFGLen = 255;
            byte[] CFGData = new byte[CFGLen];
            int result = GetCfgParameter(ref ComAddr, CFGNo, CFGData, ref CFGLen, frmComPortindex);
            if (result == 0)
            {
                keepaliveen = CFGData[4];
                keepalivetime = CFGData[6] * 256 + CFGData[7];
            }
            return result;
        }

        public static int SetAPConnectName_Pwd(ref byte ComAddr, byte ssidLen, byte[]ssid, byte pwdLen,byte[]Pwd,byte[]MAC, int frmComPortindex)
        {
            byte CFGNo = 58;
            byte CFGLen = (byte)(8+ ssidLen+ pwdLen);
            byte opt = 0;
            byte[] CFGData = new byte[CFGLen];
            CFGData[0] = (byte)ssidLen;
            Array.Copy(ssid, 0, CFGData, 1, ssidLen);
            CFGData[ssidLen+1] = pwdLen;
            Array.Copy(Pwd, 0, CFGData, ssidLen+2, pwdLen);
            Array.Copy(MAC, 0, CFGData, ssidLen+ pwdLen+2, 6);
            return SetCfgParameter(ref ComAddr, opt, CFGNo, CFGData, CFGLen, frmComPortindex);
        }

        public static int GetAPConnectName_Pwd(ref byte ComAddr,ref byte ssidLen, byte[] ssid,ref byte pwdLen, byte[] Pwd, byte[] MAC, int frmComPortindex)
        {
            byte CFGNo = 58;
            int CFGLen = 255;
            byte[] CFGData = new byte[CFGLen];
            int result = GetCfgParameter(ref ComAddr, CFGNo, CFGData, ref CFGLen, frmComPortindex);
            if (result == 0)
            {
                ssidLen = CFGData[0];
                if(ssidLen>0)
                    Array.Copy(CFGData, 1, ssid, 0, ssidLen);
                pwdLen = CFGData[1+ ssidLen];
                if(pwdLen>0)
                    Array.Copy(CFGData, 2+ ssidLen, Pwd, 0, pwdLen);
                Array.Copy(CFGData, 2 + ssidLen+ pwdLen, MAC, 0, 6);
            }
            return result;
        }


        public static int SetBluetoothNme(ref byte ComAddr, byte namelen, byte[] BTName, int frmComPortindex)
        {
            byte CFGNo = 60;
            byte CFGLen = (byte)(1 + namelen);
            byte opt = 0;
            byte[] CFGData = new byte[CFGLen];
            CFGData[0] = (byte)namelen;
            Array.Copy(BTName, 0, CFGData, 1, namelen);
           
            return SetCfgParameter(ref ComAddr, opt, CFGNo, CFGData, CFGLen, frmComPortindex);
        }

        public static int GetBluetoothNme(ref byte ComAddr, ref byte namelen, byte[] BTName, int frmComPortindex)
        {
            byte CFGNo = 60;
            int CFGLen = 255;
            byte[] CFGData = new byte[CFGLen];
            int result = GetCfgParameter(ref ComAddr, CFGNo, CFGData, ref CFGLen, frmComPortindex);
            if (result == 0)
            {
                namelen = CFGData[0];
                if (namelen > 0)
                    Array.Copy(CFGData, 1, BTName, 0, namelen);
            }
            return result;
        }


        public static int SetBleNme(ref byte ComAddr, byte namelen, byte[] BTName, int frmComPortindex)
        {
            byte CFGNo = 61;
            byte CFGLen = (byte)(1 + namelen);
            byte opt = 0;
            byte[] CFGData = new byte[CFGLen];
            CFGData[0] = (byte)namelen;
            Array.Copy(BTName, 0, CFGData, 1, namelen);

            return SetCfgParameter(ref ComAddr, opt, CFGNo, CFGData, CFGLen, frmComPortindex);
        }

        public static int GetBleNme(ref byte ComAddr, ref byte namelen, byte[] BTName, int frmComPortindex)
        {
            byte CFGNo = 61;
            int CFGLen = 255;
            byte[] CFGData = new byte[CFGLen];
            int result = GetCfgParameter(ref ComAddr, CFGNo, CFGData, ref CFGLen, frmComPortindex);
            if (result == 0)
            {
                namelen = CFGData[0];
                if (namelen > 0)
                    Array.Copy(CFGData, 1, BTName, 0, namelen);
            }
            return result;
        }

        public static int SetCommunicationSwitch(ref byte ComAddr,int connIntf, int frmComPortindex)
        {
            byte CFGNo = 62;
            byte CFGLen = 2;
            byte opt = 0;
            byte[] CFGData = new byte[2];
            CFGData[0] = (byte)(connIntf>>8);
            CFGData[1] = (byte)(connIntf);
            return SetCfgParameter(ref ComAddr, opt, CFGNo, CFGData, CFGLen, frmComPortindex);
        }

        public static int GetCommunicationSwitch(ref byte ComAddr, ref int connIntf, int frmComPortindex)
        {
            byte CFGNo = 62;
            int CFGLen = 255;
            byte[] CFGData = new byte[CFGLen];
            int result = GetCfgParameter(ref ComAddr, CFGNo, CFGData, ref CFGLen, frmComPortindex);
            if (result == 0)
            {
                connIntf = (CFGData[0] << 8) + CFGData[1];
            }
            return result;
        }

        public static int SetMatchRule(ref byte ComAddr, byte matchen,byte matchopt,byte matcgEPCLen,byte matchAddr,byte matchLen,byte[]matchData, int frmComPortindex)
        {
            byte CFGNo = 70;
            byte maskByte = (byte)((matchLen + 7) / 8);
            byte CFGLen = (byte)(5 + maskByte);
            byte opt = 0;
            byte[] CFGData = new byte[CFGLen];
            CFGData[0] = matchen;
            CFGData[1] = matchopt;
            CFGData[2] = matcgEPCLen;
            CFGData[3] = matchAddr;
            CFGData[4] = matchLen;
            if(matchLen > 0)
                Array.Copy(matchData, 0, CFGData, 5, maskByte);
            return SetCfgParameter(ref ComAddr, opt, CFGNo, CFGData, CFGLen, frmComPortindex);
        }

        public static int GetMatchRule(ref byte ComAddr,ref byte matchen, ref byte matchopt,ref byte matcgEPCLen, ref byte matchAddr, ref byte matchLen, byte[] matchData, int frmComPortindex)
        {
            byte CFGNo = 70;
            int CFGLen = 255;
            byte[] CFGData = new byte[CFGLen];
            int result = GetCfgParameter(ref ComAddr, CFGNo, CFGData, ref CFGLen, frmComPortindex);
            if (result == 0)
            {
                matchen = CFGData[0];
                matchopt = CFGData[1];
                matcgEPCLen = CFGData[2];
                matchAddr = CFGData[3];
                matchLen = CFGData[4];
                if(matchLen>0)
                    Array.Copy(CFGData,5, matchData,0, (matchLen+7)/8);
            }
            return result;
        }

        public static int SetRelayAction(ref byte ComAddr, byte RelayEn, byte ActionTime,  int frmComPortindex)
        {
            byte CFGNo = 71;
            byte CFGLen = 2;
            byte opt = 0;
            byte[] CFGData = new byte[2];
            CFGData[0] = RelayEn;
            CFGData[1] = ActionTime;
            return SetCfgParameter(ref ComAddr, opt, CFGNo, CFGData, CFGLen, frmComPortindex);
        }

        public static int GetRelayAction(ref byte ComAddr,ref byte RelayEn,ref byte ActionTime, int frmComPortindex)
        {
            byte CFGNo = 71;
            int CFGLen = 255;
            byte[] CFGData = new byte[CFGLen];
            int result = GetCfgParameter(ref ComAddr, CFGNo, CFGData, ref CFGLen, frmComPortindex);
            if (result == 0)
            {
                RelayEn = CFGData[0];
                ActionTime = CFGData[1];
            }
            return result;
        }


    }
}
