using System;
using Plugin.BLE.Abstractions.Contracts;

namespace FindMe.Models
{
    public class BLEDevice
    {
        public IDevice Device { get; private set; }

        public Guid Id => Device.Id;

        public bool IsConnected { get; set; }
        public int Rssi { get; set; }
        public string Name => Device.Name;

        public BLEDevice(IDevice device)
        {
            Device = device;
        }
    }
}