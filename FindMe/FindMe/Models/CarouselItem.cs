using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace FindMe.Models
{
    public class CarouselItem: INotifyPropertyChanged
    {
        public string ImageSource { get; set; }
        public string LabelFrame { get; set; }
        public string ButtonText { get; set; }
        public ICommand CommandeBouton { get; set;}

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}