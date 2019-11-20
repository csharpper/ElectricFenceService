using Common.Log;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace ServerHost
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
#if !DEBUG
            //if (e.Args.Length == 0 || !e.Args[0].Equals("-watch"))
            //{
            //    if (!startAppLoader())
            //        System.Windows.Forms.MessageBox.Show("无法运行AppLoader.exe！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            //    Process.GetCurrentProcess().Kill();
            //    return;
            //}
            //else

            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
#endif
            Logger.Default.Trace("----------------------------------- start ----------------------------------");
            Logger.Default.Trace("程序启动。");
            base.OnStartup(e);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.Exception.Message);
            Logger.Default.Error("线程错误", e.Exception);
            //e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Default.Error("系统错误" + e.ExceptionObject.ToString());
            Logger.Default.Trace("------------------------------------exception exit -----------------------------------\r\n\r\n\r\n");
            Process.GetCurrentProcess().Kill();
        }
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            Logger.Default.Trace("------------------------------------ exit -----------------------------------\r\n\r\n\r\n");
        }

    }
}
