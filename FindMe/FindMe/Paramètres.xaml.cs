using System;
using System.Collections.Generic;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace FindMe
{
    public partial class Paramètres : ContentPage
    {
        public Paramètres()
        {
            InitializeComponent();

        }

        void VibrationSwitch(object sender, EventArgs args)
        {
            Vibration.Vibrate();
        }

        void AlertSwitch(object sender, EventArgs args)
        {
            Vibration.Vibrate();
        }

        void SauveguarderClique(object sender, EventArgs args)
        {
            DisplayAlert ("Confirmation", "Les paramètres sont enregistrés avec succès", "OK");
            Vibration.Vibrate();
        }

        void DéconnecterClique(object sender, EventArgs args)
        {
             DisplayAlert("Deconnexion", "l'appareil est maintenant deconnecté", "OK");
            App.Current.MainPage = new NavigationPage(new Welcome());
        }
    }
}
