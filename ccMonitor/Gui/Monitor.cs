﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ccMonitor.Api;
using Newtonsoft.Json;

namespace ccMonitor.Gui
{
    public partial class Monitor : Form
    {
        private RigController _controller;

        private int _timerTime = 10000;
        private System.Threading.Timer _updateTimer; // Different thread
        private System.Windows.Forms.Timer _guiTimer; // Same thread

        public Monitor()
        {
            
            InitializeComponent();

            LoadSettings();
            LoadLogs();

            InitTimers();
            InitGui();
        }

        private void InitGui()
        {
            dgvRigs.AutoGenerateColumns = false;
            dgvRigs.DataSource = _controller.RigLogs;
        }

        private void InitTimers()
        {
            _updateTimer = new System.Threading.Timer(UpdateController, null, 0, Timeout.Infinite);

            _guiTimer = new System.Windows.Forms.Timer { Interval = _timerTime/2 };
            _guiTimer.Tick += GuiTimerTick;
            _guiTimer.Start();

            UpdateGui();
        }

        private void GuiTimerTick(object sender, EventArgs e)
        {
            UpdateGui();
            RemoveDeletedRigs();
        }

        private void RemoveDeletedRigs()
        {
            //// Really complicated way to dynamically remove deleted rigs
            //// Need to find a better way
            //Dictionary<string, bool> rigExistence = new Dictionary<string, bool>();
            //foreach (TabPage tabPage in tbcMonitor.TabPages)
            //{
            //    // Adds all tabs to a dict with def value false, except if it's the tab already there
            //    rigExistence.Add(tabPage.Text, tabPage == tabGeneral);
            //}

            //foreach (RigController.Rig rig in _controller.RigLogs)
            //{
            //    if (rig.UserFriendlyName != null && rigExistence.ContainsKey(rig.UserFriendlyName))
            //    {
            //        // If it exists in both the controller logs and in the tabs, it's value should be made true 
            //        rigExistence[rig.UserFriendlyName] = true;
            //    }
            //}

            //foreach (TabPage tabPage in tbcMonitor.TabPages)
            //{
            //    if (rigExistence[tabPage.Text] == false)
            //    {
            //        // Remove it if it's not in the controller logs anymore
            //        tbcMonitor.TabPages.Remove(tabPage);
            //    }
            //}
        }

        private void UpdateGui()
        {
            lstGeneralOverview.BeginUpdate();
            // Grabs all the selected items in the General Overview List
            int[] selectedIndexes = new int[lstGeneralOverview.SelectedIndices.Count];
            if (lstGeneralOverview.SelectedIndices.Count > 0)
            {
                for (int i = 0; i < selectedIndexes.Length; i++)
                {
                    selectedIndexes[i] = lstGeneralOverview.SelectedIndices[i];
                }
            }

            // Completely refresh the General Overview List
            lstGeneralOverview.Items.Clear();
            lstGeneralOverview.Groups.Clear();
            
            foreach (RigController.Rig rig in _controller.RigLogs)
            {
                
                // Makes sure all rigs that are in the logs are shown in RigStats
                // And updates them
                bool tabPageExists = false;
                foreach (TabPage tabPage in tbcMonitor.TabPages)
                {
                    if (tabPage.Text == rig.UserFriendlyName)
                    {
                        tabPageExists = true;
                    }

                    foreach (object control in tabPage.Controls)
                    {
                        RigTab rigTab = control as RigTab;
                        if (rigTab != null) rigTab.UpdateGui();
                    }
                }                

                if (!tabPageExists)
                {
                    // Makes new tabpage in rigstats
                    TabPage tabPage = new TabPage(rig.UserFriendlyName);
                    RigTab rigTab = new RigTab(rig) {Dock = DockStyle.Fill};
                    rigTab.UpdateGui();
                    tabPage.Controls.Add(rigTab);
                    tabPage.Dock = DockStyle.Fill;
                    tbcMonitor.TabPages.Add(tabPage);
                }
                
                
                // Adds the controller info to the listview
                ListViewGroup lvg = new ListViewGroup(rig.UserFriendlyName);
                lstGeneralOverview.Groups.Add(lvg);

                ListViewItem lvi;
                foreach (GpuLogger gpu in rig.GpuLogs)
                {
                    lvi = new ListViewItem(gpu.Info.MinerMap.ToString(CultureInfo.InvariantCulture), lvg);
                    if (gpu.CurrentBenchmark.AvailableTimeStamps.Count == 0 || gpu.CurrentBenchmark.AvailableTimeStamps.Last().Available == false) 
                        lvi.BackColor = Color.FromArgb(100, Color.Red);
                    lvi.SubItems.Add(gpu.Info.Name);
                    lvi.SubItems.Add(string.Empty);
                    if (gpu.CurrentBenchmark == null || gpu.CurrentBenchmark.CurrentStatistic == null)
                    {
                        lvi.SubItems.Add(string.Empty);
                        lvi.SubItems.Add(string.Empty);
                        lvi.SubItems.Add(string.Empty);
                        lvi.SubItems.Add(string.Empty);
                        lvi.SubItems.Add(string.Empty);
                        lvi.SubItems.Add(string.Empty);
                        lvi.SubItems.Add(string.Empty);
                    }
                    else
                    {
                        lvi.SubItems.Add(GuiHelper.GetRightMagnitude(gpu.CurrentBenchmark.CurrentStatistic.HarmonicAverageHashRate, "H"));
                        lvi.SubItems.Add(GuiHelper.GetRightMagnitude(gpu.CurrentBenchmark.CurrentStatistic.StandardDeviation, "H"));
                        lvi.SubItems.Add(GuiHelper.GetRightMagnitude(gpu.CurrentBenchmark.CurrentStatistic.TotalHashCount));
                        lvi.SubItems.Add(gpu.CurrentBenchmark.CurrentStatistic.Founds.ToString(CultureInfo.InvariantCulture));
                        lvi.SubItems.Add(string.Empty);
                        lvi.SubItems.Add(string.Empty);
                        lvi.SubItems.Add(gpu.CurrentBenchmark.SensorLog[gpu.CurrentBenchmark.SensorLog.Count - 1]
                            .Temperature.ToString(CultureInfo.InvariantCulture) + " °C");
                    }
                    lvi.SubItems.Add(string.Empty);
                    lstGeneralOverview.Items.Add(lvi);
                }

                lvi = new ListViewItem(string.Empty, lvg);
                lvi.SubItems.Add("Total");
                if (rig.CurrentStatistic == null)
                {
                    lvi.SubItems.Add(string.Empty);
                    lvi.SubItems.Add(string.Empty);
                    lvi.SubItems.Add(string.Empty);
                    lvi.SubItems.Add(string.Empty);
                    lvi.SubItems.Add(string.Empty);
                    lvi.SubItems.Add(string.Empty);
                    lvi.SubItems.Add(string.Empty);
                    lvi.SubItems.Add(string.Empty);
                    lvi.SubItems.Add(string.Empty);
                }
                else
                {
                    lvi.SubItems.Add(rig.CurrentStatistic.Algorithm);
                    lvi.SubItems.Add(GuiHelper.GetRightMagnitude(rig.CurrentStatistic.TotalHashRate, "H"));
                    lvi.SubItems.Add(GuiHelper.GetRightMagnitude(rig.CurrentStatistic.TotalStandardDeviation, "H"));
                    lvi.SubItems.Add(GuiHelper.GetRightMagnitude(rig.CurrentStatistic.TotalHashCount));
                    lvi.SubItems.Add(string.Empty);
                    lvi.SubItems.Add(rig.CurrentStatistic.Accepts.ToString(CultureInfo.InvariantCulture));
                    lvi.SubItems.Add(rig.CurrentStatistic.Rejects.ToString(CultureInfo.InvariantCulture));
                    lvi.SubItems.Add(string.Empty);
                    lvi.SubItems.Add(rig.CurrentStatistic.ShareAnswerPing.ToString(CultureInfo.InvariantCulture));
                }
                
                lstGeneralOverview.Items.Add(lvi);
            }

            // Restores all the previously selected items in the General Overview List
            if (lstGeneralOverview.Items.Count > 0)
            {
                foreach (int selectedIndex in selectedIndexes)
                {
                    lstGeneralOverview.Items[selectedIndex].Selected = true;
                    lstGeneralOverview.Select();
                }
            }
            lstGeneralOverview.EndUpdate();
        }

        private void UpdateController(object o)
        {
            _controller.Update();
            _updateTimer.Change(_timerTime, Timeout.Infinite);
        }

        private void LoadSettings()
        {
            
        }

        private void LoadLogs()
        {
            _controller = File.Exists("logs.gz") ? new RigController(JsonControl.GetSerializedGzipFile<BindingList<RigController.Rig>>("logs.gz")) : new RigController();
        }


        private void Monitor_FormClosing(object sender, FormClosingEventArgs e)
        {
            _updateTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _guiTimer.Stop();
            _controller.DisableAllRigs(true);
            SaveLogs();
            SaveSettings();
        }

        private void SaveLogs()
        {
            JsonControl.WriteSerializedGzipFile("logs.gz", _controller.RigLogs);
        }

        private void SaveSettings()
        {
            
        }

        private void lstGeneralOverview_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (lstGeneralOverview.SelectedItems.Count > 0)
            {
                ListViewItem listViewItem = lstGeneralOverview.SelectedItems[0];
                foreach (TabPage rigPage in tbcMonitor.TabPages)
                {
                    if (rigPage.Text == listViewItem.Group.Header)
                    {
                        tbcMonitor.SelectTab(rigPage);
                    }

                    if (listViewItem.Text != string.Empty)
                    {
                        foreach (object control in rigPage.Controls)
                        {
                            RigTab rigTab = control as RigTab;
                            if (rigTab != null)
                            {
                                foreach (TabPage gpuPage in rigTab.tbcRig.TabPages)
                                {
                                    if (gpuPage.Text.EndsWith(
                                        listViewItem.Text, StringComparison.Ordinal))
                                    {
                                        rigTab.tbcRig.SelectTab(gpuPage);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void dgvRigs_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            _updateTimer.Change(0, Timeout.Infinite);
            _guiTimer.Start();
        }

        private void dgvRigs_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            _updateTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _guiTimer.Stop();
        }

        private void saveLogsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveLogs();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void showRawLogsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Form form = new Form())
            {
                form.Text = "Raw logs";
                form.Size = new Size(this.Size.Width/2, this.Size.Height);


                TextBox textBox = new TextBox
                {
                    Text = JsonConvert.SerializeObject(_controller.RigLogs, Formatting.Indented),
                    Multiline = true,
                    ScrollBars = ScrollBars.Both,
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                };

                form.Controls.Add(textBox);
                form.ShowDialog();

                textBox.SelectAll();
                textBox.Focus();
            }
        }

        private void readMeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Form form = new Form())
            {
                form.Text = "Read Me";
                form.Size = new Size(this.Size.Width, this.Size.Height);

                TextBox textBox = new TextBox
                {
                    Text = File.Exists("README.txt") ? File.ReadAllText("README.txt") : GetDefaultReadme(),
                    Multiline = true,
                    ScrollBars = ScrollBars.Both,
                    Dock = DockStyle.Fill,
                    ReadOnly = true
                };

                form.Controls.Add(textBox);
                form.ShowDialog();
            }
        }

        private string GetDefaultReadme()
        {
            return
                "Oops, seems like README.txt is missing. " +
                Environment.NewLine + "You can get a copy yourself at https://github.com/KBomba/ccmonitor . " +
                Environment.NewLine + "Don't forget to share some love at (BTC) 1BombaWy46SPqX8NJumFBvSjSpry8hpzr4 :)";
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (AboutBox aboutBox = new AboutBox())
            {
                aboutBox.ShowDialog();
            }
        }

        private void takeScreenshotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Bitmap screenshot = new Bitmap(this.Width, this.Height);
            this.DrawToBitmap(screenshot, new Rectangle(0, 0, screenshot.Width, screenshot.Height));
            Image screenImage = screenshot;

            ImageCodecInfo pngEncoder = null;
            ImageCodecInfo[] codecInfos = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codecInfo in codecInfos)
            {
                if (codecInfo.MimeType == "image/png")
                {
                    pngEncoder = codecInfo;
                    break;
                }
            }

            if (pngEncoder != null)
            {
                EncoderParameter quality = new EncoderParameter(Encoder.Quality, 100);
                EncoderParameter compression = new EncoderParameter(Encoder.Compression, 0);
                EncoderParameters encoderParameters = new EncoderParameters(2);
                encoderParameters.Param[0] = quality;
                encoderParameters.Param[1] = compression;
                
                screenImage.Save(GuiHelper.GetCurrentUnixTimeStamp() + ".png", pngEncoder, encoderParameters);
            }
            else
            {
                screenImage.Save(GuiHelper.GetCurrentUnixTimeStamp() + ".png", ImageFormat.Png);
            }
            
        }

        private void scGeneral_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
