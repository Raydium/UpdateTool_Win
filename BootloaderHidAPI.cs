嚜簑sing USBHID;
using System;
using System.Collections.Generic;
using System.Text;

namespace UPDATE_FIRMWARE
{
    class BootloaderHidAPI
    {
        const int ErrorCount = 1000;
        
        //firmware USB
        const byte AP_ADDR_JUMP_TO_BOOTLOADER   = 0X52;
        const byte AP_ADDR_CMD_WRITECHECKSUM    = 0x6D;
        const byte AP_ADDR_CMD_WRITEREGISTER    = 0x71;
        const byte AP_ADDR_SET_SRAM_ADDR_AND_SIZE  = 0x3A;
        const byte AP_ADDR_READ_SRAM               = 0x3C;
        const int AP_USB_LENGTH = 60;
        
        //firmware flash
        const UInt32 KEY1_ADDRESS = 0x50000904; 
        const UInt32 UNLOCK_KEY1  =  0X000000A5;
        const UInt32 LOCK_KEY1 = 0X00000000;

        const UInt32 KEY3_ADDRESS  =  0x5000090C;
        const UInt32 UNLOCK_KEY3   =  0x000000D7;
        const UInt32 LOCK_KEY3 = 0x00000000;
        readonly UInt32[] gu32FlashUnlockKeyAddress = { KEY3_ADDRESS, KEY1_ADDRESS, KEY1_ADDRESS, KEY1_ADDRESS, KEY3_ADDRESS };
        readonly UInt32[] gu32FlashUnlockKeyValue = { UNLOCK_KEY3, UNLOCK_KEY1, LOCK_KEY1, UNLOCK_KEY1, LOCK_KEY3 };

        const UInt32 FLASH_LENGTH = 0xf000;
        const UInt32 CHECKSUMADDR =0X0000FE00;
        const UInt32 PAGESIZE = 0x80;

        //Bootloader USB
        const int Boot_USB_LENGTH = 259;
        const int Boot_USB_REPORT_ID_LENGTH = 1;
        const int Boot_USB_DATA_LENGTH = Boot_USB_LENGTH - Boot_USB_REPORT_ID_LENGTH;
        const int Boot_USB_REPORT_CMD_LENGTH = 1;
        const int Boot_USB_LOAD_LENGTH = Boot_USB_LENGTH - Boot_USB_REPORT_ID_LENGTH - Boot_USB_REPORT_CMD_LENGTH;
        const byte Boot_REPORT_ID = 0x00;


        //bootloader status
        const byte CMD_CHKVERSION = 0x01;
        const byte CMD_ERASEFLASH = 0x02;
        const byte ERASEFLASH_MODE1 = 0x0A1;
        const byte ERASEFLASH_MODE2 = 0x0A2;
        const byte CMD_WRITEFLASH = 0x03;
        const byte CMD_READFLASH = 0x04;
        const byte CMD_JUMPTOAP = 0x05;
        const byte CMD_SELECTFLASH = 0x06;
        const byte SELECTFLASH_BURN_BOOTLOADER = 0x01;
        const byte SELECTFLASH_BURN_AP = 0x00;
        const byte CMD_WRITECHECKSUM = 0x07;
        const byte CMD_WRITEREGISTER = 0x08;
        const byte CMD_IDLE = 0xFF;

        UInt32 PageNum;

        HidAPI myHid;

        public UInt32 GetNowPageNum()
        {
            return PageNum;
        }

        public BootloaderHidAPI(ref HidAPI Hid)
        {
            myHid = Hid;
        }

        public bool Connect(UInt32 pid,UInt32 vid)
        {
            myHid.SetVidPid((int)pid, (int)vid);
            myHid.ConnectDevice();

            return myHid.myDeviceDetected;
        }

        public bool JumpToBootloaderFormAP()
        {
            byte[] buf = new byte[AP_USB_LENGTH];
            myHid.write(AP_ADDR_JUMP_TO_BOOTLOADER, buf, AP_USB_LENGTH);
            return true;
        }

        public bool UnlockFlashAP()
        {
            byte[] USBLoadBuffer;
            byte[] RegAddressTmp;
            byte[] RegValueTmp;
            bool success;
            USBLoadBuffer = new byte[AP_USB_LENGTH];



            for (int i = 0; i < (gu32FlashUnlockKeyAddress.Length); i++)
            {
                RegAddressTmp = BitConverter.GetBytes(gu32FlashUnlockKeyAddress[i]);
                RegValueTmp = BitConverter.GetBytes(gu32FlashUnlockKeyValue[i]);


                Array.Copy(RegAddressTmp, 0, USBLoadBuffer, 0, 4);
                Array.Copy(RegValueTmp, 0, USBLoadBuffer, 4, 4);


                success = myHid.write(AP_ADDR_CMD_WRITEREGISTER, USBLoadBuffer, AP_USB_LENGTH);

                if (success == false)
                    return false;
            }
            return true;
        }

        public bool BurnCheckSumAndCodeLength(UInt16 CheckSum,UInt16 Length)
        {
            byte[] USBLoadBuffer;
            bool success;
            USBLoadBuffer = new byte[AP_USB_LENGTH];
            if( UnlockFlashAP()==false )
                return false;

            Array.Copy(BitConverter.GetBytes(Length), 0, USBLoadBuffer, 0, 2);
            Array.Copy(BitConverter.GetBytes(CheckSum), 0, USBLoadBuffer, 2, 2);
           
            success = myHid.write(AP_ADDR_CMD_WRITECHECKSUM, USBLoadBuffer, AP_USB_LENGTH);

            if (success == false)
                return false;


            if ( LockFlashAP()==false  )
                return false;

             return true;
        
        }

        public bool VerifyCheckSumAndCodeLength(UInt16 CheckSum, UInt16 Length)
        {
            byte[] USBLoadBuffer;
            byte[] CheckSumFlashAddress;
            bool success;

            UInt16 ReadCheckSum;
            UInt16 ReadLength; 


            USBLoadBuffer = new byte[AP_USB_LENGTH];

            CheckSumFlashAddress=BitConverter.GetBytes(CHECKSUMADDR);

            Array.Copy(CheckSumFlashAddress,0,USBLoadBuffer,0,4);
            USBLoadBuffer[4]=0x04; //read 4 bytes


            success = myHid.write(AP_ADDR_SET_SRAM_ADDR_AND_SIZE,USBLoadBuffer,AP_USB_LENGTH);

            if (success == false)
                return false;

            success = myHid.read(AP_ADDR_READ_SRAM,ref USBLoadBuffer,AP_USB_LENGTH);

            if (success == false)
                return false;

      

            ReadCheckSum=BitConverter.ToUInt16(USBLoadBuffer, 2);
            ReadLength = BitConverter.ToUInt16(USBLoadBuffer, 0);

            if (ReadCheckSum != CheckSum && ReadLength != Length)
            {
                return false;
            }




            return true;
        
        
        }

        public bool LockFlashAP()
        {
            byte[] USBLoadBuffer;
            byte[] RegAddressTmp;
            byte[] RegValueTmp;
            bool success;
            USBLoadBuffer = new byte[AP_USB_LENGTH];



            RegAddressTmp = BitConverter.GetBytes(KEY1_ADDRESS);
            RegValueTmp = BitConverter.GetBytes(LOCK_KEY1);


            Array.Copy(RegAddressTmp, 0, USBLoadBuffer, 0, 4);
            Array.Copy(RegValueTmp, 0, USBLoadBuffer, 4, 4);


            success = myHid.write(AP_ADDR_CMD_WRITEREGISTER, USBLoadBuffer, AP_USB_LENGTH);

            if (success == false)
                return false;
           
            return true;
        }



        public bool JumpToAPFormBootloader()
        {
            byte[] USBLoadBuffer;


            USBLoadBuffer = new byte[Boot_USB_LOAD_LENGTH];

            WriteBoot(CMD_JUMPTOAP, USBLoadBuffer, Boot_USB_LOAD_LENGTH);
            if (WaitForIdleBoot() == false)
                return false;

            return true;
        }

        public bool UnlockFlashBoot()
        {
            byte[] USBLoadBuffer;
            byte[] RegAddressTmp;
            byte[] RegValueTmp;

            USBLoadBuffer = new byte[Boot_USB_LOAD_LENGTH];
           


            for (int i=0; i < (gu32FlashUnlockKeyAddress.Length); i++)
            {
                RegAddressTmp = BitConverter.GetBytes(gu32FlashUnlockKeyAddress[i]);
                RegValueTmp = BitConverter.GetBytes(gu32FlashUnlockKeyValue[i]);

         
                Array.Copy(RegAddressTmp, 0, USBLoadBuffer, 0, 4);
                Array.Copy(RegValueTmp, 0, USBLoadBuffer, 4, 4);

                WriteBoot(CMD_WRITEREGISTER, USBLoadBuffer, Boot_USB_LOAD_LENGTH);

                if (WaitForIdleBoot() == false)
                    return false;
            }



             return true;
        }

        public bool LockFlashBoot()
        {
            byte[] USBLoadBuffer;
            byte[] RegAddressTmp;
            byte[] RegValueTmp;

            USBLoadBuffer = new byte[Boot_USB_LOAD_LENGTH];




            RegAddressTmp = BitConverter.GetBytes(KEY1_ADDRESS);
            RegValueTmp = BitConverter.GetBytes(LOCK_KEY1);


                Array.Copy(RegAddressTmp, 0, USBLoadBuffer, 0, 4);
                Array.Copy(RegValueTmp, 0, USBLoadBuffer, 4, 4);

                WriteBoot(CMD_WRITEREGISTER, USBLoadBuffer, Boot_USB_LOAD_LENGTH);

                if (WaitForIdleBoot() == false)
                    return false;
           



            return true;
        }

        public bool EraseFirmwareFlash(UInt32 Mode)
        {
            byte[] USBLoadBuffer;
           

            USBLoadBuffer = new byte[Boot_USB_LOAD_LENGTH];



            USBLoadBuffer[0] = SELECTFLASH_BURN_AP;
            WriteBoot(CMD_SELECTFLASH, USBLoadBuffer, Boot_USB_LOAD_LENGTH);
            if (WaitForIdleBoot() == false)
                return false;

            if (Mode == 1)
            {
                USBLoadBuffer[0] = ERASEFLASH_MODE1;    
            }
            else if (Mode == 2)
            {
                USBLoadBuffer[0] = ERASEFLASH_MODE2;
            }
            WriteBoot(CMD_ERASEFLASH, USBLoadBuffer, Boot_USB_LOAD_LENGTH);
            if (WaitForIdleBoot() == false)
                return false;
           
            return true;
        }

        public bool EraseBootloaderFlash()
        {
            byte[] USBLoadBuffer;


            USBLoadBuffer = new byte[Boot_USB_LOAD_LENGTH];



            USBLoadBuffer[0] = SELECTFLASH_BURN_BOOTLOADER;
            WriteBoot(CMD_SELECTFLASH, USBLoadBuffer, Boot_USB_LOAD_LENGTH);
            if (WaitForIdleBoot() == false)
                return false;

            WriteBoot(CMD_ERASEFLASH, USBLoadBuffer, Boot_USB_LOAD_LENGTH);
            if (WaitForIdleBoot() == false)
                return false;

            return true;
        }


        public bool ReadFlash(ref byte[] OutBuf,UInt32 Length)
        {
            byte[] USBLoadBuffer;
            


            USBLoadBuffer = new byte[Boot_USB_LOAD_LENGTH];
            OutBuf = new byte[Length];


            for (PageNum = 0; (PageNum * PAGESIZE) < Length; PageNum++)
            {

                if (PageNum==0)
                    USBLoadBuffer[0] = 0x00;
                else
                    USBLoadBuffer[0] = 0xFF;

                WriteBoot(CMD_READFLASH, USBLoadBuffer, Boot_USB_LOAD_LENGTH);
                if (WaitForIdleBoot() == false)
                    return false;

                ReadBoot(ref USBLoadBuffer, Boot_USB_DATA_LENGTH);

                if (  (PageNum + 1) * PAGESIZE < Length)
                    Array.Copy(USBLoadBuffer, 2, OutBuf, PageNum * PAGESIZE, PAGESIZE);
                else
                    Array.Copy(USBLoadBuffer, 2, OutBuf, PageNum * PAGESIZE, Length - PageNum * PAGESIZE);


            }

            


            return true;
        }

        public bool WriteFlash(byte[] InBuf, UInt32 Length)
        {
            byte[] USBLoadBuffer;
           


            USBLoadBuffer = new byte[Boot_USB_LOAD_LENGTH];
           


            for (PageNum = 0; (PageNum * PAGESIZE) < Length; PageNum++)
            {


                for (int i = 0; i < Boot_USB_LOAD_LENGTH; i++)
                {
                    USBLoadBuffer[i] = 0xff;
                }
                    if (PageNum == 0)
                        USBLoadBuffer[0] = 0x00;
                    else
                        USBLoadBuffer[0] = 0xFF;

                if ((PageNum + 1) * PAGESIZE < Length)
                    Array.Copy(InBuf,PageNum * PAGESIZE , USBLoadBuffer, 1, PAGESIZE);
                else
                    Array.Copy(InBuf,PageNum * PAGESIZE , USBLoadBuffer, 1, Length - PageNum * PAGESIZE);

                WriteBoot(CMD_WRITEFLASH, USBLoadBuffer, Boot_USB_LOAD_LENGTH);
                if (WaitForIdleBoot() == false)
                    return false;

               

            }




            return true;
        }



        public bool WaitForIdleBoot()
        {
            byte[] USBbuffer = new byte[Boot_USB_DATA_LENGTH];

            byte BootMainState=0x00;
            int iTryTimes=0;
            do
            {

               ReadBoot(ref USBbuffer, Boot_USB_DATA_LENGTH);
               BootMainState=USBbuffer[1];

               iTryTimes++;
               if (iTryTimes > ErrorCount)
               {
                   return false;
               }

            } while (BootMainState != CMD_IDLE);
            return true;
        }

        public bool WriteBoot(byte Cmd, byte[] USBLoadBuffer, int Length)
        {
            byte[] USBbuffer = new byte[Length+1];
            Boolean success;
            USBbuffer[0] = Cmd;
            Array.Copy(USBLoadBuffer, 0, USBbuffer, 1, Length);


           success= myHid.SetReport(Boot_REPORT_ID, USBbuffer, Boot_USB_DATA_LENGTH);

           return success;
        }

        public bool ReadBoot(ref byte[] OutBuf, int Length)
        {
            byte[] USBbuffer = new byte[Boot_USB_DATA_LENGTH];
            Boolean success;
            success = myHid.GetReport(Boot_REPORT_ID, ref USBbuffer, Boot_USB_DATA_LENGTH);


            OutBuf = USBbuffer;
            //Array.Copy(USBbuffer, 0, OutBuf, 0, Length);

            return success;
        }

    }
}
