using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FindMe.Models;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Extensions;
using Xamarin.Forms;

namespace FindMe.Services
{
    sealed class BLEService
    {
        private static BLEService _bleServiceInstance = new BLEService();

        // BLUETOOTH
        public IAdapter _adapterInstance;
        public IBluetoothLE _bleInstance;

        public bool IsScanning { get; set; }

        public BLEDevice _findMeConnected;
        private CancellationTokenSource _cancellationTokenSource;

        private BLEService()
        {
            // Init plugin
            _adapterInstance = CrossBluetoothLE.Current.Adapter;
            _bleInstance = CrossBluetoothLE.Current;

            // Events
            _adapterInstance.DeviceDiscovered += OnDeviceDiscovered;
            _bleInstance.StateChanged += OnStateChanged;
            _adapterInstance.ScanTimeoutElapsed += ScanTimeoutElapsed;
            _adapterInstance.DeviceDisconnected += OnDeviceDisconnected;
            _adapterInstance.DeviceConnectionLost += OnDeviceConnectionLost;
        }

        static internal BLEService Instance()
        {
            return _bleServiceInstance;
        }

        private void ScanTimeoutElapsed(object sender, EventArgs args)
        {
            IsScanning = false;
            CleanupCancellationToken();

            System.Diagnostics.Debug.WriteLine("ScanTimeoutElapsed - " + _adapterInstance.ScanTimeout);

            // Send message
            MessagingCenter.Send<BLEService>(this, "scanTimeoutElapsed");
        }

        private void OnDeviceConnectionLost(object sender, DeviceErrorEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("Connection lost with " + args.Device.Name);

            // Resert device connected
            _findMeConnected = null;

            // Send message
            MessagingCenter.Send<BLEService>(this, "deviceConnectionLost");
        }

        public void OnStateChanged(object sender, BluetoothStateChangedArgs args)
        {
            System.Diagnostics.Debug.WriteLine("State changed - New state : " + args.NewState + " Old state : " + args.OldState);

            // Send message
            MessagingCenter.Send<BLEService, BluetoothStateChangedArgs>(this, "stateChanged", args); 
        }

        private void OnDeviceDiscovered(object sender, DeviceEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("Device discovered : " + args.Device.Name);

            if (args.Device.Name == "Find")
            {
                // Send message
                MessagingCenter.Send<BLEService, BLEDevice>(this, "deviceDiscovered", new BLEDevice(args.Device));
            }
        }

        private async void ScanForDevices()
        {
            // Update rssi for already connected device (so that 0 is not shown in the list)
            if (_findMeConnected != null)
            {
                try
                {
                    await _findMeConnected.Device.UpdateRssiAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to update RSSI for " + _findMeConnected.Name + " - Error : " + ex.Message);
                }
            }

            _cancellationTokenSource = new CancellationTokenSource();

            IsScanning = false;

            _adapterInstance.ScanMode = ScanMode.LowLatency;

            System.Diagnostics.Debug.WriteLine("Start scan !");
            await _adapterInstance.StartScanningForDevicesAsync(_cancellationTokenSource.Token);
        }

        public async Task<double> UpdateRssiDevice()
        {
            if (_findMeConnected != null)
            {
                try
                {
                    await _findMeConnected.Device.UpdateRssiAsync();

                    double txPower = -72;
                    var distance =  Math.Pow(10d, ((double)txPower - _findMeConnected.Device.Rssi) / (10 * 2));

                    System.Diagnostics.Debug.WriteLine("Update RSSI for " + _findMeConnected.Device.Name + " - Distance " + distance + " m");

                    return distance;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to update RSSI for " + _findMeConnected.Device.Name + " - Error : " + ex.Message);
                    return 0;
                }
            }

            return 0;
        }

        public void StopScan()
        {
            try
            {
                if (_cancellationTokenSource != null)
                    _cancellationTokenSource.Cancel();

                CleanupCancellationToken();
            }
            catch (TaskCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine("Task canceled : " + ex.Message);
            }
            
            IsScanning = false;
        }

        public void TryStartScanning(bool refresh = false)
        {
            if ((refresh || _findMeConnected == null) && !IsScanning)
                ScanForDevices();
        }

        private void CleanupCancellationToken()
        {
            if (_cancellationTokenSource != null)
                _cancellationTokenSource.Dispose();

            _cancellationTokenSource = null;
        }

        private async void DisconnectDevice(BLEDevice device)
        {
            try
            {
                if (!device.IsConnected)
                    return;

                System.Diagnostics.Debug.WriteLine("Disconnecting " + device.Name + "...");

                await _adapterInstance.DisconnectDeviceAsync(device.Device);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Disconnect error " + ex.Message);
            }
        }

        public async Task<bool> ConnectDeviceAsync(BLEDevice device)
        {
            try
            {
                CancellationTokenSource tokenSource = new CancellationTokenSource();

                System.Diagnostics.Debug.WriteLine("Connecting to " + device.Name);
              
                await _adapterInstance.ConnectToDeviceAsync(device.Device, new ConnectParameters(autoConnect: true, forceBleTransport: false), tokenSource.Token);

                System.Diagnostics.Debug.WriteLine("Connected to " + device.Name);

                _findMeConnected = device;

                // Send message
                MessagingCenter.Send<BLEService>(this, "deviceConnected");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Connection error " + ex.Message);
                return false;
            }
        }

        private void OnDeviceDisconnected(object sender, DeviceEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("Disconnected " + args.Device.Name);

            // Reset device
            _findMeConnected = null;

            // Send message
            MessagingCenter.Send<BLEService>(this, "deviceDisconnected");
        }

        public async Task<IList<ICharacteristic>> LoadCharacteristicsFromService(IService service)
        {
            try
            {
                return await service.GetCharacteristicsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loadCharacteristics " + ex.Message);
                return null;
            }
        }

        public async Task<IList<IService>> LoadServices(IDevice device)
        {
            try
            {
                return await device.GetServicesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loadServices " + ex.Message);
                return null;
            }
        }

        public async Task<IService> LoadService(IDevice device, Guid guid)
        {
            try
            {
                return await device.GetServiceAsync(guid);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loadService " + ex.Message);
                return null;
            }
        }

        public void WriteValueAsyncInCharacteristic(byte[] bytesArray, ICharacteristic characteristic)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    bool result = await characteristic.WriteAsync(bytesArray);
                    System.Diagnostics.Debug.WriteLine("Write ok ! " + result);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error WriteValueAsyncInCharacteristic " + ex.Message);
                }
            });
        }

        public async void StartUpdatesOnCharacteristic(ICharacteristic characteristic)
        {
            try
            {
                characteristic.ValueUpdated -= CharacteristicOnValueUpdated;
                characteristic.ValueUpdated += CharacteristicOnValueUpdated;

                await characteristic.StartUpdatesAsync();

                System.Diagnostics.Debug.WriteLine("Start update notify !");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error Start update notify + " + ex.Message);
            }
        }

        private void CharacteristicOnValueUpdated(object sender, CharacteristicUpdatedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("Update notify !");

            // Send message
            MessagingCenter.Send<BLEService, CharacteristicUpdatedEventArgs>(this, "characteristicUpdated", args);
        }
    }
}