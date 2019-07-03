using System;
using Xamarin.Forms;
using Plugin.SimpleAudioPlayer;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using FindMe.Services;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using System.Collections.Generic;
using System.Linq;

namespace FindMe
{
    public partial class Localisation : ContentPage
    {
        private BLEService _bleService;
        private MyTimer _myTimer;
        private MyTimer _myTimerAlarm;
        private Queue<double> _valuesRssi = new Queue<double>();
        private bool _isStarted;
        private bool _isAlarmStarted;
        private ISimpleAudioPlayer _player;
        private int _firstTimer;

        private const int MAX_RSSI_QUEUE = 5;

        public Localisation()
        {
            InitializeComponent();

            _bleService = BLEService.Instance();

            _isAlarmStarted = false;
            _isStarted = false;
            _firstTimer = 0;

            // Init player
            _player = CrossSimpleAudioPlayer.Current;
            _player.Load("bing.mp3");
            _player.Volume = 1;
        
            MessagingCenter.Subscribe<BLEService>(this, "deviceConnectionLost", (sender) =>
            {
                OnDeviceDisconnected();
            });

            MessagingCenter.Subscribe<BLEService, CharacteristicUpdatedEventArgs>(this, "characteristicUpdated", (sender, args) =>
            {
                OnCharacteristicUpdated(args);
            });
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

        async void ButtonStartLocationClicked(object sender, EventArgs e)
        {
            if (!_isStarted)
            {
                _isStarted = true;

                // Start update rssi
                CheckRssiLive();

                // Change style of button
                buttonStartLocation.BackgroundColor = Color.Transparent;
                buttonStartLocation.BorderColor = Color.White;
                buttonStartLocation.TextColor = Color.White;
                buttonStartLocation.BorderWidth = 1;
                buttonStartLocation.Text = "Arréter la localisation";

                statusLabel.Text = "En cours...";

                return;
            }

            // Stop timer
            _myTimer.Stop();

            // Reset list of values
            _valuesRssi.Clear();

            _isStarted = false;

            // Change style of button
            buttonStartLocation.BackgroundColor = Color.White;
            buttonStartLocation.BorderWidth = 0;
            buttonStartLocation.TextColor = Color.FromHex("#5A5A5A");
            buttonStartLocation.Text = "Démarrer la localisation";

            statusImage.Source = ImageSource.FromFile("northeastCompass.png");
            statusLabel.Text = "Inconnu";
        }

        async void AlerterClique(object sender, EventArgs args)
        {
            IService service = await _bleService.LoadService(_bleService._findMeConnected.Device, Guid.Parse("00001802-0000-1000-8000-00805f9b34fb"));
            IList<ICharacteristic> characteristics = await _bleService.LoadCharacteristicsFromService(service);

            if (_isAlarmStarted)
            {
                // Change style of button
                BoutonAlerter.BackgroundColor = Color.White;
                BoutonAlerter.BorderWidth = 0;
                BoutonAlerter.TextColor = Color.FromHex("#5A5A5A");
                BoutonAlerter.Text = "Alerter mon Find Me";

                _bleService.WriteValueAsyncInCharacteristic(new byte[] { 0x00 }, characteristics.First());
                _isAlarmStarted = false;

                _myTimerAlarm.Stop();
            }
            else if (!_isAlarmStarted)
            {
                // Change style of button
                BoutonAlerter.BackgroundColor = Color.Transparent;
                BoutonAlerter.BorderColor = Color.White;
                BoutonAlerter.TextColor = Color.White;
                BoutonAlerter.BorderWidth = 1;
                BoutonAlerter.Text = "Arreter alerte ";

                _bleService.WriteValueAsyncInCharacteristic(new byte[] { 0x01 }, characteristics.First());
                _isAlarmStarted = true;

                _myTimerAlarm = new MyTimer(TimeSpan.FromSeconds(5), ActionTimerAlarm);
                _myTimerAlarm.Start();
            }
        }

        private async void ActionTimerAlarm()
        {
            if (_firstTimer == 1)
            {
                _myTimerAlarm.Stop();
                _firstTimer = 0;
                _isAlarmStarted = false;

                // Change style of button
                BoutonAlerter.BackgroundColor = Color.White;
                BoutonAlerter.BorderWidth = 0;
                BoutonAlerter.TextColor = Color.FromHex("#5A5A5A");
                BoutonAlerter.Text = "Alerter mon Find Me";
            }

            _firstTimer++;
        }

         private void CheckRssiLive()
        {
            _myTimer = new MyTimer(TimeSpan.FromSeconds(1), ActionTimer);
            _myTimer.Start();
        }

        private async void ActionTimer()
        {
            double result = await _bleService.UpdateRssiDevice();

            // FIFO Queue
            if (_valuesRssi.Count == MAX_RSSI_QUEUE)
            {
                UpdateStatusImage(_valuesRssi.Average());
                _valuesRssi.Dequeue();
            }

            // Add new value in queue
            _valuesRssi.Enqueue(result);
        }

        private void UpdateStatusImage(double result)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (Math.Abs(result) < double.Epsilon)
                {
                    statusImage.Source = ImageSource.FromFile("northeastCompass.png");
                    statusLabel.Text = "Inconnu";
                }
                else if (result > 0 && result < 1)
                {
                    statusImage.Source = ImageSource.FromFile("hotLocation.png");
                    statusLabel.Text = "Brûlant";
                }
                else if (result > 1 && result < 3)
                {
                    statusImage.Source = ImageSource.FromFile("temperateLocation.png");
                    statusLabel.Text = "Tempéré";
                }
                else if (result > 3 && result < 100)
                {
                    statusImage.Source = ImageSource.FromFile("coldLocation.png");
                    statusLabel.Text = "froid";
                }
            });
        }

        async void OnDeviceDisconnected()
        {
            if (_myTimer != null)
            {
                // Stop timer update rssi
                _myTimer.Stop();
            }

            Device.BeginInvokeOnMainThread(async () => { await DisplayAlert("Déconnecté", "Votre Find Me s'est déconnecté. Il reviendra sûrement...", "Ok"); });

            // Unsubscribe events
            MessagingCenter.Unsubscribe<BLEService>(this, "deviceConnectionLost");
            MessagingCenter.Unsubscribe<BLEService>(this, "characteristicUpdated");

            

            // Set mainpage
            App.Current.MainPage = new Welcome_Confirmation();
        }
    }
}