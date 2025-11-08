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

        public Form1()
        {
            this.Text = "CleanSweep";
            this.BackColor = Color.LightBlue;
            this.ClientSize = new Size(500, 500);
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
                Location = new Point(10, 470),
                ForeColor = Color.DarkBlue
            };

            Controls.Add(titleLabel);
            Controls.Add(optionsList);
            Controls.Add(progressBar);
            Controls.Add(statusLabel);
            Controls.Add(startButton);
            Controls.Add(logBox);
            Controls.Add(footerLabel);
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
            startButton.Enabled = true;
        }

        private void PerformTask(string taskName)
        {
            try
            {
                switch (taskName)
                {
                    case "Clean Downloads":
                        CleanFolder(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads");
                        break;
                    case "Clean Documents":
                        CleanFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
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

        private void CleanFolder(string path)
        {
            if (!Directory.Exists(path)) return;
            foreach (var file in Directory.GetFiles(path))
            {
                try { File.Delete(file); }
                catch { }
            }
            foreach (var dir in Directory.GetDirectories(path))
            {
                try { Directory.Delete(dir, true); }
                catch { }
            }
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
    }
}
