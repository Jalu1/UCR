using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using HidWizards.UCR.Core;
using HidWizards.UCR.Core.Models.Settings;
using HidWizards.UCR.Core.Utilities;
using HidWizards.UCR.Utilities;
using HidWizards.UCR.Views;
using Application = System.Windows.Application;

namespace HidWizards.UCR
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, IDisposable
    {
        private Context context;
        private HidGuardianClient _hidGuardianClient;
        private NamedPipeServerStream pipeServer;
        private SingleGlobalInstance mutex;
        private bool StartMinimized;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppDomain.CurrentDomain.UnhandledException += AppDomain_CurrentDomain_UnhandledException;

            mutex = new SingleGlobalInstance(); 
            if (mutex.HasHandle && GetProcesses().Length <= 1)
            {
                Logger.Info("Launching UCR");
                _hidGuardianClient = new HidGuardianClient();
                _hidGuardianClient.WhitelistProcess();

                StartPipeServer();
                InitializeUcr();
                CheckForBlockedDll();

                var mw = new MainWindow(context);
                context.MinimizedToTrayEvent += Context_MinimizedToTrayEvent;
                context.ParseCommandLineArguments(e.Args);
                if (!StartMinimized && SettingsCollection.LaunchMinimized) context.MinimizeToTray(true);
                if (!StartMinimized) mw.Show();
            }
            else
            {
                SendArgs(string.Join(";", e.Args));
                Current.Shutdown();
            }
        }

        private void Context_MinimizedToTrayEvent(bool x)
        {
            StartMinimized = true;
        }

        private void InitializeUcr()
        {
            new ResourceLoader().Load();
            context = Context.Load();
        }

        private void CheckForBlockedDll()
        {
            if (context.GetPlugins().Count != 0) return;

            var result = MessageBox.Show("UCR has detected blocked files which are required, do you want to unblock blocked UCR files?", "Unblock files?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            var process = new Process
            {
                StartInfo =
                {
                    FileName = "UCR_unblocker.exe",
                    UseShellExecute = true,
                    Arguments = $"\"{Environment.CurrentDirectory}\"",
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit(1000 * 60 * 5);

            var exitCode = process.ExitCode;
            if (exitCode != 0)
            {
                MessageBox.Show("UCR failed to unblock the required files", "Failed to unblock", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }

            InitializeUcr();
        }

        private static Process[] GetProcesses()
        {
            return Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
        }

        private void SendArgs(string args)
        {
            Logger.Info($"UCR is already running, sending args: {{{args}}}");
            // Find the window with the name of the main form
            var processes = GetProcesses();
            processes = processes.Where(p => p.Id != Process.GetCurrentProcess().Id).ToArray();
            if (processes.Length == 0) return;

            using(NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "ucrargumentpipe",PipeDirection.Out))
            {
                pipeClient.Connect();
                pipeClient.Write(Encoding.Default.GetBytes(args), 0, Encoding.Default.GetByteCount(args));
            }
        }

        private void StartPipeServer()
        {
            pipeServer = new NamedPipeServerStream("ucrargumentpipe", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            pipeServer.BeginWaitForConnection(ArgumentsRecieved, null);
        }

        private void ArgumentsRecieved(IAsyncResult result)
        {
            pipeServer.EndWaitForConnection(result);
            byte[] argumentsBuffer = new byte[4096];
            if (pipeServer.Read(argumentsBuffer, 0, 4096) == 0) {
                Application.Current.Dispatcher.Invoke(() =>
                    context.MinimizeToTray(false)
                );
            }
            else {
                Application.Current.Dispatcher.Invoke(() =>
                    context.ParseCommandLineArguments(Encoding.Default.GetString(argumentsBuffer).TrimEnd('\0').Split(';'))
                );
            }
            pipeServer.Disconnect();
            pipeServer.BeginWaitForConnection(ArgumentsRecieved, null);
        }

        public void Dispose()
        {
            mutex.Dispose();
            context?.Dispose();
            _hidGuardianClient?.Dispose();
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            context?.DevicesManager.UpdateDeviceCache();

            Dispose();
        }

        private static void AppDomain_CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = (Exception) e.ExceptionObject;
            Logger.Fatal(exception.Message, exception);
        }
    }
}
