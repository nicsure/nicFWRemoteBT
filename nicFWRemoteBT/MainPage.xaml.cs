using System.Diagnostics;

namespace nicFWRemoteBT
{
    public partial class MainPage : ContentPage
    {
        private readonly Display display = new();
        private readonly KeyPad keyPad = new();

        public static MainPage? Instance { get; private set; } = null;

        public MainPage()
        {
            Instance = this;
            BindingContext = VM.Instance;
            Loaded += MainPage_Loaded;
            InitializeComponent();
            MainGrid.Children.Add(display);
            MainGrid.Children.Add(keyPad);
            SetOrientation(DisplayOrientation.Portrait);
            keyPad.ButtonClicked += KeyPad_ButtonClicked;
            keyPad.ButtonUnclicked += KeyPad_ButtonUnclicked;
            RequestPermissionsAsync();            
        }

        private static bool firstLoad = true;
        private async void MainPage_Loaded(object? sender, EventArgs e)
        {
            if (firstLoad)
            {
                firstLoad = false;
                PlatformManager.SetOrientation(VM.Instance.Orientation);
                if (VM.Instance.ConnectOnStart)
                {
                    await BT.Scan();
                    string ld = Prefs.Str("LastDeviceBT");
                    if (!string.IsNullOrEmpty(ld))
                    {
                        foreach (var sd in VM.Instance.BTDevices)
                        {
                            if (sd.ToString().Equals(ld))
                            {
                                await BT.Connect(sd);
                                break;
                            }
                        }
                    }
                }
            }            
        }

        private async void RequestPermissionsAsync()
        {
            if (DeviceInfo.Current.Platform == DevicePlatform.Android)
            {
                var locationStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                var bluetoothStatus = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
                if (locationStatus != PermissionStatus.Granted)
                {
                    locationStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (bluetoothStatus != PermissionStatus.Granted)
                {
                    bluetoothStatus = await Permissions.RequestAsync<Permissions.Bluetooth>();
                }
                if (locationStatus == PermissionStatus.Granted && bluetoothStatus == PermissionStatus.Granted)
                {
                    VM.Instance.AvailBT = true;
                }
                else
                {
                    await DisplayAlert("Permissions Required", "Location and Bluetooth permissions are required for this app.", "OK");
                }
            }
            else
            if (DeviceInfo.Current.Platform == DevicePlatform.WinUI)
            {
                VM.Instance.AvailBT = true;
            }
        }

        private Settings? settingsSingleton = null;
        private async void KeyPad_ButtonClicked(object? sender, EventArgs e)
        {
            if (sender is XButton button)
            {
                if(int.TryParse(button.Tag, out int buttonId))
                {
                    await BT.SendByte((byte)(buttonId | 0x80));                    
                }
                else
                switch(button.Tag)
                {
                    case "Settings":
                        if (settingsSingleton == null) settingsSingleton = new();
                        await Navigation.PushAsync(settingsSingleton);
                        break;
                }
            }
        }

        private async void KeyPad_ButtonUnclicked(object? sender, EventArgs e)
        {
            if (sender is XButton button) 
            {
                if (int.TryParse(button.Tag, out int buttonId))
                {
                    await Task.Delay(250);
                    await BT.SendByte(0xff);
                }
            }
        }


        public void SetOrientation(DisplayOrientation orientation)
        {            
            switch (orientation)
            {
                default:
                    Debug.WriteLine("Report: Unknown Orientation [MainPage.SetOrientation()]");
                    SetOrientation(DisplayOrientation.Portrait);
                    break;
                case DisplayOrientation.Portrait:
                    LeftColumn.Width = new GridLength(1, GridUnitType.Star);
                    RightColumn.Width = new GridLength(0);
                    TopRow.Height = new GridLength (1, GridUnitType.Star);
                    BottomRow.Height = new GridLength(1, GridUnitType.Star);
                    MainGrid.SetColumn(keyPad, 0);
                    MainGrid.SetColumn(display, 0);
                    MainGrid.SetRow(keyPad, 1);
                    MainGrid.SetRow(display, 0);
                    break;
                case DisplayOrientation.Landscape:
                    LeftColumn.Width = new GridLength(1, GridUnitType.Star);
                    RightColumn.Width = new GridLength(1, GridUnitType.Star);
                    TopRow.Height = new GridLength(1, GridUnitType.Star);
                    BottomRow.Height = new GridLength(0);
                    MainGrid.SetColumn(keyPad, 1);
                    MainGrid.SetColumn(display, 0);
                    MainGrid.SetRow(keyPad, 0);
                    MainGrid.SetRow(display, 0);
                    break;
            }
        }
    }

}
