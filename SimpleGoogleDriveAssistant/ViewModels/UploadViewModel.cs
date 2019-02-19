using System.Windows.Data;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using SimpleGoogleDriveAssistant.Models;
using SimpleGoogleDriveAssistant.Views;
using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using GalaSoft.MvvmLight.Command;
using System.ComponentModel;
using GalaSoft.MvvmLight;

namespace SimpleGoogleDriveAssistant.ViewModels
{
    class UploadViewModel : ViewModelBase
    {
        private UploadFileAccess _uploadFileAccess;
        
        public UploadViewModel()
        {
            _uploadFileAccess = new UploadFileAccess();
            _uploadFileAccess.Init();

            ButtonsEnabled = true;
            ProgressBarVisible = Visibility.Hidden;
        }

        /// <summary>
        /// Свойство индикации прогресс бара.
        /// </summary>
        private double _progressIndicator;
        public double ProgressIndicator
        {
            get { return _progressIndicator; }
            set { _progressIndicator = value; RaisePropertyChanged(nameof(ProgressIndicator)); }
        }

        /// <summary>
        /// Свойство видимости прогресс бара.
        /// </summary>
        private Visibility _progressBarVisible;
        public Visibility ProgressBarVisible
        {
            get { return _progressBarVisible; }
            set { _progressBarVisible = value; RaisePropertyChanged(nameof(ProgressBarVisible)); }
        }

        /// <summary>
        /// Свойство доступности всех кнопок на форме.
        /// </summary>
        private bool _buttonsEnabled;
        public bool ButtonsEnabled
        {
            get { return _buttonsEnabled; }
            set { _buttonsEnabled = value; RaisePropertyChanged(nameof(ButtonsEnabled)); }
        }

        /// <summary>
        /// Получение данных из xml файла со списком добавленых туда каталогов, для заполнения таблицы.
        /// </summary>
        private ObservableCollection<CatalogModel> GetData
        {
            get
            {
                return new ObservableCollection<CatalogModel>(_uploadFileAccess.GetData());
            }
        }
        public ICollectionView SourceItems
        {
            get
            {
                return CollectionViewSource.GetDefaultView(GetData);
            }
        }

        /// <summary>
        /// Удаление выделенных в таблице файлов с GoogleDrive.
        /// </summary>
        public RelayCommand<Collection<object>> DeleteRowCommand
        {
            get
            {
                return new RelayCommand<Collection<object>>((selectedItems) =>
                {
                    try
                    {
                        var list = selectedItems.Cast<CatalogModel>().ToList();

                        string noteWord = list.Count > 1 ? list.Count >= 5 ? "записей" : "записи" : "запись";
                        string deleteWord = list.Count > 1 ? "удалены" : "удалена";
                        string warningWords = list.Count > 1 ? "Данные об этих каталогах будут удалены окончательно" : "Данные об этом каталоге будут удалены окончательно";

                        var result = MessageBox.Show($"{warningWords}.\nВы уверены что хотите удалить {list.Count} {noteWord}? ", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

                        if (result == MessageBoxResult.Yes)
                        {
                            _uploadFileAccess.DeleteCatalogs(list);
                            MessageBox.Show($"{list.Count} {noteWord} {deleteWord}.", "Запись удалена");
                            RaisePropertyChanged(nameof(SourceItems));
                        }
                    }
                    catch (Exception)
                    {
                        MessageBox.Show($"Снимите выделение с последней строки.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
        }

        /// <summary>
        /// Открытие окна добавления новой ссылки на каталог который нужно загрузить.
        /// </summary>
        public RelayCommand AddFilesCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    var AddCatalogDialog = new AddCatalogView();
                    AddCatalogDialog.Owner = Application.Current.MainWindow;
                    AddCatalogDialog.ShowDialog();
                    RaisePropertyChanged(nameof(SourceItems));
                });
            }
        }

        /// <summary>
        /// Загрузить каталог в GoogleDrive.
        /// </summary>
        public RelayCommand<CatalogModel> UploadFilesCommand
        {
            get
            {
                return new RelayCommand<CatalogModel>(async (item) =>
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            ProgressBarVisible = Visibility.Visible;
                            ButtonsEnabled = false;

                            string[] filesPath = Directory.GetFiles(item.CatalogPath);
                            double progressIter = 100.0 / (filesPath.Length + 2.0);

                            // Получаем id папки игры в Google Drive.
                            var gameFolderId = GoogleDriveAccess.GetFolderId(item.Name);
                            ProgressIndicator += progressIter;

                            // Загрузка файлов в папку игры в Google Drive.
                            foreach (var filePath in filesPath)
                            {
                                GoogleDriveAccess.UploadFile(gameFolderId, filePath);
                                ProgressIndicator += progressIter;
                            }
                            Thread.Sleep(500); // Небольшая задержка, чтобы прогресс бар на легких операциях типа обновления даты был чуть плавнее.

                            // Обновление даты последней загрузки файлов в Google Drive.
                            _uploadFileAccess.ChangeUploadTime(item);
                            ProgressIndicator += progressIter;

                            Thread.Sleep(500);
                            RaisePropertyChanged(nameof(SourceItems));

                            ProgressIndicator = 0;
                            ProgressBarVisible = Visibility.Hidden;
                            ButtonsEnabled = true;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            ProgressBarVisible = Visibility.Hidden;
                            ButtonsEnabled = true;
                        }

                    });
                });
            }
        }
    }
}
