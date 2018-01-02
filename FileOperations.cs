using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileCatelog
{
    partial class FileOperations
    {
        string fileDirectoryPath = "";
        internal string getDirectoryPath()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("FileCatelog\\FileCatelogPath", true);
                if (key == null)
                {
                    System.IO.DriveInfo[] drives = System.IO.DriveInfo.GetDrives();
                    List<string> driveList = new List<string>();
                    foreach (System.IO.DriveInfo drive in drives)
                    {
                        if (drive.DriveType == System.IO.DriveType.Fixed && drive.Name != "C:\\")
                        {
                            driveList.Add(drive.Name);
                        }
                    }
                    fileDirectoryPath = driveList[0];

                    key = Registry.CurrentUser.CreateSubKey("FileCatelog\\FileCatelogPath");
                    key.SetValue("FileCatelogPath", fileDirectoryPath + "FileCatelog\\");
                    key.Close();
                }
                else
                {
                    fileDirectoryPath = key.GetValue("FileCatelogPath").ToString();
                }
                if (!Directory.Exists(fileDirectoryPath))
                {
                    Directory.CreateDirectory(fileDirectoryPath);
                }

                return fileDirectoryPath;
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        internal Bitmap getImageData(string FileName)
        {
            try
            {
                byte[] blob = File.ReadAllBytes(fileDirectoryPath + FileName.Substring(FileName.LastIndexOf('/') + 1));
                MemoryStream mStream = new MemoryStream();
                byte[] pData = blob;
                mStream.Write(pData, 0, Convert.ToInt32(pData.Length));
                Bitmap bm = new Bitmap(mStream, false);
                mStream.Dispose();
                return bm;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        internal string getFileText(string fileName)
        {
            try
            {
                string fileText = File.ReadAllText(fileDirectoryPath + fileName.Substring(fileName.LastIndexOf('/') + 1));
                return fileText;
            }
            catch (Exception)
            {
                return "";
            }
        }

        internal bool isFileDeleted(string FileName)
        {
            try
            {
                File.Delete(fileDirectoryPath + FileName.Substring(FileName.LastIndexOf('/') + 1));
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }



    }
}
