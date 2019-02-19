using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SimpleGoogleDriveAssistant.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace SimpleGoogleDriveAssistant.ViewModels
{
    class DownloadViewModel : ViewModelBase
    {
        public DownloadViewModel()
        {
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
        /// Получение данных (Список файлов) из GoogleDrive, для заполнения таблицы.
        /// </summary>
        private ObservableCollection<Google.Apis.Drive.v3.Data.File> GetData
        {
            get
            {
                return new ObservableCollection<Google.Apis.Drive.v3.Data.File>(GoogleDriveAccess.GetFilesList("root"));
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
                        var list = selectedItems.Cast<Google.Apis.Drive.v3.Data.File>().ToList();

                        string noteWord = list.Count > 1 ? list.Count >= 5 ? "файлов" : "файла" : "файл";
                        string deleteWord = list.Count > 1 ? "удалены" : "удален";
                        string warningWords = list.Count > 1 ? "Эти файлы будут удалены из GoogleDrive окончательно" : "Этот файл будет удален из GoogleDrive окончательно";

                        var result = MessageBox.Show($"{warningWords}.\nВы уверены что хотите удалить {list.Count} {noteWord}? ", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

                        if (result == MessageBoxResult.Yes)
                        {
                            GoogleDriveAccess.DeleteFiles(list);
                            MessageBox.Show($"{list.Count} {noteWord} {deleteWord}.", "Файл удален");
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
        /// Скачивание каталога с GoogleDrive.
        /// </summary>
        /// <remarks>
        /// Скачивание именно каталога обусловлено тем что, загрузку в GoogleDrive я сделал только каталогов.
        /// Если потребуется то надо просто добавить возможность загружать на диск обычные файлы, а при скачивании 
        /// сделать проверку if-else на то, каталог это или нет, через MimeType.
        /// </remarks>
        public RelayCommand<Google.Apis.Drive.v3.Data.File> DownloadFilesCommand
        {
            get
            {
                return new RelayCommand<Google.Apis.Drive.v3.Data.File>(async (item) =>
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            ProgressBarVisible = Visibility.Visible;
                            ButtonsEnabled = false;

                            string downloadsPath = KnownFolders.GetPath(KnownFolder.Downloads); // Каталог "Загрузки".
                            string appCatalog = downloadsPath + "\\" + "GoogleDrive Files"; // Основной каталог в каталоге "Загрузки".
                            string dlCatalog = appCatalog + "\\" + item.Name; // Скачиваемый каталог.

                            var dlFilesList = GoogleDriveAccess.GetFilesList(item.Id); // Список файлов которые находятся в выбраном каталоге.
                            double progressIter = 100.0 / (dlFilesList.Count() + 1.0); // Считаем итерации для прогресс бара (+ 1 - итерация создания или проверки наличия каталога).

                            // Папка приложения в папке "Загрузки".
                            if (Directory.Exists(appCatalog) == false) { Directory.CreateDirectory(appCatalog); }

                            // Скачиваемый каталог в папке "Загрузки".
                            if (Directory.Exists(dlCatalog) == false) { Directory.CreateDirectory(dlCatalog); }

                            // Скачивает только каталоги. Не стал заморачиваться с обычными файлами т.к. не сделал возможность загружать
                            // что то кроме каталогов. Если что, просто сделать проверку на то что скачиваешь, каталог или нет через if - else.
                            foreach (var file in dlFilesList)
                            {
                                GoogleDriveAccess.DownloadFile(file.Id, string.Format(@"{0}\{1}", dlCatalog, file.Name));
                                ProgressIndicator += progressIter;
                            }

                            Thread.Sleep(500); // Небольшая задержка, чтобы прогресс бар на легких операциях типа обновления даты был чуть плавнее.
                            
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

        /// <summary>
        /// Обновить таблицу файлов с GoogleDrive.
        /// </summary>
        public RelayCommand RefreshFileListCommand
        {
            get
            {
                return new RelayCommand(() =>
                {                   
                    RaisePropertyChanged(nameof(SourceItems));
                });
            }
        }
    }
}
