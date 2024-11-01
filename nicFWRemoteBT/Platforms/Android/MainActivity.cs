using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Views;

namespace nicFWRemoteBT
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity, IPlatformManager
    {
        private MyOrientationEventListener? myOrientationEventListener = null;        

        public void SetOrientation(int orientation)
        {
            if(myOrientationEventListener != null) 
                myOrientationEventListener.OrientationCode = orientation;
            RequestedOrientation = orientation switch
            {
                1 => ScreenOrientation.ReverseLandscape,
                2 => ScreenOrientation.ReversePortrait,
                3 => ScreenOrientation.Landscape,
                _ => ScreenOrientation.Portrait,
            };
            if (MainPage.Instance != null)
            {
                switch (orientation)
                {
                    case 0:
                    case 2:
                        MainPage.Instance.SetOrientation(DisplayOrientation.Portrait);
                        break;
                    default:
                        MainPage.Instance.SetOrientation(DisplayOrientation.Landscape);
                        break;
                }
            }
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            PlatformManager.Instance = this;
            myOrientationEventListener = new(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            BT.Disconnect();
        }

        protected override void OnPause()
        {
            myOrientationEventListener?.Disable();
            base.OnPause();
        }

        protected override void OnResume()
        {
            base.OnResume();
            myOrientationEventListener?.Enable();
        }

    }


    public class MyOrientationEventListener : OrientationEventListener
    {
        public MyOrientationEventListener(Context? context) : base(context) {  }
        public MyOrientationEventListener(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }
        public MyOrientationEventListener(Context? context, [GeneratedEnum] SensorDelay rate) : base(context, rate) { }

        public int OrientationCode { get; set; } = -1;

        public override void OnOrientationChanged(int orientation)
        {
            if (VM.Instance.AllowOrientation && OrientationCode != -1)
            {
                int oCode;
                if (orientation > 45 && orientation <= 135)
                    oCode = 1;
                else if (orientation > 135 && orientation <= 225)
                    oCode = 2;
                else if (orientation > 225 && orientation <= 315)
                    oCode = 3;
                else
                    oCode = 0;
                if (oCode != OrientationCode)
                {
                    PlatformManager.SetOrientation(oCode);
                }
            }
        }
    }
}
