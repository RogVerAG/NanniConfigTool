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
using static System.Windows.Forms.AxHost;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static Canlib.CanlibAPI;


namespace Nanni_ScreenConfigurator
{
    public partial class Form1 : Form
    {
        public enum SendingStates
        {
            DEFAULT, ENABLE_UDS, EXTEND_DIAG_SESSION, WAIT_UDS_EXTEND, SEND_START_FRAME, WAIT_START_FRAME_ANSWER, SEND_CONFIG, WAIT_ACK, DONE
        }


        private CAN can = new CAN();                // Instance of CAN-class
        Dictionary<int, t_ProductInformation> OL43List = new Dictionary<int, t_ProductInformation>();
        private SendingStates SendingState = SendingStates.DEFAULT;
        private int TimeoutCounter = 0;
        private int CurrentTargetAdr = -1;
        private int CurrentConfigNr = -1;

        private NanniConfigurations ScreenConfigs = new NanniConfigurations();

        public Form1()
        {
            InitializeComponent();
            InitializeKvaserInterface();

        }

        private void InitializeKvaserInterface()
        {
            can.setCanHandle(0);                        // random number, 0 cause there is only one bus
            can.setCanState(1);                         // state 1 = DeviceList
            can.setToolAddress(0xF9);                   // random number -> unlikely to be occupied

            int CanStatus = (int)can.InitializeCAN();   // start init
            if (CanStatus == -10)
            {
                //Error detected (Diag.-Tool not connected) 
                MessageBox.Show(this, "Please connect Diagnostic Tool and restart Application", "Diagnostic Tool not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (CanStatus < 0)
            {
                // any other Error detected
                MessageBox.Show(this, "Communication failed! CAN status: " + Convert.ToString(CanStatus), "...", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            can.start_CanPollingThread();
        }

        private void bt_Refresh_Click(object sender, EventArgs e)
        {
            OL43List.Clear();
            cb_Display.Items.Clear();
            DeviceListRequest();
        }


        private void bt_Write_Click(object sender, EventArgs e)
        {
            string Configuration = cb_Engines.Text;
            if (Configuration != null && Configuration.Length > 1)
            {
                startSendingConfiguration(Configuration);
            }
            else
            {
                MessageBox.Show("Please select a configuration to upload.", "Missing Entry", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeviceListRequest()
        {
            can.Request_DeviceListUpdate();
            tmr_WaitForDevices.Interval = 100;
            tmr_WaitForDevices.Enabled = true;
            tmr_WaitForDevices.Start();
        }

        private void startSendingConfiguration(string ConfigName)
        {
            switch (ConfigName)         // made with a switch, so names can be changed to something different (like Engine type)
            {
                case "Configuration 01":
                    CurrentConfigNr = 1;
                    break;
                case "Configuration 02":
                    CurrentConfigNr = 2;
                    break;
                case "Configuration 03":
                    CurrentConfigNr = 3;
                    break;
                case "Configuration 04":
                    CurrentConfigNr = 4;
                    break;
                case "Configuration 05":
                    CurrentConfigNr = 5;
                    break;
                case "Configuration 06":
                    CurrentConfigNr = 6;
                    break;
                case "Configuration 07":
                    CurrentConfigNr = 7;
                    break;
                case "Configuration 08":
                    CurrentConfigNr = 8;
                    break;
                case "Configuration 09":
                    CurrentConfigNr = 9;
                    break;
                case "Configuration 10":
                    CurrentConfigNr = 10;
                    break;
                case "Configuration 11":
                    CurrentConfigNr = 11;
                    break;
                case "Configuration 12":
                    CurrentConfigNr = 12;
                    break;
                default:
                    Debug.WriteLine("Error: C005");
                    break;
            }

            string SelectedDisplay = cb_Display.Text;
            if (SelectedDisplay == null || SelectedDisplay.Length == 0)
            {
                MessageBox.Show("Please select a display to configure.", "Missing Entry", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            CurrentTargetAdr = Convert.ToInt16(SelectedDisplay.Substring(2, 2), 16);
            SendingState = SendingStates.ENABLE_UDS;
            can.setCanState(4);     // state 4 = NanniConfigSending
            TimeoutCounter = 100;    
            tmr_SendConfigStateMachine.Enabled = true;
            tmr_SendConfigStateMachine.Start();
        }

        private void tmr_SendConfigStateMachine_Tick(object sender, EventArgs e)
        {
            if (TimeoutCounter > 0)
            {
                if (--TimeoutCounter == 0)
                {
                    stopConfigSendingProcess();
                    MessageBox.Show("Timeout", "No answer from device", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            switch (SendingState)
            {
                case SendingStates.ENABLE_UDS:
                    Debug.WriteLine(" --- Reached State 1: Enable UDS ---");
                    //GetCtrlVal(panel, PANEL_RNG_DEVICE, &ucSrcAddr);          // !! What is this doing ??? !!!!
                    bool Result = can.EnableUds(CurrentTargetAdr);
                    if(Result == false)
                    {
                        MessageBox.Show("CAN Error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    SendingState = SendingStates.EXTEND_DIAG_SESSION;
                    break;

                case SendingStates.EXTEND_DIAG_SESSION:
                    Debug.WriteLine(" --- Reached State 2: Extend Diag Session ---");
                    bool r1 = can.ExtendDiagSession();
                    if(r1 == false)
                    {
                        Debug.WriteLine("CAN ERROR");
                    }
                    SendingState = SendingStates.WAIT_UDS_EXTEND;
                    break;

                case SendingStates.WAIT_UDS_EXTEND:
                    Debug.WriteLine(" --- Reached State 3: Wait for ExtendDiag Answer ---");
                    bool Ackn_ExtDiagSession = can.getExtDiagAnswer();
                    if(Ackn_ExtDiagSession)
                    {
                        SendingState = SendingStates.SEND_START_FRAME;
                    }
                    break;

                case SendingStates.SEND_START_FRAME:
                    Debug.WriteLine(" --- Reached State 4: Sending start frame ---");
                    can.SendScreenConfigStartFrame( ScreenConfigs.getConfig_StartingFrame(CurrentConfigNr) );
                    SendingState = SendingStates.WAIT_START_FRAME_ANSWER;
                    break;

                case SendingStates.WAIT_START_FRAME_ANSWER:
                    Debug.WriteLine(" --- Reached State 5: Wait for answer on startFrame ---");
                    bool gotFrameAnswer = can.getFrameAnswer();
                    if(gotFrameAnswer)
                    {
                        SendingState = SendingStates.SEND_CONFIG;
                    }
                    break;

                case SendingStates.SEND_CONFIG:
                    Debug.WriteLine(" --- Reached State 6: Send entire configuration ---");
                    can.SendScreenConfigConsecutiveFrames( ScreenConfigs.getConfig(CurrentConfigNr) );
                    SendingState = SendingStates.WAIT_ACK;
                    break;

                case SendingStates.WAIT_ACK:
                    Debug.WriteLine(" --- Reached State 7: wait for acknowledge ---");
                    bool gotConfigAck = can.getConfigAcknowledgment();
                    if (gotConfigAck)
                    {
                        SendingState = SendingStates.DONE;
                    }
                    break;
                case SendingStates.DONE:
                    Debug.WriteLine(" --- Reached State 8: Succesfully Finished ---");
                    stopConfigSendingProcess();
                    break;

                default:
                    Debug.WriteLine("Error C004: Non-Expected Switch state");
                    CurrentTargetAdr = -1;
                    break;
            }
        }

        private void stopConfigSendingProcess()
        {
            SendingState = SendingStates.DEFAULT;
            tmr_SendConfigStateMachine.Stop();
            tmr_SendConfigStateMachine.Enabled = false;
            TimeoutCounter = 0;
            CurrentTargetAdr = -1;
            CurrentConfigNr = -1;
            can.resetProcessAnswers();  // make sure, flaggs are reset
            can.setCanState(1);         // back to deviceList state
        }


        private void tmr_WaitForDevices_Tick(object sender, EventArgs e)
        {
            if (can.flg_newDeviceFound)
            {
                can.flg_newDeviceFound = false;
                tmr_WaitForDevices.Start();
                UpdateDisplayList();
                tmr_WaitForDevices.Interval = 100;
            }
            else
            {
                tmr_WaitForDevices.Interval = 2000;
            }
        }

        private void UpdateDisplayList()
        {
            Dictionary<int, t_ProductInformation> NewDeviceList = can.getDevices();
            foreach (var Device in NewDeviceList)
            {
                int serialCode = Convert.ToInt32(Device.Value.SerialCode);
                int SourceAddress = Device.Value.SourceAdr;
                int DeviceClass = Device.Value.DeviceClass;

                if (Device.Value.ManufactCode == 443 && DeviceClass == 120 && IsInSerialNumberRange(serialCode))
                {
                    // It's Produced by Veratron & is a display & is in OL4.3 serial number range
                    if (!OL43List.ContainsKey(SourceAddress))
                    {
                        // Add display to global device list
                        OL43List.Add(SourceAddress, Device.Value);
                        // add display to drop down menu
                        string SrcAdr_HexString = Convert.ToString(SourceAddress, 16).PadLeft(2, '0').ToUpper();
                        cb_Display.Items.Add("0x" + SrcAdr_HexString + " - OceanLink 4.3");
                        Debug.WriteLine("OceanLink 4.3 found!");
                    }
                }
                else
                {
                    Debug.WriteLine("Other Device Found");
                }
            }
        }

        private bool IsInSerialNumberRange(int number)
        {
            if (number >= 0x0A0000 && number <= 0x0AFFFF)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            can.TerminateCanBusConnection();
        }
    }
}