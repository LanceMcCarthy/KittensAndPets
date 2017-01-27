using Windows.UI.Xaml.Controls;
using KittensAndPets.ViewModels;

namespace KittensAndPets.Views
{
    public sealed partial class DashboardPage : Page
    {
        MainViewModel ViewModel => this.DataContext as MainViewModel;

        public DashboardPage()
        {
            this.InitializeComponent();
        }
    }
}
