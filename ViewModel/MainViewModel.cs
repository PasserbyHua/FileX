using ConnHelper;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WpfApp1.Model;
using WpfApp1.Views;

namespace WpfApp1.ViewModel
{
    class MainViewModel : ViewModelBase
    {
        //默认服务器名
        public static string defauleHostName;
        //绑定服务器工具类
        ConnBindServer connbds;
        //服务器工具类
        ConnServer conn;
        //运行状态
        private static int status;
        //显示本地地址文本框
        private string ipname;
        public string Ipname { get => ipname; set => ipname = value; }
        //绑定服务器IP文本框
        string bindIPTextBox;
        public string BindIPTextBox
        {
            get => bindIPTextBox; set
            {
                bindIPTextBox = value;
                RaisePropertyChanged();
            }
        }
        //聊天对话内容
        StringBuilder chatInfo;
        public string ChatInfo
        {
            get => chatInfo.ToString();
            set
            {
                chatInfo.Append(value);
                RaisePropertyChanged();
            }
        }
        //用户对话输入框
        private string myChatText;
        public string MyChatText { get => myChatText; set => myChatText = value; }
        //显示选中用户数据
        private UserInfo selectUser;
        public UserInfo SelectUser
        {
            get => selectUser;
            set
            {
                selectUser = value;
                RaisePropertyChanged();
            }
        }
        //选择传输的文件
        private string selectFileName;
        public string SelectFileName
        {
            get => selectFileName.Split('\\').Last();
            set
            {
                selectFileName = value;
                RaisePropertyChanged();
            }
        }

        //显示用户连接集合
        private ObservableCollection<UserInfo> userInfos;
        public ObservableCollection<UserInfo> UserInfos
        {
            get => userInfos;
            set
            {
                userInfos = value;
                RaisePropertyChanged();//刷新数据源
            }
        }

        //文件传输数据集合
        private ObservableCollection<FileTransferInfo> userLoadModelList;
        public ObservableCollection<FileTransferInfo> UserLoadModelList
        {
            get => userLoadModelList;
            set
            {
                userLoadModelList = value;
                RaisePropertyChanged();//刷新数据源
            }
        }
        //文件传输后台列表
        private List<FileTransferInfo> fileTransferInfos;

        #region 绑定按钮
        public RelayCommand SelectFileCommand { get; set; }
        public RelayCommand SendFileCommand { get; set; }
        public RelayCommand BindingIPCommand { get; set; }
        public RelayCommand StartServerCommand { get; set; }
        public RelayCommand SendChatCommand { get; set; }
        public RelayCommand ReceiveFileCommand { get; set; }
        #endregion

        public MainViewModel()//窗口构造
        {
            UserInfos = new ObservableCollection<UserInfo>();
            UserLoadModelList = new ObservableCollection<FileTransferInfo>();
            fileTransferInfos = new List<FileTransferInfo>();
            SelectFileCommand = new RelayCommand(SelectFile);
            SendFileCommand = new RelayCommand(SendFile);
            BindingIPCommand = new RelayCommand(BindingIP);
            StartServerCommand = new RelayCommand(StartServer);
            SendChatCommand = new RelayCommand(SendChat);
            ReceiveFileCommand = new RelayCommand(ReceiveFile);
            Ipname = "本机IP:" + ConnServer.GetIPS();
            defauleHostName = "WindowHost";
            BindIPTextBox = "绑定IP";
            MyChatText = "";
            chatInfo = new StringBuilder("");
            selectFileName = string.Empty;
            selectUser = null;
            status = 0;
        }

        public void SendChat()//发送对话
        {
            if (MyChatText == null)
            {
                Console.WriteLine("窗口未加载");
                return;
            }
            if (MyChatText.Trim().Length != 0)
            {
                ChatMessage chatMessage = new ChatMessage();
                chatMessage.ChatInfo = MyChatText.Trim();
                switch (status)
                {
                    case 1://本地服务器
                        chatMessage.Name = ConnServer.name;
                        ThreadHelper.NewParameterizedThread(conn.SendChatForAll, chatMessage);
                        AddChatMsg("[你]" + DateTime.Now.ToString("[yyyy-MM-dd H:mm:ss]") + MyChatText.Trim() + "\n");
                        break;
                    case 2://绑定服务器
                        chatMessage.Name = ConnBindServer.name;
                        ThreadHelper.NewParameterizedThread(connbds.SendChatForServer, chatMessage);
                        AddChatMsg("[你]" + DateTime.Now.ToString("[yyyy-MM-dd H:mm:ss]") + MyChatText.Trim() + "\n");
                        break;
                    default:
                        Console.WriteLine("未绑定服务器或启动本地服务器");
                        break;
                }
            }
        }

        public void SelectFile()//选择文件
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            var res = openFileDialog.ShowDialog();
            if (res == DialogResult.OK)
            {
                SelectFileName = openFileDialog.FileName;
            }
        }

        public void ReceiveFile()//接收文件位置
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            var res = folderBrowserDialog.ShowDialog();
            if (res == DialogResult.OK)
            {
                GetFile.ReceiveFilePath = folderBrowserDialog.SelectedPath;
            }
        }

        public void SendFile()//发送文件
        {
            if (selectUser != null)
            {
                if (selectFileName != string.Empty)
                {
                    switch (status)
                    {
                        case 1:
                            conn.SendFile(selectUser, selectFileName);
                            break;
                        case 2:
                            connbds.SendFile(selectFileName);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("未选择文件");
                }
            }
            else
            {
                Console.WriteLine("未选择用户");
            }
        }

        public void BindingIP()//绑定到服务器
        {
            if (conn != null)
            {
                Console.WriteLine("已开启本地服务器");
                return;
            }
            if (connbds == null)
            {
                BindServer bindServer = new BindServer();
                var result = bindServer.ShowDialog();
                if (result.Value)
                {
                    if (connbds != null)
                    {
                        connbds.Close();
                        connbds = null;
                    }
                    connbds = new ConnBindServer(bindServer.bindIP);
                    RegisteredRefresh();
                    BindIPTextBox = bindServer.bindIP;
                    status = 2;
                    ThreadHelper.NewThread(connbds.Connect);
                    Console.WriteLine("连接服务器线程启动");
                }
            }
            else
            {
                Console.WriteLine("已经绑定服务器");
                if (!ConnBindServer.BindSeverEnable)
                {
                    Console.WriteLine("绑定失败,请重新尝试");
                    BindIPTextBox = string.Empty;
                    Close();
                }
            }
        }

        public void StartServer()//启动本机服务器
        {
            if (connbds != null)
            {
                System.Console.WriteLine("已绑定服务器");
                return;
            }
            if (conn == null)
            {
                conn = new ConnServer(defauleHostName);
                RegisteredRefresh();
                status = 1;
                ThreadHelper.NewThread(conn.Start);
                System.Console.WriteLine("服务器线程启动");
            }
            else
            {
                if (!ConnServer.ServerEnable)
                {
                    Console.WriteLine("服务器出错,请重新尝试");
                    Close();
                    conn = null;
                }
                System.Console.WriteLine("服务器已启动");
            }
        }

        public void Refresh(List<UserInfo> users)//刷新数据源
        {
            UserInfos = new ObservableCollection<UserInfo>(users);
        }

        public void AddChatMsg(string newMsg)//返回对话信息
        {
            ChatInfo = newMsg;
        }

        public void AddFileInfo(FileTransferInfo fileTransferInfo)//添加文件列表信息
        {
            //如果文件信息不存在
            if (fileTransferInfos.Count(q => q.SendFileID == fileTransferInfo.SendFileID) == 0)
            {
                //添加新文件信息
                fileTransferInfo.SendFileID = fileTransferInfos.Count + 1;
                fileTransferInfos.Add(fileTransferInfo);
            }
            else
            {
                //刷新状态
                FileTransferInfo file = fileTransferInfos.Find(q => q.SendFileID == fileTransferInfo.SendFileID);
                file.Status = fileTransferInfo.Status;
                file.TimeOut = fileTransferInfo.TimeOut;
            }
            UserLoadModelList = new ObservableCollection<FileTransferInfo>(fileTransferInfos);
        }

        private void RegisteredRefresh()//注册刷新事件
        {
            RefreshWindowHelper.RefreshUsersEvent += Refresh;
            RefreshWindowHelper.RefreshChatsEvent += AddChatMsg;
            RefreshWindowHelper.AddFileEvent += AddFileInfo;
        }

        public void Close()
        {
            switch (status)
            {
                case 1:
                    conn.Close();
                    conn = null;
                    break;
                case 2:
                    connbds.Close();
                    connbds = null;
                    break;
                default:
                    break;
            }
        }
    }
}
