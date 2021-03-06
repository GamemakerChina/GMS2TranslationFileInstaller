﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using GMS2GiMiSi.Class;

namespace GMS2GiMiSi
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public class App : Application
    {
        /// <summary>
        /// 用于 App.xaml 的交互逻辑
        /// </summary>
        private class AppRun : Application
        {
            public AppRun()
            {
                // TODO
                Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                Startup += App_Startup;
            }

            private static void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
            {
                UnhandledExceptionFileLog(e.Exception.ToString());
                e.Handled = true;//使用这一行代码告诉运行时，该异常被处理了，不再作为UnhandledException抛出了。
            }

            private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
            {
                UnhandledExceptionFileLog(e.ExceptionObject.ToString());
            }

            private static void UnhandledExceptionFileLog(string log)
            {
                try
                {
                    var logFilePath = Environment.CurrentDirectory + @"\GiMiSiTemp\Log\"; ; //设置文件夹位置
                    if (Directory.Exists(logFilePath) == false) //若文件夹不存在
                    {
                        Directory.CreateDirectory(logFilePath);
                    }
                    var logFilename = Global.logfileName; //设置文件名
                    var logPath = logFilePath + logFilename;
                    if (!File.Exists(logPath))
                    {
                        var fs = File.Create(logPath);
                        fs.Close();
                    }
                    var fileStream = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                    var streamWriter = new StreamWriter(fileStream);
                    streamWriter.WriteLine(DateTime.Now.ToString("[HH:mm.ss]") + "[错误]" + "错误信息：\r\n" + log);
                    streamWriter.Flush();
                    streamWriter.Close();
                    fileStream.Close();
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            private static void App_Startup(object sender, StartupEventArgs e)
            {
                //添加资源字典
                var resourceDictionaries = new Collection<ResourceDictionary>
                {
                    new ResourceDictionary
                    {
                        Source = new Uri(
                            "pack://application:,,,/GMS2GiMiSi;component/Dictionary/ButtonDictionary.xaml",
                            UriKind.Absolute)
                    },
                    new ResourceDictionary
                    {
                        Source = new Uri(
                            "pack://application:,,,/GMS2GiMiSi;component/Dictionary/CheckBoxDictionary.xaml",
                            UriKind.Absolute)
                    },
                    new ResourceDictionary
                    {
                        Source = new Uri(
                            "pack://application:,,,/GMS2GiMiSi;component/Dictionary/ComboBoxDictionary.xaml",
                            UriKind.Absolute)
                    },
                    new ResourceDictionary
                    {
                        Source = new Uri(
                            "pack://application:,,,/GMS2GiMiSi;component/Dictionary/DataGridDictionary.xaml",
                            UriKind.Absolute)
                    },
                    new ResourceDictionary
                    {
                        Source = new Uri(
                            "pack://application:,,,/GMS2GiMiSi;component/Dictionary/ListBoxItemDictionary.xaml",
                            UriKind.Absolute)
                    },
                    new ResourceDictionary
                    {
                        Source = new Uri(
                            "pack://application:,,,/GMS2GiMiSi;component/Dictionary/ProgressBarDictionary.xaml",
                            UriKind.Absolute)
                    },
                    new ResourceDictionary
                    {
                        Source = new Uri(
                            "pack://application:,,,/GMS2GiMiSi;component/Dictionary/RadioButtonDictionary.xaml",
                            UriKind.Absolute)
                    },
                    new ResourceDictionary
                    {
                        Source = new Uri(
                            "pack://application:,,,/GMS2GiMiSi;component/Dictionary/TextBoxDictionary.xaml",
                            UriKind.Absolute)
                    }
                };
                foreach (var resourceDictionary in resourceDictionaries)
                {
                    Current.Resources.MergedDictionaries.Add(resourceDictionary);
                }
                
                var mainWindowShow = new MainWindow();
                mainWindowShow.InitializeComponent();
                mainWindowShow.Show();
                mainWindowShow.Activate();
            }
        }

        /// <summary>
        /// 在对程序集的解析失败时发生(嵌入DLL)
        /// </summary>
        public static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var dllName = new AssemblyName(args.Name).Name;
            try
            {
                //if (!dllName.Contains("PresentationFramework"))
                //{
                var resourceName = Assembly.GetExecutingAssembly().GetName().Name + ".DynamicLinkLibrary." + dllName + ".dll";
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    // ReSharper disable once PossibleNullReferenceException
                    var assemblyData = new byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    return Assembly.Load(assemblyData);
                }
                //}
            }
            catch (Exception e)
            {
                //if (dllName != "GMS2GiMiSi.resources")
                //MessageBox.Show("DLL未能正确加载，详细信息：\r\n" + e.Message + "\r\n" + dllName);
                return null;
            }
        }

        /// <summary>
        /// Entry point class to handle single instance of the application
        /// </summary>
        public static Semaphore SingleInstanceWatcher { get; private set; }

        private static bool _createdNew;

        public static class EntryPoint
        {
            [STAThread]
            public static void Main(string[] args)
            {
                // 工程名("GMS2GiMiSi")
                var projectName = Assembly.GetExecutingAssembly().GetName().Name;
                // 单实例监视
                SingleInstanceWatcher = new Semaphore(0, 1, projectName, out _createdNew);
                if (_createdNew)
                {
                    //加载DLL
                    AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                    //启动
                    var app = new AppRun();
                    app.Run();
                }
                else
                {
                    MessageBox.Show("请不要重复运行(ノ｀Д)ノ");
                    Environment.Exit(-2);
                }
            }
        }
    }
}
