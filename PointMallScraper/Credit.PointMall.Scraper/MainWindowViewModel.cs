using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Runtime.Serialization.Json;

using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Taskbar;

namespace Credit.PointMall.Scraper
{
    class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            this.Start = new DelegateCommand<IMainWindow>(this.StartCommand);
            this.Save = new DelegateCommand(this.SaveCommand);
        }

        private string userName = "";

        /// <summary>
        /// ユーザー名
        /// </summary>
        public string UserName
        {
            get
            {
                return this.userName;
            }
            set
            {
                this.userName = value;
                this.OnPropertyChanged("UserName");

                if (this.SelectedPointMall != null)
                {
                    Task.Run(() =>
                    {
                        this.SaveSettingUserName(value);
                        Properties.Settings.Default.Save();

                    });
                }
            }
        }


        /// <summary>
        /// 設定に保存されたユーザー名を取得する
        /// </summary>
        /// <returns></returns>
        private string LoadSettingUserName()
        {
            if (this.SelectedPointMall != null)
            {
                return SettingUtility.LoadSettingUserName(this.SelectedPointMall.Id);
            }

            return null;
        }

        /// <summary>
        /// 設定にユーザー名を保存する
        /// </summary>
        /// <returns></returns>
        private void SaveSettingUserName(string userName)
        {
            if (this.SelectedPointMall != null)
            {
                SettingUtility.SaveSettingUserName(this.SelectedPointMall.Id, userName);
            }
        }

        private int timeout = Properties.Settings.Default.Timeout;

        public int Timeout
        {
            get
            {
                return this.timeout;
            }
            set
            {
                this.timeout = value < 0 ? 0 : value;
                this.OnPropertyChanged("Timeout");

                Task.Run(() =>
                {
                    Properties.Settings.Default.Timeout = value;
                    Properties.Settings.Default.Save();
                });
            }
        }

        private string execButtonText = "取得開始";

        /// <summary>
        /// 実行ボタンのテキスト
        /// </summary>
        public string ExecButtonText
        {
            get
            {
                return this.execButtonText;
            }
            set
            {
                this.execButtonText = value;
                this.OnPropertyChanged("ExecButtonText");
            }
        }

        private bool isProgress = false;

        /// <summary>
        /// 処理中であるかを表す
        /// </summary>
        public bool IsProgress
        {
            get
            {
                return this.isProgress;
            }
            set
            {
                this.isProgress = value;

                this.OnPropertyChanged("IsProgress");
                this.OnPropertyChanged("IsIndeterminate");
                this.OnPropertyChanged("ProgressPercentage");

                if (value == true)
                {
                    if (this.ProgressMaximum == 0)
                    {
                        TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Indeterminate);
                    }
                    else
                    {
                        TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);
                    }
                }
                else
                {
                    TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
                }
            }
        }

        private int progressMaximum = 0;

        /// <summary>
        /// 処理の最大数
        /// </summary>
        public int ProgressMaximum
        {
            get
            {
                return this.progressMaximum;
            }
            set
            {
                this.progressMaximum = value;
                this.OnPropertyChanged("ProgressMaximum");
                this.OnPropertyChanged("ProgressPercentage");
                this.OnPropertyChanged("IsIndeterminate");
            }
        }

        private int progressValue = 0;

        /// <summary>
        /// 処理の進行状況を表す
        /// </summary>
        public int ProgressValue
        {
            get
            {
                return this.progressValue;
            }
            set
            {
                this.progressValue = value;
                this.OnPropertyChanged("ProgressValue");
                this.OnPropertyChanged("ProgressPercentage");

                if (this.ProgressMaximum > 0)
                {
                    TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);
                    TaskbarManager.Instance.SetProgressValue(value, this.ProgressMaximum);
                }
            }
        }

        /// <summary>
        /// 処理の進行状況をパーセンテージで表す
        /// </summary>
        public int ProgressPercentage
        {
            get {
                var max = this.ProgressMaximum;

                if (!this.IsProgress || max == 0)
                {
                    return 0;
                }

                return (int)((this.ProgressValue / (double)max) * 100);
            }
        }

        /// <summary>
        /// 処理の進行状況が数値的に表すことができるか
        /// </summary>
        public bool IsIndeterminate
        {
            get
            {
                return this.IsProgress &&　this.ProgressMaximum == 0;
            }
        }

        private bool isCompleted = false;

        /// <summary>
        /// 処理が完了したか
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                return this.isCompleted;
            }
            set
            {
                this.isCompleted = value;
                this.OnPropertyChanged("IsCompleted");
            }
        }

        private int linkLength = 0;

        /// <summary>
        /// 現在取得したリンク数
        /// </summary>
        public int LinkLength
        {
            get
            {
                return this.linkLength;
            }
            set
            {
                this.linkLength = value;
                this.OnPropertyChanged("LinkLength");
            }
        }

        private int windowWidth = Properties.Settings.Default.WindowWidth;

        /// <summary>
        /// ウィンドウ幅
        /// </summary>
        public int WindowWidth
        {
            get
            {
                return this.windowWidth;
            }
            set
            {
                this.windowWidth = value;
                this.OnPropertyChanged("WindowWidth");

                Task.Run(() =>
                {
                    Properties.Settings.Default.WindowWidth = value;
                    Properties.Settings.Default.Save();
                });
            }
        }

        private int windowHeight = Properties.Settings.Default.WindowHeight;

        public int WindowHeight
        {
            get
            {
                return this.windowHeight;
            }
            set
            {
                this.windowHeight = value;
                this.OnPropertyChanged("WindowHeight");

                Task.Run(() =>
                {
                    Properties.Settings.Default.WindowHeight = value;
                    Properties.Settings.Default.Save();
                });
            }
        }

        private Api.PointMall[] pointMalls = Api.PointMall.GetList();

        public Api.PointMall[] PointMalls
        {
            get
            {
                if (selectedPointMall == null && pointMalls.Length > 0)
                {
                    this.SelectedPointMall = pointMalls[0];
                }

                return this.pointMalls;
            }
        }

        public Api.PointMall selectedPointMall = null;

        public Api.PointMall SelectedPointMall
        {
            get
            {
                return this.selectedPointMall;
            }
            set
            {
                this.selectedPointMall = value;
                this.userName = this.LoadSettingUserName();
                this.OnPropertyChanged("SelectedPointMall");
                this.OnPropertyChanged("UserName");
            }
        }   
     
        public ICommand Start
        {
            get;
            private set;
        }

        public ICommand Save
        {
            get;
            private set;
        }

        private Api.PointMallApi api = null;
        private List<Api.Shop> ShopLinks;

        private void StartCommand(IMainWindow window)
        {
            var browser = window.WebBrowser;
            var password = window.PasswordBox.Password;
            var userName = this.UserName;

            if (string.IsNullOrEmpty(userName))
            {
                MessageBox.Show("ユーザー名を入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("パスワードを入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (this.IsProgress)
            {
                MessageBox.Show("現在取得中です。 ");
                return;
            }

            this.ExecButtonText = "取得中";
            this.ProgressMaximum = 0;
            this.ProgressValue = 0;
            this.IsProgress = true;

            if (this.api != null)
            {
                this.api.Dispose();
                this.api = null;
            }

            var api = this.SelectedPointMall.CreateApiInstance();

            api.Failed += this.Failure;
            api.LinkLengthChanged += this.LinkLengthChanged;
            api.ProgressMaxChanged += this.ProgressMaxChanged;
            api.ProgressValueChanged += this.ProgressValueChanged;
            api.Ended += this.Ended;

            api.Start(browser, this.UserName, password, this.Timeout * 1000);

            this.api = api;
        }

        private void SaveCommand()
        {
            var sfd = new SaveFileDialog();
            sfd.DefaultExt = "json";
            sfd.Filter = "JSON File (*.json)|*.json|すべてのファイル (*.*)|*.*";

            var result = sfd.ShowDialog(Application.Current.MainWindow);

            if (result.HasValue && result.Value)
            {
                var serializer = new DataContractJsonSerializer(typeof(List<Api.Shop>));

                try
                {
                    using (var fs = sfd.OpenFile())
                    {
                        serializer.WriteObject(fs, this.ShopLinks);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "保存に失敗しました。");
                }
            }
        }

        private void Failure(object sender, string e)
        {
            MessageBox.Show(e);
            this.IsProgress = false;
        }

        private void ProgressValueChanged(object sender, int e)
        {
            this.ProgressValue = e;
        }

        private void ProgressMaxChanged(object sender, int e)
        {
            this.ProgressMaximum = e;
        }

        private void LinkLengthChanged(object sender, int e)
        {
            this.LinkLength = e;
        }

        private void Ended(object sender, List<Api.Shop> shopLinks)
        {
            this.IsProgress = false;
            this.IsCompleted = true;
            this.ExecButtonText = "取得済";
            this.ShopLinks = shopLinks;
        }
    }
}
