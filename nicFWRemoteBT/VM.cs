using Plugin.BLE.Abstractions.Contracts;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace nicFWRemoteBT
{
    public class VM : INotifyPropertyChanged
    {
        public static VM Instance { get; private set; }
        static VM()
        {
            Instance = new();
            Instance.SetCustomFont();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? UpdateNotify = null; 
        public void OnPropertyChanged(string propertyName)
        {
            (_ = PropertyChanged)?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            (_ = UpdateNotify)?.Invoke(propertyName, EventArgs.Empty);
        }

#pragma warning disable CA1822 // Mark members as static (Intellisense is a dick head)
        public string Version => "V1.2.1";
#pragma warning restore CA1822 // Mark members as static

        private void SetCustomFont()
        {
            var temp = CustomTypeface;
            CustomTypeface = null;
            using (temp) { }
            try
            {
                using FileStream stream = new(CustomFontFile, FileMode.Open);
                CustomTypeface = SKTypeface.FromStream(stream);
            }
            catch { }
        }

        public string CustomFontFile
        {
            get => Prefs.Str(nameof(CustomFontFile));
            set
            {
                Prefs.Str(nameof(CustomFontFile), value);
                OnPropertyChanged(nameof(CustomFontFile));
                SetCustomFont();
                OnPropertyChanged(nameof(CustomFontName));
            }
        }

        public string CustomFontName => CustomTypeface?.FamilyName ?? "None Loaded";
        
        public SKTypeface? CustomTypeface { get; set; } = null;

        public int FontSize0
        {
            get => Prefs.Int(nameof(FontSize0)).IfZero(24);
            set
            {
                if (Prefs.Int(nameof(FontSize0)) != value)
                {
                    Prefs.Int(nameof(FontSize0), value);
                    OnPropertyChanged(nameof(FontSize0));
                }
            }
        }

        public int FontSize1
        {
            get => Prefs.Int(nameof(FontSize1)).IfZero(32);
            set
            {
                if (Prefs.Int(nameof(FontSize1)) != value)
                {
                    Prefs.Int(nameof(FontSize1), value);
                    OnPropertyChanged(nameof(FontSize1));
                }
            }
        }

        public int FontSize2
        {
            get => Prefs.Int(nameof(FontSize2)).IfZero(64);
            set
            {
                if (Prefs.Int(nameof(FontSize2)) != value)
                {
                    Prefs.Int(nameof(FontSize2), value);
                    OnPropertyChanged(nameof(FontSize2));
                }
            }
        }


        public int Orientation
        {
            get => Prefs.Int(nameof(Orientation));
            set
            {
                if (Prefs.Int(nameof(Orientation)) != value)
                {
                    Prefs.Int(nameof(Orientation), value);
                    OnPropertyChanged(nameof(Orientation));
                }
            }
        }

        public bool AllowOrientation
        {
            get => Prefs.Bool(nameof(AllowOrientation));
            set
            {
                if (Prefs.Bool(nameof(AllowOrientation)) != value)
                {
                    Prefs.Bool(nameof(AllowOrientation), value);
                    OnPropertyChanged(nameof(AllowOrientation));
                }
            }
        }

        public bool BlueBoost
        {
            get => Prefs.Bool(nameof(BlueBoost));
            set
            {
                if (Prefs.Bool(nameof(BlueBoost)) != value)
                {
                    Prefs.Bool(nameof(BlueBoost), value);
                    OnPropertyChanged(nameof(BlueBoost));
                }
            }
        }

        public bool ConnectOnStart
        {
            get => Prefs.Bool(nameof(ConnectOnStart));
            set
            {
                if (Prefs.Bool(nameof(ConnectOnStart)) != value)
                {
                    Prefs.Bool(nameof(ConnectOnStart), value);
                    OnPropertyChanged(nameof(ConnectOnStart));
                }
            }
        }


        public string BTStatus
        {
            get => btStatus;
            set
            {
                btStatus = value;
                OnPropertyChanged(nameof(BTStatus));
            }
        }
        private string btStatus = string.Empty;

        public bool ReadyBT
        {
            get => readyBT;
            set
            {
                if (readyBT != (readyBT = value))
                {
                    OnPropertyChanged(nameof(ReadyBT));
                    OnPropertyChanged(nameof(NotReadyBT));
                    OnPropertyChanged(nameof(AllBT));
                    OnPropertyChanged(nameof(ScanBT));
                }
            }
        }
        public bool NotReadyBT => !readyBT;
        private bool readyBT = false;


        public bool AvailBT
        {
            get => availBT;
            set
            {
                if (availBT != (availBT = value))
                {
                    OnPropertyChanged(nameof(AvailBT));
                    OnPropertyChanged(nameof(NotAvailBT));
                    OnPropertyChanged(nameof(AllBT));
                    OnPropertyChanged(nameof(ScanBT));
                }
            }
        }
        public bool NotAvailBT => !availBT;
        private bool availBT = false;


        public bool BusyBT
        {
            get => busyBT;
            set
            {
                if (busyBT != (busyBT = value))
                {
                    OnPropertyChanged(nameof(BusyBT));
                    OnPropertyChanged(nameof(NotBusyBT));
                    OnPropertyChanged(nameof(AllBT));
                    OnPropertyChanged(nameof(ScanBT));
                }
            }
        }
        public bool NotBusyBT => !busyBT;
        private bool busyBT = false;
        public bool AllBT => availBT && readyBT && !busyBT;
        public bool ScanBT => availBT && !busyBT;


        public bool RemoteIsEnabled
        {
            get => remoteIsEnabled;
            set
            {
                if (remoteIsEnabled != (remoteIsEnabled = value))
                {
                    OnPropertyChanged(nameof(RemoteIsEnabled));
                    OnPropertyChanged(nameof(RemoteNotEnabled));
                }
            }
        }
        public bool RemoteNotEnabled => !remoteIsEnabled;
        private bool remoteIsEnabled = false;

        public ObservableCollection<BTDevice> BTDevices { get; } = [];

        public bool LedGreenLeft
        {
            get => ledGreenLeft;
            set
            {
                if (ledGreenLeft != value)
                {
                    ledGreenLeft = value;
                    OnPropertyChanged(nameof(LedGreenLeft));
                    OnPropertyChanged(nameof(LedLeftColor));
                }
            }
        }
        private bool ledGreenLeft = false;

        public bool LedRedLeft
        {
            get => ledRedLeft;
            set
            {
                if (ledRedLeft != value)
                {
                    ledRedLeft = value;
                    OnPropertyChanged(nameof(LedRedLeft));
                    OnPropertyChanged(nameof(LedLeftColor));
                }
            }
        }
        private bool ledRedLeft = false;

        public bool LedGreenRight
        {
            get => ledGreenRight;
            set
            {
                if (ledGreenRight != value)
                {
                    ledGreenRight = value;
                    OnPropertyChanged(nameof(LedGreenRight));
                    OnPropertyChanged(nameof(LedRightColor));
                }
            }
        }
        private bool ledGreenRight = false;

        public bool LedRedRight
        {
            get => ledRedRight;
            set
            {
                if (ledRedRight != value)
                {
                    ledRedRight = value;
                    OnPropertyChanged(nameof(LedRedRight));
                    OnPropertyChanged(nameof(LedRightColor));
                }
            }
        }
        private bool ledRedRight = false;

        private static Color LedColor(bool g, bool r)
        {
            return g ?
                (r ? Colors.Yellow : Colors.LimeGreen) :
                (r ? Colors.Red : Colors.Black);
        }

        public Color LedLeftColor => LedColor(ledGreenLeft, ledRedLeft);

        public Color LedRightColor => LedColor(ledGreenRight, ledRedRight);

        public int SelectedChannel
        {
            get => selectedChannel;
            set
            {
                selectedChannel = value;
                OnPropertyChanged(nameof(SelectedChannel));
                OnPropertyChanged(nameof(ValidSelectedChannel));
            }
        }
        public bool ValidSelectedChannel => selectedChannel > -1 && selectedChannel < 198;
        private int selectedChannel = -1;


        public bool PendingEdit
        { 
            get => pendingEdit;
            set
            {
                pendingEdit = value;
                OnPropertyChanged(nameof(PendingEdit));
                OnPropertyChanged(nameof(NoPendingEdit));
            }
        }
        public bool NoPendingEdit => !pendingEdit;
        private bool pendingEdit = false;

        public string ForceUpdate
        {
            get => string.Empty;
            set => OnPropertyChanged(value);
        }
    }
}
