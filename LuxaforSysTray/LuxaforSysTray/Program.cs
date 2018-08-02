using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSCore.CoreAudioAPI;
using LuxaforSharp;
using Color = LuxaforSharp.Color;

namespace LuxaforSysTray
{
    public class LuxaforSysTrayApp : Form
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            Application.Run(new LuxaforSysTrayApp());
        }

        private NotifyIcon trayIcon;
        private static MMDevice audioDevice;
        private ContextMenu trayMenu;
        private static MenuItem redItem;
        private static MenuItem greenItem;
        private static bool isStopping;
        private static readonly Color green = new Color(0, 255, 0);
        private static readonly Color red = new Color(255, 0, 0);
        private static readonly Color yellow = new Color(255, 255, 0);

        private static Color defaultColor = red;

        public LuxaforSysTrayApp()
        {
            trayMenu = new ContextMenu();
            var defaultColor = new MenuItem("Default Color");

            redItem = new MenuItem("Red", ChangeDefaultToRed) {Checked = true};
            greenItem = new MenuItem("Green", ChangeDefaultToGreen);
            defaultColor.MenuItems.Add(redItem);
            defaultColor.MenuItems.Add(greenItem);

            trayMenu.MenuItems.Add(defaultColor);
            trayMenu.MenuItems.Add("Exit", OnExit);

            trayIcon = new NotifyIcon
            {
                Text = "LuxaforSysTrayApp",
                Icon = Properties.Resources.TrayIcon,
                ContextMenu = trayMenu,
                Visible = true
            };
            audioDevice = GetDefaultRenderDevice();
            ThreadPool.QueueUserWorkItem(ServiceWorkerThread);
        }

        private static void ServiceWorkerThread(object state)
        {
            while (!isStopping)
            {
                IDeviceList list = new DeviceList();
                list.Scan();
                if (list.Any())
                {
                    var device = list.First();
                    if (device != null && IsAudioPlaying(audioDevice))
                    {
                        device.SetColor(LedTarget.All, yellow);
                    } else
                    {
                        device?.SetColor(LedTarget.All, defaultColor);
                    }
                }
                Thread.Sleep(500);
            }
        }

        private static void setColor()
        {
            if (IsAudioPlaying(audioDevice)) return;
            IDeviceList list = new DeviceList();
            list.Scan();
            var device = list.First();
            device.SetColor(LedTarget.All, defaultColor);
        }

        private static void ChangeDefaultToRed(object sender, EventArgs e)
        {
            redItem.Checked = true;
            greenItem.Checked = false;
            defaultColor = red;
            setColor();
        }

        private static void ChangeDefaultToGreen(object sender, EventArgs e)
        {
            greenItem.Checked = true;
            redItem.Checked = false;
            defaultColor = green;
            setColor();
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false;
            ShowInTaskbar = false;
            base.OnLoad(e);
        }

        private static void OnExit(object sender, EventArgs e)
        {
            isStopping = true;
            Application.Exit();
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                trayIcon.Dispose();
            }
            base.Dispose(isDisposing);
        }

        public static MMDevice GetDefaultRenderDevice()
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            }
        }

        public static bool IsAudioPlaying(MMDevice device)
        {
            using (var meter = AudioMeterInformation.FromDevice(device))
            {
                return meter.PeakValue > 0;
            }
        }
        
    }
}
