using System;
using Xamarin.Forms;

namespace FindMe
{
    public partial class App : Application
    {
        public static bool IsInForeground { get; set; } = false;
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new Welcome());
        }

        protected override void OnStart()
        {
            IsInForeground = true;
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            IsInForeground = false;
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            IsInForeground = true;
            // Handle when your app resumes
        }
    }
}
