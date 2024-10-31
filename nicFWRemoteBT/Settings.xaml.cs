namespace nicFWRemoteBT;

public partial class Settings : ContentPage
{
    private bool isLoaded = false;

	public Settings()
	{
        Loaded += Settings_Loaded;
        BindingContext = VM.Instance;
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
        if(VM.Instance.ReadyBT)
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
}