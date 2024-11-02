

using System.Text;

namespace nicFWRemoteBT;



public partial class ChannelEditor : ContentPage, IByteProcessor
{
    private int blockCnt = 0;
    private byte blockCS = 0;
    private int currentChannel = -1;
    private readonly byte[] recBuffer = new byte[32];
    private readonly byte[] data = new byte[32];
    private bool suppressUpdate = false;
    private double offset = 0;
    public CPState State { get; set; } = CPState.Idle;
	public ChannelEditor()
	{
        BindingContext = VM.Instance;
        Loaded += ChannelEditor_Loaded;
        InitializeComponent();
        for (int i = 1; i < 199; i++)
            ChannelNum.Items.Add(i.ToString());
        RxCTS.SelectedIndex =
            TxCTS.SelectedIndex =
            RxDCS.SelectedIndex =
            TxDCS.SelectedIndex = 0;        
        foreach (string cts in SubTones.CTS)
        {
            RxCTS.Items.Add(cts);
            TxCTS.Items.Add(cts);
        }
        RxDCS.Items.Add("Off");
        TxDCS.Items.Add("Off");
        foreach (string dcs in SubTones.DCS)
        {
            RxDCS.Items.Add($"{dcs}N");
            TxDCS.Items.Add($"{dcs}N");
        }
        foreach (string dcs in SubTones.DCS)
        {
            RxDCS.Items.Add($"{dcs}I");
            TxDCS.Items.Add($"{dcs}I");
        }
    }

    private async void ChannelEditor_Loaded(object? sender, EventArgs e)
    {
        await BT.SendByte(0x4b); // disable remote
        BT.DataTarget = this;
    }

    protected override bool OnBackButtonPressed()
    {
        BT.DataTarget = MainPage.Display;
        _ = BT.SendByte(0x4a); // enable remote
        _ = Navigation.PopAsync();
        return true;
    }

    private async void ChannelNum_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (VM.Instance.PendingEdit)
        {
            if (currentChannel > -1 && await DisplayAlert("Alert!", $"Channel {currentChannel + 1} changes are unsaved", "Save Now", "Discard"))
            {
                await SaveChannel();
                int to = 100;
                while (VM.Instance.BusyBT && to-- > 0)
                {
                    await Task.Delay(20);
                }
            }
            VM.Instance.PendingEdit = false;
        }
        VM.Instance.BusyBT = true;
        currentChannel = ChannelNum.SelectedIndex;
        await BT.Write([0x30, (byte)(currentChannel + 2)]); // request eeprom read      
    }

    private void ChannelDown_Clicked(object sender, EventArgs e)
    {
        int i = ChannelNum.SelectedIndex - 1;
        if (i < 0) i = 197;
        ChannelNum.SelectedIndex = i;
    }

    private void ChannelUp_Clicked(object sender, EventArgs e)
    {
        int i = ChannelNum.SelectedIndex + 1;
        if (i > 197) i = 0;
        ChannelNum.SelectedIndex = i;
    }

    private void Value_Changed(object sender, EventArgs e)
    {
        if(!suppressUpdate)
        {
            byte[] cmp = data.Copy();
            UpdateToData(sender == RXFreq);
            if(!VM.Instance.PendingEdit && !data.IsEqual(cmp))
                VM.Instance.PendingEdit = true;
        }
    }

    private void Subtone_SelectedIndexChanged(object sender, EventArgs e)
    {        
        if(sender is Picker picker)
        {
            if (picker.SelectedIndex > 0)
            {
                if (sender == RxCTS || sender == RxDCS)
                {
                    RXTone.Text = picker.SelectedItem.ToString();
                    Value_Changed(RXTone, e);
                }
                else
                if (sender == TxCTS || sender == TxDCS)
                {
                    TXTone.Text = picker.SelectedItem.ToString();
                    Value_Changed(TXTone, e);
                }
                picker.SelectedIndex = 0;    
            }
        }         
    }

    private static string SubTone2String(int st)
    {
        if (st == 0) return "Off";
        if (st < 0x8000) return $"{st / 10.0:F1}";
        return $"D{Convert.ToString(st & 0x3fff, 8).PadLeft(3, '0')}{((st & 0x4000) == 0 ? 'N' : 'I')}";
    }

    private static int String2SubTone(string? sts)
    {
        if (string.IsNullOrEmpty(sts)) return 0;
        sts = sts.ToLower().Trim();
        if (string.IsNullOrEmpty(sts)) return 0;
        if (sts.StartsWith('d'))
        {
            int r;
            if (sts.EndsWith('n'))
                r = 0x8000;
            else
            if (sts.EndsWith('i'))
                r = 0xc000;
            else
                return 0;
            sts = sts.Replace("d", "").Replace("n", "").Replace("i", "");
            int len = sts.Length;
            if (len == 0 || len > 3) return 0;
            int mul = 1;
            for (int i = sts.Length - 1; i >= 0; i--)
            {
                char c = sts[i];
                if (c < '0' || c > '7') return 0;
                r += (c - '0') * mul;
                mul *= 8;
            }
            return r;
        }
        else
        if (double.TryParse(sts, out double d))
        {
            return d <= 300 ? (int)Math.Round(d * 10) : 0;
        }
        else
            return 0;
    }

    private static string GroupW2String(int groupw)
    {
        string s = string.Empty;
        for (int i = 0; i < 4; i++)
        {
            int nyb = groupw & 0xf;
            groupw >>= 4;
            if (nyb > 0)
                s += (char)(nyb + 64);
        }
        return s;
    }

    private static int String2GroupW(string gString)
    {
        int r = 0;
        gString = gString.ToUpper().Trim().PadRight(4)[..4];
        for (int i = gString.Length - 1; i >= 0; i--)
        {
            char c = gString[i];
            if (c >= 'A' && c <= 'O')
            {
                int g = (c - 'A') + 1;
                r <<= 4;
                r += g;
            }
        }
        return r;
    }

    private static string Modulation2String(int mod)
    {
        return mod switch
        {
            1 => "FM",
            2 => "AM",
            3 => "USB",
            _ => "Auto",
        };
    }

    private static int String2Modulation(string mods)
    {
        return mods.ToUpper().Trim() switch
        {
            "FM" => 1,
            "AM" => 2,
            "USB" => 3,
            _ => 0
        };
    }

    private void UpdateFromData()
    {
        double rx = BitConverter.ToInt32(data, 0) / 100000.0;
        double tx = BitConverter.ToInt32(data, 4) / 100000.0;
        offset = tx - rx;
        string rxtone = SubTone2String(BitConverter.ToUInt16(data, 8));
        string txtone = SubTone2String(BitConverter.ToUInt16(data, 10));
        int txpower = data[12];
        string groups = GroupW2String(BitConverter.ToUInt16(data, 13));
        string modul = Modulation2String((data[15] >> 1) & 3);
        string bw = (data[15] & 1) == 0 ? "Wide" : "Narrow";
        string name = Encoding.ASCII.GetString(data, 20, 12).Trim('\0');
        suppressUpdate = true;

        Active.IsChecked = rx >= 18 && rx <= 1300;
        RXFreq.Text = rx.ToString("F5");
        TXFreq.Text = tx.ToString("F5");
        RXTone.Text = rxtone;
        TXTone.Text = txtone;
        TXPower.Text = txpower.ToString();
        Groups.Text = groups;
        Modulation.SelectedItem = modul;
        Bandwidth.SelectedItem = bw;
        Name.Text = name;

        suppressUpdate = false;
    }

    private void UpdateToData(bool rxEdit = false)
    {
        Array.Fill<byte>(data, 0);
        bool active = Active.IsChecked;
        if (active)
        {
            double rxd = double.TryParse(RXFreq.Text, out double d) ? d : 144;
            double txd = double.TryParse(TXFreq.Text, out d) ? d : 144;
            if(rxEdit) txd = rxd + offset;
            if (rxd == 0) rxd = txd = 144;
            int rx = ((int)Math.Round(rxd * 100000.0)).Clamp(1800000, 130000000);
            int tx = ((int)Math.Round(txd * 100000.0)).Clamp(1800000, 130000000);
            int rxtone = String2SubTone(RXTone.Text);
            int txtone = String2SubTone(TXTone.Text);
            int txpower = (int.TryParse(TXPower.Text, out int i) ? i : 0).Clamp(0, 254);
            int groups = String2GroupW(Groups.Text);
            int modul = String2Modulation(Modulation.SelectedItem.ToString() ?? string.Empty);
            int bw = (Bandwidth.SelectedItem?.ToString() ?? string.Empty).ToLower().Trim().Equals("narrow") ? 1 : 0;
            string name = Name.Text.PadRight(12, '\0')[..12];
            BitConverter.GetBytes(rx).CopyTo(data, 0);
            BitConverter.GetBytes(tx).CopyTo(data, 4);
            BitConverter.GetBytes((ushort)rxtone).CopyTo(data, 8);
            BitConverter.GetBytes((ushort)txtone).CopyTo(data, 10);
            data[12] = (byte)txpower;
            BitConverter.GetBytes((ushort)groups).CopyTo(data, 13);
            data[15] |= (byte)(modul << 1);
            data[15] |= (byte)bw;
            Encoding.ASCII.GetBytes(name).CopyTo(data, 20);
        }
        UpdateFromData();
    }

    public void ProcessByte(int byt)
    {
        switch(State)
        {
            case CPState.Idle:
                switch(byt)
                {
                    case 0x4a: // remote mode ACK
                        VM.Instance.RemoteIsEnabled = true;
                        break;
                    case 0x4b: // end remote mode ACK
                        VM.Instance.RemoteIsEnabled = false;
                        break;
                    case 0x30: // read eeprom block
                        blockCnt = 0;
                        blockCS = 0;
                        State = CPState.ReadEeprom;
                        break;
                    case 0x31: // ack block write
                        VM.Instance.BusyBT = false;
                        break;
                }
                break;
            case CPState.ReadEeprom:
                blockCS += (byte)byt;
                recBuffer[blockCnt++] = (byte)byt;
                if (blockCnt == 32)
                    State = CPState.ReadEepromCS;
                break;
            case CPState.ReadEepromCS:
                if(blockCS == byt)
                {
                    Array.Copy(recBuffer, 0, data, 0, 32);
                    UpdateFromData();
                    VM.Instance.PendingEdit = false;
                }
                State = CPState.Idle;
                VM.Instance.BusyBT = false;
                break;
        }
    }

    private async Task SaveChannel()
    {
        VM.Instance.PendingEdit = false;
        VM.Instance.BusyBT = true;
        byte cs = 0;
        foreach (byte b in data) cs += b;
        if (currentChannel > -1)
        {
            await BT.Write([0x31, (byte)(currentChannel + 2), .. data, cs]); // eeprom write
        }        
    }

    private async void SaveButton_Clicked(object sender, EventArgs e)
    {
        await SaveChannel();
    }

    private void CancelButton_Clicked(object sender, EventArgs e)
    {
        VM.Instance.PendingEdit = false;
        ChannelNum_SelectedIndexChanged(sender, e);
    }
}

public enum CPState
{
    Idle,
    ReadEeprom,
    ReadEepromCS,

}