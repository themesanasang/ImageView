﻿using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;

namespace ImageView
{


    /// <summary>
    /// Container for everything related to the current data being used by ImageView
    /// </summary>
    public class WorkingData
    {
        public Image image;
        public DirectoryInfo directoryInfo;
        public FileInfo fileInfo;
        public string[] directoryFiles;
        public int directoryIndex;

        public WorkingData()
        {
            reset();
        }

        public void reset()
        {
            directoryInfo = null;
            fileInfo = null;
            directoryIndex = -1;
            directoryFiles = null;
            image = null;
        }
    }

    public partial class FrmMain : Form
    {


        WorkingData workingData;

        /// <summary>
        /// Restore the app to default conditions
        /// </summary>
        private void close()
        {
            pictureBox.Image = null;

            workingData.reset();

            toolStripStatusLabelImageInfo.Text = "Welcome! Open an image file to begin browsing.";
            this.Text = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            toolStripComboBoxNavigation.Text = "";

            toolStripStatusLabelImagePosition.Visible = false;
            toolStripStatusLabelImagePosition.Text = "";
            toolStripStatusLabelZoom.Visible = false;
            toolStripStatusLabelZoom.Text = "";
            toolStripStatusLabelFileSize.Visible = false;
            toolStripStatusLabelFileSize.Text = "";
        }


        private void resizePictureBox()
        {
            resizePictureBox(null);
        }
        private void resizePictureBox(Image i)
        {

#if DEBUG
            if (workingData.fileInfo != null)
                System.Diagnostics.Debug.WriteLine("resizePictureBox " + workingData.fileInfo.Name);
            else
                System.Diagnostics.Debug.WriteLine("resizePictureBox");
#endif

            //if not argument is provided attempt to get it from the picture box
            if (i == null)
            {
                i = pictureBox.Image;
            }


            if (i == null) return;


            if (config.Display.SizeMode == ImageSizeMode.Autosize)
            {

                panelMain.AutoScroll = false;

                //zoom calculated for the autosize. default 100 then re-adjusted if the image cant fit
                int zoom = 100;

                if (panelMain.Width >= i.Width && panelMain.Height >= i.Height)
                {
                    //image can fit entirely on the screen so the display is actually normal
                    pictureBox.SizeMode = PictureBoxSizeMode.Normal;
                    pictureBox.Width = i.Width;
                    pictureBox.Height = i.Height;


                    //center image on screen
                    Point xy = new Point(0, 0);
                    if (i != null)
                    {
                        if (panelMain.Width > i.Width)
                        {
                            xy.X = (int)((panelMain.Width - i.Width) / 2.0f);
                        }
                        if (panelMain.Height > i.Height)
                        {
                            xy.Y = (int)((panelMain.Height - i.Height) / 2.0f);
                        }
                    }
                    if (!xy.Equals(pictureBox.Location))
                        pictureBox.Location = xy;



                }
                else
                {
                    pictureBox.Location = Point.Empty;

                    pictureBox.Width = panelMain.Width;
                    pictureBox.Height = panelMain.Height;
                    pictureBox.SizeMode = PictureBoxSizeMode.Zoom;

                    float aspec = (float)i.Width / (float)i.Height;
                    float windowAspec = (float)panelMain.Width / (float)panelMain.Height;
                    if (aspec > windowAspec)
                    {
                        zoom = (int)((float)panelMain.Width / (float)i.Width * 100.0f);
                    }
                    else
                    {
                        zoom = (int)((float)panelMain.Height / (float)i.Height * 100.0f);
                    }

                }

                toolStripStatusLabelZoom.Visible = true;
                toolStripStatusLabelZoom.Text = String.Format("{0} %", zoom);
                toolStripComboBoxZoom_UpdateText(String.Format("{0}%", zoom));


            }
            else if (config.Display.SizeMode == ImageSizeMode.Zoom || config.Display.SizeMode == ImageSizeMode.Normal)
            {

                int newWidth = i.Width;
                int newHeight = i.Height;

                //for zoom we calculate new height and width but the code is otherwise the same than drawing the regular picture
                if (config.Display.SizeMode == ImageSizeMode.Zoom)
                {
                    float zoomf = config.Display.Zoom / 100.0f;
                    newWidth = (int)Math.Round(((float)i.Width * zoomf));
                    newHeight = (int)Math.Round(((float)i.Height * zoomf));
                    pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                }
                else
                {
                    pictureBox.SizeMode = PictureBoxSizeMode.Normal;
                }

                pictureBox.Width = newWidth;
                pictureBox.Height = newHeight;

                //center image if needed
                panelMain.AutoScroll = false;
                //pictureBox.Location = Point.Empty;
                Point xy = new Point(0, 0);
                if (panelMain.Width > newWidth)
                {
                    xy.X = (int)((panelMain.Width - newWidth) / 2.0f);
                }
                if (panelMain.Height > newHeight)
                {
                    xy.Y = (int)((panelMain.Height - newHeight) / 2.0f);
                }
                pictureBox.Location = xy;
                panelMain.AutoScroll = true;

                //zoom state
                int zoom = config.Display.SizeMode == ImageSizeMode.Normal ? 100 : config.Display.Zoom;
                toolStripStatusLabelZoom.Visible = true;
                toolStripStatusLabelZoom.Text = String.Format("{0} %", zoom);
                toolStripComboBoxZoom_UpdateText(String.Format("{0}%", zoom));
            }
        }


        /// <summary>
        /// TODO: find the next existing file. If a file doesnt exist force a reload of the folder structure. apply the same to previous
        /// </summary>
        private void next()
        {
            if (workingData.directoryIndex != -1)
            {
                workingData.directoryIndex++;

                //loop
                if (workingData.directoryIndex >= workingData.directoryFiles.Length)
                {
                    workingData.directoryIndex = 0;

                }

                string file = workingData.directoryFiles[workingData.directoryIndex];
                loadPicture(file, false);
            }
        }
        private void previous()
        {
            if (workingData.directoryIndex != -1)
            {
                workingData.directoryIndex--;

                //loop
                if (workingData.directoryIndex < 0)
                {
                    workingData.directoryIndex = workingData.directoryFiles.Length - 1;
                }

                string file = workingData.directoryFiles[workingData.directoryIndex];
                loadPicture(file, false);
            }

        }


        private static string NiceFileSize(FileInfo fi)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = fi.Length;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            string result = String.Format("{0:0.##} {1}", len, sizes[order]);

            return result;
        }


        /// <summary>
        /// Todo: add a check if the file exists. If it doesn't and there's currently a working dir then refresh folder structure.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="loadFolderStructure"></param>
        private void loadPicture(string filename, bool loadFolderStructure = true)
        {
            try
            {
                //FileInfo
                workingData.fileInfo = new FileInfo(filename);

                if (workingData.fileInfo.Exists)
                {

                    //Image is loaded from stream instead of FromFile because Image.FromFile locks the file
                    using (FileStream fs = new FileStream(workingData.fileInfo.FullName, FileMode.Open, FileAccess.Read))
                    {
                        workingData.image = Image.FromStream(fs);
                    }
                    
                    
                    

                    //Assign image to picture box then refresh sizing
                    pictureBox.Image = workingData.image;
                    resizePictureBox();


                    //Add loaded file to history if necessary
                    config.History.AddFile(workingData.fileInfo.FullName);
                    //attempt to delete first to re-add it on top of the pile and not duplicate data
                    toolStripComboBoxNavigation.Items.Remove(workingData.fileInfo.FullName);
                    toolStripComboBoxNavigation.Items.Insert(0, workingData.fileInfo.FullName);
                    //if now the list is above max size then we clean up the last item
                    removeExcessHistoryItems();

                    toolStripComboBoxNavigation_UpdateText(workingData.fileInfo.FullName);
                    toolStripStatusLabelImageInfo.Text = String.Format("{0} x {1} x {2} BPP", workingData.image.Width, workingData.image.Height, Image.GetPixelFormatSize(workingData.image.PixelFormat));
                    toolStripStatusLabelImageInfo.Visible = true;
                    toolStripStatusLabelFileSize.Text = NiceFileSize(workingData.fileInfo);
                    toolStripStatusLabelFileSize.Visible = true;
                    this.Text = String.Format("{0} - {1}", filename, System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);


                    // Check if we are in the current working dir and pic position
                    // This whole check can be completely ignored. This avoids useless computations in case a user click on next/previous image
                    // By default it is set to true
                    if (loadFolderStructure)
                    {
                        string path = Path.GetDirectoryName(filename);
                        DirectoryInfo di = new DirectoryInfo(path);
                        //if (workingData.directoryInfo == null || di.FullName != workingData.directoryInfo.FullName)
                        //{
                            workingData.directoryInfo = di;
                            workingData.directoryFiles = Directory.EnumerateFiles(di.FullName, "*.*", SearchOption.TopDirectoryOnly).Where(file => config.ExtensionFilter.Any(x => file.EndsWith(x, StringComparison.OrdinalIgnoreCase))).ToArray();
                            workingData.directoryIndex = Array.FindIndex(workingData.directoryFiles, x => x.Contains(filename));
                        //}
                    }


                    toolStripStatusLabelImagePosition.Text = String.Format("{0} / {1}", workingData.directoryIndex + 1, workingData.directoryFiles.Length);
                    toolStripStatusLabelImagePosition.Visible = true;
                }
                else
                {
                    //there is an edge case here where the user was browsing a folder and then an image of said folder was moved/deleted
                    MessageBox.Show("File " + filename + " does not exist", "Unable to load file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    close();
                }



            }
            catch (OutOfMemoryException)
            {
                // Image.FromFile(String) 
                //The file does not have a valid image format. -or - GDI + does not support the pixel format of the file.
            }
            catch (Exception)
            {

            }




        }


        private void copy()
        {
            if(workingData.image != null)
            {
                Clipboard.SetImage(workingData.image);
            }
        }

        private void showInformation()
        {
            if(workingData.fileInfo != null)
            {
                FrmInformation f = new FrmInformation(workingData);
                f.ShowDialog();
            }
        }


        public void removeExcessHistoryItems()
        {
            removeExcessHistoryItems(config.History.MaxSize);
        }
        /// <summary>
        /// Forces a refresh of history, this is called if user changes its history size and all of a sudden there is not enough history size to store user history
        /// </summary>
        public void removeExcessHistoryItems(int size)
        {

            while(size < toolStripComboBoxNavigation.Items.Count)
            {
                toolStripComboBoxNavigation.Items.RemoveAt(toolStripComboBoxNavigation.Items.Count - 1);
            }
            
        }



        private void exitFullScreen()
        {

            WinTaskbar.Show();
            toolStrip.Visible = true;
            statusStrip.Visible = true;
            menuStrip.Visible = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.TopMost = false;
            panelMain.BorderStyle = BorderStyle.Fixed3D;

            //restore window state
            this.WindowState = fullScreenSaveState.WindowState;
            this.Location = fullScreenSaveState.Location;
            this.Size = fullScreenSaveState.Size;


            fullscreen = false;
        }
        private void enterFullScreen()
        {


            //save window state before entering full screen so that it can be restored when exiting
            fullScreenSaveState.WindowState = this.WindowState;
            if (this.WindowState == FormWindowState.Maximized)
            {
                fullScreenSaveState.Location = this.RestoreBounds.Location;
                fullScreenSaveState.Size = this.RestoreBounds.Size;
            }
            else
            {
                fullScreenSaveState.Location = this.Location;
                fullScreenSaveState.Size = this.Size;
            }


            WinTaskbar.Hide();
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            panelMain.BorderStyle = BorderStyle.None;
            this.TopMost = true;

            this.WindowState = FormWindowState.Normal;
            this.Location = new Point(0, 0);
            this.Size = new Size(Screen.PrimaryScreen.Bounds.Size.Width, Screen.PrimaryScreen.Bounds.Size.Height);

            toolStrip.Visible = false;
            statusStrip.Visible = false;
            menuStrip.Visible = false;



            fullscreen = true;
        }
        private void toggleFullScreen()
        {
            if (fullscreen)
            {
                exitFullScreen();
            }
            else
            {
                enterFullScreen();
            }
        }


        private void exitSlideshow()
        {
            exitFullScreen();
            timerSlideShow.Stop();
        }
        private void enterSlideshow()
        {
            enterFullScreen();
            timerSlideShow.Interval = config.Slideshow.Timer;
            timerSlideShow.Start();
        }



        /// <summary>
        /// Deletes the file currently being viewed. 
        /// TODO: add a switch based on the configuration to move to recycle bin by default instead of deleting.
        /// TODO: In case of AunauthorizedAccessException prompt user to restart the app in admin mode
        /// </summary>
        private void delete()
        {
            if (workingData.fileInfo != null)
            {
                if (MessageBox.Show(String.Format("The file {0} will be permanently deleted.\nAre you sure you want to continue?", workingData.fileInfo.Name), "Delete file?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    //before deleting, try to get access to the previous image, which will be automatically loaded upon file deletion.
                    //if there was only one image in the current working folder, then the app will close all

                    string nextFileToLoad = String.Empty;
                    if (workingData.directoryFiles.Length > 1)
                    {
                        //will try to move to the file
                        int moveToIndex = workingData.directoryIndex;
                        moveToIndex--;
                        if (moveToIndex < 0) moveToIndex = workingData.directoryFiles.Length - 1; //auto loop to the end
                        nextFileToLoad = workingData.directoryFiles[moveToIndex];
                    }


                    try
                    {
                        workingData.fileInfo.Delete();
                    }
                    catch (UnauthorizedAccessException uaex)
                    {
                        MessageBox.Show("Error wile deleting file\n.Insufficient user privilege.\nPlease restart the app as an administrator.\n\n" + uaex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("Error wile deleting file\n\n" + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    if (nextFileToLoad == String.Empty)
                    {
                        //only a single file in the folder -- close
                        close();
                    }
                    else
                    {
                        //load the next image and force a refresh of the folder structure
                        loadPicture(nextFileToLoad);
                    }


                }
            }
        }
    }
}