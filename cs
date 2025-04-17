using System;
using cAlgo.API;

namespace cAlgo.Robots
{
    [Robot(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class RobotWithEnumsAndOnTick : Robot
    {
        public enum LineColors
        {
            Yellow,
            Red,
            Green,
            Blue,
            Black
        }

        public enum LineStyles
        {
            Solid,
            DotsRare,
            Dots,
            Dashes
        }

        [Parameter("Line Color", DefaultValue = LineColors.Yellow)]
        public LineColors SelectedLineColor { get; set; }

        [Parameter("Line Style", DefaultValue = LineStyles.DotsRare)]
        public LineStyles SelectedLineStyle { get; set; }

        [Parameter("Line Thickness", DefaultValue = 2, MinValue = 1, MaxValue = 5)]
        public int LineThickness { get; set; }

        [Parameter("Scan Interval (Seconds)", DefaultValue = 0.5, MinValue = 0.1)]
        public double ScanInterval { get; set; }

        private Button startButton;
        private Button stopButton;
        private bool isScanning;
        private string lastFlagLineName; // To track and delete the last drawn flag line
        private int currentScanIndex;
        private DateTime nextScanTime; // To control the scan interval manually

        protected override void OnStart()
        {
            InitializeControls();
            currentScanIndex = Bars.OpenTimes.Count - 1;
            lastFlagLineName = null; // Initialize the last flag line name
            nextScanTime = Server.Time.AddSeconds(ScanInterval); // Initialize the next scan time
        }

        private void InitializeControls()
        {
            var controlPanel = new Canvas
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 150,
                Height = 100,
                BackgroundColor = Color.FromArgb(100, 0, 0, 0)
            };

            startButton = new Button
            {
                Text = "Start",
                Width = 140,
                Height = 30,
                Margin = 5,
                BackgroundColor = Color.Green,
                ForegroundColor = Color.White
            };
            startButton.Click += StartScan;

            stopButton = new Button
            {
                Text = "Stop",
                Width = 140,
                Height = 30,
                Margin = new Thickness(5, 40, 0, 0),
                BackgroundColor = Color.Red,
                ForegroundColor = Color.White,
                IsEnabled = false
            };
            stopButton.Click += StopScan;

            controlPanel.AddChild(startButton);
            controlPanel.AddChild(stopButton);
            Chart.AddControl(controlPanel);
        }

        private void StartScan(ButtonClickEventArgs obj)
        {
            if (isScanning) return;

            isScanning = true;
            startButton.IsEnabled = false;
            stopButton.IsEnabled = true;
        }

        private void StopScan(ButtonClickEventArgs obj)
        {
            if (!isScanning) return;

            isScanning = false;
            startButton.IsEnabled = true;
            stopButton.IsEnabled = false;
        }

        protected override void OnTick()
        {
            if (!isScanning) return;

            if (Server.Time >= nextScanTime)
            {
                DrawFlagLine(currentScanIndex);
                currentScanIndex--;

                // Update next scan time
                nextScanTime = Server.Time.AddSeconds(ScanInterval);

                // Stop scanning when reaching the end of available bars
                if (currentScanIndex < 0)
                {
                    StopScan(null);
                }
            }
        }

        private void DrawFlagLine(int index)
        {
            if (index < 0 || index >= Bars.OpenTimes.Count)
                return;

            // Remove the last flag line before drawing a new one
            if (!string.IsNullOrEmpty(lastFlagLineName))
            {
                Chart.RemoveObject(lastFlagLineName);
            }

            // Define the new flag line name
            lastFlagLineName = $"FlagLine_{index}";

            // Draw the new flag line
            var color = GetColorFromEnum(SelectedLineColor);
            var lineStyle = GetLineStyleFromEnum(SelectedLineStyle);
            Chart.DrawVerticalLine(lastFlagLineName, Bars.OpenTimes[index], color, LineThickness, lineStyle);
        }

        private Color GetColorFromEnum(LineColors lineColor)
        {
            return lineColor switch
            {
                LineColors.Yellow => Color.Yellow,
                LineColors.Red => Color.Red,
                LineColors.Green => Color.Green,
                LineColors.Blue => Color.Blue,
                LineColors.Black => Color.Black,
                _ => Color.Yellow // Default to Yellow if something goes wrong
            };
        }

        private LineStyle GetLineStyleFromEnum(LineStyles lineStyle)
        {
            return lineStyle switch
            {
                LineStyles.Solid => LineStyle.Solid,
                LineStyles.DotsRare => LineStyle.DotsRare,
                LineStyles.Dots => LineStyle.Dots,
                LineStyles.Dashes => LineStyle.Dashes,
                _ => LineStyle.DotsRare // Default to DotsRare if something goes wrong
            };
        }

        protected override void OnStop()
        {
            isScanning = false;

            // Clean up the last flag line when stopping
            if (!string.IsNullOrEmpty(lastFlagLineName))
            {
                Chart.RemoveObject(lastFlagLineName);
            }

            Print("Scanning stopped and last flag line removed.");
        }
    }
}
