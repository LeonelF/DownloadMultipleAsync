using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AisUriProviderApi;
using System.Threading;
using System.IO;

namespace AISFileCatelog
{
    public partial class FileCatelog : Form
    {
        AisUriProvider uriProvider = new AisUriProvider();
        IEnumerable<Uri> uriList;
        string directoryPath = "";
        DownloadClient client = new DownloadClient();
        FileOperations fileOper = new FileOperations();
        public FileCatelog()
        {
            InitializeComponent();
            this.FormClosing += FileCatelog_FormClosing;
            //Load data on app start
            loadData();
        }

        enum ImageFormat
        {
            jpeg,jpg,jif,jfif,gif,tif,tiff,jp2,jpx,j2k,j2c,png,pcd
        }

        enum mediaExtensions
        {
            wav, mid, midi, wma, mp3, ogg, rma, avi, mp4, divx, wmv
        }

        private void loadData()
        {
            uriList = uriProvider.Get();
            gridFiles.AutoGenerateColumns = false;
            gridFiles.DataSource = uriList.ToList();
            
            gridFiles.Columns["FileName"].DataPropertyName = "AbsolutePath";
            gridFiles.Columns["OriginalPathName"].DataPropertyName = "OriginalString";
           
            //It will retrieve directory name in which files will save
            directoryPath = fileOper.getDirectoryPath();
            if (directoryPath != "")
            {
                //Set grid data to download
                DownloadFiles((List<Uri>)gridFiles.DataSource);
            }
            else
            {
                writeOperation("Error while accessing directory path");
            }
        }

        private void FileCatelog_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (pgrBarDowload.Value != 100)
            {
                DialogResult result = MessageBox.Show(this,"Download is in progress. Do you still wanna close?","File catelog",MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    timerDownload.Enabled = false;
                    //It will cancel all download requests
                    client.CancelFileAsync();
                    e.Cancel = false;
                }
                else if (result == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }
        
        private async void DownloadFiles(List<Uri> urls)
        {
            try
            {
                Progress<double> progress = new Progress<double>();
                foreach (Uri uri in urls)
                {
                    if (!client.isProcessCancel)
                    {
                        //Gets download progress
                        progress.ProgressChanged += (sender, value) => pgrBarDowload.Value = (int)value;
                    }

                    var cancellationToken = new CancellationTokenSource();

                    writeOperation("Downloading File: " + uri.OriginalString);
                    
                    //Set files in download queue
                    client.isProcessCancel = false;
                    await client.DownloadFileAsync(uri.OriginalString, progress, cancellationToken.Token, directoryPath);
                }
            }
            catch (Exception ex)
            {
                writeOperation(ex.Message);
            }
        }
        
        private void btnCancelDownload_Click(object sender, EventArgs e)
        {
            try
            {
                writeOperation("Download operation cancelled by user");
                client.isProcessCancel = true;
                pgrBarDowload.Value = 100;
                //Cancel all download requests
                client.CancelFileAsync();
            }
            catch (Exception ex)
            {
                writeOperation(ex.Message);
            }
        }

        private void btnResumeSync_Click(object sender, EventArgs e)
        {
            try
            {
                client = new DownloadClient();
                writeOperation("Download operation resumed by user");
                client.isProcessCancel = false;
                //Resume all downloads
                DownloadFiles((List<Uri>)gridFiles.DataSource);
            }
            catch (Exception ex)
            {
                writeOperation(ex.Message);
            }
        }

        private void timerDownload_Tick(object sender, EventArgs e)
        {
            try
            {
                client.isProcessCancel = false;
                gridFiles.DataSource = uriList.ToList();
                gridFiles.Refresh();

                //After 5 mins download will start again
                DownloadFiles((List<Uri>)gridFiles.DataSource);
            }
            catch (Exception ex)
            {
                writeOperation(ex.Message);
            }
        }

        private void btnDeleteFile_Click(object sender, EventArgs e)
        {
            try
            {
                if (gridFiles.SelectedRows.Count > 0)
                {
                    for (int i = 0; i < gridFiles.SelectedRows.Count; i++)
                    {
                        DataGridViewRow dr = gridFiles.SelectedRows[i];
                        string fileURL = dr.Cells[0].Value.ToString();
                        try
                        {
                            //Check if file is deleted
                            bool isDeleted = fileOper.isFileDeleted(fileURL);
                            if (isDeleted)
                            {
                                writeOperation("File deleted successfully: " + directoryPath + fileURL.Substring(fileURL.LastIndexOf('/') + 1));
                            }
                            else
                            {
                                writeOperation("Error while deleting file: " + directoryPath + fileURL.Substring(fileURL.LastIndexOf('/') + 1));
                            }
                        }
                        catch (Exception ex)
                        {
                            writeOperation(ex.Message.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                writeOperation(ex.Message.ToString());
            }
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            string fileURL = "";
            try
            {
                rtxtFilePreview.Text = "";
                picFilePreview.Image = null;
                DataGridViewRow dr = gridFiles.SelectedRows[0];
                fileURL = dr.Cells[0].Value.ToString();
                //Check file is Image or not
                if (Enum.IsDefined(typeof(ImageFormat), Path.GetExtension(fileURL).Substring(1)))
                {
                    rtxtFilePreview.Visible = false;
                    Bitmap bm = fileOper.getImageData(fileURL);
                    if (bm != null)
                    {
                        picFilePreview.Image = bm;
                        writeOperation("Previewing File : " + directoryPath + fileURL.Substring(fileURL.LastIndexOf('/') + 1));
                    }
                    else
                    {
                        PreviewFailed(fileURL);
                    }
                }
                else if (Enum.IsDefined(typeof(mediaExtensions), Path.GetExtension(fileURL).Substring(1)))
                {
                    PreviewFailed(fileURL);
                }
                else
                {
                    rtxtFilePreview.Font = new Font("Microsoft Sans Serif", 9);
                    rtxtFilePreview.SelectionAlignment = HorizontalAlignment.Left;
                    rtxtFilePreview.Visible = true;
                    string fileText = fileOper.getFileText(fileURL);
                    if (fileText != "")
                    {
                        rtxtFilePreview.Text = fileText;
                        writeOperation("Previewing File : " + directoryPath + fileURL.Substring(fileURL.LastIndexOf('/') + 1));
                    }
                    else
                    {
                        PreviewFailed(fileURL);
                    }
                }
            }
            catch (Exception ex)
            {
                PreviewFailed(fileURL);
            }
        }

        //Function if Preview is failed
        private void PreviewFailed(string FileName)
        {
            rtxtFilePreview.Font = new Font("Microsoft Sans Serif", 16);
            rtxtFilePreview.SelectionAlignment = HorizontalAlignment.Center;
            rtxtFilePreview.Visible = true;
            rtxtFilePreview.Text = "No Preview";
            writeOperation("Failed to Preview File : " + directoryPath + FileName.Substring(FileName.LastIndexOf('/') + 1));
        }

        //Function to write log data
        private void writeOperation(string OperationText)
        {
            rtxtOperationStats.Text = (rtxtOperationStats.Text == "") ? OperationText : rtxtOperationStats.Text + "\r\n" + OperationText;
        }

        private void gridFiles_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            
        }
    }
}
