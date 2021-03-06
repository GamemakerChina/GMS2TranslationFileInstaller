﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
using GMS2GiMiSi.Class;

namespace GMS2GiMiSi.View.GMS2ChildPage
{
    /// <summary>
    /// RuntimePage.xaml 的交互逻辑
    /// </summary>
    public partial class RuntimePage : Page
    {

        public void LoadCompleted(object sender, NavigationEventArgs e)
        {
            if (Global.RootFrame != null)
            {
                if (Global.RootFrame.NavigationService.Content.ToString().Contains("RuntimePage"))
                {
                    // 下载 Runtime Rss 文件
                    RuntimeRssDownloadTask();
                    // 加载已安装 Runtime
                    LoadInstalledRuntime();
                }
            }
        }

        public RuntimePage()
        {
            InitializeComponent();
            Global.RootFrame.NavigationService.LoadCompleted += LoadCompleted;
        }

        /// <summary>
        /// GameMaker Studio 2 配置文件夹
        /// </summary>
        private static readonly string GMS2ConfigFilePath = Environment.GetEnvironmentVariable("systemdrive") + @"\ProgramData\GameMakerStudio2";
        private readonly string GMS2runtimesPath = GMS2ConfigFilePath + @"\Cache\runtimes";
        private List<Item> zeusRuntimeItem = new List<Item>();

        #region 下载 runtime
        private void RuntimeRssDownloadTask()
        {
            Global.GMS2RuntimeRssDownloading = true;
            // 异步加载 runtime，不卡界面
            Log.WriteLog(Log.LogLevel.信息, "异步更新 Zeus-Runtime.rss");
            Task task = new Task(tb => ActionRuntimeRssDownload(), ComboBoxRuntimeVersion);
            task.Start();
        }

        /// <summary>
        /// RuntimeRssDownload Action
        /// </summary>
        private void ActionRuntimeRssDownload()
        {
            Action updateAction = RuntimeRssDownload;
            ComboBoxRuntimeVersion.Dispatcher.BeginInvoke(updateAction);
        }

        private async void RuntimeRssDownload()
        {
            try
            {
                if (!Directory.Exists(GMS2ConfigFilePath))
                {
                    Directory.CreateDirectory(GMS2ConfigFilePath);
                }
                if (!Directory.Exists(GMS2runtimesPath))
                {
                    Directory.CreateDirectory(GMS2runtimesPath);
                }
                if (File.Exists(@".\GiMiSiTemp\rss\Zeus-Runtime.rss"))
                {
                    File.Delete(@".\GiMiSiTemp\rss\Zeus-Runtime.rss");
                }
                await Network.DownloadRssFileAsync();
                // 打开文件
                FileStream fileStream = new FileStream(@".\GiMiSiTemp\rss\Zeus-Runtime.rss", FileMode.Open, FileAccess.Read, FileShare.Read);
                // 读取文件的 byte[]
                byte[] bytes = new byte[fileStream.Length];
                fileStream.Read(bytes, 0, bytes.Length);
                fileStream.Close();
                // 把 byte[] 转换成 Stream
                Stream stream = new MemoryStream(bytes);
                var rssString = new StreamReader(stream, Encoding.UTF8).ReadToEnd();
                stream.Close();
                Rss zeusRuntime = (Rss)XmlHelper.Deserialize(typeof(Rss), rssString);
                // item节
                zeusRuntimeItem = zeusRuntime.Channel.Item;
                // 逆序（使高版本在上）
                zeusRuntimeItem.Reverse();
                // 如果为LiarOnce国内镜像则截取前三
                if (Global.GMS2RuntimeRss.ToString() == "https://gms.magecorn.com/Zeus-Runtime.rss"|| Global.GMS2RuntimeRss.ToString() == "https://gms.jilcky.cn/Zeus-Runtime.rss")
                {
                    zeusRuntimeItem = zeusRuntimeItem.GetRange(0, zeusRuntimeItem.Count > 3 ? 3 : zeusRuntimeItem.Count);
                }
                ComboBoxRuntimeVersion.Items.Clear();
                foreach (var item in zeusRuntimeItem)
                {
                    ComboBoxRuntimeVersion.Items.Add(item.Title.Replace("Version ", ""));
                }
                ComboBoxRuntimeVersion.SelectedIndex = 0;
            }
            catch (Exception e)
            {
                ComboBoxRuntimeVersion.IsEnabled = false;
                ButtonRuntimeDownload.IsEnabled = false;
                Log.WriteLog(Log.LogLevel.警告, e.ToString());
                if (e.ToString().Contains("\r\n"))
                {
                    //GroupBoxRuntime.Header = "Rumtime 安装 - " + e.ToString().Substring(0, e.ToString().IndexOf("\r\n", StringComparison.Ordinal));
                }
                else
                {
                    //GroupBoxRuntime.Header = "Rumtime 安装 - " + e;
                }
            }
            Global.GMS2RuntimeRssDownloading = false;
        }
        #endregion

        private async void ButtonRuntimeDownload_Click(object sender, RoutedEventArgs e)
        {
            if (Global.GMS2ProcessIsRun())
            {
                MessageBox.Show("检测到 GameMaker Studio 2 进程，请关闭程序后进行 runtime 下载操作！", "警告");
                return;
            }
            // TODO 防止误操作？
            string runtimeVersion = ComboBoxRuntimeVersion.Text;
            int gms2Version = ComboBoxGms2Version.SelectedIndex;
            string runtimeVersionPath = GMS2runtimesPath + @"\runtime-" + runtimeVersion;
            if (Directory.Exists(runtimeVersionPath))
            {
                if (!Directory.Exists(runtimeVersionPath + @"\download") &&
                    Directory.GetFileSystemEntries(runtimeVersionPath).Length > 0)
                {
                    var result = MessageBox.Show(runtimeVersion + " 版本可能已经安装，是否重新安装？", "警告", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                    {
                        Directory.Delete(runtimeVersionPath, true);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    Directory.Delete(runtimeVersionPath, true);
                }
            }
            Directory.CreateDirectory(runtimeVersionPath + @"\download");
            List<string> runtimeFileUrlList = new List<string>();
            Item runtimeItem = null;
            foreach (var item in zeusRuntimeItem)
            {
                if (item.Title.Contains(runtimeVersion))
                {
                    runtimeItem = item;
                    break;
                }
            }
            if (runtimeItem != null)
            {
                runtimeFileUrlList.Add(runtimeItem.Enclosure.Url);
                foreach (var module in runtimeItem.Enclosure.Module)
                {
                    switch (gms2Version)
                    {
                        case 0:// Desktop
                            if (module.Name == "windows" || module.Name == "windowsYYC" || module.Name == "linux" ||
                                module.Name == "linuxYYC" || module.Name == "mac" || module.Name == "macYYC")
                            {
                                runtimeFileUrlList.Add(module.Url);
                            }
                            break;
                        case 1:// Mac
                            if (module.Name == "mac" || module.Name == "macYYC")
                            {
                                runtimeFileUrlList.Add(module.Url);
                            }
                            break;
                        case 2:// Windows
                            if (module.Name == "windows" || module.Name == "windowsYYC")
                            {
                                runtimeFileUrlList.Add(module.Url);
                            }
                            break;
                        case 3:// Fire
                            if (module.Name == "amazonfire")
                            {
                                runtimeFileUrlList.Add(module.Url);
                            }
                            break;
                        case 4:// Web
                            if (module.Name == "html5")
                            {
                                runtimeFileUrlList.Add(module.Url);
                            }
                            break;
                        case 5:// Mobile
                            if (module.Name == "amazonfire" || module.Name == "android" || module.Name == "ios")
                            {
                                runtimeFileUrlList.Add(module.Url);
                            }
                            break;
                        case 6:// UWP
                            if (module.Name == "windowsuap")
                            {
                                runtimeFileUrlList.Add(module.Url);
                            }
                            break;
                        case 7:// PlayStation 4
                            if (module.Name == "ps4")
                            {
                                runtimeFileUrlList.Add(module.Url);
                            }
                            break;
                        case 8:// Nintendo Switch
                            if (module.Name == "switch")
                            {
                                runtimeFileUrlList.Add(module.Url);
                            }
                            break;
                        case 9:// XboxOne
                            if (module.Name == "xboxone")
                            {
                                runtimeFileUrlList.Add(module.Url);
                            }
                            break;
                        default:// 旗舰版
                            runtimeFileUrlList.Add(module.Url);
                            break;
                    }
                }
            }
            try
            {
                foreach (var runtimeFile in runtimeFileUrlList)
                {
                    await Network.DownloadRuntimeFileAsync(runtimeFile, runtimeVersionPath + @"\download");
                }
                MessageBox.Show("下载 runtime 成功，请在 Gamemaker Stuido 2 偏好设置 - 运行库管理中选择相应版本安装");
            }
            catch (Exception exception)
            {
                MessageBox.Show("下载 runtime 文件失败，" + exception.ToString().Substring(0, exception.ToString().IndexOf("\r\n", StringComparison.Ordinal)));
            }
        }

        #region 加载已安装runtime

        private void LoadInstalledRuntime()
        {
            DataGridInstalledRuntime.Columns.Add(new DataGridTextColumn
            {
                Width = 80,
                Header = "版本号",
                Binding = new Binding($"[{0}]")
            });
            DataGridInstalledRuntime.Columns.Add(new DataGridTextColumn
            {
                Width = 420,
                Header = "位置",
                Binding = new Binding($"[{1}]")
            });
            RefreshInstalledRuntime(null, null);
        }

        private void RefreshInstalledRuntime(object sender, RoutedEventArgs e)
        {
            Log.WriteLog(Log.LogLevel.信息, "刷新已安装或已下载 Runtime");
            DirectoryInfo Dir = new DirectoryInfo(GMS2runtimesPath);
            DirectoryInfo[] DirSub = Dir.GetDirectories();
            List<string[]> installedRuntimeList = new List<string[]>();
            for (int i = DirSub.Length - 1; i >= 0; i--)
            {
                installedRuntimeList.Add(new[] { DirSub[i].Name.Replace("runtime-", ""), GMS2runtimesPath + "\\" + DirSub[i].Name });
            }
            DataGridInstalledRuntime.ItemsSource = installedRuntimeList;
            foreach (var column in DataGridInstalledRuntime.Columns)
            {
                column.IsReadOnly = true;
            }
        }

        private void DeleteInstalledRuntime(object sender, RoutedEventArgs e)
        {
            if (Global.GMS2ProcessIsRun())
            {
                MessageBox.Show("检测到 GameMaker Studio 2 进程，请关闭程序后进行 runtime 删除操作！", "警告");
                return;
            }
            Log.WriteLog(Log.LogLevel.信息, "删除选择的 Runtime");
            var DialogResult = MessageBox.Show("确定要删除 " + ((string[])DataGridInstalledRuntime.SelectedCells[1].Item)[0] + " 版本runtime吗？",
                "警告", MessageBoxButton.OKCancel);
            var runtimePath = ((string[])DataGridInstalledRuntime.SelectedCells[1].Item)[1];
            if (DialogResult == MessageBoxResult.OK)
            {
                if (Directory.Exists(runtimePath))
                {
                    Directory.Delete(runtimePath, true);
                }
                RefreshInstalledRuntime(null, null);
            }
        }


        /*/// <summary>
        /// um文档反序列化 - 获取用户id以获取用户配置文件夹名
        /// </summary>
        private string umDeserialize()
        {
            if (!Directory.Exists(GMS2ConfigFilePath))
            {
                throw new Exception("GameMaker Studio 2 配置文件夹不存在");
            }
            var filePath = GMS2ConfigFilePath + @"\um.json";
            if (!File.Exists(filePath))
            {
                throw new Exception("GameMaker Studio 2 配置文件不存在");
            }
            // 打开文件 
            FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            // 读取文件的 byte[] 
            byte[] bytes = new byte[fileStream.Length];
            fileStream.Read(bytes, 0, bytes.Length);
            fileStream.Close();
            // 把 byte[] 转换成 Stream 
            Stream stream = new MemoryStream(bytes);
            var umStr = new StreamReader(stream, Encoding.UTF8).ReadToEnd();
            var um = JsonConvert.DeserializeObject<umRootObject>(umStr);
            // 获取userID以确认文件夹名
            return um.userID;
        }*/
        #endregion
    }
}
