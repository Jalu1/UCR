using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HidWizards.UCR.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for FirstLaunchDialog.xaml
    /// </summary>
    public partial class FirstLaunchDialog : UserControl
    {
        public FirstLaunchDialog()
        {
            
            InitializeComponent();
            System.Management.SelectQuery query = new System.Management.SelectQuery("Win32_SystemDriver");
            query.Condition = "Name = 'keyboard'";
            System.Management.ManagementObjectSearcher searcher = new System.Management.ManagementObjectSearcher(query);
            var drivers = searcher.Get();
            if (drivers.Count > 0) InterceptionCheck.IsEnabled = false;
            /*query.Condition = "Name = 'vjoy";
            drivers = searcher.Get();
            if (drivers.Count > 0) VjoyCheck.IsEnabled = false;*/
            query.Condition = "Name = 'ViGEmBus";
            drivers = searcher.Get();
            if (drivers.Count > 0) ViGEmCheck.IsEnabled = false;
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(DoInstall);
        }

        private void DoInstall()
        {
            bool _InterceptionChecked, _VjoyChecked, _ViGEmChecked;
            _InterceptionChecked = Dispatcher.Invoke(() => { return InterceptionCheck.IsChecked ?? false; });
            //_VjoyChecked = Dispatcher.Invoke(() => { return VjoyCheck.IsChecked ?? false; });
            _ViGEmChecked = Dispatcher.Invoke(() => { return ViGEmCheck.IsChecked ?? false; });
            /*() =>
        {
            _InterceptionChecked = (InterceptionCheck.IsChecked ?? false);
            _VjoyChecked = (VjoyCheck.IsChecked ?? false);
            _ViGEmChecked = (ViGEmCheck.IsChecked ?? false);
        });*/

            if (_InterceptionChecked || /*_VjoyChecked ||*/ _ViGEmChecked)
            {
                System.IO.Directory.CreateDirectory("temp");
                using (System.Net.WebClient wc = new System.Net.WebClient())
                {
                    //wc.Credentials = CredentialCache.DefaultCredentials;
                    wc.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) UCR");
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    if (_InterceptionChecked)
                    {
                        Dispatcher.Invoke(()=>
                        {
                            ProgressDescriptor.Text = "Downloading Interception";
                            ProgressBar.Value = 0;
                            ProgressDescriptor.Visibility = Visibility.Visible;
                        });
                        wc.DownloadFile("https://github.com/oblitum/Interception/releases/latest/download/Interception.zip", ".\\temp\\Interception.zip");
                        
                        Dispatcher.Invoke(() =>
                        {
                            ProgressDescriptor.Text = "Extracting Interception";
                            ProgressBar.Value = 33;
                        });
                        System.IO.Compression.ZipFile.ExtractToDirectory(".\\temp\\Interception.zip", ".\\temp");

                        Dispatcher.Invoke(() =>
                        {
                            ProgressDescriptor.Text = "Installing Interception";
                            ProgressBar.Value = 66;
                        });

                        var process = new Process
                        {
                            StartInfo =
                            {
                                FileName = ".\\temp\\Interception\\command line installer\\install-interception.exe",
                                Arguments = "/install",
                                UseShellExecute = true,
                                CreateNoWindow = true
                            }
                        };
                        process.Start();
                        process.WaitForExit();
                        Dispatcher.Invoke(() => ProgressBar.Value = 100);
                    }
                    /*if (_VjoyChecked)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            ProgressDescriptor.Text = "Downloading vJoy";
                            ProgressBar.Value = 0;
                        });
                        wc.DownloadFile("https://downloads.sourceforge.net/project/vjoystick/Beta%202.x/2.1.9.1-160719/vJoySetup.exe?r=https%3A%2F%2Fsourceforge.net%2Fprojects%2Fvjoystick%2Ffiles%2Flatest%2Fdownload&ts=1594250611", ".\\temp\\vJoySetup.exe");
                        Dispatcher.Invoke(() =>
                        {
                            ProgressDescriptor.Text = "Installing vJoy";
                            ProgressBar.Value = 50;
                        });
                        var process = new Process
                        {
                            StartInfo =
                            {
                                FileName = ".\\temp\\vJoySetup.exe",
                                UseShellExecute = true
                            }
                        };
                        process.Start();
                        process.WaitForExit();
                        Dispatcher.Invoke(() => ProgressBar.Value = 100);
                    }*/
                    if (_ViGEmChecked)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            ProgressDescriptor.Text = "Downloading ViGEm";
                            ProgressBar.Value = 0;
                        });
                        wc.DownloadFile("https://github.com/ViGEm/ViGEmBus/releases/download/setup-v1.16.116/ViGEmBus_Setup_1.16.116.exe", ".\\temp\\ViGEmBus_Setup.exe");
                        Dispatcher.Invoke(() =>
                        {
                            ProgressDescriptor.Text = "Installing ViGEm";
                            ProgressBar.Value = 50;
                        });
                        var process = new Process
                        {
                            StartInfo =
                            {
                                FileName = ".\\temp\\ViGEmBus_Setup.exe",
                                UseShellExecute = true
                            }
                        };
                        process.Start();
                        process.WaitForExit();
                        Dispatcher.Invoke(() => ProgressBar.Value = 100);
                    }
                    System.IO.Directory.Delete(".\\temp", true);
                }
            }
        }
    }
}
