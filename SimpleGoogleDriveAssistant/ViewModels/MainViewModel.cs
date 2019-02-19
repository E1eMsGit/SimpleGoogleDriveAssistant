using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SimpleGoogleDriveAssistant.Models;
using SimpleGoogleDriveAssistant.Views;
using System.ComponentModel;
using System.Windows;

namespace SimpleGoogleDriveAssistant.ViewModels
{
    class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            MenuItems = new[]
            {
                new MenuItemModel("в GoogleDrive", new UploadView { DataContext = new UploadViewModel() }, "Загрузить в GoogleDrive"),
                new MenuItemModel("из GoogleDrive", new DownloadView { DataContext = new DownloadViewModel() }, "Загрузить из GoogleDrive")
            };
        }

        /// <summary>
        /// Команда закрытия программы.
        /// </summary>
        public RelayCommand<CancelEventArgs> ClosingProgramCommand
        {
            get
            {
                return new RelayCommand<CancelEventArgs>((e) =>
                {
                    var result = MessageBox.Show("Вы действительно хотите закрыть программу?", "Выход", MessageBoxButton.YesNo);

                    if (result == MessageBoxResult.Yes)
                    {
                        Application.Current.Shutdown();
                    }
                    else
                    {
                        e.Cancel = true;
                    }
                });
            }
        }

        /// <summary>
        /// Элементы меню навигации.
        /// </summary>
        public MenuItemModel[] MenuItems { get; }

    }
}
