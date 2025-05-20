﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace testDemo.Taskbar
{
    public class TaskbarHelper
    {
        // 消息常量
        private const int WM_COMMAND = 0x0111;
        private const int THBN_CLICKED = 0x1800;

        // SetWindowSubclass 函数
        [DllImport("Comctl32.dll", SetLastError = true)]
        private static extern bool SetWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, IntPtr uIdSubclass, IntPtr dwRefData);

        // DefSubclassProc 函数
        [DllImport("Comctl32.dll", SetLastError = true)]
        private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [ComImport]
        [Guid("56FDF344-FD6D-11d0-958A-006097C9A090")]
        [ClassInterface(ClassInterfaceType.None)]
        internal class TaskbarList { }

        // LoadIcon 函数
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        // 系统图标资源ID
        private static IntPtr IDI_APPLICATION = (IntPtr)32512;

        // 子类过程委托
        private delegate IntPtr SubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData);
        private readonly IntPtr _hwnd;
        private ITaskbarList3 _taskbarList;
        private ThumbButton[] _buttons;
        private SubclassProc _wndProc; // 保持引用，防止被垃圾回收

        private const int LR_LOADFROMFILE = 0x0010;
        private const int LR_DEFAULTSIZE = 0x0040;
        private const int IMAGE_ICON = 1;

        // 加载图片并创建图标
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadImage(
            IntPtr hinst,
            string lpszName,
            uint uType,
            int cxDesired,
            int cyDesired,
            uint fuLoad);

        // 销毁图标
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private IntPtr[] _iconHandles = new IntPtr[3]; // 存储图标句柄以便后续释放

        private bool playStatus = false; // 播放状态

        // 缩略图按钮点击事件
        public event EventHandler<ThumbButtonClickedEventArgs> ThumbButtonClicked;

        public TaskbarHelper(IntPtr hwnd)
        {
            _hwnd = hwnd;
        }

        private IntPtr CreateIconFromImage(string imagePath, int size = 16)
        {
            if (!System.IO.File.Exists(imagePath))
            {
                System.Diagnostics.Debug.WriteLine($"图片文件不存在: {imagePath}");
                return LoadIcon(IntPtr.Zero, IDI_APPLICATION); // 使用默认图标
            }

            try
            {
                IntPtr hIcon = LoadImage(
                    IntPtr.Zero,
                    imagePath,
                    IMAGE_ICON,
                    size,
                    size,
                    LR_LOADFROMFILE | LR_DEFAULTSIZE);

                if (hIcon == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    System.Diagnostics.Debug.WriteLine($"加载图片失败，错误码: {error}");
                    return LoadIcon(IntPtr.Zero, IDI_APPLICATION); // 使用默认图标
                }

                return hIcon;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建图标时出错: {ex.Message}");
                return LoadIcon(IntPtr.Zero, IDI_APPLICATION); // 使用默认图标
            }
        }

        // 释放图标资源
        private void ReleaseIcons()
        {
            foreach (IntPtr handle in _iconHandles)
            {
                if (handle != IntPtr.Zero)
                {
                    DestroyIcon(handle);
                }
            }
        }

        public void InitializeThumbButtons()
        {
            try
            {
                // 创建TaskbarList实例
                _taskbarList = (ITaskbarList3)new TaskbarList();
                _taskbarList.HrInit();

                // 创建3个任务栏按钮
                _buttons = new ThumbButton[3];

                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                _iconHandles[0] = CreateIconFromImage(System.IO.Path.Combine(appDir, "Assets\\last.ico"), 32);
                _iconHandles[1] = CreateIconFromImage(System.IO.Path.Combine(appDir, "Assets\\play.ico"), 32);
                _iconHandles[2] = CreateIconFromImage(System.IO.Path.Combine(appDir, "Assets\\next.ico"), 32);

                // 按钮1 
                _buttons[0] = new ThumbButton
                {
                    dwMask = ThumbButtonMask.Icon | ThumbButtonMask.Tooltip | ThumbButtonMask.THB_FLAGS,
                    iId = 0,
                    hIcon = _iconHandles[0],
                    szTip = "应用",
                    dwFlags = ThumbButtonFlags.Enabled
                };

                // 按钮2 
                _buttons[1] = new ThumbButton
                {
                    dwMask = ThumbButtonMask.Icon | ThumbButtonMask.Tooltip | ThumbButtonMask.THB_FLAGS,
                    iId = 1,
                    hIcon = _iconHandles[1],
                    szTip = "信息",
                    dwFlags = ThumbButtonFlags.Enabled
                };

                // 按钮3 
                _buttons[2] = new ThumbButton
                {
                    dwMask = ThumbButtonMask.Icon | ThumbButtonMask.Tooltip | ThumbButtonMask.THB_FLAGS,
                    iId = 2,
                    hIcon = _iconHandles[2],
                    szTip = "警告",
                    dwFlags = ThumbButtonFlags.Enabled
                };

                // 添加任务栏按钮
                _taskbarList.ThumbBarAddButtons(_hwnd, (uint)_buttons.Length, _buttons);

                // 设置窗口子类过程来处理按钮点击事件
                _wndProc = new SubclassProc(WindowSubclassProc);
               SetWindowSubclass(_hwnd, _wndProc, (IntPtr)1, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化任务栏按钮出错: {ex.Message}");
                throw;
            }
        }

        // 窗口子类过程，用于处理任务栏按钮点击消息
        private IntPtr WindowSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData)
        {
            // 处理按钮点击事件
            if (uMsg == WM_COMMAND)
            {
                int notifyCode = ((int)wParam >> 16) & 0xFFFF;
                int buttonId = ((int)wParam) & 0xFFFF;

                if (notifyCode == THBN_CLICKED)
                {
                    HandleThumbButtonClick(buttonId);
                    return IntPtr.Zero;
                }
            }

            // 调用默认的窗口过程
            return DefSubclassProc(hWnd, uMsg, wParam, lParam);
        }

        // TaskbarHelper.cs 中添加更新按钮图标的方法
        public void UpdateButtonIcon(int buttonId, string iconPath, int size = 32)
        {
            try
            {
                // 创建新图标
                IntPtr newIcon = CreateIconFromImage(iconPath, size);

                // 保存之前的图标句柄以便释放
                IntPtr oldIcon = _iconHandles[buttonId];

                // 更新图标句柄数组
                _iconHandles[buttonId] = newIcon;

                // 更新按钮结构
                _buttons[buttonId].hIcon = newIcon;

                // 更新任务栏按钮
                _taskbarList.ThumbBarUpdateButtons(_hwnd, (uint)_buttons.Length, _buttons);

                // 释放旧图标资源
                if (oldIcon != IntPtr.Zero)
                {
                    DestroyIcon(oldIcon);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新按钮图标出错: {ex.Message}");
            }
        }

        // 处理任务栏按钮点击事件
        private void HandleThumbButtonClick(int buttonId)
        {
            System.Diagnostics.Debug.WriteLine($"任务栏按钮点击：按钮ID {buttonId}");

            if (buttonId == 1) // 按钮1的ID是0
            {         
                playStatus = !playStatus; // 切换播放状态
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string newIconPath = System.IO.Path.Combine(appDir, "Assets\\stop.ico");
                if (playStatus)
                {
                    newIconPath = System.IO.Path.Combine(appDir, "Assets\\stop.ico");
                }
                else {
                    newIconPath = System.IO.Path.Combine(appDir, "Assets\\play.ico");
                }                
                UpdateButtonIcon(1, newIconPath); // 按钮2的ID是1
                System.Diagnostics.Debug.WriteLine("已将按钮2的图标更改为Button4.ico");
            }

            // 触发事件
            ThumbButtonClicked?.Invoke(this, new ThumbButtonClickedEventArgs(buttonId));
        }

        public void Dispose()
        {
            // 释放图标资源
            ReleaseIcons();
        }
    }
}
