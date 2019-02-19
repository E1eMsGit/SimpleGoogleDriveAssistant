using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Windows.Forms;
using System.IO;
using SimpleGoogleDriveAssistant.Models;
using System.Windows;

namespace SimpleGoogleDriveAssistant.ViewModels
{
    class AddCatalogViewModel : ViewModelBase
    {
        private string _catalogPath;

        public AddCatalogViewModel()
        {
            AddCommand = new RelayCommand<Window>(OnAddCommand, o => CustomCatalogName.Length > 0 && CatalogName != "Каталог не выбран");

            CatalogName = "Каталог не выбран";
            CustomCatalogName = string.Empty;
            NameResultVisibility = Visibility.Hidden;
        }

        /// <summary>
        /// Имя выбранного каталога.
        /// </summary>
        private string _catalogName;
        public string CatalogName
        {
            get => _catalogName;
            set
            {
                _catalogName = value;
                RaisePropertyChanged(nameof(CatalogName));
            }
        }

        /// <summary>
        /// Имя каталога которое будет отображено в таблице и в GoogleDrive.
        /// </summary>
        private string _customCatalogName;
        public string CustomCatalogName 
        {
            get => _customCatalogName;
            set
            {
                _customCatalogName = value;
                NameResultVisibility = Visibility.Hidden;
                AddCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Свойство видимости предупреждения о том что такое имя уже используется.
        /// </summary>
        private Visibility _nameResultVisibility; 
        public Visibility NameResultVisibility
        {
            get => _nameResultVisibility;
            set
            {
                _nameResultVisibility = value;
                RaisePropertyChanged(nameof(NameResultVisibility));
            }
        }

        /// <summary>
        /// Команда открытия окна выбора каталога.
        /// </summary>
        public RelayCommand OpenFileCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    using (var fbd = new FolderBrowserDialog())
                    {
                        fbd.Description = "Выберите каталог";

                        DialogResult result = fbd.ShowDialog();

                        if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                        {
                            _catalogPath = fbd.SelectedPath;
                            CatalogName = $".../{Path.GetFileName(fbd.SelectedPath)}";
                            AddCommand.RaiseCanExecuteChanged();
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Команда добавления выбранного каталога в таблицу.
        /// </summary>
        public RelayCommand<Window> AddCommand { get; }
        private void OnAddCommand(Window window)
        {
            var catalog = new CatalogModel
            {
                Name = CustomCatalogName,
                UploadTime = "Не загружалось",
                CatalogPath = _catalogPath
            };

            var _uploadFileAccess = new UploadFileAccess();
            var itemsNames = _uploadFileAccess.GetDataNames();

            if (itemsNames.Contains(catalog.Name))
            {
                NameResultVisibility = Visibility.Visible;
            }
            else
            {
                _uploadFileAccess.AddCatalog(catalog);
                window.Close();
            }
        }
    }
}
