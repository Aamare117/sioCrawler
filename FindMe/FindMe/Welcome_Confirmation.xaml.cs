using System;
using System.Collections.Generic;
using System.Linq;
using FindMe.Models;
using FindMe.Services;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.SimpleAudioPlayer;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;

namespace FindMe
{
    public partial class Welcome_Confirmation : ContentPage
    {
        private BLEService _bleService;
        private BluetoothState _bluetoothState;
        private bool _isConnected;
        private ISimpleAudioPlayer _player;
        private int _firstTimer;



        public Welcome_Confirmation()
        {
            InitializeComponent();

            _isConnected = false;

            _player = CrossSimpleAudioPlayer.Current;
            _player.Load("bing.mp3");
            _player.Volume = 1;

            var ble = CrossBluetoothLE.Current;
            var adapter = CrossBluetoothLE.Current.Adapter;

            NavigationPage.SetHasNavigationBar(this, false);
            // Subscribe events
            MessagingCenter.Subscribe<BLEService, BLEDevice>(this, "deviceDiscovered", async (sender, bleDevice) =>
            {
                await _bleService.ConnectDeviceAsync(bleDevice);
            });

            MessagingCenter.Subscribe<BLEService>(this, "deviceConnected", (sender) =>
            {
                OnDeviceConnected();
            });

            MessagingCenter.Subscribe<BLEService>(this, "deviceConnectionLost", (sender) =>
            {
                OnDeviceDisconnected();
            });

            MessagingCenter.Subscribe<BLEService>(this, "deviceDisconnected", (sender) =>
            {
                OnDeviceDisconnected();
            });

            MessagingCenter.Subscribe<BLEService, BluetoothStateChangedArgs>(this, "stateChanged", (sender, args) =>
            {
                OnStateChanged(args);
            });

            MessagingCenter.Subscribe<BLEService>(this, "scanTimeoutElapsed", (sender) =>
            {
                OnScanTimeoutElapsed();
            });

            MessagingCenter.Subscribe<BLEService, CharacteristicUpdatedEventArgs>(this, "characteristicUpdated", (sender, args) =>
            {
                OnCharacteristicUpdated(args);
            });

            // Start scan
            _bleService = BLEService.Instance();
            _bleService.TryStartScanning(true);
        
        }

        void OnCharacteristicUpdated(CharacteristicUpdatedEventArgs args)
        {
            if (args.Characteristic.Value[0] == 1)
            {
                _player.Play();
            }
            else if (args.Characteristic.Value[0] == 0)
            {
                _player.Stop();
            }
        }

        void ConnectionButtonClique(object sender,EventArgs args)
        {
            var tabbedPage = new Xamarin.Forms.TabbedPage();
            tabbedPage.Children.Add(new Localisation());
            tabbedPage.Children.Add(new Paramètres());

            tabbedPage.BarBackgroundColor = Color.White;
            tabbedPage.BackgroundColor = Color.White;
            tabbedPage.UnselectedTabColor = Color.FromHex("#5A5A5A");
            tabbedPage.SelectedTabColor = Color.FromHex("#646BEB");
            tabbedPage.BarTextColor = Color.FromHex("#5A5A5A");

            tabbedPage.On<Xamarin.Forms.PlatformConfiguration.Android>().SetToolbarPlacement(ToolbarPlacement.Bottom);

            // Unsubscribe events
            MessagingCenter.Unsubscribe<BLEService, BLEDevice>(this, "deviceDiscovered");
            MessagingCenter.Unsubscribe<BLEService>(this, "deviceConnected");
            MessagingCenter.Unsubscribe<BLEService>(this, "deviceConnectionLost");
            MessagingCenter.Unsubscribe<BLEService>(this, "deviceDisconnected");
            MessagingCenter.Unsubscribe<BLEService, BluetoothStateChangedArgs>(this, "stateChanged");
            MessagingCenter.Unsubscribe<BLEService>(this, "scanTimeoutElapsed");
            MessagingCenter.Unsubscribe<BLEService>(this, "characteristicUpdated");

            App.Current.MainPage = tabbedPage;
        }

        void RelancerButtonClique(object sender, EventArgs args)
        {
            // UI
            AnimationView.Animation = "loader.json";
            AnimationView.IsVisible = true;
            labelStatus.Text = "En cours...";
            labelStatusDescription.Text = "Votre Find Me est en cours de connexion...";
            successConnectedImage.IsVisible = false;
            buttonRetry.IsVisible = false;

            // Start scan
            _bleService.TryStartScanning(true);
        }

        void OnScanTimeoutElapsed()
        {
            _bluetoothState = _bleService._bleInstance.State;

            // Check state because task not canceled when desactivate ble on scan
            if (_bluetoothState == BluetoothState.On && _isConnected == false)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    AnimationView.Animation = "cloud.json";
                    AnimationView.IsVisible = true;
                    labelStatus.Text = "Non détecté";
                    labelStatusDescription.Text = "Votre Find Me n'a pas pu être détecté...";
                    successConnectedImage.IsVisible = false;
                    buttonRetry.IsVisible = true;
                });
            }
        }

        async void OnDeviceConnected()
        {
            _isConnected = true;
            _bleService.StopScan();

            AnimationView.IsVisible = false;
            successConnectedImage.IsVisible = true;

            labelStatus.Text = "Connecté";
            labelStatusDescription.Text = "Votre Find Me est maintenant connecté.";
            buttonStart.IsEnabled = true;

            // Start update notify
            IService service = await _bleService.LoadService(_bleService._findMeConnected.Device, Guid.Parse("a55c5042-90ea-11e9-bc42-526af7764f64"));
            IList<ICharacteristic> characteristics = await _bleService.LoadCharacteristicsFromService(service);
            _bleService.StartUpdatesOnCharacteristic(characteristics.First());
        }

        void OnDeviceDisconnected()
        {
            AnimationView.Animation = "cloud.json";
            AnimationView.IsVisible = true;
            labelStatus.Text = "Déconnecté";
            labelStatusDescription.Text = "Votre Find Me s'est déconnecté.";
            successConnectedImage.IsVisible = false;
            buttonRetry.IsVisible = true;
            buttonStart.IsEnabled = false;
        }

        void OnStateChanged(BluetoothStateChangedArgs args)
        {
            switch (args.NewState)
            {
                case BluetoothState.On:
                    _bluetoothState = BluetoothState.On;

                    AnimationView.Animation = "loader.json";
                    AnimationView.IsVisible = true;
                    labelStatus.Text = "En cours...";
                    labelStatusDescription.Text = "Votre Find Me est en cours de connexion";
                    successConnectedImage.IsVisible = false;

                    // Start scan
                    _bleService.TryStartScanning(true);
                    return;
                case BluetoothState.Off:
                    _bleService.StopScan();

                    _bluetoothState = BluetoothState.Off;

                    AnimationView.Animation = "cloud.json";
                    AnimationView.IsVisible = true;
                    labelStatus.Text = "Erreur";
                    labelStatusDescription.Text = "Le bluetooth de cet appareil semble désactivé.";
                    successConnectedImage.IsVisible = false;
                    buttonRetry.IsVisible = false;
                    buttonStart.IsEnabled = false;
                    return;
                default:
                    return;
            }
        }
    }
}