using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace NavaInjectorGUI
{
    public partial class MainWindow : Window
    {
        private Process injectorProcess;
        private DispatcherTimer statusCheckTimer;
        private bool isInjecting = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            LogMessage("Nava Injector GUI initialized");
            CheckAdminRights();
            CheckDefenderStatus();
            SetupStatusMonitoring();
        }

        private void SetupStatusMonitoring()
        {
            statusCheckTimer = new DispatcherTimer();
            statusCheckTimer.Interval = TimeSpan.FromSeconds(2);
            statusCheckTimer.Tick += (s, e) =>
            {
                CheckProcessStatus();
                CheckDefenderStatus();
            };
            statusCheckTimer.Start();
        }

        private void CheckAdminRights()
        {
            try
            {
                bool isAdmin = new System.Security.Principal.WindowsPrincipal(
                    System.Security.Principal.WindowsIdentity.GetCurrent())
                    .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

                AdminStatusText.Text = isAdmin ? "Elevated ✓" : "Not Elevated ✗";
                AdminStatusText.Foreground = isAdmin ? new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Color.FromRgb(255, 102, 102));

                if (!isAdmin)
                {
                    LogMessage("WARNING: Running without administrator privileges. Injection may fail.");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error checking admin rights: {ex.Message}");
            }
        }

        private void CheckDefenderStatus()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("root\\SecurityCenter2", "SELECT * FROM AntiVirusProduct");
                var antivirusProducts = searcher.Get();

                bool defenderRunning = false;
                foreach (ManagementObject product in antivirusProducts)
                {
                    string productName = (string)product["displayName"];
                    if (productName.Contains("Windows Defender") || productName.Contains("Microsoft Defender"))
                    {
                        object productState = product["productState"];
                        defenderRunning = true;
                        break;
                    }
                }

                DefenderStatusText.Text = defenderRunning ? "RUNNING - Disable it!" : "Disabled ✓";
                DefenderStatusText.Foreground = defenderRunning ? new SolidColorBrush(Color.FromRgb(255, 170, 0)) : new SolidColorBrush(Colors.LimeGreen);

                if (defenderRunning)
                {
                    LogMessage("Real-time Protection is running. Please disable it before injection.");
                }
            }
            catch
            {
                DefenderStatusText.Text = "Unable to check";
                DefenderStatusText.Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200));
            }
        }

        private void CheckProcessStatus()
        {
            try
            {
                var sebProcesses = Process.GetProcessesByName("SafeExamBrowser");
                var clientProcesses = Process.GetProcessesByName("SafeExamBrowser.Client");

                bool sebRunning = sebProcesses.Length > 0 || clientProcesses.Length > 0;

                ProcessStatusText.Text = sebRunning ? "Running ✓" : "Not Running";
                ProcessStatusText.Foreground = sebRunning ? new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Color.FromRgb(255, 102, 102));
            }
            catch { }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // Check prerequisites
            bool isAdmin = new System.Security.Principal.WindowsPrincipal(
                System.Security.Principal.WindowsIdentity.GetCurrent())
                .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

            if (!isAdmin)
            {
                MessageBox.Show("Please run this application as Administrator!", "Admin Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            StartInjection();
        }

        private void StartInjection()
        {
            try
            {
                LogMessage("═══════════════════════════════════════════════════════");
                LogMessage("Nava Injector starting...");
                LogMessage("═══════════════════════════════════════════════════════");

                // Update UI
                StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(255, 200, 0));
                StatusText.Text = "Injecting...";
                StartButton.IsEnabled = false;
                StopButton.IsEnabled = true;
                isInjecting = true;

                LogMessage("Checking prerequisites...");

                // Check if nava_standalone64.exe exists
                string navaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nava_standalone64.exe");
                if (!File.Exists(navaPath))
                {
                    LogMessage("ERROR: nava_standalone64.exe not found!");
                    LogMessage("Please place nava_standalone64.exe in the application directory.");
                    ResetUI(false);
                    return;
                }

                LogMessage("✓ Found nava_standalone64.exe");

                // Check Defender
                if (DisableDefenderCheckBox.IsChecked == true)
                {
                    LogMessage("Attempting to disable Real-time Protection...");
                    LogMessage("NOTE: You may need to confirm this action in Windows Security.");
                }

                LogMessage("═══════════════════════════════════════════════════════");
                LogMessage("[Nava] Initializing injection process...");
                LogMessage("[Nava] Waiting for SafeExamBrowser process...");

                // Start the injector process
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = navaPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                };

                injectorProcess = Process.Start(psi);
                injectorProcess.OutputDataReceived += InjectorProcess_OutputDataReceived;
                injectorProcess.ErrorDataReceived += InjectorProcess_ErrorDataReceived;
                injectorProcess.BeginOutputReadLine();
                injectorProcess.BeginErrorReadLine();

                LogMessage("═══════════════════════════════════════════════════════");
                LogMessage("[Nava] Starting");
                LogMessage("═══════════════════════════════════════════════════════");
                LogMessage("");
                LogMessage("INJECTOR ACTIVE - Keep this window open!");
                LogMessage("Double-click a .seb file or start from exam provider...");
                LogMessage("");

                StatusIndicator.Fill = new SolidColorBrush(Colors.LimeGreen);
                StatusText.Text = "Active";
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: Failed to start injection: {ex.Message}");
                ResetUI(false);
            }
        }

        private void InjectorProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Dispatcher.Invoke(() => LogMessage(e.Data));
            }
        }

        private void InjectorProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Dispatcher.Invoke(() => LogMessage($"[ERROR] {e.Data}"));
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopInjection();
        }

        private void StopInjection()
        {
            try
            {
                if (injectorProcess != null && !injectorProcess.HasExited)
                {
                    LogMessage("");
                    LogMessage("═══════════════════════════════════════════════════════");
                    LogMessage("Stopping injection...");
                    injectorProcess.Kill();
                    injectorProcess.WaitForExit();
                    LogMessage("Injection stopped");
                    LogMessage("═══════════════════════════════════════════════════════");
                }

                ResetUI(true);
            }
            catch (Exception ex)
            {
                LogMessage($"Error stopping injection: {ex.Message}");
            }
        }

        private void ResetUI(bool success)
        {
            isInjecting = false;
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;

            if (success)
            {
                StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(0, 170, 0));
                StatusText.Text = "Stopped";
            }
            else
            {
                StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(255, 68, 68));
                StatusText.Text = "Error";
            }
        }

        private void AdminButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "ms-settings:about",
                    UseShellExecute = true
                };
                Process.Start(psi);
                LogMessage("Opening Windows Settings for privilege check...");
            }
            catch
            {
                LogMessage("Could not open Windows Settings.");
            }
        }

        private void LogMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                LogsTextBox.AppendText($"[{timestamp}] {message}\n");
                LogsTextBox.ScrollToEnd();
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (isInjecting)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Injection is still active. Close anyway?\n\nNOTE: Closing will stop the injection.",
                    "Confirm Exit",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }

                StopInjection();
            }

            statusCheckTimer?.Stop();
        }
    }
}
