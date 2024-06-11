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
    public partial class NanniConfig_GUI : Form
    {
        #region Variables
        private enum SendingScreenStates
        {
            DEFAULT,
            ENABLE_UDS,
            EXTEND_DIAG_SESSION,
            WAIT_UDS_EXTEND,
            SEND_START_FRAME,
            WAIT_START_FRAME_ANSWER,
            SEND_CONFIG,
            WAIT_ACK,
            DONE
        }

        private enum SendingPinsStates
        {
            DEFAULT,
            ENABLE_UDS,
            EXTEND_DIAG_SESSION,
            WAIT_UDS_EXTEND,
            SEND_START_FRAME_P1,
            WAIT_START_FRAME_ANSWER_P1,
            SEND_CONFIG_P1,
            WAIT_ACK_P1,
            SEND_START_FRAME_P2,
            WAIT_START_FRAME_ANSWER_P2,
            SEND_CONFIG_P2,
            WAIT_ACK_P2,
            SEND_START_FRAME_P3,
            WAIT_START_FRAME_ANSWER_P3,
            SEND_CONFIG_P3,
            WAIT_ACK_P3,
            SEND_START_FRAME_P4,
            WAIT_START_FRAME_ANSWER_P4,
            SEND_CONFIG_P4,
            WAIT_ACK_P4,
            DONE
        }


        private readonly CAN can = new();                // Instance of CAN-class
        private readonly Dictionary<int, t_ProductInformation> OL43List = new();
        private SendingScreenStates SendingState1 = SendingScreenStates.DEFAULT;
        private SendingPinsStates SendingState2 = SendingPinsStates.DEFAULT;
        private int SendPinConfigCnt = 0;
        private int TimeoutCounter = 0;
        private int CurrentTargetAdr = -1;
        private int CurrentConfigNr = -1;

        private readonly NanniConfigurations ScreenConfigs = new();
        #endregion

        #region Init
        public NanniConfig_GUI()
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
        #endregion

        #region Buttons
        private void bt_Refresh_Click(object sender, EventArgs e)
        {
            DisplayWaitCursor();
            OL43List.Clear();
            cb_Display.Items.Clear();
            DeviceListRequest();
        }


        private void bt_Write_Click(object sender, EventArgs e)
        {
            string Configuration = cb_Engines.Text;
            progressBar.Value = 0;
            progressBar.Refresh();
            if (Configuration != null && Configuration.Length > 1)
            {
                startSendingConfiguration(Configuration);
            }
            else
            {
                MessageBox.Show("Please select a configuration to upload.", "Missing Entry", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region GUI_Functions
        private void DisplayWaitCursor()
        {
            //Cursor.Current = Cursors.WaitCursor;
            this.UseWaitCursor = true;

            tmr_GuiDelays.Enabled = true;
            tmr_GuiDelays.Start();
        }

        private void DeviceListRequest()
        {
            can.Request_DeviceListUpdate();
            tmr_WaitForDevices.Interval = 750;
            tmr_WaitForDevices.Enabled = true;
            tmr_WaitForDevices.Start();
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
            }
        }

        private static bool IsInSerialNumberRange(int number)
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
        #endregion

        #region Sending
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

            CurrentTargetAdr = Convert.ToInt16(SelectedDisplay.Substring(2, 2), 16);    // 3rd&4th char represent address in Hex
            startSendingScreenConfiguration();
        }

        private void startSendingScreenConfiguration()
        {
            SendingState1 = SendingScreenStates.ENABLE_UDS;
            can.setCanState(4);     // state 4 = NanniConfigSending
            tmr_SendScreenConfigStateMachine.Enabled = true;
            tmr_SendScreenConfigStateMachine.Start();
        }

        private void startSendingPinConfiguration()
        {
            SendingState2 = SendingPinsStates.ENABLE_UDS;
            can.setCanState(4);     // state 4 = NanniConfigSending
            tmr_SendPinConfigStateMachine.Enabled = true;
            tmr_SendPinConfigStateMachine.Start();
        }

        private void stopConfigSendingProcess(bool hardStop = false)
        {
            SendingState1 = SendingScreenStates.DEFAULT;
            tmr_SendScreenConfigStateMachine.Stop();
            tmr_SendScreenConfigStateMachine.Enabled = false;
            tmr_SendPinConfigStateMachine.Stop();
            tmr_SendPinConfigStateMachine.Enabled = false;
            TimeoutCounter = 0;
            can.resetProcessAnswers();  // make sure, flaggs are reset

            if (hardStop)
            {
                can.setCanState(1);         // back to deviceList state
                CurrentTargetAdr = -1;
                CurrentConfigNr = -1;
                //progressBar.Value = 0;
            }
        }

        private void tmr_SendScreenConfigStateMachine_Tick(object sender, EventArgs e)
        {
            if (TimeoutCounter > 0)
            {
                if (--TimeoutCounter == 0)
                {
                    stopConfigSendingProcess(hardStop: true);
                    MessageBox.Show("Timeout", "No answer from device", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            switch (SendingState1)
            {
                case SendingScreenStates.ENABLE_UDS:
                    Debug.WriteLine(" --- Reached State 1: Enable UDS ---");
                    //GetCtrlVal(panel, PANEL_RNG_DEVICE, &ucSrcAddr);          // !! What is this doing ??? !!!!
                    bool Result = can.EnableUds(CurrentTargetAdr);
                    if (Result == false)
                    {
                        MessageBox.Show("CAN Error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    SendingState1 = SendingScreenStates.EXTEND_DIAG_SESSION;
                    progressBar.PerformStep();
                    break;

                case SendingScreenStates.EXTEND_DIAG_SESSION:
                    Debug.WriteLine(" --- Reached State 2: Extend Diag Session ---");
                    bool r1 = can.ExtendDiagSession();
                    if (r1 == false)
                    {
                        Debug.WriteLine("CAN ERROR");
                    }
                    SendingState1 = SendingScreenStates.WAIT_UDS_EXTEND;
                    progressBar.PerformStep();
                    break;

                case SendingScreenStates.WAIT_UDS_EXTEND:
                    Debug.WriteLine(" --- Reached State 3: Wait for ExtendDiag Answer ---");
                    bool Ackn_ExtDiagSession = can.getExtDiagAnswer();
                    if (Ackn_ExtDiagSession)
                    {
                        SendingState1 = SendingScreenStates.SEND_START_FRAME;
                    }
                    progressBar.PerformStep();
                    break;

                case SendingScreenStates.SEND_START_FRAME:
                    Debug.WriteLine(" --- Reached State 4: Sending start frame ---");
                    can.Byte4 = 0x2C;
                    can.StartingFrameRecognitionByte = 0x36;
                    bool r2 = can.SendConfigStartFrame(ScreenConfigs.getScreenConfig_StartingFrame(CurrentConfigNr));
                    if (r2 == false)
                    {
                        Debug.WriteLine("Error at state 4");
                    }
                    SendingState1 = SendingScreenStates.WAIT_START_FRAME_ANSWER;
                    progressBar.PerformStep();
                    break;

                case SendingScreenStates.WAIT_START_FRAME_ANSWER:
                    Debug.WriteLine(" --- Reached State 5: Wait for answer on startFrame ---");
                    bool gotFrameAnswer = can.getFrameAnswer();
                    if (gotFrameAnswer)
                    {
                        SendingState1 = SendingScreenStates.SEND_CONFIG;
                        progressBar.PerformStep();
                    }
                    break;

                case SendingScreenStates.SEND_CONFIG:
                    Debug.WriteLine(" --- Reached State 6: Send entire configuration ---");
                    bool r3 = can.SendScreenConfigConsecutiveFrames(ScreenConfigs.getConfig(CurrentConfigNr));
                    if (r3 == false)
                    {
                        Debug.WriteLine("CAN Error");
                    }
                    SendingState1 = SendingScreenStates.WAIT_ACK;
                    progressBar.PerformStep();
                    break;

                case SendingScreenStates.WAIT_ACK:
                    Debug.WriteLine(" --- Reached State 7: wait for acknowledge ---");
                    bool gotConfigAck = can.getConfigAcknowledgment();
                    if (gotConfigAck)
                    {
                        SendingState1 = SendingScreenStates.DONE;
                        progressBar.PerformStep();
                    }
                    break;

                case SendingScreenStates.DONE:
                    Debug.WriteLine(" --- Reached State 8: Succesfully Finished ---");
                    progressBar.PerformStep();
                    stopConfigSendingProcess(hardStop: false);
                    //Prepare for sending Pin Configurations too
                    ScreenConfigs.defineCurrentPinConfig("StandardCurves_P8_Coolant120_P9OilPress10");
                    startSendingPinConfiguration();
                    break;

                default:
                    Debug.WriteLine("Error C004: Non-Expected Switch state");
                    CurrentTargetAdr = -1;
                    break;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            can.TerminateCanBusConnection();
        }


        private void tmr_GuiDelays_Tick(object sender, EventArgs e)
        {
            tmr_GuiDelays.Enabled = false;
            tmr_GuiDelays.Stop();
            this.UseWaitCursor = false;             // no idea why both are required to get the cursor to update immediately ?
            Cursor.Current = Cursors.Default;
            if (OL43List.Count > 0)
            {
                cb_Display.DroppedDown = true;
            }
        }

        private void tmr_SendPinConfigStateMachine_Tick(object sender, EventArgs e)
        {
            if (TimeoutCounter > 0)
            {
                if (--TimeoutCounter == 0)
                {
                    stopConfigSendingProcess(hardStop: true);
                    MessageBox.Show("Timeout", "No answer from device", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            switch (SendingState2)
            {
                case SendingPinsStates.ENABLE_UDS:
                    Debug.WriteLine(" --- Reached State 9: Enable UDS ---");
                    bool Result = can.EnableUds(CurrentTargetAdr);
                    if (Result == false)
                    {
                        MessageBox.Show("CAN Error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    SendingState2 = SendingPinsStates.EXTEND_DIAG_SESSION;
                    progressBar.PerformStep();
                    break;

                case SendingPinsStates.EXTEND_DIAG_SESSION:
                    Debug.WriteLine(" --- Reached State 10: Extend Diag Session ---");
                    bool r1 = can.ExtendDiagSession();
                    if (r1 == false)
                    {
                        Debug.WriteLine("CAN ERROR");
                    }
                    SendingState2 = SendingPinsStates.WAIT_UDS_EXTEND;
                    progressBar.PerformStep();
                    break;

                case SendingPinsStates.WAIT_UDS_EXTEND:
                    Debug.WriteLine(" --- Reached State 11: Wait for ExtendDiag Answer ---");
                    bool Ackn_ExtDiagSession = can.getExtDiagAnswer();
                    if (Ackn_ExtDiagSession)
                    {
                        SendingState2 = SendingPinsStates.SEND_START_FRAME_P1;
                    }
                    progressBar.PerformStep();
                    break;





                case SendingPinsStates.SEND_START_FRAME_P1:
                    var StartingFrame1 = ScreenConfigs.PinConfig_Part1.GetRange(0, 3);
                    can.Byte4 = 0x44;
                    can.StartingFrameRecognitionByte = 0x19;
                    can.SendConfigStartFrame(StartingFrame1);
                    SendingState2 = SendingPinsStates.WAIT_START_FRAME_ANSWER_P1;
                    break;

                case SendingPinsStates.WAIT_START_FRAME_ANSWER_P1:
                    bool gotFrameAnswer1 = can.getFrameAnswer();
                    if (gotFrameAnswer1)
                    {
                        SendingState2 = SendingPinsStates.SEND_CONFIG_P1;
                    }
                    break;

                case SendingPinsStates.SEND_CONFIG_P1:
                    can.SendScreenConfigConsecutiveFrames(ScreenConfigs.PinConfig_Part1);
                    SendingState2 = SendingPinsStates.WAIT_ACK_P1;
                    break;

                case SendingPinsStates.WAIT_ACK_P1:
                    bool gotConfigAck1 = can.getConfigAcknowledgment();
                    if (gotConfigAck1)
                    {
                        SendingState2 = SendingPinsStates.SEND_START_FRAME_P2;
                        progressBar.PerformStep();
                        Debug.WriteLine(" --- Reached State 12: Part1 Sent ---");
                    }
                    break;

                case SendingPinsStates.SEND_START_FRAME_P2:
                    var StartingFrame2 = ScreenConfigs.PinConfig_Part2.GetRange(0, 3);
                    can.Byte4 = 0x45;
                    can.SendConfigStartFrame(StartingFrame2);
                    SendingState2 = SendingPinsStates.WAIT_START_FRAME_ANSWER_P2;
                    break;

                case SendingPinsStates.WAIT_START_FRAME_ANSWER_P2:
                    bool gotFrameAnswer2 = can.getFrameAnswer();
                    if (gotFrameAnswer2)
                    {
                        SendingState2 = SendingPinsStates.SEND_CONFIG_P2;
                    }
                    break;

                case SendingPinsStates.SEND_CONFIG_P2:
                    can.SendScreenConfigConsecutiveFrames(ScreenConfigs.PinConfig_Part2);
                    SendingState2 = SendingPinsStates.WAIT_ACK_P2;
                    break;

                case SendingPinsStates.WAIT_ACK_P2:
                    bool gotConfigAck2 = can.getConfigAcknowledgment();
                    if (gotConfigAck2)
                    {
                        SendingState2 = SendingPinsStates.SEND_START_FRAME_P3;
                        progressBar.PerformStep();
                        Debug.WriteLine(" --- Reached State 13: Part2 Sent ---");
                    }
                    break;

                case SendingPinsStates.SEND_START_FRAME_P3:
                    var StartingFrame3 = ScreenConfigs.PinConfig_Part3.GetRange(0, 3);
                    can.Byte4 = 0x46;
                    can.SendConfigStartFrame(StartingFrame3);
                    SendingState2 = SendingPinsStates.WAIT_START_FRAME_ANSWER_P3;
                    break;

                case SendingPinsStates.WAIT_START_FRAME_ANSWER_P3:
                    bool gotFrameAnswer3 = can.getFrameAnswer();
                    if (gotFrameAnswer3)
                    {
                        SendingState2 = SendingPinsStates.SEND_CONFIG_P3;
                    }
                    break;

                case SendingPinsStates.SEND_CONFIG_P3:
                    can.SendScreenConfigConsecutiveFrames(ScreenConfigs.PinConfig_Part3);
                    SendingState2 = SendingPinsStates.WAIT_ACK_P3;
                    break;

                case SendingPinsStates.WAIT_ACK_P3:
                    bool gotConfigAck3 = can.getConfigAcknowledgment();
                    if (gotConfigAck3)
                    {
                        SendingState2 = SendingPinsStates.SEND_START_FRAME_P4;
                        progressBar.PerformStep();
                        Debug.WriteLine(" --- Reached State 14: Part3 Sent ---");
                    }
                    break;



                case SendingPinsStates.SEND_START_FRAME_P4:
                    var StartingFrame4 = ScreenConfigs.PinConfig_Part4.GetRange(0, 3);
                    can.Byte4 = 0x47;
                    can.SendConfigStartFrame(StartingFrame4);
                    SendingState2 = SendingPinsStates.WAIT_START_FRAME_ANSWER_P4;
                    break;

                case SendingPinsStates.WAIT_START_FRAME_ANSWER_P4:
                    bool gotFrameAnswer4 = can.getFrameAnswer();
                    if (gotFrameAnswer4)
                    {
                        SendingState2 = SendingPinsStates.SEND_CONFIG_P4;
                    }
                    break;

                case SendingPinsStates.SEND_CONFIG_P4:
                    can.SendScreenConfigConsecutiveFrames(ScreenConfigs.PinConfig_Part4);
                    SendingState2 = SendingPinsStates.WAIT_ACK_P4;
                    break;

                case SendingPinsStates.WAIT_ACK_P4:
                    bool gotConfigAck4 = can.getConfigAcknowledgment();
                    if (gotConfigAck4)
                    {
                        SendingState2 = SendingPinsStates.DONE;
                        progressBar.PerformStep();
                        Debug.WriteLine(" --- Reached State 15: Part4 Sent ---");
                    }
                    break;

                case SendingPinsStates.DONE:
                    Debug.WriteLine(" --- Reached State 16: Succesfully Finished ---");
                    progressBar.PerformStep();
                    stopConfigSendingProcess(hardStop: true);
                    break;

                default:
                    Debug.WriteLine("Error C004: Non-Expected Switch state");
                    CurrentTargetAdr = -1;
                    break;
            }
        }
    }
    #endregion
}