using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using FindMe.Models;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using Xamarin.Forms;

namespace FindMe
{
    public partial class Welcome : ContentPage
    {
        public ICommand NextPositionCommand { set; get; }
        public ICommand AllowCommand { set; get; }
        public ICommand ConnectFindMeCommand { get; set; }

        public Welcome()
        {
            InitializeComponent();

            NavigationPage.SetHasNavigationBar(this, false);

            NextPositionCommand = new Command(NextPosition);
            AllowCommand = new Command(Allow);
            ConnectFindMeCommand = new Command(ConnectFindMe);

            Carousel.ItemsSource = new List<CarouselItem>()
            {
                new CarouselItem{ImageSource="lighthouse_on",LabelFrame="Suivez les étapes afin de configurer votre Find Me. Vous pourrez revenir sur vos choix ultérieurement.",ButtonText="Suivant",CommandeBouton=NextPositionCommand},
                new CarouselItem{ImageSource="lockOutlinedPadlockSymbolForSecurityInterface",LabelFrame="Votre Find Me a besoin d’accéder à votre position pour fonctionner correctement.",ButtonText="Autoriser la localisation",CommandeBouton=AllowCommand},
                new CarouselItem{ImageSource="hand-finger-pressing-a-circular-ring-button",LabelFrame="Appuyez sur le bouton de votre Find Me puis connecter le en appuyant sur le bouton ci-dessous.",ButtonText="Connecter mon Find Me",CommandeBouton=ConnectFindMeCommand}
            };
        }

        public Welcome(object p)
        {
        }

        public void NextPosition()
        {
            Carousel.Position = 1;
        }

        async public void Allow()
        {
            try
            {
                var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Location);

                if (status != PermissionStatus.Granted)
                {
                    if (await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(Permission.Location))
                    {
                        await DisplayAlert("Localisation", "Votre Find Me a besoin d’accéder à votre position pour fonctionner correctement.", "Ok");
                    }

                    var results = await CrossPermissions.Current.RequestPermissionsAsync(Permission.Location);

                    status = results[Permission.Location];
                }

                if (status == PermissionStatus.Granted)
                {
                    await DisplayAlert("Localisation", "Votre Find Me dispose de l'autorisation pour vous localiser.", "Ok");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error request location permission : " + ex.Message);
            }
        }

        public async void ConnectFindMe()
        {
            await Navigation.PushModalAsync(new Welcome_Confirmation());
        }
    }
}