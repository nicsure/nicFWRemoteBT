namespace nicFWRemoteBT;

public partial class Settings : ContentPage
{
    private bool isLoaded = false;
    private static ChannelEditor? chanEditSingleton = null;

    private static readonly FilePickerFileType customTtfFileType = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        { DevicePlatform.Android, ["application/x-font-ttf", "application/x-font", "font/ttf"] },
        { DevicePlatform.iOS, [ "public.truetype-ttf-font" ] },
        { DevicePlatform.WinUI,[ ".ttf" ] },
        { DevicePlatform.MacCatalyst, [ "ttf" ] }
    });

    public Settings()
	{
        BindingContext = VM.Instance;
        Loaded += Settings_Loaded;        
        InitializeComponent();    
        VM.Instance.UpdateNotify += Instance_UpdateNotify;        
    }

    protected override bool OnBackButtonPressed()
    {
        _ = Navigation.PopAsync();
        return true;
    }

    private void Instance_UpdateNotify(object? sender, EventArgs e)
    {
        if (sender is string prop && prop.Equals("ReadyBT"))
        {
            if(VM.Instance.ReadyBT)
            {
                BTDevices.SelectedItem = BT.ConnectedDevice;
            }
        }
    }


    private void Settings_Loaded(object? sender, EventArgs e)
    {
        if(VM.Instance.ReadyBT && BTDevices.SelectedItem != BT.ConnectedDevice)
        {
            BTDevices.SelectedItem = BT.ConnectedDevice;
        }
        isLoaded = true;
    }

    private void ScanButton_Clicked(object sender, EventArgs e)
    {
        _ = BT.Scan();
    }

    private async void BTDevices_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (BTDevices.SelectedItem is BTDevice bt && isLoaded && bt != BT.ConnectedDevice)
        {
            await BT.Connect(bt);
            if (VM.Instance.ReadyBT)
            {
                Prefs.Str("LastDeviceBT", bt.ToString());
            }
        }
    }

    private void InitialOrientation_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (!VM.Instance.AllowOrientation && isLoaded)
        {
            PlatformManager.SetOrientation(InitialOrientation.SelectedIndex);
        }
    }

    private void ChanEditButton_Clicked(object sender, EventArgs e)
    {
        chanEditSingleton ??= new();
        Navigation.PushAsync(chanEditSingleton);
    }

    private async void FindFontButton_Clicked(object sender, EventArgs e)
    {
        PickOptions po = new()
        {
            PickerTitle = "Select Custom Font File",
            FileTypes = customTtfFileType
        };
        FileResult? result = await FilePicker.Default.PickAsync(po);
        if (result != null)
        {
            VM.Instance.CustomFontFile = result.FullPath;
        }
    }

    private void ClearFontButton_Clicked(object sender, EventArgs e)
    {
        VM.Instance.CustomFontFile = string.Empty;
    }

    private void DisconnectButton_Clicked(object sender, EventArgs e)
    {
        BT.Disconnect();
        BTDevices.SelectedIndex = -1;
    }
}