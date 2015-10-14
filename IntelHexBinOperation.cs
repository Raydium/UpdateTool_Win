嚜簑sing System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UPDATE_FIRMWARE
{
    class IntelHexBinOperation
    {
        string sInputfileName;
        byte[] bBinContent;

        public void SetFileName(string sfileName)
        {
            sInputfileName = sfileName;
        }

        public byte[] GetBinary()
        {
            return bBinContent;
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public bool WriteBinFile(string sfileName)
        {
            FileStream fs = new FileStream(sfileName, FileMode.Create, FileAccess.ReadWrite);
            BinaryWriter bw = new BinaryWriter(fs);
            bw.Write(bBinContent);
            bw.Close();
            fs.Close();

            return true;        
        }

        public bool ReadHexFile()
        {
            try
            {
                string tempFolder = System.IO.Path.GetTempPath();

                using (StreamReader sr = new StreamReader(sInputfileName))
                {
                    StringBuilder sbWrite = new StringBuilder();
                    String binaryval = "";

                    String line;

                    //Get rid of first line
                    sr.ReadLine();

                    while ((line = sr.ReadLine()) != null)
                    {
                        binaryval = "";
                        line = line.Substring(9);
                        char[] charArray = line.ToCharArray();

                        if (charArray.Length > 32)
                        {
                            binaryval = new string(charArray, 0, 32);
                            sbWrite.Append(binaryval);
                        }
                    }

                    bBinContent = StringToByteArray(sbWrite.ToString());

                    sr.Close();
                }                                
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool ReadBinFile()
        {
            FileStream fs= new FileStream(sInputfileName, FileMode.Open,FileAccess.Read, FileShare.Read);

            try
            {
                string tempFolder = System.IO.Path.GetTempPath();

                using (BinaryReader sr = new BinaryReader(  fs))
                {
                    StringBuilder sbWrite = new StringBuilder();

                    bBinContent = new byte[fs.Length];
                    fs.Read(bBinContent,0,(int)fs.Length );
                }
            }
            catch
            {
                return false;
            }
            return true;
        }
    }    
}
