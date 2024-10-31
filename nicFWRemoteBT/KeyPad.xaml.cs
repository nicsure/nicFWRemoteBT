namespace nicFWRemoteBT;

public partial class KeyPad : ContentView
{
    public event EventHandler? ButtonClicked = null;
    public event EventHandler? ButtonUnclicked = null;

    public KeyPad()
	{
        BindingContext = VM.Instance;
        Loaded += KeyPad_Loaded;
		InitializeComponent();
	}

    private void KeyPad_Loaded(object? sender, EventArgs e)
    {
        foreach(var v in KeyGrid.Children)
        {
            if(v is XButton xbutton)
            {
                xbutton.ResizeText();
                xbutton.Pressed -= Xbutton_Clicked;
                xbutton.Pressed += Xbutton_Clicked;
                xbutton.Released -= Xbutton_Released;
                xbutton.Released += Xbutton_Released;
            }
        }
    }

    private void Xbutton_Released(object? sender, EventArgs e)
    {
        (_ = ButtonUnclicked)?.Invoke(sender, EventArgs.Empty);

    }

    private void Xbutton_Clicked(object? sender, EventArgs e)
    {
        (_ = ButtonClicked)?.Invoke(sender, EventArgs.Empty);
    }
}