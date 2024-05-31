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
        public NanniConfigurations()
        {
            AddConfig01();
        }


        private Dictionary<int, List<byte>> ConfigMessages = new Dictionary<int, List<byte>>();
        
        private void AddConfig01()
        {
            List<byte> Config01 = new List<byte>();

            Config01.Add(0x02);
            Config01.Add(0x01);
            Config01.Add(0x05);

            Config01.Add(0x21);
            Config01.Add(0x04);
            Config01.Add(0x07);
            Config01.Add(0x03);
            Config01.Add(0x01);
            Config01.Add(0x0);
            Config01.Add(0x0);
            Config01.Add(0x0);

            Config01.Add(0x22);
            Config01.Add(0x03);
            Config01.Add(0x01);
            Config01.Add(0x0);
            Config01.Add(0x0);
            Config01.Add(0x0A);
            Config01.Add(0x01);
            Config01.Add(0x05);

            Config01.Add(0x23);
            Config01.Add(0x0);
            Config01.Add(0x04);
            Config01.Add(0x10);
            Config01.Add(0x0);
            Config01.Add(0x0);
            Config01.Add(0x0);
            Config01.Add(0x0D);

            Config01.Add(0x24);
            Config01.Add(0x0);
            Config01.Add(0x0);
            Config01.Add(0x0);
            Config01.Add(0x0);
            Config01.Add(0x0);
            Config01.Add(0x0);
            Config01.Add(0x0);

            Config01.Add(0x25);
            Config01.Add(0x0);
            Config01.Add(0x0);
            Config01.Add(0x0);
            Config01.Add(0x0);
            Config01.Add(0x0);
            Config01.Add(0x0);
            Config01.Add(0x0);

            Config01.Add(0x26);
            Config01.Add(0x0);
            Config01.Add(0x0);
            Config01.Add(0x0);
            Config01.Add(0x0);
            Config01.Add(0x0);
            Config01.Add(0x0);
            Config01.Add(0x0);

            Config01.Add(0x27);
            Config01.Add(0x0);
            Config01.Add(0x0);
            Config01.Add(0x0);
            Config01.Add(0x0);
            Config01.Add(0x0);
            Config01.Add(0x0);
            // BYTE 8

            ConfigMessages.Add(1, Config01);
        }

        private void AddConfig02()
        {
            List<byte> Config02 = new List<byte>();

            /*Config01.Add(0x02);
            Config01.Add(0x02);
            Config01.Add(0x02);

            Config01.Add(0x);
            Config01.Add(0x);
            Config01.Add(0x);
            Config01.Add(0x);
            Config01.Add(0x);
            Config01.Add(0x);
            Config01.Add(0x);
            Config01.Add(0x);*/
            ConfigMessages.Add(2, Config02);
        }


        public List<byte> getConfig(int configNr)
        {
            List<byte> config = new List<byte>();
            if(ConfigMessages.ContainsKey(configNr))
            {
                config = ConfigMessages[configNr];
            }
            else
            {
                Debug.WriteLine("ERROR C003: Invalid Config Message requested!");
            }
            return config;
        }

        public List<byte> getConfig_StartingFrame(int configNr)
        {
            List<byte> config = new List<byte>();
            if (ConfigMessages.ContainsKey(configNr))
            {
                for(int i=0; i<3; i++)
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
    }
}
