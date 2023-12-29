using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using AForge.Imaging.Filters;
using IronOcr;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace QueueNotify
{
    public partial class Form1 : Form
    {
        private System.Windows.Forms.Timer screenshotTimer;

        public Form1()
        {
            InitializeComponent();

            PopulateProcessComboBox();

            // Initialize the timer
            screenshotTimer = new System.Windows.Forms.Timer();
            screenshotTimer.Interval = 10000; // 10 seconds in milliseconds
            screenshotTimer.Tick += ScreenshotTimer_Tick;
        }

        private void PopulateProcessComboBox()
        {
            // Clear existing items
            comboBox1.Items.Clear();

            // Get all running processes
            Process[] processes = Process.GetProcesses();

            // Add process names to the ComboBox
            foreach (Process process in processes)
            {
                comboBox1.Items.Add(process.ProcessName);
            }
        }

        private void ScreenshotTimer_Tick(object sender, EventArgs e)
        {
            CaptureScreenshot();
        }

        private void CaptureScreenshot()
        {
            try
            {
                // Get the selected process name from the ComboBox
                string processName = comboBox1.SelectedItem as string;

                if (string.IsNullOrEmpty(processName))
                {
                    MessageBox.Show("Please select a process from the ComboBox.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Find the process by name
                Process[] processes = Process.GetProcessesByName(processName);

                if (processes.Length == 0)
                {
                    MessageBox.Show($"Process '{processName}' not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Get the main window handle (HWND) of the first instance of the process
                IntPtr mainWindowHandle = processes[0].MainWindowHandle;

                // Get the bounds of the application's main window
                RECT windowRect;
                GetWindowRect(mainWindowHandle, out windowRect);

                // Calculate the width and height of the window
                int width = windowRect.right - windowRect.left;
                int height = windowRect.bottom - windowRect.top;

                // Create a Bitmap object to hold the screenshot
                Bitmap screenshot = new Bitmap(width, height);

                // Create a Graphics object from the Bitmap
                using (Graphics g = Graphics.FromImage(screenshot))
                {
                    // Copy the window to the Bitmap
                    PrintWindow(mainWindowHandle, g.GetHdc(), 0);
                    g.ReleaseHdc();
                }

                // Do something with the captured screenshot (e.g., call RecognizeQueueFromImage)
                RecognizeQueueFromImage(screenshot);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error capturing screenshot: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        /*private void CaptureScreenshot()
        {
            try
            {
                // Create a Bitmap object to hold the screenshot
                Bitmap screenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);

                // Create a Graphics object from the Bitmap
                using (Graphics g = Graphics.FromImage(screenshot))
                {
                    // Copy the screen to the Bitmap
                    g.CopyFromScreen(0, 0, 0, 0, screenshot.Size);
                }

                RecognizeQueueFromImage(screenshot);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error capturing screenshot: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }*/

        private void RecognizeQueueFromImage(Bitmap image)
        {
            try
            {
                // Convert the image to grayscale for better processing
                Bitmap grayImage = Grayscale.CommonAlgorithms.BT709.Apply(image);

                // Apply OCR using IronOcr
                var ocr = new IronTesseract();
                var result = ocr.Read(grayImage);

                // Extract the text
                string extractedText = result.Text;

                // Extract integers from the text using regular expressions
                var integers = ExtractIntegersFromText(extractedText);

                // Display the extracted integers
                //MessageBox.Show($"Queue Position: {integers.Item1}, Queue Length: {integers.Item2}", "Extracted Integers", MessageBoxButtons.OK, MessageBoxIcon.Information);

                if(integers.Item1 != 0 || integers.Item2 != 0)
                    ShowResult($"You are {integers.Item1}/{integers.Item2} in queue", integers.Item1);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error recognizing queue: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private (int, int) ExtractIntegersFromText(string text)
        {
            // Use a regular expression to find the pattern "You are 212/282 in queue"
            var match = Regex.Match(text, @"You are (\d+)/(\d+) in queue");

            // Check if the match was successful and extract the integers
            if (match.Success)
            {
                int queuePos = int.Parse(match.Groups[1].Value);
                int queueLength = int.Parse(match.Groups[2].Value);

                return (queuePos, queueLength);
            }

            // Return default values if no match was found
            return (0, 0);
        }

        private async void ShowResult(string message, int queuePos)
        {
            textBox1.Text = message;

            if (queuePos < 5)
            {
                PushoverService pushover = new PushoverService();

                await pushover.SendPushoverNotificationAsync(message);

                MessageBox.Show($"Important Message: {message}", "Important Notification", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            screenshotTimer.Stop();
            textBox1.BackColor = System.Drawing.Color.Red;
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            CaptureScreenshot();
            screenshotTimer.Start();
            textBox1.BackColor = System.Drawing.Color.Green;
        }
    }
}
