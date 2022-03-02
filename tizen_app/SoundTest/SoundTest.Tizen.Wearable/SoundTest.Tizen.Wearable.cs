// contains code to instantiate your wearable application within the Tizen framework.

using System;
using Xamarin.Forms;

using Tizen;
using Tizen.NUI;
using Tizen.Applications;
//using Tizen.Network.WiFi;



namespace SoundTest
{
    class Program : global::Xamarin.Forms.Platform.Tizen.FormsApplication
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            LoadApplication(new App());
            //Initialize();
        }

        void Initialize()
        {
            Window.Instance.TouchEvent += OnTouchEvent;
        }

        public void OnTouchEvent(object sender, Window.TouchEventArgs e)
        {
            Log.Info("LOG_TAG", "HELLO");
            Log.Info("LOG_TAG", Convert.ToString(e.Touch.ToString()));
            Log.Info("LOG_TAG", e.Touch.GetLocalPosition(0).X + ", " + Convert.ToString(e.Touch.GetLocalPosition(0).Y));
        }

        static void Main(string[] args)
        {
            var app = new Program();
            Forms.Init(app);
            app.Run(args);
        }

        //Override this method if you want to execute some functionality in  case of low battery
        protected override void OnLowBattery(LowBatteryEventArgs e)
        {
            Log.Info("LOG_TAG", e.LowBatteryStatus.ToString());
        }

        //Override this method if you want to execute some functionality in case of low memory 
        protected override void OnLowMemory(LowMemoryEventArgs e)
        {
            Log.Info("LOG_TAG", e.LowMemoryStatus.ToString());
        }


    }
}
