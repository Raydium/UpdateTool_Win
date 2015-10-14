嚜簑sing System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Collections;

namespace UPDATE_FIRMWARE
{
    class Parameter
    {
        private XmlDocument XmlDoc;

        public UInt32 u32BootloaderVID, u32BootloaderPID;
        public UInt32 u32FirmwareVID, u32FirmwarePID;

        public UInt32 u32BaseLineEnable;
        public UInt32 u32BurnBootLoaderEnable;
        public UInt32 u32CheckUSBConnectTimeOutSec;

        public UInt32 u32EraseMode;

        public UInt32 u32MaxCodeLength;

        public UInt32 u32AutoBurn;
        public string sImageName;

        public Parameter()
        {
            load_default_config();
        }

        public Parameter(string config_file)
        {
            if (config_file.Equals(""))
            {
                load_default_config();
                return;
            }

            XmlDoc = new XmlDocument();
            XmlDoc.Load(config_file);

            parse_config_file();
        }

        private void load_default_config()
        {
            u32BootloaderVID = 0x2386;
            u32BootloaderPID = 0xFFFF;
            u32FirmwareVID=0x2386;
            u32FirmwarePID = 0x0402;

            u32BaseLineEnable = 0;
            u32BurnBootLoaderEnable= 0;
            u32CheckUSBConnectTimeOutSec = 10;

            u32EraseMode = 1;

            u32MaxCodeLength=0xD000;

            u32AutoBurn=0;
            sImageName="";
        }

        private UInt32 ReadXmlReturnUint32(string sXmlPath, string sAttributes,UInt32 u32DefaultValue)
        {
            UInt32 u32ReturnValue;
            XmlNodeList NodeLists = XmlDoc.SelectNodes(sXmlPath);
            foreach (XmlNode OneNode in NodeLists)
            {               
                foreach (XmlAttribute Attr in OneNode.Attributes)
                {
                    String StrAttr = Attr.Name.ToString();
                    String StrValue = OneNode.Attributes[Attr.Name.ToString()].Value;
                    String StrInnerText = OneNode.InnerText;

                    if (StrAttr.Equals(sAttributes))
                    {
                        u32ReturnValue=Convert.ToUInt32(StrValue,16);
                        return u32ReturnValue;
                    }
                }
            }

            return u32DefaultValue;
        }

        private string ReadXmlReturnString(string sXmlPath, string sAttributes, string sDefaultValue)
        {
            string sReturnValue;
            XmlNodeList NodeLists = XmlDoc.SelectNodes(sXmlPath);
            foreach (XmlNode OneNode in NodeLists)
            {
                foreach (XmlAttribute Attr in OneNode.Attributes)
                {
                    String StrAttr = Attr.Name.ToString();
                    String StrValue = OneNode.Attributes[Attr.Name.ToString()].Value;
                    String StrInnerText = OneNode.InnerText;

                    if (StrAttr.Equals(sAttributes))
                    {
                        sReturnValue = StrValue;
                        return sReturnValue;
                    }
                }
            }

            return sDefaultValue;
        }

        private void parse_config_file()
        {
            //string txt;
            //XmlNodeList NodeLists = XmlDoc.SelectNodes("CONFIG/USB_VID_PID");
            u32BootloaderVID = ReadXmlReturnUint32("CONFIG/USB_VID_PID", "BOOTLOADER_VID",0x2386);
            u32BootloaderPID = ReadXmlReturnUint32("CONFIG/USB_VID_PID", "BOOTLOADER_PID",0xFFFF);
           u32FirmwareVID = ReadXmlReturnUint32("CONFIG/USB_VID_PID", "AP_VID", 0x2386);
            u32FirmwarePID = ReadXmlReturnUint32("CONFIG/USB_VID_PID", "AP_PID", 0x0402);

            u32BaseLineEnable = ReadXmlReturnUint32("CONFIG/FUNCTION", "BaseLineEnable", 0x0000);
            u32BurnBootLoaderEnable = ReadXmlReturnUint32("CONFIG/FUNCTION", "BurnBootLoaderEnable", 0x0000);
            u32CheckUSBConnectTimeOutSec = ReadXmlReturnUint32("CONFIG/FUNCTION", "CheckUSBConnectTimeOutSec", 10);

            u32EraseMode = ReadXmlReturnUint32("CONFIG/ERASE_FLASH", "EraseMode", 0x0001);

            u32MaxCodeLength = ReadXmlReturnUint32("CONFIG/CODE_SETTING", "MaxCodeLength", 0xd000);

            u32AutoBurn = ReadXmlReturnUint32("CONFIG/AUTO_BURN", "AutoBurn", 0x0000);
            sImageName = ReadXmlReturnString("CONFIG/AUTO_BURN", "ImageName", "");
        }
    }
}
