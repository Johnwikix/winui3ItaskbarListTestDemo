using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using testDemo.Taskbar;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace testDemo
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private TaskbarHelper _taskbarHelper;
        public MainWindow()
        {
            this.InitializeComponent();
            Title = "WinUI3 任务栏按钮演示";

            // 窗口加载完成后初始化任务栏按钮
            this.Activated += MainWindow_Activated;
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (_taskbarHelper == null)
            {
                InitializeTaskbarHelper();
            }
        }

        private void InitializeTaskbarHelper()
        {
            try
            {
                // 获取窗口句柄
                IntPtr hwnd = WindowNative.GetWindowHandle(this);

                // 创建任务栏助手并初始化
                _taskbarHelper = new TaskbarHelper(hwnd);
                _taskbarHelper.InitializeThumbButtons();

                // 注册按钮点击事件
                _taskbarHelper.ThumbButtonClicked += TaskbarHelper_ThumbButtonClicked;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化任务栏助手出错: {ex.Message}");
            }
        }

        private void TaskbarHelper_ThumbButtonClicked(object sender, ThumbButtonClickedEventArgs e)
        {
            // 在主窗口处理按钮点击事件
            System.Diagnostics.Debug.WriteLine($"主窗口收到任务栏按钮点击事件: 按钮 {e.ButtonId}");
        }

    }
}
