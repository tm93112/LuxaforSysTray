using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Timers;
using System.Runtime.InteropServices;
using CSCore.CoreAudioAPI;
using LuxaforSharp;

namespace WindowsLuxaforMusicListener
{
    public partial class LuxaforMusicListener : ServiceBase
    {
        private bool isStopping;
        private EventLog eventLog;
        private MMDevice audioDevice;

        public LuxaforMusicListener(string[] args)
        {
            InitializeComponent();
            eventLog = new EventLog();
            if (!EventLog.SourceExists("LuxaforMusicListenerSource"))
            {
                EventLog.CreateEventSource(
                    "LuxaforMusicListenerSource", "LuxaforMusicListenerLog");
            }
            eventLog.Source = "LuxaforMusicListenerSource";
            eventLog.Log = "LuxaforMusicListenerLog";
            this.isStopping = false;
        }

        protected override void OnStart(string[] args)
        {
            eventLog.WriteEntry("LuxaforMusicListener In OnStart");
            audioDevice = GetDefaultRenderDevice();
            ThreadPool.QueueUserWorkItem(new WaitCallback(ServiceWorkerThread));
        }

        private void ServiceWorkerThread(object state)
        {
            while (!this.isStopping)
            {
                IDeviceList list = new DeviceList();
                list.Scan();
                IDevice device = list.First();
                if (device != null && IsAudioPlaying(audioDevice))
                {
                    device.SetColor(LedTarget.All, new Color(255, 255, 0));
                } else if (device != null)
                {
                    device.SetColor(LedTarget.All, new Color(0, 255, 0));
                }
                Thread.Sleep(500);
            }
        }

        protected override void OnStop()
        {
            eventLog.WriteEntry("LuxaforMusicListener In onStop");
            this.isStopping = true;
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
