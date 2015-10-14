嚜?/#define VERIFY_ERASE_FLASH

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using USBHID;




namespace UPDATE_FIRMWARE
{
    class UpdateFirmwareControl
    {
        string BurnFileName;
        string sConfigFileName ;//= "UpdateFirmwareConfig.xml";
        IntelHexBinOperation cIntelHexBinOperation;
        Parameter MyParameter;
        public string sShowMessage;
        BootloaderHidAPI BootHidAPI;
        byte[] FlashReadBuffer ;
        byte[] FlashWriteBuffer;
        HidAPI myHid;

        int TotalPageNum;

        UInt32 CheckSum;
        bool IsVerifyCheckSumBeforeProgramFlash;
        int USBConnectTimeOutSec;

        public UpdateFirmwareControl(ref HidAPI Hid)
        {

            Initiali(ref Hid);
            sConfigFileName="UpdateFirmwareConfig.xml";
            IsVerifyCheckSumBeforeProgramFlash = false;

        }

        public UpdateFirmwareControl(ref HidAPI Hid, string configfile, int vid, int pid, bool IsVerifyCheckSumBeforeProgramFlash)
        {
            
            Initiali(ref Hid);
            sConfigFileName = configfile;
            MyParameter.u32FirmwareVID = Convert.ToUInt32(vid);
            MyParameter.u32FirmwarePID = Convert.ToUInt32(pid);
            this.IsVerifyCheckSumBeforeProgramFlash = IsVerifyCheckSumBeforeProgramFlash;
        }


        public void Initiali(ref HidAPI Hid)
        {
            myHid = Hid;
            sConfigFileName = "UpdateFirmwareConfig.xml";
            MyParameter = new Parameter(sConfigFileName);
            sShowMessage = "";
            BurnFileName = "";
            USBConnectTimeOutSec = Convert.ToInt32(MyParameter.u32CheckUSBConnectTimeOutSec);
            
        }

        public void SetHexFilePath(string path)
        {
            BurnFileName = path; 
        }

        public void ReadConfig()
        {
        }

        public int GetNowPageNum()
        {
            if (BootHidAPI == null)
                return 0;
            else
                return Convert.ToInt32(BootHidAPI.GetNowPageNum());
        }

        public int GetTotalPageNum()
        {
            //int Total=( (FlashWriteBuffer.Length+0x7F)/0x80 );
            return TotalPageNum;
        }

        public void SetTotalPageNum(int Length)
        {
            TotalPageNum = ((Length + 0x7F) / 0x80);
            
        }


        public int GetProgramValue()
        {
            if (FlashWriteBuffer == null)
                return 0;
            else
            {
                int ProgramValue = GetNowPageNum()*100 / GetTotalPageNum();
                return ProgramValue;
            }
        }

        public bool Start()
        {
            int i;

            if (File.Exists(BurnFileName) == false)
            {
                ShowToMessage("NG (Can't Find Hex File)");
                return false;
            }

            BootHidAPI = new BootloaderHidAPI(ref myHid);
            cIntelHexBinOperation = new IntelHexBinOperation();


            cIntelHexBinOperation.SetFileName(BurnFileName);
            cIntelHexBinOperation.ReadHexFile();
            FlashWriteBuffer = cIntelHexBinOperation.GetBinary();
            SetTotalPageNum(FlashWriteBuffer.Length);

            CheckSum = 0;
            for (i = 0; i < FlashWriteBuffer.Length; i++)
            {
                CheckSum = Convert.ToUInt32(FlashWriteBuffer[i] + CheckSum);
            }
            CheckSum = CheckSum & 0x0000ffff;



            ShowToMessage("Start");
            if (BootHidAPI.Connect(MyParameter.u32BootloaderVID, MyParameter.u32BootloaderPID) == false)
            {

                
                
                
                if (BootHidAPI.Connect(MyParameter.u32FirmwareVID, MyParameter.u32FirmwarePID) == true)
                {
                    if (IsVerifyCheckSumBeforeProgramFlash == true)
                    {
                        if (BootHidAPI.VerifyCheckSumAndCodeLength(Convert.ToUInt16(CheckSum), Convert.ToUInt16(FlashWriteBuffer.Length)) == true)
                        {
                            ShowToMessage("Checksum is the same.");
                            return true;
                        }
                    }
                    
                    
                    
                    BootHidAPI.JumpToBootloaderFormAP();
                    for (i = 0; i < USBConnectTimeOutSec; i++)
                    {
                        Thread.Sleep(1000);
                        if (BootHidAPI.Connect(MyParameter.u32BootloaderVID, MyParameter.u32BootloaderPID) == true)
                            break;
                    }
                }
            }

            if (BootHidAPI.Connect(MyParameter.u32BootloaderVID, MyParameter.u32BootloaderPID) == false)
            {
                ShowToMessage("NG (Bootloader No Link)");
                return false;
            }

            

            if (FlashWriteBuffer.Length > MyParameter.u32MaxCodeLength)
            {
                ShowToMessage("NG (FW Length is Oversize)");
                return false;
            }
            if (FlashWriteBuffer.Length < 0x2005)
            {
                ShowToMessage("NG (FW Length is too Short)");
                return false;
            }
            if (FlashWriteBuffer[0x2000]    != 'R'
                ||FlashWriteBuffer[0x2001]  != 'T'
                ||FlashWriteBuffer[0x2002]  != 'S'
                || FlashWriteBuffer[0x2003] != 'D'
                || FlashWriteBuffer[0x2004] != 'K' 
                )
            {
               ShowToMessage("NG (Flash 0x2000 not RTSDK)");
                return false;
            }




            ShowToMessage("Erasing Flash!!");
            if (BootHidAPI.UnlockFlashBoot() == false)
            {
                ShowToMessage("NG (Unlock Flash Fail)");
                return false;
            }
            if (BootHidAPI.EraseFirmwareFlash(MyParameter.u32EraseMode) == false)
            {
                ShowToMessage("NG (Erase Flash Fail)");
                return false;
            }

#if VERIFY_ERASE_FLASH //check erase flash
            ShowToMessage("Verify Erasing Flash!!");
            UInt32 EraseLength = 0;
            if (MyParameter.u32EraseMode == 1)
            {
                 EraseLength = 0xd000;
            }
            else if (MyParameter.u32EraseMode == 2)
            {
                EraseLength = 0xf000;
            }
            SetTotalPageNum(Convert.ToInt32(EraseLength));

            if (BootHidAPI.ReadFlash(ref FlashReadBuffer, EraseLength) == false)
            {
                ShowToMessage("NG (Read Flash Fail-Erase)");
                return false;
            }
            Thread.Sleep(1000);

            ShowToMessage("Verify Erasing Flash!!");

            byte[] EraseFlashBuffer = new byte[EraseLength];
            for (i = 0; i < EraseLength; i++)
            {
                EraseFlashBuffer[i] = 0xff;
            }

            if (Enumerable.SequenceEqual(EraseFlashBuffer, FlashReadBuffer) == false)
                {
                    ShowToMessage("NG (Verify Flash Fail-Erase)");
                    return false;
                }

            SetTotalPageNum(FlashWriteBuffer.Length);
#endif
            ShowToMessage("Programming Flash!!");
            if (BootHidAPI.WriteFlash(FlashWriteBuffer, (UInt32)FlashWriteBuffer.Length) == false)
            {
                ShowToMessage("NG (Program Flash Fail)");
                return false;
            }
            Thread.Sleep(500);
            if (BootHidAPI.LockFlashBoot() == false)
            {
                ShowToMessage("NG (Lock Flash Fail)");
                return false;
            }

            ShowToMessage("Verifying!!");
            if (BootHidAPI.ReadFlash(ref FlashReadBuffer, (UInt32)FlashWriteBuffer.Length) == false)
            {
                ShowToMessage("NG (Read Flash Fail)");
                return false;
            }

            if (Enumerable.SequenceEqual(FlashWriteBuffer, FlashReadBuffer) == false)
            {
                ShowToMessage("NG (Verify Flash Fail)");
                return false;
            }

            BootHidAPI.JumpToAPFormBootloader();
            ShowToMessage("Check FW Communication!!");

            for (i = 0; i < USBConnectTimeOutSec; i++)
            {
                Thread.Sleep(1000);
                if (BootHidAPI.Connect(MyParameter.u32FirmwareVID, MyParameter.u32FirmwarePID) == true)
                    break;
            }
            if (BootHidAPI.Connect(MyParameter.u32FirmwareVID, MyParameter.u32FirmwarePID) == false)
            {
                ShowToMessage("NG (Burned FW Communication Fail)");
                return false;
            }

          
            
            ShowToMessage("Burn CheckSum!!");
           
            if (BootHidAPI.BurnCheckSumAndCodeLength(Convert.ToUInt16(CheckSum), Convert.ToUInt16(FlashWriteBuffer.Length)) == false)
            {
                ShowToMessage("NG (Burned CheckSum Fail)");
                return false;
            }

            if (BootHidAPI.VerifyCheckSumAndCodeLength(Convert.ToUInt16(CheckSum), Convert.ToUInt16(FlashWriteBuffer.Length)) == false)
            {
                ShowToMessage("NG (Verify CheckSum Fail)");
                return false;
            }

            ShowToMessage("Burn Finish!!" + " Checksum=" + String.Format("{0:X}", CheckSum));            
                return true;
        }

        private void ShowToMessage(string sMessage)
        {
            sShowMessage=sMessage;
        }
    }


}
