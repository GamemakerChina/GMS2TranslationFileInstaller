﻿using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Web;
//using System.Net.Http;
using System.IO.Compression;

namespace GMS2TranslationFileInstaller
{
    public partial class MainWindow : Window
    {
        private readonly WebClient webClient = new WebClient();
        

        private void DownloadUpdate()
        {
            webClient.DownloadFileAsync(new Uri("https://liaronce.coding.me/gms2translation/UpdatePackages/vers.zip"), @".\vers.zip");
            webClient.DownloadProgressChanged += WebC_ProgChanged;
            BtnUpdateControl.Content = "更新中";
            BtnUpdateControl.IsEnabled = false;
            ListUpdProcedure.Items.Add("更新中，请稍候……");
        }

        private void WebC_ProgChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ProgDownload.Value = e.ProgressPercentage;
        }

        private void WebC_DownloadCompleted(object sender,EventArgs e)
        {
            BtnUpdateControl.Content = "开始更新";
            BtnUpdateControl.IsEnabled = true;
            ListUpdProcedure.Items.Add("资源更新包下载完毕，正在使更新包生效……");
            using (FileStream fZip = new FileStream(@".\vers.zip", FileMode.Open))
            using (ZipArchive zipArch = new ZipArchive(fZip))
            {
                //ZipFile.ExtractToDirectory(@".\vers.zip", @".\vers\");
                if(Directory.Exists(@".\vers"))
                {
                    DirectoryDestroy(@".\vers");
                }
                zipArch.ExtractToDirectory(@".\vers");
                fZip.Close();
            }
            File.Delete(@".\vers.zip");
            ListUpdProcedure.Items.Add("更新包安装完毕，请重新启动本程序！");
        }
        
    }
}