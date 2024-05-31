using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Canlib;
using static System.Windows.Forms.AxHost;
using static Canlib.CanlibAPI;

namespace Nanni_ScreenConfigurator
{

    public class t_ProductInformation
    {
        // ToDo: should variables be initialized with 0/"" ?
        public t_ProductInformation(UInt16 SrcAdr)
        {
            SourceAdr = SrcAdr;
        }
        public UInt16 SourceAdr;
        // ISO Address
        public UInt16 ManufactCode;
        public UInt16 DeviceClass;
        public UInt16 FuctionCode;
        public UInt16 IndustryGroup;
        //Product Info
        public int ProductCode;
        public string ModelID;
        public string SW_Version;
        public string ModelVersion;
        public string SerialCode;
    }

    public class t_CanMessage
    {
        public t_CanMessage(UInt32 ID)
        {
            Identifier = ID;
        }
        private UInt32 Identifier;
        public int Src;
        public int Pgn;
        //public string Data;
        public List<byte> Data;
        public DateTime TimeStamp;
        public int Priority;
        public int MsgCounter = 0;

        public string MultiFrameBuffer = "";
        public int LastSeqNr = 0xFF;

        public void FillMessageContent(int src = 0, int pgn = 0, List<byte> data = null, DateTime? timestamp = null, int prio = 0)
        {
            Src = src;
            Pgn = pgn;
            Data = data;
            TimeStamp = timestamp ?? DateTime.MinValue;
            Priority = prio;

            MsgCounter ++;
        }

        public UInt32 getID()
        {
            return Identifier;
        }

        public void resetMessageCouter()
        {
            MsgCounter = 0;
        }
    }

    class t_PGN
    {
        public t_PGN(int _n, string _name, int _freq, bool _singleFrame, UInt16 _DefPrio)
        {
            Number = _n;
            Name = _name;
            Frequency = _freq;
            IsSingleFrame = _singleFrame;
            DefaultPriority = _DefPrio;
        }
        public readonly int Number;
        public readonly string Name;
        public readonly int Frequency;
        public readonly bool IsSingleFrame;
        public readonly int DefaultPriority;

        public string PgnInHex()
        {
            return "0x" + Convert.ToString(Number, 16).ToUpper();
        }

        public string get_DefaultPriority()
        {
            if(DefaultPriority != UInt16.MaxValue)
            {
                return DefaultPriority.ToString();
            }
            else
            {
                return "not defined";
            }
        }
    }


    public class t_NameValueUnit
    {
        public string Name = "";
        public float Value = 0;
        public string Unit = "";
        public bool DataAvailable = true;
        public int NumberOfDecimals = 0xFF;
        public bool StringOnly = false;
        public t_NameValueUnit(string name, float value, string unit, bool dataAvailable = true, int numberOfDecimals = 0xFF, bool stringOnly = false)
        {
            Name = name;
            Value = value;
            Unit = unit;
            DataAvailable = dataAvailable;
            NumberOfDecimals = numberOfDecimals;
            StringOnly = stringOnly;
        }
    }

    public class ImportSettings
    {
        public bool IsDefined = false;
        public bool UsingHexaDec;
        public char ColSeparator = '\t';
        public char ByteSeparator = ' ';
        public string FilePath = "";
        public int PageLength = 25;
    }

    public class t_CanMsg_Log
    {
        public int line = -1;
        public int src;
        public int pgn;
        public List<byte> data;
    }
}