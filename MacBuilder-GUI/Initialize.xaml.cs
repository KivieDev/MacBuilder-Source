using MacBuilder.Core.Global;
using MacBuilder.Core.Logger;
using MacBuilder_GUI.Core.Components.Assets.Computer;
using MacBuilder_GUI.Core.Components.Assets.Dialogs;
using MacBuilder_GUI.Core.Components.Extras.Github;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using static MacBuilder_GUI.Core.Components.Core.Classes.MacClasses;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MacBuilder_GUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Initialize : Page
    {
        public Initialize()
        {
            this.InitializeComponent();
        }
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Log($"Initialized MacBuilder Created/Developed by Kivie. v{Global.Version}");
                Logger.Log("Kivie @ 2024 all rights reserved.");
                Logger.Log("Pre-Loading UI...");
                await Task.Delay(2000); // wait for window to load to avoid exceptions

                string mhc = Path.Combine(AppContext.BaseDirectory, "MHC.exe");
                string mhcdump = Path.Combine(Global.GetDownloadPath(), "hardware_dump.json");
                if (!File.Exists(mhc))
                {
                    Logger.Log("MHC does not exist downloading latest github release...");
                    await DownloadLatestGithub.DownloadAsync(Global.MHCDownloadURL, AppContext.BaseDirectory);
                }

                if (!File.Exists(mhcdump))
                {
                    try
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = mhc,
                            Arguments = "--silent --dump",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = false
                        };

                        Process process = Process.Start(startInfo);

                        if (process != null)
                        {
                            string output = process.StandardOutput.ReadToEnd();
                            string errorOutput = process.StandardError.ReadToEnd();

                            Logger.Log("Process output: " + output);
                            Logger.Log("Process error output: " + errorOutput);

                            process.WaitForExit();
                        }
                        else
                        {
                            Logger.Log("Failed to start the process.", Logger.LogLevel.Error);
                            DisplayError();
                            return;

                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("An error occurred: " + ex.Message, Logger.LogLevel.Error);
                        DisplayError();
                        return;
                    }
                }
                string json = File.ReadAllText(mhcdump);

                HardwareInfo hardwareInfo = JsonConvert.DeserializeObject<HardwareInfo>(json);

                if (hardwareInfo == null)
                {
                    Logger.Log("Failed to deserialize hardware information.", Logger.LogLevel.Error);
                    DisplayError();
                    return;
                }
                Global.hardwareInfo = hardwareInfo;

                Logger.Log($"Detected CPU: {hardwareInfo.CPU.Name} supported: {hardwareInfo.CPU.IsCPUSupported}");
                Logger.Log($"Detected GPU: {hardwareInfo.GPU.Model} supported: {hardwareInfo.GPU.IsGPUSupported}");

                if (hardwareInfo.GPU.IsGPUSupported && hardwareInfo.CPU.IsCPUSupported && hardwareInfo.Supported.SupportedMacVersions != null && hardwareInfo.Supported.isUEFISupported)
                {
                    Logger.Log("Your PC is supported. Loading content...");
                    MainWindow.Navigate(typeof(MainMenu));
                }
                else
                {
                    if (!hardwareInfo.Supported.isUEFISupported)
                        Logger.Log("UEFI is not supported on this hardware.");
                    if (!hardwareInfo.CPU.IsCPUSupported)
                        Logger.Log("Your CPU is not supported.");
                    if (!hardwareInfo.GPU.IsGPUSupported)
                        Logger.Log("Your GPU is not supported.");
                    DisplayError();
                }
            } catch (Exception ex)
            {
                // will never log unless in log.txt
                Logger.Log("STARTUP EXCEPTION! " + ex, Logger.LogLevel.Error);
                DisplayError();
            }
        }
        public void DisplayError()
        {
            progbar.IsIndeterminate = false;
            ConsoleMessage.Visibility = Visibility.Visible;
        }
    }
}
