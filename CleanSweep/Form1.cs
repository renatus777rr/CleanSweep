using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CleanSweep
{
    public class Form1 : Form
    {
        private Label titleLabel;
        private CheckedListBox optionsList;
        private ProgressBar progressBar;
        private Label statusLabel;
        private Button startButton;
        private TextBox logBox;
        private Label footerLabel;
        private CheckBox darkModeToggle;
        private NotifyIcon notifyIcon;
        private long totalBytesDeleted = 0;

        public Form1()
        {
            this.Text = "CleanSweep";
            this.BackColor = Color.LightBlue;
            this.ClientSize = new Size(500, 520);
            this.StartPosition = FormStartPosition.CenterScreen;

            titleLabel = new Label
            {
                Text = "CleanSweep (ALPHA)",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(130, 20)
            };

            optionsList = new CheckedListBox
            {
                Location = new Point(50, 70),
                Size = new Size(400, 100)
            };
            optionsList.Items.AddRange(new object[]
            {
                "Clean Downloads",
                "Clean Documents",
                "Clean Cache",
                "Clean Windows Update Files",
                "Chkdsk check (Advanced)"
            });

            progressBar = new ProgressBar
            {
                Location = new Point(50, 200),
                Size = new Size(400, 25)
            };

            statusLabel = new Label
            {
                Text = "0% (idle)",
                AutoSize = true,
                Location = new Point(50, 235)
            };

            startButton = new Button
            {
                Text = "Start",
                Location = new Point(200, 270),
                Size = new Size(100, 40)
            };
            startButton.Click += StartButton_Click;

            logBox = new TextBox
            {
                Location = new Point(50, 330),
                Size = new Size(400, 120),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true
            };

            footerLabel = new Label
            {
                Text = "By renatus777rr on github",
                AutoSize = true,
                Location = new Point(10, 490),
                ForeColor = Color.DarkBlue
            };

            darkModeToggle = new CheckBox
            {
                Text = "Dark Mode",
                Location = new Point(380, 490),
                AutoSize = true
            };
            darkModeToggle.CheckedChanged += DarkModeToggle_CheckedChanged;

            notifyIcon = new NotifyIcon
            {
                Visible = true,
                Icon = SystemIcons.Information
            };

            Controls.Add(titleLabel);
            Controls.Add(optionsList);
            Controls.Add(progressBar);
            Controls.Add(statusLabel);
            Controls.Add(startButton);
            Controls.Add(logBox);
            Controls.Add(footerLabel);
            Controls.Add(darkModeToggle);
        }

        private async void StartButton_Click(object sender, EventArgs e)
        {
            int total = optionsList.CheckedItems.Count;
            if (total == 0)
            {
                MessageBox.Show("Please select at least one option.");
                return;
            }

            startButton.Enabled = false;
            progressBar.Value = 0;
            logBox.Clear();
            statusLabel.Text = "0% (starting)";
            totalBytesDeleted = 0;

            int step = Math.Max(1, 100 / total);
            foreach (var item in optionsList.CheckedItems)
            {
                string taskName = item.ToString();
                statusLabel.Text = $"{progressBar.Value}% ({taskName}...)";
                logBox.AppendText($"Starting: {taskName}{Environment.NewLine}");

                await Task.Run(() => PerformTask(taskName));

                progressBar.Value = Math.Min(progressBar.Value + step, 100);
                statusLabel.Text = $"{progressBar.Value}% ({taskName} done)";
                logBox.AppendText($"Completed: {taskName}{Environment.NewLine}");
            }

            progressBar.Value = 100;
            statusLabel.Text = "100% (Completed)";
            logBox.AppendText("All tasks completed." + Environment.NewLine);

            string cleanedSize = FormatSize(totalBytesDeleted);
            MessageBox.Show($"{cleanedSize} was cleaned!", "CleanSweep Report");

            notifyIcon.ShowBalloonTip(3000, "CleanSweep", "All tasks completed successfully.", ToolTipIcon.Info);

            startButton.Enabled = true;
        }

        private void PerformTask(string taskName)
        {
            try
            {
                switch (taskName)
                {
                    case "Clean Downloads":
                        if (Confirm("Downloads")) CleanFolder(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads");
                        break;
                    case "Clean Documents":
                        if (Confirm("Documents")) CleanFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                        break;
                    case "Clean Cache":
                        CleanFolder(Environment.GetFolderPath(Environment.SpecialFolder.InternetCache));
                        break;
                    case "Clean Windows Update Files":
                        CleanFolder(@"C:\Windows\SoftwareDistribution\Download");
                        break;
                    case "Chkdsk check (Advanced)":
                        RunChkdsk();
                        break;
                }
            }
            catch (Exception ex)
            {
                logBox.Invoke(new Action(() =>
                {
                    logBox.AppendText($"Error in {taskName}: {ex.Message}{Environment.NewLine}");
                }));
            }
        }

        private bool Confirm(string folderName)
        {
            var result = MessageBox.Show($"Are you sure to clean {folderName}?", "Confirmation", MessageBoxButtons.YesNo);
            return result == DialogResult.Yes;
        }

        private void CleanFolder(string path)
        {
            if (!Directory.Exists(path)) return;
            foreach (var file in Directory.GetFiles(path))
            {
                try
                {
                    long size = new FileInfo(file).Length;
                    File.Delete(file);
                    totalBytesDeleted += size;
                }
                catch { }
            }
            foreach (var dir in Directory.GetDirectories(path))
            {
                try
                {
                    DirectoryInfo di = new DirectoryInfo(dir);
                    long size = GetDirectorySize(di);
                    Directory.Delete(dir, true);
                    totalBytesDeleted += size;
                }
                catch { }
            }
        }

        private long GetDirectorySize(DirectoryInfo dir)
        {
            long size = 0;
            try
            {
                foreach (var file in dir.GetFiles("*", SearchOption.AllDirectories))
                {
                    size += file.Length;
                }
            }
            catch { }
            return size;
        }

        private string FormatSize(long bytes)
        {
            if (bytes > 1024 * 1024 * 1024)
                return $"{bytes / (1024 * 1024 * 1024)} GB";
            else if (bytes > 1024 * 1024)
                return $"{bytes / (1024 * 1024)} MB";
            else if (bytes > 1024)
                return $"{bytes / 1024} KB";
            else
                return $"{bytes} bytes";
        }

        private void RunChkdsk()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("chkdsk.exe", "C:")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    logBox.Invoke(new Action(() =>
                    {
                        logBox.AppendText(output + Environment.NewLine);
                    }));
                }
            }
            catch { }
        }

        private void DarkModeToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (darkModeToggle.Checked)
            {
                this.BackColor = Color.Black;

                titleLabel.ForeColor = Color.White;
                statusLabel.ForeColor = Color.White;
                footerLabel.ForeColor = Color.LightGray;

                logBox.BackColor = Color.Black;
                logBox.ForeColor = Color.White;

                startButton.BackColor = Color.DimGray;
                startButton.ForeColor = Color.White;

                darkModeToggle.ForeColor = Color.White;
                optionsList.BackColor = Color.Black;
                optionsList.ForeColor = Color.White;
            }
            else
            {
                this.BackColor = Color.LightBlue;

                titleLabel.ForeColor = Color.Black;
                statusLabel.ForeColor = Color.Black;
                footerLabel.ForeColor = Color.DarkBlue;

                logBox.BackColor = Color.White;
                logBox.ForeColor = Color.Black;

                startButton.BackColor = SystemColors.Control;
                startButton.ForeColor = Color.Black;

                darkModeToggle.ForeColor = Color.Black;
                optionsList.BackColor = Color.White;
                optionsList.ForeColor = Color.Black;
            }
        }
    }
}
