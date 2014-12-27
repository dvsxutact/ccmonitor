﻿using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ccMonitor.Gui
{
    public partial class GpuTab : UserControl
    {
        public GpuLogger Gpu { get; set; }

        public List<UserFriendlyBenchmark> UserFriendlyBenchmarks { get; set; }
        public class UserFriendlyBenchmark
        {
            // All strings, because that's user friendly :D
            public string TimeStarted { get; set; }
            public string TimeLastUpdate { get; set; }
            public string Algorithm { get; set; }
            public string AverageHashRate { get; set; }
            public string StandardDeviation { get; set; }
            public string HashCount { get; set; }
            public string AverageTemperature { get; set; }
            public string MinerNameVersion { get; set; }
            public string Stratum { get; set; }
        }
        
        public GpuTab(GpuLogger gpu)
        {
            Gpu = gpu;
            InitializeComponent();
            InitGpuDetails();
            InitCharts();
        }

        private void InitCharts()
        {
            HashChart hashChart = new HashChart(1) {Dock = DockStyle.Fill};
            tabHashCharts.Controls.Add(hashChart);

            SensorChart sensorChart = new SensorChart(1) {Dock = DockStyle.Fill};
            tabSensorCharts.Controls.Add(sensorChart);
        }

        private void InitGpuDetails()
        {
            BenchmarkDetails gpuDetails = new BenchmarkDetails(Gpu.Info, 6){Dock = DockStyle.Fill};
            tabGpuDetails.Controls.Add(gpuDetails);
        }

        public void UpdateGui()
        {
            UpdateInternalControls();

            UserFriendlyBenchmarks = new List<UserFriendlyBenchmark>(Gpu.BenchLogs.Count);
            foreach (GpuLogger.Benchmark benchmark in Gpu.BenchLogs)
            {
                UserFriendlyBenchmark userFriendlyBenchmark = new UserFriendlyBenchmark
                {
                    TimeStarted = GuiHelper.UnixTimeStampToDateTime(benchmark.TimeStamp).ToString("g"),
                    TimeLastUpdate = GuiHelper.UnixTimeStampToDateTime(benchmark.SensorLog
                                        [benchmark.SensorLog.Count - 1].TimeStamp).ToString("g"),
                    Algorithm = benchmark.Algorithm,
                    AverageHashRate = GuiHelper.GetRightMagnitude(benchmark.Statistic.AverageHashRate, "H"),
                    StandardDeviation = GuiHelper.GetRightMagnitude(benchmark.Statistic.StandardDeviation, "H"),
                    HashCount = GuiHelper.GetRightMagnitude(benchmark.Statistic.TotalHashCount),
                    AverageTemperature = benchmark.Statistic.AverageTemperature.ToString("##.##") + " °C",
                    MinerNameVersion = benchmark.MinerSetup.ToString(),
                    Stratum = benchmark.MinerSetup.MiningUrl
                };

                UserFriendlyBenchmarks.Insert(0, userFriendlyBenchmark);
            }

            int rowIndex = dgvBenchmarks.SelectedRows.Count > 0 ? dgvBenchmarks.SelectedRows[0].Index : 0;
            dgvBenchmarks.DataSource = UserFriendlyBenchmarks;
            if(dgvBenchmarks.Rows.Count > 0) dgvBenchmarks.CurrentCell = dgvBenchmarks.Rows[rowIndex].Cells[0];
        }

        private void UpdateInternalControls()
        {
            foreach (object control in tabGpuDetails.Controls)
            {
                BenchmarkDetails gpuDetails = control as BenchmarkDetails;
                if (gpuDetails != null) gpuDetails.UpdateStats(Gpu.CurrentBenchmark);
            }

            foreach (object control in tabHashCharts.Controls)
            {
                HashChart hashChart = control as HashChart;
                if (hashChart != null)
                {
                    hashChart.UpdateCharts(Gpu.CurrentBenchmark.HashLogs);
                }
            }

            foreach (object control in tabSensorCharts.Controls)
            {
                SensorChart hashChart = control as SensorChart;
                if (hashChart != null)
                {
                    hashChart.UpdateCharts(Gpu.CurrentBenchmark.SensorLog, Gpu.CurrentBenchmark.MinerSetup.OperatingSystem);
                }
            }
        }

        private void dgvBenchmarks_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            int rowIndex = e.RowIndex;
            if(rowIndex<0) return;

            
            BenchmarkOverview form = new BenchmarkOverview(Gpu.BenchLogs[Gpu.BenchLogs.Count - rowIndex - 1], Gpu.Info)
            {
                Text = Gpu.Info + " - Benchmark overview",
                Size = new Size(this.Size.Width, this.Size.Height)
            };
            
            form.Show();
        }
    }

    
}
