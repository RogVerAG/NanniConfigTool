using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Canlib;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Windows.Forms.AxHost;
using static Canlib.CanlibAPI;

namespace Nanni_ScreenConfigurator
{
    internal class CAN
    {
        #region Global_Variables
        enum Tool_CAN_State {
            Log,
            DeviceList,
            Analyzer,
            MessageViewer,
            NanniConfigurator,
            UNDEFINED
        };
        private const int veratron_ErrorCode = -44;
        private Tool_CAN_State ToolCanState;
        private int ToolAddress = 0xFE;
        private int CanHandle = 0xFF;
        private bool CanIsRunning = false;
        private bool CanConnectionIsOpen = false;
        private Thread? thread_CanMsgPolling;                    // Instance of polling thread
        public bool MessageCount_Enable = false;

        // DeviceList-Variables
        private Dictionary<int, t_ProductInformation> DevList = new ();
        private Dictionary<int, int> TopSeqNrs_ProdInfo = new ();               // Required for MultiFrame Message
        private Dictionary<int, List<byte>> ProdInfo_Buffers = new ();   // Required for MultiFrame Message
        public bool flg_newDeviceFound = false;

        //Variables for Nanni Tool
        private int UDS_TX = 0x7F2;
        private int UDS_RX = 0x7FA;
        private bool ExtendDiagAnswer = false;
        private bool FrameAnswer = false;
        private bool ConfigAcknowledgement = false;

        #endregion

        #region General_CAN_Functions
        public canStatus InitializeCAN()
        {
            if (CanConnectionIsOpen == true)
            {
                return (canStatus)veratron_ErrorCode;
            }
            CanlibAPI.canInitializeLibrary();                                                          // doesn't matter when called several times
            CanHandle = CanlibAPI.canOpenChannel(0, CanlibAPI.canOPEN_EXCLUSIVE); 
            CanlibAPI.canStatus status = CanlibAPI.canSetBusParams(CanHandle, 250000, 4, 3, 1, 1, 0);  // Bitrate=250000 | ?=4 | ?=3 | ?=1 | SJW=1 0
            if (status < 0)         //Error-Codes < 0
            {
                return status;      // Error on Interface
            }
            status = CanlibAPI.canBusOn(CanHandle);     // Takes the specified channel on-bus
            if (status != CanlibAPI.canStatus.canOK)
            {
                // no connection possible
                CanlibAPI.canClose(CanHandle);
            }
            else
            {
                CanConnectionIsOpen = true;             // set flag
                start_CanPollingThread();
            }
            return status;
        }

        public void TerminatePollingThread()
        {
            CanIsRunning = false;
        }

        public void TerminateCanBusConnection()
        {
            if (CanIsRunning)
            {
                TerminatePollingThread();
            }
            CanlibAPI.canClose(CanHandle);
            ToolCanState = Tool_CAN_State.UNDEFINED;
            CanConnectionIsOpen = false;
        }

        public bool start_CanPollingThread()
        {
            //start continuous CAN-polling in new thread
            thread_CanMsgPolling = new Thread(new ThreadStart(Poll_CanMsg));    // create instance of a new thread
            if (!CanIsRunning)
            {
                CanIsRunning = true;
                thread_CanMsgPolling.Start();
                return true;
            }
            else
            {
                return false;
            }
        }

        private void Poll_CanMsg()
        {
            while (CanIsRunning)
            {
                ReadCan();
            }
        }

        private byte getSourceAdr(int id)
        {
            return (byte)id;
        }

        private int getPgn(int id)
        {
            return ((id & 0x03FFFFFF) >> 8);
        }

        private void ReadCan()
        {
            uint dlc, flags, timestamp;
            int id;
            byte[] data = new byte[8];
            canStatus state = CanlibAPI.canRead(CanHandle, out id, data, out dlc, out flags, out timestamp);

            byte Rx_SrcAdr = getSourceAdr(id);
            int Rx_Pgn = getPgn(id);

            if (state == canStatus.canOK)   // prevents reading, when no messages are available ( CanNoMsg_Err )
            {
                switch (ToolCanState)
                {
                    case Tool_CAN_State.DeviceList:
                        ProcessDevListMsg(Rx_SrcAdr, Rx_Pgn, data);
                        break;

                    case Tool_CAN_State.NanniConfigurator:
                        if(id == UDS_RX)
                        {
                            if (data[0] == 0x02 && data[1] == 0x50 && data[2] == 0x03)
                            {
                                // UDS extended anwer
                                ExtendDiagAnswer = true;    //ExtendDiagReq. acknowledged -> set flag -> value will be polled via getExtDiagAnswer()
                            }
                            else if (data[0] == 0x30 && data[2] == 0x14)
                            {
                                // consecutive frames ansewr
                                FrameAnswer = true;
                            }
                            else if (data[0] == 0x03 && data[1] == 0x6E && data[2] == 0x01 && data[3] == 0x2C)
                            {
                                // all written
                                ConfigAcknowledgement = true;
                            }
                        }
                        break;
                }
            }
        }
        #endregion

        #region DeviceList_Functions
        public canStatus Request_DeviceListUpdate()
        {
            DevList.Clear();                                // Delete current Device list
            canStatus state = Request_ISOAdrClaim();        // send new request (0xEA00)
            if (state != canStatus.canOK)
            {
                return state;
            }
            state = Request_ProductInfo();                  // send new request (0x1F014)
            if (state != canStatus.canOK)
            {
                return state;
            }
            return canStatus.canOK;
        }

        public canStatus Request_ISOAdrClaim()
        {
            int DestAddr = 0xFF;
            int id = 0x18EA0000;
            id += DestAddr << 8;
            id += ToolAddress;
            int requestedPGN = 0xEE00;
            byte[] data = new byte[8];
            data[0] = (byte)requestedPGN;
            data[1] = (byte)(requestedPGN >> 8);
            data[2] = (byte)(requestedPGN >> 16);
            uint dlc = (uint)data.Length;
            canStatus status = CanlibAPI.canWrite(CanHandle, id, data, dlc, CanlibAPI.canMSG_EXT);
            return status;
        }

        public canStatus Request_ProductInfo()
        {
            int ucDestAddr = 0xFF;
            canStatus state;
            int id = 0x18EA0000;
            id += ucDestAddr << 8;
            id += ToolAddress;
            int requestedPGN = 0x1F014;
            byte[] data = new byte[8];
            data[0] = (byte)requestedPGN;
            data[1] = (byte)(requestedPGN >> 8);
            data[2] = (byte)(requestedPGN >> 16);
            uint dlc = (uint)data.Length;
            state = CanlibAPI.canWrite(CanHandle, id, data, dlc, CanlibAPI.canMSG_EXT);
            return state;
        }

        private void ProcessDevListMsg(byte SrcAdr, int Pgn, byte[] data)
        {
            switch(Pgn)
            {
                case 0x0EEFF:
                    Process_0xEE00(data, SrcAdr);
                    break;
                case 0x1F014:
                    Process_0x1F014(data, SrcAdr);
                    break;
            }
        }

        private void Process_0xEE00(byte[] data, byte SrcAdr)
        {
            if (!DevList.ContainsKey(SrcAdr))
            {
                // Create required containers for new device
                DevList.Add(SrcAdr, new t_ProductInformation(SrcAdr) { });
            }
            DevList[SrcAdr].ManufactCode = (UInt16)((data[2] >> 5) | (data[3] << 3));
            DevList[SrcAdr].DeviceClass = (UInt16)(data[6] >> 1);
            DevList[SrcAdr].FuctionCode = (UInt16)data[5];
            DevList[SrcAdr].IndustryGroup = (UInt16)(data[7] >> 4 & 0x07);
            flg_newDeviceFound = true;
        }

        private void Process_0x1F014(byte[] data, byte SrcAdr)
        {
            if (!ProdInfo_Buffers.ContainsKey(SrcAdr))
            {
                // Create required containers for new device
                ProdInfo_Buffers.Add(SrcAdr, new List<byte> { });
                TopSeqNrs_ProdInfo.Add(SrcAdr, 0);
            }
            if (data[0] <= TopSeqNrs_ProdInfo[SrcAdr])
            {
                // Seq.nr stopped counting upwards
                ProdInfo_Buffers[SrcAdr].Clear();          // reset message buffer - Message incomplete
                TopSeqNrs_ProdInfo[SrcAdr] = data[0];
            }
            // store current message in buffer
            for (int i = 1; i < 8; i++)
            {
                ProdInfo_Buffers[SrcAdr].Add(data[i]);
            }

            int message_length = ProdInfo_Buffers[SrcAdr][0];                   // length according to first byte of the message
            int buffer_content_length = ProdInfo_Buffers[SrcAdr].Count;         // 

            if (buffer_content_length >= message_length && buffer_content_length >= 1)
            {                                           // length should be 134
                // Whole Multiframe Message received
                Decode_MultiFr_0x1F014(SrcAdr, message_length);
                // reset buffer
                ProdInfo_Buffers[SrcAdr].Clear();
                TopSeqNrs_ProdInfo[SrcAdr] = 0;
            }
        }

        private void Decode_MultiFr_0x1F014(byte SrcAdr, int msg_len)
        {
            if (!DevList.ContainsKey(SrcAdr))
            {
                //Create container if there is none yet
                DevList.Add(SrcAdr, new t_ProductInformation(SrcAdr) { });
            }
            string cache = "";
            //Product Code
            DevList[SrcAdr].ProductCode = (ProdInfo_Buffers[SrcAdr][2] + ProdInfo_Buffers[SrcAdr][3] * 256);

            int BufferSize = ProdInfo_Buffers[SrcAdr].Count;

            //Model ID
            for (int i = 5; i < 37; i++)
            {
                if (BufferSize<=i)
                {
                    ProdInfo_Buffers[SrcAdr].Clear();
                    return;
                }
                if (ProdInfo_Buffers[SrcAdr][i] != 0xFF)
                {
                    cache += (char)ProdInfo_Buffers[SrcAdr][i];
                }
            }
            DevList[SrcAdr].ModelID = cache;

            //Software Version
            cache = "";
            for (int i = 37; i < (37 + 32); i++)
            {
                if (BufferSize <= i)
                {
                    ProdInfo_Buffers[SrcAdr].Clear();
                    return;
                }
                if (ProdInfo_Buffers[SrcAdr][i] != 0xFF)
                {
                    cache += (char)ProdInfo_Buffers[SrcAdr][i];
                }
            }
            DevList[SrcAdr].SW_Version = cache;

            //Model Version
            cache = "";
            for (int i = 69; i < (69 + 32); i++)
            {
                if (BufferSize <= i)
                {
                    ProdInfo_Buffers[SrcAdr].Clear();
                    return;
                }
                if (ProdInfo_Buffers[SrcAdr][i] != 0xFF)
                {
                    cache += (char)ProdInfo_Buffers[SrcAdr][i];
                }
            }
            DevList[SrcAdr].ModelVersion = cache;

            //Serial Code
            cache = "";
            for (int i = 101; i < (101 + 32); i++)
            {
                if (BufferSize <= i)
                {
                    ProdInfo_Buffers[SrcAdr].Clear();
                    return;
                }
                if (ProdInfo_Buffers[SrcAdr][i] != 0xFF)
                {
                    cache += (char)ProdInfo_Buffers[SrcAdr][i];
                }
            }
            DevList[SrcAdr].SerialCode = cache;
            flg_newDeviceFound = true;
        }
        #endregion

        #region Get_Set_Functions
        public void setToolAddress(int Adr)
        {
            ToolAddress = Adr;
        }

        public void setCanHandle(int Handle)
        {
            CanHandle = Handle;
        }

        public void setCanState(int state)
        {
            ToolCanState = (Tool_CAN_State)state;
        }

        public Dictionary<int, t_ProductInformation> getDevices()
        {
            // create and return a copy of the instance "DevList" (explicitly make copy to prevent error from two threads)
            Dictionary<int, t_ProductInformation> CopyOf_DevList = new Dictionary<int, t_ProductInformation>(DevList);
            return CopyOf_DevList;
        }
        #endregion

        #region NanniFunctions
        public bool EnableUds(int CurrentTargetAdr)
        {
            byte[] data = new byte[8];
            data[0] = 0xBB;     //Message ID
            data[1] = 0x99;
            data[2] = 0x07;
            data[3] = (byte)CurrentTargetAdr;
            data[4] = 0x01;     // UDS - Enable
            data[5] = 0x00;     // show instrument info
            data[6] = 0x00;     // show info / blinking ???
            data[7] = 0x00;     

            uint dlc = (uint)data.Length;
            canStatus status = CanlibAPI.canWrite(CanHandle, 0x00FF01FF, data, dlc, CanlibAPI.canMSG_EXT);
            if(status != CanlibAPI.canStatus.canOK)
            {
                //Debug.WriteLine("Error: C0001");
                return false;
            }
            return true;
        }


        public bool ExtendDiagSession()
        {
            byte[] data = new byte[3];
            data[0] = 0x02;     //Message ID
            data[1] = 0x10;
            data[2] = 0x03;

            uint dlc = (uint)data.Length;
            canStatus status = CanlibAPI.canWrite(CanHandle, UDS_TX, data, dlc, CanlibAPI.canMSG_STD);
            if (status != CanlibAPI.canStatus.canOK)
            {
                //Debug.WriteLine("Error: C0001");
                return false;
            }
            return true;
        }

        public bool getExtDiagAnswer()
        {
            if(ExtendDiagAnswer)
            {
                ExtendDiagAnswer = false;
                return true;
            }
            return false;
        }

        public bool SendScreenConfigStartFrame(List<byte> StartingFrames)
        {
            byte[] data = new byte[8];
            data[0] = 0x10;     
            data[1] = 0x36;
            data[2] = 0x2E;
            data[3] = 0x01;
            data[4] = 0x2C;

            data[5] = StartingFrames.ElementAt(0);
            data[6] = StartingFrames.ElementAt(1);
            data[7] = StartingFrames.ElementAt(2);

            uint dlc = (uint)data.Length;
            canStatus status = CanlibAPI.canWrite(CanHandle, UDS_TX, data, dlc, CanlibAPI.canMSG_STD);
            if (status != CanlibAPI.canStatus.canOK)
            {
                return false;
            }
            return true;
        }

        public bool getFrameAnswer()
        {
            if (FrameAnswer)
            {
                FrameAnswer = false;
                return true;
            }
            return false;
        }

        public bool SendScreenConfigConsecutiveFrames(List<byte> ConfigMessage)
        {
            int length = ConfigMessage.Count;
            int byteCnt = 2;    //Start at byte 3, as bytes 0-2 are already sent in StartingFrame

            while((byteCnt+8) < length)
            {
                byte[] data = new byte[8];
                data[0] = ConfigMessage.ElementAt(++byteCnt);
                data[1] = ConfigMessage.ElementAt(++byteCnt);
                data[2] = ConfigMessage.ElementAt(++byteCnt);
                data[3] = ConfigMessage.ElementAt(++byteCnt);
                data[4] = ConfigMessage.ElementAt(++byteCnt);
                data[5] = ConfigMessage.ElementAt(++byteCnt);
                data[6] = ConfigMessage.ElementAt(++byteCnt);
                data[7] = ConfigMessage.ElementAt(++byteCnt);

                canStatus s = CanlibAPI.canWrite(CanHandle, UDS_TX, data, 8, CanlibAPI.canMSG_STD);
                if (s != CanlibAPI.canStatus.canOK)
                {
                    return false;
                }
            }
            uint lastFrameLength = (uint)(length - (byteCnt+1));
            byte[] lastFrame = new byte[lastFrameLength];
            
            for(int i=0; i<lastFrameLength; i++)
            {
                lastFrame[i] = ConfigMessage.ElementAt(++byteCnt);
            }
            canStatus status = CanlibAPI.canWrite(CanHandle, UDS_TX, lastFrame, lastFrameLength, CanlibAPI.canMSG_STD);
            if (status != CanlibAPI.canStatus.canOK)
            {
                return false;
            }
            return true;
        }


        public bool getConfigAcknowledgment()
        {
            if (ConfigAcknowledgement)
            {
                ConfigAcknowledgement = false;
                return true;
            }
            return false;
        }

        public void resetProcessAnswers()
        {
            ConfigAcknowledgement = false;
            ExtendDiagAnswer = false;
            FrameAnswer = false;
        }

        #endregion

        #region Conversion
        private string ByteArrayToHexString(byte[] data)
        {
            StringBuilder sb = new StringBuilder(data.Length * 3);
            foreach (byte b in data)
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0').PadRight(3, ' '));
            return sb.ToString().ToUpper();
        }
        #endregion
    }

}
