using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Principal;
using System.Linq;

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

        private const string GitHubUrl = "https://github.com/renatus777rr";

        public Form1()
        {
            this.Text = "CleanSweep";
            this.BackColor = Color.LightBlue;
            this.ClientSize = new Size(500, 520);
            this.StartPosition = FormStartPosition.CenterScreen;

            titleLabel = new Label
            {
                Text = "CleanSweep (BETA)",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(130, 20)
            };

            optionsList = new CheckedListBox
            {
                Location = new Point(50, 70),
                Size = new Size(400, 120)
            };
            optionsList.Items.AddRange(new object[]
            {
                "Clean Downloads",
                "Clean Documents",
                "Clean Cache",
                "Clean Windows Update Files",
                "Clean Temporary Files",
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
                Text = "By renatus777rr on GitHub",
                AutoSize = true,
                Location = new Point(10, 490),
                ForeColor = Color.DarkBlue,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9, FontStyle.Underline)
            };
            footerLabel.Click += FooterLabel_Click;

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

            CheckAdminPrivileges();
        }
        private void FooterLabel_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(GitHubUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void CheckAdminPrivileges()
        {
            if (!IsAdministrator())
            {
                logBox.AppendText("Warning: Application is not running as Administrator." + Environment.NewLine);
                logBox.AppendText("System cleaning tasks (e.g., Windows Update, Chkdsk) may fail due to 'Access Denied'." + Environment.NewLine);
            }
        }

        private bool IsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                    .IsInRole(WindowsBuiltInRole.Administrator);
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
            var checkedItems = optionsList.CheckedItems.Cast<object>().ToList();

            foreach (var item in checkedItems)
            {
                string taskName = item.ToString();

                statusLabel.Invoke(new Action(() => statusLabel.Text = $"{progressBar.Value}% ({taskName}...)"));
                logBox.Invoke(new Action(() => logBox.AppendText($"Starting: {taskName}{Environment.NewLine}")));

                await Task.Run(() => PerformTask(taskName));

                progressBar.Invoke(new Action(() => progressBar.Value = Math.Min(progressBar.Value + step, 100)));
                statusLabel.Invoke(new Action(() => statusLabel.Text = $"{progressBar.Value}% ({taskName} done)"));
                logBox.Invoke(new Action(() => logBox.AppendText($"Completed: {taskName}{Environment.NewLine}")));
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
                    case "Clean Temporary Files":
                        CleanTempFiles();
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

        private void CleanTempFiles()
        {
            string userTempPath = Path.GetTempPath();
            CleanFolderInternal(userTempPath, "User Temp");

            // System temp path is usually C:\Windows\Temp, accessible via Environment variable, but requires Admin rights.
            string systemTempPath = Environment.GetEnvironmentVariable("windir") + @"\Temp";
            CleanFolderInternal(systemTempPath, "System Temp");
        }

        private bool Confirm(string folderName)
        {
            var result = MessageBox.Show($"Are you sure you want to clean all contents of the {folderName} folder?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            return result == DialogResult.Yes;
        }

        private void CleanFolder(string path)
        {
            CleanFolderInternal(path, new DirectoryInfo(path).Name);
        }

        private void CleanFolderInternal(string path, string name)
        {
            logBox.Invoke(new Action(() =>
            {
                logBox.AppendText($"  Processing folder '{name}' at: {path}{Environment.NewLine}");
            }));

            if (!Directory.Exists(path))
            {
                logBox.Invoke(new Action(() => logBox.AppendText($"  Path does not exist: {path}{Environment.NewLine}")));
                return;
            }

            foreach (var file in Directory.GetFiles(path))
            {
                try
                {
                    long size = new FileInfo(file).Length;
                    File.Delete(file);
                    totalBytesDeleted += size;
                }
                catch (UnauthorizedAccessException ex)
                {
                    logBox.Invoke(new Action(() => logBox.AppendText($"    Access Denied to file {Path.GetFileName(file)}. Run as Admin. ({ex.GetType().Name}){Environment.NewLine}")));
                }
                catch (Exception ex)
                {
                    logBox.Invoke(new Action(() => logBox.AppendText($"    Failed to delete file {Path.GetFileName(file)}: {ex.Message}{Environment.NewLine}")));
                }
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
                catch (UnauthorizedAccessException ex)
                {
                    logBox.Invoke(new Action(() => logBox.AppendText($"    Access Denied to directory {Path.GetFileName(dir)}. Run as Admin. ({ex.GetType().Name}){Environment.NewLine}")));
                }
                catch (IOException ex)
                {
                    logBox.Invoke(new Action(() => logBox.AppendText($"    Directory is in use/not empty {Path.GetFileName(dir)}: {ex.Message}{Environment.NewLine}")));
                }
                catch (Exception ex)
                {
                    logBox.Invoke(new Action(() => logBox.AppendText($"    Failed to delete directory {Path.GetFileName(dir)}: {ex.Message}{Environment.NewLine}")));
                }
            }
        }

        private long GetDirectorySize(DirectoryInfo dir)
        {
            long size = 0;
            try
            {
                foreach (var file in dir.GetFiles("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        size += file.Length;
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
            return size;
        }

        private string FormatSize(long bytes)
        {
            string[] suffixes = { "bytes", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dblBytes = bytes;

            while (dblBytes >= 1024 && i < suffixes.Length - 1)
            {
                dblBytes /= 1024;
                i++;
            }

            return $"{dblBytes:0.##} {suffixes[i]}";
        }

        private void RunChkdsk()
        {
            try
            {
                if (!IsAdministrator())
                {
                    logBox.Invoke(new Action(() => logBox.AppendText("  Chkdsk requires Administrator privileges to run effectively." + Environment.NewLine)));
                }

                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/C chkdsk C:")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    string error = p.StandardError.ReadToEnd();
                    p.WaitForExit();

                    logBox.Invoke(new Action(() =>
                    {
                        logBox.AppendText("  --- Chkdsk Output ---" + Environment.NewLine);
                        logBox.AppendText(output);
                        if (!string.IsNullOrWhiteSpace(error))
                        {
                            logBox.AppendText("  --- Chkdsk Error ---" + Environment.NewLine);
                            logBox.AppendText(error + Environment.NewLine);
                        }
                        logBox.AppendText("  --- End Chkdsk ---" + Environment.NewLine);
                    }));
                }
            }
            catch (Exception ex)
            {
                logBox.Invoke(new Action(() => logBox.AppendText($"  Failed to run Chkdsk: {ex.Message}{Environment.NewLine}")));
            }
        }

        private void DarkModeToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (darkModeToggle.Checked)
            {
                this.BackColor = Color.FromArgb(30, 30, 30); // Dark Gray background

                titleLabel.ForeColor = Color.White;
                statusLabel.ForeColor = Color.White;
                footerLabel.ForeColor = Color.LightGray;

                logBox.BackColor = Color.FromArgb(50, 50, 50); // Slightly lighter log background
                logBox.ForeColor = Color.White;

                startButton.BackColor = Color.DimGray;
                startButton.ForeColor = Color.White;

                darkModeToggle.ForeColor = Color.White;
                optionsList.BackColor = Color.FromArgb(50, 50, 50);
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