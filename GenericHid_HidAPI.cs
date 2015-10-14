嚜簑sing System;
using System.Collections.Generic;

using System.Text;
using USBHID;
using System.IO;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;


namespace USBHID
{
    enum CMD_ADDR
    {
        ADDR_TEST_MODE =                0x03,
        ADDR_TEST_SET_RAW_DATA =        0x05,
        ADDR_TEST_SELF_TEST =           0x06,
        ADDR_TOTAL_X =                  0x0D,
        ADDR_TOTAL_Y =                  0x0E,
        ADDR_BASIC_PARA_START_ADDR0 =   0x18,
        ADDR_GETCOOR_PARA_START_ADDR0 = 0x20,
        ADDR_ID_PARA_START_ADDR0 =      0x24,
        ADDR_LPF_PARA_START_ADDR0 =     0x28,
        ADDR_NOISE_PARA_START_ADDR0 =   0x2C,
        ADDR_POWER_PARA_START_ADDR0 =   0x30,
        ADDR_WRITE_SRAM =               0x3B,
        ADDR_READ_SRAM =                0x3C,
        ADDR_WRITE_PARA_DATA =          0x3E,
        ADDR_READ_PARA_DATA =           0x3F,
        ADDR_READ_FI_DIFF_RAW_X =       0x40,
        ADDR_READ_FI_DIFF_RAW_Y =       0x41,
        ADDR_READ_FI_BASELINE_X =       0x42,
        ADDR_READ_FI_BASELINE_Y =       0x43,
        ADDR_READ_FI_INTGL_RAW_X =      0x44,
        ADDR_READ_FI_INTGL_RAW_Y =      0x45,
        ADDR_READ_ST_DIFF_RAW =         0x46,
        ADDR_READ_ST_BASELINE =         0x47,
        ADDR_READ_ST_INTGL_RAW =        0x48,
        ADDR_READ_FI_COMP_X =           0x49,
        ADDR_READ_FI_COMP_Y =           0x4A,
        ADDR_READ_ST_COMP =             0x4B,
        ADDR_READ_FT_RAW_DATA =         0x4C,
        ADDR_TOUCH_POINTS =   		    0x50,
        ADDR_READ_RAWDATA_12BIT =       0x5B,
        ADDR_READ_BASELINE_12BIT =      0x5C,
        ADDR_READ_RAWDATA_HOOLA =       0x5D,
        ADDR_BREAKPOINT_ENABLE_SET =    0x61,
        ADDR_BREAKPOINT_ENABLE_GET =	0x62,
        ADDR_BREAKPOINT_DISABLE	=		0x63,
        ADDR_BREAKPOINT_LIST =			0x64,
        ADDR_MEM_ADDRESS_SET =			0x65,	
        ADDR_MEM_READ =					0x66,
        ADDR_MEM_WRITE =				0x67,
        ADDR_MESSAGE_ENABLE_SET	=		0x68,
        ADDR_MESSAGE_ENABLE_GET	=		0x69,
        ADDR_MESSAGE_DISABLE =			0x6A,
        ADDR_REG_READ =					0x6B,
        ADDR_REG_WRITE =				0x6C,
        ADDR_CMD_WRITECHECKSUM =        0x6D,
        ADDR_FUNCTION_LIST =			0x6E,
        ADDR_FUNCTION_RUN =				0x6F,
        ADDR_FUNCTION_STATE =			0x70,
        ADDR_WRITE_RAW_DATA =           0xFF, //0x54 
        ADDR_READ_RAW_DATA =            0xFE, //0x55 
    }


    class HidAPI
    {
        // default VID PID
        Int32 myVendorID = 0x2386;
        Int32 myProductID = 0x82cd;
        public Int32 CurrentPid;

        private IntPtr deviceNotificationHandle;

        private FileStream fileStreamDeviceData = null;
        private SafeFileHandle hidHandle;
        private String hidUsage;
        public Boolean myDeviceDetected;
        private Boolean NotificationRegistered = false;
        private String myDevicePathName;

        private Debugging MyDebugging; //  For viewing results of API calls via Debug.Write.
        private DeviceManagement MyDeviceManagement;
        private Hid MyHid = new Hid();
        System.Windows.Forms.ListBox lstResults;
        System.Windows.Forms.Form FrmMy;

        public void Init(System.Windows.Forms.Form frm, System.Windows.Forms.ListBox lstbox)
        {
            FrmMy = frm;
            lstResults = lstbox;
            MyDebugging = new Debugging();
            MyDeviceManagement = new DeviceManagement();
        }

        public void SetVidPid(int vid, int pid)
        {
            myVendorID = vid;
            myProductID = pid;
        }
      


        public Boolean SetReport(Byte Report_ID, Byte[] buf, int length)
        {
            Boolean success = false;
            Byte[] outputReportBuffer = null;
            int USBPacketLength = length+1;
            int i;

            try
            {
                //  ***
                //  API function: HidD_SetOutputReport

                //  Purpose: 
                //  Attempts to send an Output report to the device using a control transfer.
                //  Requires Windows XP or later.

                //  Accepts:
                //  A handle to a HID
                //  A pointer to a buffer containing the report ID and report
                //  The size of the buffer. 

                //  Returns: true on success, false on failure.
                //  ***                    

                outputReportBuffer = new Byte[USBPacketLength];

                /* ========================================================================= */
                /* original protocol */
                /* ========================================================================= */
                outputReportBuffer[0] = Report_ID; // Report ID
                for (i = 0; i < length; i++)
                {
                    outputReportBuffer[i + 1] = buf[i];
                }

                success = MyHid.SendOutputReportViaControlTransfer(hidHandle, outputReportBuffer, USBPacketLength);

                Debug.Print("HidD_SetOutputReport success = " + success);

                return success;
            }
            catch (Exception ex)
            {
                DisplayException("setOutputReport", ex);
                throw;
            }
        }
        public Boolean GetReport(Byte Report_ID, ref Byte[] buf, int length)
        {
            Boolean success;
            Byte[] inputReportBuffer = null;
            int USBPacketLength = length + 1;

           
            try
            {
                inputReportBuffer = new Byte[USBPacketLength];

                /* ========================================================================= */
                /* original protocol */
                /* ========================================================================= */
                inputReportBuffer[0] = Report_ID; // Report id

                success = MyHid.GetInputReportViaControlTransfer(hidHandle, ref inputReportBuffer, USBPacketLength);

                memcpy(ref buf, inputReportBuffer, 1, length);

                Debug.Print("GetInputReportViaControlTransfer success = " + success);

                return success;
            }
            catch (Exception ex)
            {
                DisplayException("getInputReport", ex);
                throw;
            }
        }

        public Boolean SetReport256(Byte Report_ID, Byte[] buf, int length)
        {
            Boolean success = false;
            Byte[] outputReportBuffer = null;
            int i;

            if (length > 252)
                length = 252;

            try
            {
                //  ***
                //  API function: HidD_SetOutputReport

                //  Purpose: 
                //  Attempts to send an Output report to the device using a control transfer.
                //  Requires Windows XP or later.

                //  Accepts:
                //  A handle to a HID
                //  A pointer to a buffer containing the report ID and report
                //  The size of the buffer. 

                //  Returns: true on success, false on failure.
                //  ***                    

                outputReportBuffer = new Byte[256];

                /* ========================================================================= */
                /* original protocol */
                /* ========================================================================= */
                outputReportBuffer[0] = Report_ID; // Report ID
                for (i = 0; i < length; i++)
                {
                    outputReportBuffer[i + 1] = buf[i];
                }

                success = MyHid.SendOutputReportViaControlTransfer(hidHandle, outputReportBuffer, 256);

                Debug.Print("HidD_SetOutputReport success = " + success);

                return success;
            }
            catch (Exception ex)
            {
                DisplayException("setOutputReport", ex);
                throw;
            }
        }
        public Boolean GetReport256(Byte Report_ID, ref Byte[] buf, int length)
        {
            Boolean success;
            Byte[] inputReportBuffer = null;

            // reserved 1 byte for command address
            if (length > 255)
                length = 255;

            try
            {
                inputReportBuffer = new Byte[256];

                /* ========================================================================= */
                /* original protocol */
                /* ========================================================================= */
                inputReportBuffer[0] = Report_ID; // Report id

                success = MyHid.GetInputReportViaControlTransfer(hidHandle, ref inputReportBuffer, 256);

                memcpy(ref buf, inputReportBuffer, 1, length);

                Debug.Print("GetInputReportViaControlTransfer success = " + success);

                return success;
            }
            catch (Exception ex)
            {
                DisplayException("getInputReport", ex);
                throw;
            }
        }


        public Boolean SetReport_dynamic_length(Byte Report_ID, Byte[] buf, int length, int USB_supoort_length)
        {
            Boolean success = false;
            Byte[] outputReportBuffer = null;
            int i;

            if (length > (USB_supoort_length - 4))
                length = USB_supoort_length - 4;

            try
            {
                //  ***
                //  API function: HidD_SetOutputReport

                //  Purpose: 
                //  Attempts to send an Output report to the device using a control transfer.
                //  Requires Windows XP or later.

                //  Accepts:
                //  A handle to a HID
                //  A pointer to a buffer containing the report ID and report
                //  The size of the buffer. 

                //  Returns: true on success, false on failure.
                //  ***                    

                outputReportBuffer = new Byte[USB_supoort_length];

                /* ========================================================================= */
                /* original protocol */
                /* ========================================================================= */
                outputReportBuffer[0] = Report_ID; // Report ID
                for (i = 0; i < length; i++)
                {
                    outputReportBuffer[i + 1] = buf[i];
                }

                success = MyHid.SendOutputReportViaControlTransfer(hidHandle, outputReportBuffer, USB_supoort_length);

                Debug.Print("HidD_SetOutputReport success = " + success);

                return success;
            }
            catch (Exception ex)
            {
                DisplayException("setOutputReport", ex);
                throw;
            }
        }
        public Boolean GetReport_dynamic_length(Byte Report_ID, ref Byte[] buf, int USB_supoort_length)
        {
            Boolean success;
            Byte[] inputReportBuffer = null;
            int length;
            
            length = USB_supoort_length-1;

            try
            {
                inputReportBuffer = new Byte[USB_supoort_length];

                /* ========================================================================= */
                /* original protocol */
                /* ========================================================================= */
                inputReportBuffer[0] = Report_ID; // Report id

                success = MyHid.GetInputReportViaControlTransfer(hidHandle, ref inputReportBuffer, USB_supoort_length);

                memcpy(ref buf, inputReportBuffer, 1, length);

                Debug.Print("GetInputReportViaControlTransfer success = " + success);

                return success;
            }
            catch (Exception ex)
            {
                DisplayException("getInputReport", ex);
                throw;
            }
        }

        public Boolean read(Byte cmd_addr, ref Byte[] buf, int length)
        {
            Boolean success;
            Byte[] inputReportBuffer = null;

            // reserved 1 byte for command address
            if (length > 63)
                length = 63;

            try
            {
                inputReportBuffer = new Byte[64];

                /* ========================================================================= */
                /* original protocol */
                /* ========================================================================= */
                inputReportBuffer[0] = 0x05; // Report id
                inputReportBuffer[1] = 0x60;
                inputReportBuffer[2] = 0x01;
                inputReportBuffer[3] = 0x3c; // length
                inputReportBuffer[4] = cmd_addr; // command addr

                /* ========================================================================= */
                /* New protocol */
                /* ========================================================================= */
                /*
                inputReportBuffer[0] = 0x05; // Report id
                inputReportBuffer[1] = cmd_addr; // Command address
                inputReportBuffer[2] = 64; // Data length
                inputReportBuffer[3] = 0x3c; // command read OID
                //inputReportBuffer[4] = 0x3c; // command read OID
                //inputReportBuffer[5] = 0x00;
                 */

                success = MyHid.SendOutputReportViaControlTransfer(hidHandle, inputReportBuffer, 64);

                inputReportBuffer[0] = 0x02; // Report id
                //inputReportBuffer[1] = 0x00; // Command address
                //inputReportBuffer[2] = 0x00; // Data length
                //inputReportBuffer[3] = 0x00; // command read OID
                success = MyHid.GetInputReportViaControlTransfer(hidHandle, ref inputReportBuffer, 64);

                memcpy(ref buf, inputReportBuffer, 1, length);

                Debug.Print("HidD_GetInputReport success = " + success);

                return success;
            }
            catch (Exception ex)
            {
                DisplayException("getInputReport", ex);
                throw;
            }
        }

        public Boolean write(Byte cmd_addr, Byte[] buf, int length)
        {
            Boolean success = false;
            Byte[] outputReportBuffer = null;
            int i;

            if (length > 60)
                length = 60;

            try
            {
                //  ***
                //  API function: HidD_SetOutputReport

                //  Purpose: 
                //  Attempts to send an Output report to the device using a control transfer.
                //  Requires Windows XP or later.

                //  Accepts:
                //  A handle to a HID
                //  A pointer to a buffer containing the report ID and report
                //  The size of the buffer. 

                //  Returns: true on success, false on failure.
                //  ***                    

                outputReportBuffer = new Byte[64];

                /* ========================================================================= */
                /* original protocol */
                /* ========================================================================= */
                outputReportBuffer[0] = 0x01; // Report ID
                outputReportBuffer[1] = 0x60; // Device Address 
                outputReportBuffer[2] = (byte)length;
                outputReportBuffer[3] = cmd_addr;
                for (i = 0; i < length; i++)
                {
                    outputReportBuffer[i + 4] = buf[i];
                }


                /* ========================================================================= */
                /* New protocol */
                /* ========================================================================= */
                /*
                outputReportBuffer[0] = 0x01; // Report ID
                outputReportBuffer[1] = 0x60; // Device Address 
                outputReportBuffer[2] = 0x40; // Data Len
                outputReportBuffer[3] = cmd_addr; // Cmd Address
                 */

                success = MyHid.SendOutputReportViaControlTransfer(hidHandle, outputReportBuffer, 64);

                Debug.Print("HidD_SetOutputReport success = " + success);

                return success;
            }
            catch (Exception ex)
            {
                DisplayException("setOutputReport", ex);
                throw;
            }     
        }

        public bool SetTestMode(byte isTestMode)
        {
            byte[] buf = {0, 0, 0};
            byte cmd_addr = (byte)CMD_ADDR.ADDR_TEST_MODE;

            buf[0] = isTestMode;
            return write(cmd_addr, buf, 2);
        }

        internal void memcpy(ref byte[] dst, byte[] src, byte offset, int len)
        {
            for (int i = 0; i < len; i++)
            {
                dst[i] = src[i+offset];
            }
        }

        internal void OnDeviceChange(Message m)
        {
            Debug.WriteLine("WM_DEVICECHANGE");

            try
            {
                if ((m.WParam.ToInt32() == DeviceManagement.DBT_DEVICEARRIVAL))
                {
                    //  If WParam contains DBT_DEVICEARRIVAL, a device has been attached.

                    Debug.WriteLine("A device has been attached.");

                    if (myDeviceDetected == false)
                    {
                        ConnectTouchSystem();
                    }

                    //  Find out if it's the device we're communicating with.

                    if (MyDeviceManagement.DeviceNameMatch(m, myDevicePathName))
                    {
                        lstResults.Items.Add("My device attached.");
                    }

                }
                else if ((m.WParam.ToInt32() == DeviceManagement.DBT_DEVICEREMOVECOMPLETE))
                {

                    //  If WParam contains DBT_DEVICEREMOVAL, a device has been removed.

                    Debug.WriteLine("A device has been removed.");

                    //  Find out if it's the device we're communicating with.

                    if (MyDeviceManagement.DeviceNameMatch(m, myDevicePathName))
                    {

                        lstResults.Items.Add("My device removed.");

                        //  Set MyDeviceDetected False so on the next data-transfer attempt,
                        //  FindTheHid() will be called to look for the device 
                        //  and get a new handle.

                        myDeviceDetected = false;
                    }
                }
                ScrollToBottomOfListBox();

            }
            catch (Exception ex)
            {
                DisplayException("OnDeviceChange", ex);
                throw;
            }
        }

        private int GetInputReportBufferSize()
        {
            Int32 numberOfInputBuffers = 0;
            Boolean success;

            try
            {
                //  Get the number of input buffers.
                success = MyHid.GetNumberOfInputBuffers(hidHandle, ref numberOfInputBuffers);

                //  Display the result in the text box.
                //lstResults.Items.Add("Input report buffer size:" + Convert.ToString(numberOfInputBuffers));
                return numberOfInputBuffers;
            }
            catch (Exception ex)
            {
                DisplayException("GetInputReportBufferSize", ex);
                throw;
            }
        }


        public Boolean ConnectDevice()
        {
            Boolean deviceFound = false;
            String[] devicePathName = new String[128];
            String functionName = "";
            Guid hidGuid = Guid.Empty;
            Int32 memberIndex = 0;
            Boolean success = false;

            try
            {
                myDeviceDetected = false;
                CloseCommunications();

                //  ***
                //  API function: 'HidD_GetHidGuid

                //  Purpose: Retrieves the interface class GUID for the HID class.

                //  Accepts: 'A System.Guid object for storing the GUID.
                //  ***

                Hid.HidD_GetHidGuid(ref hidGuid);

                functionName = "GetHidGuid";
                Debug.WriteLine(MyDebugging.ResultOfAPICall(functionName));
                Debug.WriteLine("  GUID for system HIDs: " + hidGuid.ToString());

                //  Fill an array with the device path names of all attached HIDs.

                deviceFound = MyDeviceManagement.FindDeviceFromGuid(hidGuid, ref devicePathName);

                //  If there is at least one HID, attempt to read the Vendor ID and Product ID
                //  of each device until there is a match or all devices have been examined.

                if (deviceFound)
                {
                    memberIndex = 0;

                    do
                    {
                        //  ***
                        //  API function:
                        //  CreateFile

                        //  Purpose:
                        //  Retrieves a handle to a device.

                        //  Accepts:
                        //  A device path name returned by SetupDiGetDeviceInterfaceDetail
                        //  The type of access requested (read/write).
                        //  FILE_SHARE attributes to allow other processes to access the device while this handle is open.
                        //  A Security structure or IntPtr.Zero. 
                        //  A creation disposition value. Use OPEN_EXISTING for devices.
                        //  Flags and attributes for files. Not used for devices.
                        //  Handle to a template file. Not used.

                        //  Returns: a handle without read or write access.
                        //  This enables obtaining information about all HIDs, even system
                        //  keyboards and mice. 
                        //  Separate handles are used for reading and writing.
                        //  ***

                        // Open the handle without read/write access to enable getting information about any HID, even system keyboards and mice.

                        hidHandle = FileIO.CreateFile(devicePathName[memberIndex],  FileIO.GENERIC_READ | FileIO.GENERIC_WRITE,  FileIO.FILE_SHARE_READ | FileIO.FILE_SHARE_WRITE, IntPtr.Zero, FileIO.OPEN_EXISTING, FileIO.FILE_FLAG_OVERLAPPED, 0);
                        //hidHandle = FileIO.CreateFile(devicePathName[memberIndex], 0, FileIO.FILE_SHARE_READ | FileIO.FILE_SHARE_WRITE, IntPtr.Zero, FileIO.OPEN_EXISTING, 0, 0);
                        functionName = "CreateFile";
                        Debug.WriteLine(MyDebugging.ResultOfAPICall(functionName));

                        Debug.WriteLine("  Returned handle: " + hidHandle.ToString());

                        if (!hidHandle.IsInvalid)
                        {
                            //  The returned handle is valid, 
                            //  so find out if this is the device we're looking for.

                            //  Set the Size property of DeviceAttributes to the number of bytes in the structure.

                            MyHid.DeviceAttributes.Size = Marshal.SizeOf(MyHid.DeviceAttributes);

                            //  ***
                            //  API function:
                            //  HidD_GetAttributes

                            //  Purpose:
                            //  Retrieves a HIDD_ATTRIBUTES structure containing the Vendor ID, 
                            //  Product ID, and Product Version Number for a device.

                            //  Accepts:
                            //  A handle returned by CreateFile.
                            //  A pointer to receive a HIDD_ATTRIBUTES structure.

                            //  Returns:
                            //  True on success, False on failure.
                            //  ***                            

                            success = Hid.HidD_GetAttributes(hidHandle, ref MyHid.DeviceAttributes);

                            if (success)
                            {
                                Debug.WriteLine("  HIDD_ATTRIBUTES structure filled without error.");
                                Debug.WriteLine("  Structure size: " + MyHid.DeviceAttributes.Size);
                                Debug.WriteLine("  Vendor ID: " + Convert.ToString(MyHid.DeviceAttributes.VendorID, 16));
                                Debug.WriteLine("  Product ID: " + Convert.ToString(MyHid.DeviceAttributes.ProductID, 16));
                                Debug.WriteLine("  Version Number: " + Convert.ToString(MyHid.DeviceAttributes.VersionNumber, 16));

                                //  Find out if the device matches the one we're looking for.
                                if (MyHid.DeviceAttributes.VendorID == myVendorID)
                                    CurrentPid = MyHid.DeviceAttributes.ProductID;

                                if ((MyHid.DeviceAttributes.VendorID == myVendorID) && (MyHid.DeviceAttributes.ProductID == myProductID))
                                {
                                    Debug.WriteLine("  My device detected");

                                    CurrentPid = MyHid.DeviceAttributes.ProductID;

                                    //  Display the information in form's list box.

                                    lstResults.Items.Add("Device detected:");
                                    functionName = String.Format("  Vendor ID=0x{0}", Convert.ToString(MyHid.DeviceAttributes.VendorID, 16).PadLeft(4, '0'));
                                    lstResults.Items.Add(functionName);
                                    functionName = String.Format("  Product ID=0x{0}", Convert.ToString(MyHid.DeviceAttributes.ProductID, 16).PadLeft(4, '0'));
                                    lstResults.Items.Add(functionName);

                                    ScrollToBottomOfListBox();

                                    myDeviceDetected = true;

                                    //  Save the DevicePathName for OnDeviceChange().

                                    myDevicePathName = devicePathName[memberIndex];
                                }
                                else
                                {
                                    //  It's not a match, so close the handle.

                                    myDeviceDetected = false;
                                    hidHandle.Close();
                                }
                            }
                            else
                            {
                                //  There was a problem in retrieving the information.

                                Debug.WriteLine("  Error in filling HIDD_ATTRIBUTES structure.");
                                myDeviceDetected = false;
                                hidHandle.Close();
                            }
                        }

                        //  Keep looking until we find the device or there are no devices left to examine.

                        memberIndex = memberIndex + 1;
                    }
                    while (!((myDeviceDetected || (memberIndex == devicePathName.Length))));
                }

                if (myDeviceDetected)
                {
                    //  The device was detected.
                    //  Register to receive notifications if the device is removed or attached.

                    if (NotificationRegistered == false)
                    {
                        success = MyDeviceManagement.RegisterForDeviceNotifications(myDevicePathName, FrmMy.Handle, hidGuid, ref deviceNotificationHandle);
                        NotificationRegistered = true;
                    }

                    Debug.WriteLine("RegisterForDeviceNotifications = " + success);

                    //  Learn the capabilities of the device.

                    MyHid.Capabilities = MyHid.GetDeviceCapabilities(hidHandle);

                    if (success)
                    {
                        //  Find out if the device is a system mouse or keyboard.

                        hidUsage = MyHid.GetHidUsage(MyHid.Capabilities);

                        //  Get the Input report buffer size.

                        GetInputReportBufferSize();

                        //Close the handle and reopen it with read/write access.

                        hidHandle.Close();

                        hidHandle = FileIO.CreateFile(myDevicePathName, FileIO.GENERIC_READ | FileIO.GENERIC_WRITE, FileIO.FILE_SHARE_READ | FileIO.FILE_SHARE_WRITE, IntPtr.Zero, FileIO.OPEN_EXISTING, 0, 0);

                        if (hidHandle.IsInvalid)
                        {
                            lstResults.Items.Add("The device is a system " + hidUsage + ".");
                            lstResults.Items.Add("Windows 2000 and Windows XP obtain exclusive access to Input and Output reports for this devices.");
                            lstResults.Items.Add("Applications can access Feature reports only.");
                            ScrollToBottomOfListBox();
                        }

                        else
                        {
                            if (MyHid.Capabilities.InputReportByteLength > 0)
                            {
                                //  Set the size of the Input report buffer. 

                                Byte[] inputReportBuffer = null;

                                inputReportBuffer = new Byte[MyHid.Capabilities.InputReportByteLength];

                                //fileStreamDeviceData = new FileStream(hidHandle, FileAccess.Read | FileAccess.Write, inputReportBuffer.Length, false);
                            }

                            if (MyHid.Capabilities.OutputReportByteLength > 0)
                            {
                                Byte[] outputReportBuffer = null;
                                outputReportBuffer = new Byte[MyHid.Capabilities.OutputReportByteLength];
                            }

                            //  Flush any waiting reports in the input buffer. (optional)

                            MyHid.FlushQueue(hidHandle);
                        }
                    }
                }
                else
                {
                    //  The device wasn't detected.

                    lstResults.Items.Add("Device not found.");
                    Debug.WriteLine(" Device not found.");

                    ScrollToBottomOfListBox();

                    if (NotificationRegistered == false)
                    {
                        MyDeviceManagement.RegisterForDeviceNotifications(myDevicePathName, FrmMy.Handle, hidGuid, ref deviceNotificationHandle);
                        NotificationRegistered = true;
                    }
                }
                return myDeviceDetected;
            }
            catch (Exception ex)
            {
                DisplayException("FindTheHid", ex);
                throw;
            }
        }

        public void CloseCommunications()
        {
            if (fileStreamDeviceData != null)
            {
                fileStreamDeviceData.Close();
            }

            if ((hidHandle != null) && (!(hidHandle.IsInvalid)))
            {
                hidHandle.Close();
            }

            // The next attempt to communicate will get new handles and FileStreams.

            myDeviceDetected = false;
        }

        public void ConnectTouchSystem()
        {
            try
            {
                ConnectDevice();
            }
            catch (Exception ex)
            {
                DisplayException("ConnectDevice", ex);
                throw;
            }
        }

        private void ScrollToBottomOfListBox()
        {
            try
            {
                Int32 count = 0;

                lstResults.SelectedIndex = lstResults.Items.Count - 1;

                //  If the list box is getting too large, trim its contents by removing the earliest data.

                if (lstResults.Items.Count > 1000)
                {
                    for (count = 1; count <= 500; count++)
                    {
                        lstResults.Items.RemoveAt(4);
                    }
                }
            }
            catch (Exception ex)
            {
                DisplayException("ScrollToBottomOfListBox", ex);
                throw;
            }
        }

        internal static void DisplayException(String moduleName, Exception e)
        {
            String message = null;
            String caption = null;

            //  Create an error message.

            message = "Exception: " + e.Message + "\n" + "Module: " + moduleName + "\n" + "Method: " + e.TargetSite.Name;

            caption = "Unexpected Exception";

            MessageBox.Show(message, caption, MessageBoxButtons.OK);
            Debug.Write(message);
        } 
    }
}
