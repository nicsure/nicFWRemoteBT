using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nicFWRemoteBT
{
    public static class BT
    {
        private readonly static IAdapter adapter = CrossBluetoothLE.Current.Adapter;
        private static ICharacteristic? reader = null, writer = null;
        public static BTDevice? ConnectedDevice { get; private set; } = null;
        public static IByteProcessor? DataTarget { get; set; } = null;
        public static IDispatcher? Dispatcher { get; set; } = null;

        static BT()
        {
            adapter.DeviceDiscovered += Adapter_DeviceDiscovered;
        }

        private static void Adapter_DeviceDiscovered(object? sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
        {
            Dispatcher?.Dispatch(() =>
                {
                    VM.Instance.BTDevices.Add(new(e.Device));
                });
        }

        public static string ToHexBleAddress(this Guid id)
        {
            return id.ToString("N")[20..].ToUpperInvariant();
        }

        public static async Task Scan()
        {
            await Disconnect(ConnectedDevice);
            foreach (var device in VM.Instance.BTDevices)
                device.Dispose();
            VM.Instance.BTDevices.Clear();
            VM.Instance.BTStatus = $"Scanning...";
            VM.Instance.BusyBT = true;
            await adapter.StartScanningForDevicesAsync();
            VM.Instance.BusyBT = false;
            VM.Instance.ForceUpdate = "BTDevices";
            VM.Instance.BTStatus = $"{VM.Instance.BTDevices.Count} Devices Found";
        }

        public static void Disconnect() => _ = Disconnect(ConnectedDevice);

        public static async Task Disconnect(BTDevice? device, bool showStatus = true)
        {
            await SendByte(0x4b); // stop remote command
            ConnectedDevice = null;
            if(showStatus)
                VM.Instance.BTStatus = $"Disconnecting...";
            VM.Instance.ReadyBT = false;
            VM.Instance.BusyBT = true;
            try
            {
                if (reader != null)
                    await reader.StopUpdatesAsync();
            }
            catch { }
            try
            {
                if (device != null)
                    await adapter.DisconnectDeviceAsync(device.Device);
            }
            catch { }
            Display.State = DPState.Idle;
            reader = null;
            writer = null;
            if (showStatus)
                VM.Instance.BTStatus = $"Disconnected";
            VM.Instance.BusyBT = false;
        }

        public static async Task SendByte(int byt)
        {
            await Write([(byte)byt]);
        }

        public static async Task Write(byte[] bytes)
        {
            if (writer != null)
            {
                try
                {
                    await writer.WriteAsync(bytes);
                }
                catch { }
            }
        }

        public static async Task Connect(BTDevice device)
        {
            await Disconnect(ConnectedDevice);
            try
            {
                ConnectedDevice = device;
                VM.Instance.BTStatus = $"Connecting...";
                VM.Instance.BusyBT = true;
                await adapter.ConnectToDeviceAsync(device.Device);
                var services = await device.Device.GetServicesAsync();
                VM.Instance.BTStatus = $"Establishing Connection";
                foreach (var service in services)
                {
                    if (service.Id.PartialFromUuid().ToLower().Equals("0xff00"))
                    {
                        var characteristics = await service.GetCharacteristicsAsync();
                        foreach (var characteristic in characteristics)
                        {
                            if (characteristic.CanUpdate)
                                reader = characteristic;
                            if (characteristic.CanWrite)
                                writer = characteristic;
                            if (reader != null && writer != null)
                            {
                                VM.Instance.ReadyBT = true;
                                VM.Instance.BTStatus = $"Connected";
                                reader.ValueUpdated -= Reader_ValueUpdated;
                                reader.ValueUpdated += Reader_ValueUpdated;
                                await reader.StartUpdatesAsync();
                                await SendByte(0x4a); // start remote command
                                return;
                            }
                        }
                    }
                }
            }
            catch { }
            finally { VM.Instance.BusyBT = false; }
            VM.Instance.BTStatus = $"Unable To Connect";
            await Disconnect(device, false);
        }

        private static void Reader_ValueUpdated(object? sender, Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs e)
        {
            if (DataTarget != null)
            {
                Dispatcher?.Dispatch(() =>
                {
                    foreach (byte b in e.Characteristic.Value)
                    {
                        DataTarget.ProcessByte(b);
                    }
                });
            }
        }
    }

    public class BTDevice(IDevice device) : IDisposable
    {
        public IDevice Device { get; } = device;

        public override string ToString()
        {
            return $"{Device.Name} {Device.Id.ToHexBleAddress()[^5..]}";
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Device.Dispose();
        }
    }
}
