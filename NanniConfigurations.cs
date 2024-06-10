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
        private readonly Dictionary<int, List<byte>> ConfigMessages = new();

        public NanniConfigurations()
        {
            AddConfig01();
            AddConfig02();
            AddConfig03();
        }

        #region Getters
        public List<byte> getConfig(int configNr)
        {
            List<byte> config = new();
            if (ConfigMessages.ContainsKey(configNr))
            {
                config = ConfigMessages[configNr];
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
            if (ConfigMessages.ContainsKey(configNr))
            {
                for (int i = 0; i < 3; i++)
                {
                    config.Add(ConfigMessages[configNr].ElementAt(i));
                }
            }
            else
            {
                Debug.WriteLine("ERROR C004: Invalid Config Message requested!");
            }
            return config;
        }

        #endregion

        #region ScreenConfig_Definitions
        private void AddConfig01()
        {
            List<byte> Config01 = new()
            {
                0x02,   // scr1 - screen type: 2=bar
                0x01,   // scr1 - engine1 rpm 
                0x05,   // scr1 - engine1 battery

                0x21,
                0x04,   // scr1 - engine1 coolant temp
                0x07,   // scr1 - engine1 fuel level    --> SCREEN ONE FINISHED
                0x03,   // scr2 - screen type: 3=triple
                0x01,   // scr2 - upper = eng1-RPM
                0x00,
                0x00,
                0x00,

                0x22,
                0x03,   // scr3 - screen type: 3=triple
                0x01,   // scr3 - upper = eng1-rpm
                0x00,
                0x00,
                0x0A,   // scr2 - lower left = eng1-oil press
                0x01,   // scr4 - screen type: 1=single
                0x05,   // scr4 - engine1 battery

                0x23,
                0x00,
                0x04,   // scr2 - lower right = coolant
                0x10,   // scr3 - lower left = fuel level
                0x00,
                0x00,
                0x00,
                0x0D,   // scr3 - lower rigt = eng1-hours

                0x24,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,

                0x25,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,

                0x26,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,

                0x27,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00
            };

            ConfigMessages.Add(1, Config01);
        }

        private void AddConfig02()
        {
            List<byte> Config = new List<byte>();

            /*Config01.Add(0x02);
            Config.Add(0x02);
            Config.Add(0x02);

            Config.Add(0x);
            Config.Add(0x);
            Config.Add(0x);
            Config.Add(0x);
            Config.Add(0x);
            Config.Add(0x);
            Config.Add(0x);
            Config.Add(0x);*/
            ConfigMessages.Add(2, Config);
        }

        private void AddConfig03()
        {
            List<byte> Config = new List<byte>();

            /*Config.Add(0x02);
            Config.Add(0x02);
            Config.Add(0x02);

            Config.Add(0x);
            Config.Add(0x);
            Config.Add(0x);
            Config.Add(0x);
            Config.Add(0x);
            Config.Add(0x);
            Config.Add(0x);
            Config.Add(0x);*/
            ConfigMessages.Add(3, Config);
        }
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
        #endregion
    }
}
