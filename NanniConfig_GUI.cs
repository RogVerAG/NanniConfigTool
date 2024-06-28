using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
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
        private enum SimpleMsgSendingStates
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

        private enum ProcessStates
        {
            OFF,
            IN_PROGRESS,
            SUCCESS,
            FAIL
        }

        private enum ConfigSteps
        {
            SCREENS,
            ANALOG_PINS,
            FREQUENCY_PINS,
            CUSTOM_SENSOR_SELECTION,
            BARGRAPH_RANGES,
            DEFAULT
        }

        private readonly CAN can = new();                // Instance of CAN-class
        private readonly Dictionary<int, t_ProductInformation> OL43List = new();
        private SimpleMsgSendingStates SndStt_ScreenConfig = SimpleMsgSendingStates.DEFAULT;
        private SendingPinsStates SndStt_PinConfig = SendingPinsStates.DEFAULT;
        private SimpleMsgSendingStates SndStt_SensorTypeCustom = SimpleMsgSendingStates.DEFAULT;
        private SimpleMsgSendingStates SndStt_BargraphConfig = SimpleMsgSendingStates.DEFAULT;
        private int SendPinConfigCnt = 0;
        private int TimeoutCounter = 0;
        private int CurrentTargetAdr = -1;
        private int CurrentConfigNr = -1;
        private ConfigSteps CurrentConfigStep;

        private readonly NanniConfigurations ScreenConfigs = new();
        #endregion

        #region Init
        public NanniConfig_GUI()
        {
            InitializeComponent();
            try
            {
                InitializeKvaserInterface();
            }
            catch (Exception)
            {
                //Exceptions can only been thrown by KvaserAPI -> can occur, when drivers have not been installed on user computer
                MessageBox.Show(this,
                    "An Error occured on starting the CAN communication\n" +
                    "Make sure the Kvaser Drivers are installed",
                    "Exception caught",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                this.Close();
            }
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
            CancelRunningSendingProcess();
            OL43List.Clear();
            cb_Display.Items.Clear();
            changeStatusLabel(ProcessStates.OFF);
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

        public void CancelRunningSendingProcess()
        {
            if (tmr_SendingStateMachine.Enabled)
            {
                stopConfigSendingProcess(hardStop: true);
                changeStatusLabel(ProcessStates.OFF);
            }
        }

        private void UpdateDisplayList()
        {
            Thread.Sleep(400);
            Dictionary<int, t_ProductInformation> NewDeviceList = can.getDevices();
            foreach (var Device in NewDeviceList)
            {
                try
                {
                    // because the device list request sometimes receives invalid characters (STX)
                    // -> causes exception to be thrown in SerialCode convesion
                    // only happens on busy busses - I think

                    long serialCode = Convert.ToInt64(Device.Value.SerialCode);
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
                catch (Exception ex)
                {
                    Debug.WriteLine("----------- CONVERSION ERROR -----------------------");
                    Debug.WriteLine(ex);
                    Debug.WriteLine(Device.Value.SerialCode);
                    continue;
                }

            }
        }

        private static bool IsInSerialNumberRange(long number)
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
        #endregion

        #region Sending
        private void startSendingConfiguration(string ConfigName)
        {
            switch (ConfigName)         // made with a switch, so names can easily be changed to something different (like Engine type)
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
            CurrentConfigStep = ConfigSteps.SCREENS;
            SndStt_ScreenConfig = SimpleMsgSendingStates.ENABLE_UDS;
            can.setCanState(4);     // state 4 = NanniConfigSending
            TimeoutCounter = 40;
            tmr_SendingStateMachine.Enabled = true;
            tmr_SendingStateMachine.Start();
            changeStatusLabel(ProcessStates.IN_PROGRESS);
        }

        private void startSendingPinConfiguration()
        {
            ScreenConfigs.defineCurrentPinConfig("StandardCurves_P8_Coolant120_P9OilPress10");  // currently the only config
                                                                                                // must implement further getter if there are different configs required
            CurrentConfigStep = ConfigSteps.ANALOG_PINS;
            SndStt_PinConfig = SendingPinsStates.ENABLE_UDS;
            can.setCanState(4);     // state 4 = NanniConfigSending
            TimeoutCounter = 40;
            tmr_SendingStateMachine.Enabled = true;
            tmr_SendingStateMachine.Start();
        }

        private void startSendingCustomSensorConfiguration()
        {
            CurrentConfigStep = ConfigSteps.CUSTOM_SENSOR_SELECTION;
            SndStt_SensorTypeCustom = SimpleMsgSendingStates.ENABLE_UDS;
            can.setCanState(4);     // state 4 = NanniConfigSending
            TimeoutCounter = 40;
            tmr_SendingStateMachine.Enabled = true;
            tmr_SendingStateMachine.Start();
            changeStatusLabel(ProcessStates.IN_PROGRESS);
        }

        private void startSendingBargraphConfiguration()
        {
            CurrentConfigStep = ConfigSteps.BARGRAPH_RANGES;
            SndStt_BargraphConfig = SimpleMsgSendingStates.ENABLE_UDS;
            can.setCanState(4);     // state 4 = NanniConfigSending
            TimeoutCounter = 40;
            tmr_SendingStateMachine.Enabled = true;
            tmr_SendingStateMachine.Start();
            changeStatusLabel(ProcessStates.IN_PROGRESS);
        }

        private void stopConfigSendingProcess(bool hardStop = false)
        {
            SndStt_ScreenConfig = SimpleMsgSendingStates.DEFAULT;
            StopTimer(tmr_SendingStateMachine);
            TimeoutCounter = 0;
            can.resetProcessAnswers();  // make sure, flaggs are reset
            CurrentConfigStep = ConfigSteps.DEFAULT;

            if (hardStop)
            {
                can.setCanState(1);         // back to deviceList state
                CurrentTargetAdr = -1;
                CurrentConfigNr = -1;
                //progressBar.Value = 0;
            }
        }

        private void changeStatusLabel(ProcessStates state)
        {
            switch (state)
            {
                case ProcessStates.OFF:
                    lb_SendingStatus.Visible = false;
                    break;
                case ProcessStates.IN_PROGRESS:
                    lb_SendingStatus.Text = "In Progress";
                    lb_SendingStatus.ForeColor = Color.Orange;
                    lb_SendingStatus.Visible = true;
                    break;
                case ProcessStates.SUCCESS:
                    lb_SendingStatus.Text = "Success";
                    lb_SendingStatus.ForeColor = Color.Green;
                    lb_SendingStatus.Visible = true;
                    break;
                case ProcessStates.FAIL:
                    lb_SendingStatus.Text = "Failed";
                    lb_SendingStatus.ForeColor = Color.Red;
                    lb_SendingStatus.Visible = true;
                    break;
            }
        }

        public void FailExitSendingProcess(string Error)
        {
            changeStatusLabel(ProcessStates.FAIL);
            stopConfigSendingProcess(hardStop: true);
            progressBar.Value = 0;
            MessageBox.Show(Error, "FAIL", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void StopTimer(System.Windows.Forms.Timer tmr)
        {
            tmr.Stop();
            tmr.Enabled = false;
        }

        private void tmr_SendingStateMachine_Tick(object sender, EventArgs e)
        {
            if (TimeoutCounter > 0)
            {
                if (--TimeoutCounter == 0)
                {
                    FailExitSendingProcess("Timeout Error");
                }
            }

            switch (CurrentConfigStep)
            {
                case ConfigSteps.SCREENS:
                    ScreenConfig_StateMachine();
                    break;

                case ConfigSteps.CUSTOM_SENSOR_SELECTION:
                    CustomAsSensorType_StateMachine();
                    break;

                case ConfigSteps.ANALOG_PINS:
                    PinConfig_StateMachine();
                    break;

                case ConfigSteps.FREQUENCY_PINS:
                    sendFrequencyPinsConfig();
                    break;

                case ConfigSteps.BARGRAPH_RANGES:
                    BargraphConfig_SateMachine();
                    break;
            }
        }

        private void ScreenConfig_StateMachine()
        {
            switch (SndStt_ScreenConfig)
            {
                case SimpleMsgSendingStates.ENABLE_UDS:
                    //Debug.WriteLine(" --- Reached State 1: Enable UDS ---");
                    bool Result = can.EnableUds(CurrentTargetAdr);
                    if (Result == false)
                    {
                        MessageBox.Show("CAN Error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    SndStt_ScreenConfig = SimpleMsgSendingStates.EXTEND_DIAG_SESSION;
                    progressBar.PerformStep();
                    break;

                case SimpleMsgSendingStates.EXTEND_DIAG_SESSION:
                    //Debug.WriteLine(" --- Reached State 2: Extend Diag Session ---");
                    bool r1 = can.ExtendDiagSession();
                    if (r1 == false)
                    {
                        Debug.WriteLine("CAN ERROR");
                    }
                    SndStt_ScreenConfig = SimpleMsgSendingStates.WAIT_UDS_EXTEND;
                    progressBar.PerformStep();
                    break;

                case SimpleMsgSendingStates.WAIT_UDS_EXTEND:
                    //Debug.WriteLine(" --- Reached State 3: Wait for ExtendDiag Answer ---");
                    bool Ackn_ExtDiagSession = can.getExtDiagAnswer();
                    if (Ackn_ExtDiagSession)
                    {
                        SndStt_ScreenConfig = SimpleMsgSendingStates.SEND_START_FRAME;
                        progressBar.PerformStep();
                    }
                    break;

                case SimpleMsgSendingStates.SEND_START_FRAME:
                    //Debug.WriteLine(" --- Reached State 4: Sending start frame ---");
                    bool r2 = can.SendConfigStartFrame(ScreenConfigs.getScreenConfig_StartingFrame(CurrentConfigNr), 0x36, 0x2C);
                    if (r2 == false)
                    {
                        Debug.WriteLine("Error at state 4");
                    }
                    SndStt_ScreenConfig = SimpleMsgSendingStates.WAIT_START_FRAME_ANSWER;
                    progressBar.PerformStep();
                    break;

                case SimpleMsgSendingStates.WAIT_START_FRAME_ANSWER:
                    //Debug.WriteLine(" --- Reached State 5: Wait for answer on startFrame ---");
                    bool gotFrameAnswer = can.getFrameAnswer();
                    if (gotFrameAnswer)
                    {
                        SndStt_ScreenConfig = SimpleMsgSendingStates.SEND_CONFIG;
                        progressBar.PerformStep();
                    }
                    break;

                case SimpleMsgSendingStates.SEND_CONFIG:
                    //Debug.WriteLine(" --- Reached State 6: Send entire configuration ---");
                    bool r3 = can.SendScreenConfigConsecutiveFrames(ScreenConfigs.getScreenConfig(CurrentConfigNr));
                    if (r3 == false)
                    {
                        Debug.WriteLine("CAN Error");
                    }
                    SndStt_ScreenConfig = SimpleMsgSendingStates.WAIT_ACK;
                    progressBar.PerformStep();
                    break;

                case SimpleMsgSendingStates.WAIT_ACK:
                    //Debug.WriteLine(" --- Reached State 7: wait for acknowledge ---");
                    bool gotConfigAck = can.getConfigAcknowledgment();
                    if (gotConfigAck)
                    {
                        SndStt_ScreenConfig = SimpleMsgSendingStates.DONE;
                        progressBar.PerformStep();
                    }
                    break;

                case SimpleMsgSendingStates.DONE:
                    //Debug.WriteLine(" --- Reached State 8: Succesfully Finished ---");
                    progressBar.PerformStep();
                    stopConfigSendingProcess(hardStop: false);
                    //Prepare for sending Pin Configurations too
                    startSendingCustomSensorConfiguration();
                    break;

                default:
                    FailExitSendingProcess("Unexpected Status");
                    break;
            }
        }

        private void CustomAsSensorType_StateMachine()
        {
            // in order to have a sensor configuration sent via UDS, it's required to have the sensor type "custom" selected.
            // This is not automatically done when sending the configuration. Instead this message is required as well.
            switch (SndStt_SensorTypeCustom)
            {
                case SimpleMsgSendingStates.ENABLE_UDS:
                    //Debug.WriteLine(" --- Reached State 1: Enable UDS ---");
                    bool Result = can.EnableUds(CurrentTargetAdr);
                    if (Result == false)
                    {
                        MessageBox.Show("CAN Error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    SndStt_SensorTypeCustom = SimpleMsgSendingStates.EXTEND_DIAG_SESSION;
                    progressBar.PerformStep();
                    break;

                case SimpleMsgSendingStates.EXTEND_DIAG_SESSION:
                    //Debug.WriteLine(" --- Reached State 2: Extend Diag Session ---");
                    bool r1 = can.ExtendDiagSession();
                    if (r1 == false)
                    {
                        Debug.WriteLine("CAN ERROR");
                    }
                    SndStt_SensorTypeCustom = SimpleMsgSendingStates.WAIT_UDS_EXTEND;
                    progressBar.PerformStep();
                    break;

                case SimpleMsgSendingStates.WAIT_UDS_EXTEND:
                    //Debug.WriteLine(" --- Reached State 3: Wait for ExtendDiag Answer ---");
                    bool Ackn_ExtDiagSession = can.getExtDiagAnswer();
                    if (Ackn_ExtDiagSession)
                    {
                        SndStt_SensorTypeCustom = SimpleMsgSendingStates.SEND_START_FRAME;
                        progressBar.PerformStep();
                    }
                    break;

                case SimpleMsgSendingStates.SEND_START_FRAME:
                    //Debug.WriteLine(" --- Reached State 4: Sending start frame ---");
                    /*can.StartingFrameRecognitionByte1 = 0x2C;
                    can.StartingFrameRecognitionByte2 = 0x3A;*/
                    bool r2 = can.SendConfigStartFrame(ScreenConfigs.getCustomSensor_StartingFrame(), 0x2C, 0x3A);
                    if (r2 == false)
                    {
                        Debug.WriteLine("Error at state 4");
                    }
                    SndStt_SensorTypeCustom = SimpleMsgSendingStates.WAIT_START_FRAME_ANSWER;
                    progressBar.PerformStep();
                    break;

                case SimpleMsgSendingStates.WAIT_START_FRAME_ANSWER:
                    //Debug.WriteLine(" --- Reached State 5: Wait for answer on startFrame ---");
                    bool gotFrameAnswer = can.getFrameAnswer();
                    if (gotFrameAnswer)
                    {
                        SndStt_SensorTypeCustom = SimpleMsgSendingStates.SEND_CONFIG;
                        progressBar.PerformStep();
                    }
                    break;

                case SimpleMsgSendingStates.SEND_CONFIG:
                    //Debug.WriteLine(" --- Reached State 6: Send entire configuration ---");
                    bool r3 = can.SendScreenConfigConsecutiveFrames(ScreenConfigs.getCustomSensorMessage());
                    if (r3 == false)
                    {
                        Debug.WriteLine("CAN Error");
                    }
                    SndStt_SensorTypeCustom = SimpleMsgSendingStates.WAIT_ACK;
                    progressBar.PerformStep();
                    break;

                case SimpleMsgSendingStates.WAIT_ACK:
                    //Debug.WriteLine(" --- Reached State 7: wait for acknowledge ---");
                    bool gotConfigAck = can.getConfigAcknowledgment();
                    if (gotConfigAck)
                    {
                        SndStt_SensorTypeCustom = SimpleMsgSendingStates.DONE;
                        progressBar.PerformStep();
                    }
                    break;

                case SimpleMsgSendingStates.DONE:
                    //Debug.WriteLine(" --- Reached State 8: Succesfully Finished ---");
                    progressBar.PerformStep();
                    stopConfigSendingProcess(hardStop: false);
                    //Prepare for sending Pin Configurations too
                    startSendingPinConfiguration();
                    break;

                default:
                    FailExitSendingProcess("Unexpected Status");
                    break;
            }
        }

        private void PinConfig_StateMachine()
        {
            switch (SndStt_PinConfig)
            {
                case SendingPinsStates.ENABLE_UDS:
                    //Debug.WriteLine(" --- Reached State 9: Enable UDS ---");
                    bool Result = can.EnableUds(CurrentTargetAdr);
                    if (Result == false)
                    {
                        MessageBox.Show("CAN Error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        FailExitSendingProcess("CAN Error");
                    }
                    SndStt_PinConfig = SendingPinsStates.EXTEND_DIAG_SESSION;
                    progressBar.PerformStep();
                    break;

                case SendingPinsStates.EXTEND_DIAG_SESSION:
                    //Debug.WriteLine(" --- Reached State 10: Extend Diag Session ---");
                    bool r1 = can.ExtendDiagSession();
                    if (r1 == false)
                    {
                        Debug.WriteLine("CAN ERROR");
                        FailExitSendingProcess("CAN Error");
                    }
                    SndStt_PinConfig = SendingPinsStates.WAIT_UDS_EXTEND;
                    progressBar.PerformStep();
                    break;

                case SendingPinsStates.WAIT_UDS_EXTEND:
                    //Debug.WriteLine(" --- Reached State 11: Wait for ExtendDiag Answer ---");
                    bool Ackn_ExtDiagSession = can.getExtDiagAnswer();
                    if (Ackn_ExtDiagSession)
                    {
                        SndStt_PinConfig = SendingPinsStates.SEND_START_FRAME_P1;
                        progressBar.PerformStep();
                    }
                    break;





                case SendingPinsStates.SEND_START_FRAME_P1:
                    var StartingFrame1 = ScreenConfigs.PinConfig_Part1.GetRange(0, 3);
                    can.SendConfigStartFrame(StartingFrame1, 0x19, 0x44);
                    SndStt_PinConfig = SendingPinsStates.WAIT_START_FRAME_ANSWER_P1;
                    break;

                case SendingPinsStates.WAIT_START_FRAME_ANSWER_P1:
                    bool gotFrameAnswer1 = can.getFrameAnswer();
                    if (gotFrameAnswer1)
                    {
                        SndStt_PinConfig = SendingPinsStates.SEND_CONFIG_P1;
                    }
                    break;

                case SendingPinsStates.SEND_CONFIG_P1:
                    can.SendScreenConfigConsecutiveFrames(ScreenConfigs.PinConfig_Part1);
                    SndStt_PinConfig = SendingPinsStates.WAIT_ACK_P1;
                    break;

                case SendingPinsStates.WAIT_ACK_P1:
                    bool gotConfigAck1 = can.getConfigAcknowledgment();
                    if (gotConfigAck1)
                    {
                        SndStt_PinConfig = SendingPinsStates.SEND_START_FRAME_P2;
                        progressBar.PerformStep();
                        //Debug.WriteLine(" --- Reached State 12: Part1 Sent ---");
                    }
                    break;

                case SendingPinsStates.SEND_START_FRAME_P2:
                    var StartingFrame2 = ScreenConfigs.PinConfig_Part2.GetRange(0, 3);
                    can.SendConfigStartFrame(StartingFrame2, 0x19, 0x45);
                    SndStt_PinConfig = SendingPinsStates.WAIT_START_FRAME_ANSWER_P2;
                    break;

                case SendingPinsStates.WAIT_START_FRAME_ANSWER_P2:
                    bool gotFrameAnswer2 = can.getFrameAnswer();
                    if (gotFrameAnswer2)
                    {
                        SndStt_PinConfig = SendingPinsStates.SEND_CONFIG_P2;
                    }
                    break;

                case SendingPinsStates.SEND_CONFIG_P2:
                    can.SendScreenConfigConsecutiveFrames(ScreenConfigs.PinConfig_Part2);
                    SndStt_PinConfig = SendingPinsStates.WAIT_ACK_P2;
                    break;

                case SendingPinsStates.WAIT_ACK_P2:
                    bool gotConfigAck2 = can.getConfigAcknowledgment();
                    if (gotConfigAck2)
                    {
                        SndStt_PinConfig = SendingPinsStates.SEND_START_FRAME_P3;
                        progressBar.PerformStep();
                        //Debug.WriteLine(" --- Reached State 13: Part2 Sent ---");
                    }
                    break;

                case SendingPinsStates.SEND_START_FRAME_P3:
                    var StartingFrame3 = ScreenConfigs.PinConfig_Part3.GetRange(0, 3);
                    can.SendConfigStartFrame(StartingFrame3, 0x19, 0x46);
                    SndStt_PinConfig = SendingPinsStates.WAIT_START_FRAME_ANSWER_P3;
                    break;

                case SendingPinsStates.WAIT_START_FRAME_ANSWER_P3:
                    bool gotFrameAnswer3 = can.getFrameAnswer();
                    if (gotFrameAnswer3)
                    {
                        SndStt_PinConfig = SendingPinsStates.SEND_CONFIG_P3;
                    }
                    break;

                case SendingPinsStates.SEND_CONFIG_P3:
                    can.SendScreenConfigConsecutiveFrames(ScreenConfigs.PinConfig_Part3);
                    SndStt_PinConfig = SendingPinsStates.WAIT_ACK_P3;
                    break;

                case SendingPinsStates.WAIT_ACK_P3:
                    bool gotConfigAck3 = can.getConfigAcknowledgment();
                    if (gotConfigAck3)
                    {
                        SndStt_PinConfig = SendingPinsStates.SEND_START_FRAME_P4;
                        progressBar.PerformStep();
                        //Debug.WriteLine(" --- Reached State 14: Part3 Sent ---");
                    }
                    break;



                case SendingPinsStates.SEND_START_FRAME_P4:
                    var StartingFrame4 = ScreenConfigs.PinConfig_Part4.GetRange(0, 3);
                    can.SendConfigStartFrame(StartingFrame4, 0x19, 0x47);
                    SndStt_PinConfig = SendingPinsStates.WAIT_START_FRAME_ANSWER_P4;
                    break;

                case SendingPinsStates.WAIT_START_FRAME_ANSWER_P4:
                    bool gotFrameAnswer4 = can.getFrameAnswer();
                    if (gotFrameAnswer4)
                    {
                        SndStt_PinConfig = SendingPinsStates.SEND_CONFIG_P4;
                    }
                    break;

                case SendingPinsStates.SEND_CONFIG_P4:
                    can.SendScreenConfigConsecutiveFrames(ScreenConfigs.PinConfig_Part4);
                    SndStt_PinConfig = SendingPinsStates.WAIT_ACK_P4;
                    break;

                case SendingPinsStates.WAIT_ACK_P4:
                    bool gotConfigAck4 = can.getConfigAcknowledgment();
                    if (gotConfigAck4)
                    {
                        SndStt_PinConfig = SendingPinsStates.DONE;
                        progressBar.PerformStep();
                        //Debug.WriteLine(" --- Reached State 15: Part4 Sent ---");
                    }
                    break;

                case SendingPinsStates.DONE:
                    //Debug.WriteLine(" --- Reached State 16: Succesfully Finished ---");
                    progressBar.PerformStep();
                    CurrentConfigStep = ConfigSteps.FREQUENCY_PINS;
                    break;

                default:
                    //Debug.WriteLine("Error C004: Non-Expected Switch state");
                    FailExitSendingProcess("Unexpected State");
                    break;
            }
        }

        private void sendFrequencyPinsConfig()
        {
            int pprVal = ScreenConfigs.PPRevConfigValues[CurrentConfigNr];

            bool res = can.SendFreqPinConfig(pprVal);
            if (res == false)
            {
                FailExitSendingProcess("CAN Error");
                return;
            }
            stopConfigSendingProcess(hardStop: false);
            startSendingBargraphConfiguration();
        }

        private void BargraphConfig_SateMachine()
        {
            switch (SndStt_BargraphConfig)
            {
                case SimpleMsgSendingStates.ENABLE_UDS:
                    Debug.WriteLine(" --- Reached State 1: Enable UDS ---");
                    bool Result = can.EnableUds(CurrentTargetAdr);
                    if (Result == false)
                    {
                        MessageBox.Show("CAN Error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    SndStt_BargraphConfig = SimpleMsgSendingStates.EXTEND_DIAG_SESSION;
                    progressBar.PerformStep();
                    break;

                case SimpleMsgSendingStates.EXTEND_DIAG_SESSION:
                    Debug.WriteLine(" --- Reached State 2: Extend Diag Session ---");
                    bool r1 = can.ExtendDiagSession();
                    if (r1 == false)
                    {
                        Debug.WriteLine("CAN ERROR");
                    }
                    SndStt_BargraphConfig = SimpleMsgSendingStates.WAIT_UDS_EXTEND;
                    progressBar.PerformStep();
                    break;

                case SimpleMsgSendingStates.WAIT_UDS_EXTEND:
                    Debug.WriteLine(" --- Reached State 3: Wait for ExtendDiag Answer ---");
                    bool Ackn_ExtDiagSession = can.getExtDiagAnswer();
                    if (Ackn_ExtDiagSession)
                    {
                        SndStt_BargraphConfig = SimpleMsgSendingStates.SEND_START_FRAME;
                        progressBar.PerformStep();
                    }
                    break;

                case SimpleMsgSendingStates.SEND_START_FRAME:
                    Debug.WriteLine(" --- Reached State 4: Sending start frame ---");
                    bool r2 = can.SendConfigStartFrame(ScreenConfigs.getBargraph_StartingFrame(CurrentConfigNr), 0x18, 0x3C);
                    if (r2 == false)
                    {
                        Debug.WriteLine("Error at state 4");
                    }
                    SndStt_BargraphConfig = SimpleMsgSendingStates.WAIT_START_FRAME_ANSWER;
                    progressBar.PerformStep();
                    break;

                case SimpleMsgSendingStates.WAIT_START_FRAME_ANSWER:
                    Debug.WriteLine(" --- Reached State 5: Wait for answer on startFrame ---");
                    bool gotFrameAnswer = can.getFrameAnswer();
                    if (gotFrameAnswer)
                    {
                        SndStt_BargraphConfig = SimpleMsgSendingStates.SEND_CONFIG;
                        progressBar.PerformStep();
                    }
                    break;

                case SimpleMsgSendingStates.SEND_CONFIG:
                    Debug.WriteLine(" --- Reached State 6: Send entire configuration ---");
                    bool r3 = can.SendScreenConfigConsecutiveFrames(ScreenConfigs.getBargraph_Message(CurrentConfigNr));
                    if (r3 == false)
                    {
                        Debug.WriteLine("CAN Error");
                    }
                    SndStt_BargraphConfig = SimpleMsgSendingStates.WAIT_ACK;
                    progressBar.PerformStep();
                    break;

                case SimpleMsgSendingStates.WAIT_ACK:
                    Debug.WriteLine(" --- Reached State 7: wait for acknowledge ---");
                    bool gotConfigAck = can.getConfigAcknowledgment();
                    if (gotConfigAck)
                    {
                        SndStt_BargraphConfig = SimpleMsgSendingStates.DONE;
                        progressBar.PerformStep();
                    }
                    break;

                case SimpleMsgSendingStates.DONE:
                    Debug.WriteLine(" --- Reached State 8: Succesfully Finished ---");
                    progressBar.PerformStep();
                    stopConfigSendingProcess(hardStop: false);
                    sendAlarmConfigurations();
                    break;

                default:
                    FailExitSendingProcess("Unexpected Status");
                    break;
            }
        }

        private void sendAlarmConfigurations()
        {
            bool result = can.SendAlarmConfigurations(ScreenConfigs.getAlarmConfigVals(CurrentConfigNr));
            if (result == false)
            {
                FailExitSendingProcess("Sending Alarm Configurations Failed");
            }
            else
            {
                progressBar.PerformStep();
                stopConfigSendingProcess(hardStop: true);
                changeStatusLabel(ProcessStates.SUCCESS);
            }
        }
        #endregion

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            can.TerminateCanBusConnection();
        }
    }
}