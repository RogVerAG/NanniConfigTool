using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nanni_ScreenConfigurator
{
    internal class NanniConfigurations
    {
        private const byte EngTwoOffset = 46;
        private readonly Dictionary<int, List<byte>> ScreenConfigMessages = new();
        public readonly Dictionary<int, int> PPRevConfigValues = new()
        {
            // Key = ConfigNr
            // Value = Pulses Per revolution * 100 (in order to allow for decimals)
            {1,  6000},   // Configuration 01 = 60 pprev
            {2,  6000},   // Configuration 02 = 60 pprev
            {3,  6000},   // Configuration 03 = 60 pprev
            {4,  6000},   // Configuration 04 = 60 pprev
            {5,  6000},   // Configuration 05 = 60 pprev
            {6,  6000},   // Configuration 06 = 60 pprev
            {7,  6000},   // Configuration 07 = 60 pprev
            {8,  6000},   // Configuration 08 = 60 pprev
            {9,  6000},   // Configuration 09 = 60 pprev
            {10,  6000},  // Configuration 10 = 60 pprev
            {11,  6000},  // Configuration 11 = 60 pprev
            {12,  6000}   // Configuration 12 = 60 pprev
        };

        public NanniConfigurations()
        {
            AssignScreenConfigMessages();
        }

        #region Getters
        public List<byte> getScreenConfig(int configNr)
        {
            List<byte> config = new();
            if (ScreenConfigMessages.ContainsKey(configNr))
            {
                config = ScreenConfigMessages[configNr];
            }
            else
            {
                Debug.WriteLine("ERROR C003: Invalid Config Message requested!");
            }
            return config;
        }

        public List<byte> getScreenConfig_StartingFrame(int configNr)
        {
            List<byte> config = new List<byte>();
            if (ScreenConfigMessages.ContainsKey(configNr))
            {
                for (int i = 0; i < 3; i++)
                {
                    config.Add(ScreenConfigMessages[configNr].ElementAt(i));
                }
            }
            else
            {
                Debug.WriteLine("ERROR C004: Invalid Config Message requested!");
            }
            return config;
        }

        public List<byte> getCustomSensor_StartingFrame()
        {
            return SensorTypeCustom_Message.GetRange(0, 3);
        }

        public List<byte> getCustomSensorMessage()
        {
            return SensorTypeCustom_Message;
        }
        #endregion

        #region ScreenConfig_Definitions
        private void AssignScreenConfigMessages()
        {
            // Assign the prepared configuration messages to the different configurations 
            ScreenConfigMessages.Add( 1, ScrConfig01);
            ScreenConfigMessages.Add( 2, ScrConfig02);
            ScreenConfigMessages.Add( 3, ScrConfig01);
            ScreenConfigMessages.Add( 4, ScrConfig04);
            ScreenConfigMessages.Add( 5, ScrConfig05);
            ScreenConfigMessages.Add( 6, ScrConfig04);
            ScreenConfigMessages.Add( 7, ScrConfig01);
            ScreenConfigMessages.Add( 8, ScrConfig02);
            ScreenConfigMessages.Add( 9, ScrConfig01);
            ScreenConfigMessages.Add(10, ScrConfig04);
            ScreenConfigMessages.Add(11, ScrConfig05);
            ScreenConfigMessages.Add(12, ScrConfig04);
        }

        private readonly List<byte> ScrConfig01 = new()
        {
            0x02,   // scr1 - screen type: 2=bar
            0x01,   // scr1 - engine1 rpm 
            0x08,   // scr1 - engine1 oil Press (! not same code as on data screens !)

            0x21,
            0x04,   // scr1 - engine1 coolant temp
            0x05,   // scr1 - engine1 battery voltage
            0x04,   // scr2 - screen type: 4 = Quad
            0x01,   // scr2 - upper left = eng1-RPM
            0x00,
            0x00,
            0x00,

            0x22,
            0x04,   // scr3 - screen type: 4= quad  &&  scr2 - upper right = coolant
            0x01,   // scr3 - upper = eng1-rpm
            0x00,
            0x00,
            0x0A,   // scr2 - lower left = eng1-oil press
            0x00,   
            0x00,   

            0x23,
            0x00,
            0x05,   // scr2 - lower right = battery voltage
            0x10,   // scr3 - lower left = fuel level
            0x00,   
            0x00,   
            0x00,
            0x0D,   // scr3 - lower rigt = eng1-hours

            0x24,  0x00,   0x00,   0x00,   0x00,   0x00,   0x00,   0x00,
            0x25,  0x00,   0x00,   0x00,   0x00,   0x00,   0x00,   0x00,
            0x26,  0x00,   0x00,   0x00,   0x00,   0x00,   0x00,   0x00,
            0x27,  0x00,   0x00,   0x00,   0x00,   0x00,   0x00
        };

        private readonly List<byte> ScrConfig02 = new()
        {
            0x02,   // scr1 - screen type: 2=bar
            0x01,   // scr1 - engine1 rpm 
            0x08,   // scr1 - engine1 oil Press (! not same code as on data screens !)

            0x21,
            0x04,   // scr1 - engine1 coolant temp
            0x05,   // scr1 - engine1 battery voltage
            0x04,   // scr2 - screen type: 4 = Quad
            0x01,   // scr2 - upper left = eng1-RPM
            0x00,
            0x00,
            0x00,

            0x22,
            0x04,   // scr3 - screen type: 4= quad  &&  scr2 - upper right = coolant
            0x01,   // scr3 - upper = eng1-rpm
            0x00,
            0x00,
            0x0A,   // scr2 - lower left = eng1-oil press
            0x04,   // scr4 - screen type: 4 = quad   &&  scr3 upper right
            0x03,   // scr4 - upper left = boost

            0x23,
            0x00,
            0x05,   // scr2 - lower right = battery voltage
            0x10,   // scr3 - lower left = fuel level
            0x01,   // scr4 - upper right = rpm   &&  scr5 = single
            0x01,   // scr5 = rpm
            0x00,
            0x0D,   // scr3 - lower rigt = eng1-hours

            0x24,
            0x06,   // scr 4 - 6 = fuel rate
            0x00,
            0x00,
            0x00,
            0x07,   // scr 4 - 7 = engine load
            0x00,
            0x00,

            0x25,  0x00,    0x00,   0x00,   0x00,   0x00,   0x00,   0x00,
            0x26,  0x00,    0x00,   0x00,   0x00,   0x00,   0x00,   0x00,
            0x27,  0x00,   0x00,   0x00,   0x00,   0x00,   0x00
        };

        // ScrConfig03 = ScrConfig 01

        private readonly List<byte> ScrConfig04 = new()
        {
            0x02,   // scr1 - screen type: 2=bar
            0x01+EngTwoOffset,   // scr1 - engine1 rpm 
            0x08+EngTwoOffset,   // scr1 - engine1 oil Press (! not same code as on data screens !)

            0x21,
            0x04+EngTwoOffset,   // scr1 - engine1 coolant temp
            0x05+EngTwoOffset,   // scr1 - engine1 battery voltage
            0x03,                // scr2 - screen type: 3 = triple
            0x01+EngTwoOffset,   // scr2 - upper left = eng1-RPM
            0x00,
            0x00,
            0x00,

            0x22,
            0x03,                // scr3 - screen type: 3= triple
            0x01+EngTwoOffset,   // scr3 - upper = eng1-rpm
            0x00,
            0x00,
            0x0A+EngTwoOffset,   // scr2 - lower left = eng1-oil press
            0x01,                // scr4 = single
            0x05+EngTwoOffset,   // scr4 = battey voltage

            0x23,
            0x00,
            0x04+EngTwoOffset,   // scr2 - lower right = coolant
            0x3F,                // scr3 - lower left = fuel level 2
            0x00,
            0x00,
            0x00,
            0x0D+EngTwoOffset,   // scr3 - lower rigt = eng1-hours

            0x24,  0x00,   0x00,   0x00,   0x00,   0x00,   0x00,   0x00,
            0x25,  0x00,   0x00,   0x00,   0x00,   0x00,   0x00,   0x00,
            0x26,  0x00,   0x00,   0x00,   0x00,   0x00,   0x00,   0x00,
            0x27,  0x00,   0x00,   0x00,   0x00,   0x00,   0x00
        };

        private readonly List<byte> ScrConfig05 = new()
        {
            0x02,   // scr1 - screen type: 2=bar
            0x01+EngTwoOffset,   // scr1 - engine1 rpm 
            0x08+EngTwoOffset,   // scr1 - engine1 oil Press (! not same code as on data screens !)

            0x21,
            0x04+EngTwoOffset,   // scr1 - engine1 coolant temp
            0x05+EngTwoOffset,   // scr1 - engine1 battery voltage
            0x03,                // scr2 - screen type: 3 = triple
            0x01+EngTwoOffset,   // scr2 - upper left = eng1-RPM
            0x00,
            0x00,
            0x00,

            0x22,
            0x03,                // scr3 - screen type: 3= triple
            0x01+EngTwoOffset,   // scr3 - upper = eng1-rpm
            0x00,
            0x00,
            0x0A+EngTwoOffset,   // scr2 - lower left = eng1-oil press
            0x03,                // scr4 = triple
            0x01+EngTwoOffset,   // scr4 = upper left = rpm

            0x23,
            0x00,
            0x04+EngTwoOffset,   // scr2 - lower right = coolant
            0x3F,                // scr3 - lower left = fuel level 2
            0x03,                // scr5 - triple
            0x01+EngTwoOffset,   // scr5 - upper = rpm
            0x00,
            0x0D+EngTwoOffset,   // scr3 - lower rigt = eng1-hours

            0x24,  
            0x06+EngTwoOffset,   // scr4 lower left - fuel rate
            0x00,
            0x00,
            0x00,   
            0x07+EngTwoOffset,   // scr4 lower right - engine load
            0x05+EngTwoOffset,   // scr5 - battery voltage   
            0x00, 

            0x25,
            0x00,
            0x00,   
            0x03+EngTwoOffset,   // scr5 - boost   
            0x00,    
            0x00,   
            0x00,   
            0x00,

            0x26,  0x00,   0x00,   0x00,   0x00,   0x00,   0x00,   0x00,
            0x27,  0x00,   0x00,   0x00,   0x00,   0x00,   0x00
        };
        #endregion

        #region PinConfig_Definitions
        private readonly List<byte> StandardCurves_P8_Coolant120_P9OilPress10_Part1 = new()
        {
            0x5E, 0x0B, 0x3C,
            0x21, 0x05, 0xBC, 0x02, 0x7C, 0x01, 0xDC, 0x00,
            0x22, 0x53, 0x7A, 0x23, 0x82, 0xF3, 0x89, 0xC3,
            0x23, 0x91, 0x93, 0x99, 0x4, 0xFF
        };
        private readonly List<byte> StandardCurves_P8_Coolant120_P9OilPress10_Part2 = new()
        {
            0x64, 0x00, 0x08,
            0x21, 0x02, 0x70, 0x03, 0xD8, 0x4, 0x30, 0x7,
            0x22, 0x00, 0x00, 0xD0, 0x07, 0xA0, 0x0F, 0x70,
            0x23, 0x17, 0x10, 0x27, 0x6, 0xFF
        };
        private readonly List<byte> StandardCurves_P8_Coolant120_P9OilPress10_Part3 = new()
        {
            0x0, 0x00, 0x0,
            0x21, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
            0x22, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
            0x23, 0x0, 0x0, 0x0, 0x3, 0xFF
        };
        private readonly List<byte> StandardCurves_P8_Coolant120_P9OilPress10_Part4 = new()
        {
            0x0, 0x00, 0x0,
            0x21, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
            0x22, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
            0x23, 0x0, 0x0, 0x0, 0x3, 0xFF
        };



        public List<byte> PinConfig_Part1 = new();
        public List<byte> PinConfig_Part2 = new();
        public List<byte> PinConfig_Part3 = new();
        public List<byte> PinConfig_Part4 = new();

        public void defineCurrentPinConfig(string configName)
        {
            switch(configName)
            {
                case "StandardCurves_P8_Coolant120_P9OilPress10":
                    PinConfig_Part1 = StandardCurves_P8_Coolant120_P9OilPress10_Part1;
                    PinConfig_Part2 = StandardCurves_P8_Coolant120_P9OilPress10_Part2;
                    PinConfig_Part3 = StandardCurves_P8_Coolant120_P9OilPress10_Part3;
                    PinConfig_Part4 = StandardCurves_P8_Coolant120_P9OilPress10_Part4;
                    break;
            }
        }

        // Message for: Sensor-type = "CUSTOM"
        private static List<byte> SensorTypeCustom_Message = new()
        {
            12, 0,0,
            0x20, 0,0,0,0,0,0,0,
            0x21, 12,0,0,0,0,0,0,
            0x22, 0,0,0,0,0,0,0,//0,0,0,4,0,0,0,
            0x23, 0,0,0,0,0,0,0,//0,0,0,0,0,0,4,
            0x24, 0,0,0,0,0,0,0,
            0x25, 0,0
        };
        #endregion

        #region BargraphConfigs
        private readonly List<byte> BargraphSettings_12V_System = new()
        {
            0x10, 0x18, 0x2E, 0x01, 0x3C,
            0x21, 0x00, 0x00, 0x00, 0x00, 0x0/*Oil Press - Low 1*/, 0x0/*Oil Press - Low 2*/, 0x01/*Oil Press - High 1*/,
            0x22, 0xF4/*Oil Press - High 1*/, 0x0/*Eng Temp - low 1*/, 0x0/*Eng Temp - low 2*/, 0x0/*Eng Temp - High 1*/, 0x0/*Eng Temp - High 2*/, 0x0/*Bat Low 1*/, 0x0/*Bat Low 2*/,
            0x23, 0x0/*Bat High 1*/, 0x0/*Bat High 2*/, 0x0, 0x0, 0x0, 0x0
        };
        private readonly List<byte> BargraphSettings_24V_System = new()
        {
            0x10, 0x18, 0x2E, 0x01, 0x3C,
            0x21, 0x00, 0x00, 0x00, 0x00, 0x0/*Oil Press - Low 1*/, 0x0/*Oil Press - Low 2*/, 0x01/*Oil Press - High 1*/,
            0x22, 0xF4/*Oil Press - High 1*/, 0x0/*Eng Temp - low 1*/, 0x0/*Eng Temp - low 2*/, 0x0/*Eng Temp - High 1*/, 0x0/*Eng Temp - High 2*/, 0x0/*Bat Low 1*/, 0x0/*Bat Low 2*/,
            0x23, 0x0/*Bat High 1*/, 0x0/*Bat High 2*/, 0x0, 0x0, 0x0, 0x0
        };
        #endregion

    }
}
