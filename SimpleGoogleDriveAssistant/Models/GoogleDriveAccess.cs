using SimpleGoogleDriveAssistant.Properties;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Google.Apis.Download;

namespace SimpleGoogleDriveAssistant.Models
{
    public static class GoogleDriveAccess
    {
        static string[] Scopes = { DriveService.Scope.Drive };
        
        // Взять из https://developers.google.com/drive/api/v3/quickstart/dotnet
        static string clientId = ""; 
        static string clientSecret = "";

        static string ApplicationName = "Simple Google Drive Assistant";

        public static string GetFolderId(string folderName)
        {
            DriveService service = GetService();

            // Параметры для запроса по поиску файлов.
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.Fields = "nextPageToken, files(id, name, mimeType)";
            listRequest.Q = $"mimeType = 'application/vnd.google-apps.folder' and name = '{folderName}' and trashed = false and '{GetRootFolderId(service)}' in parents";
  
            var request = listRequest.Execute().Files; // Список найденых файлов.

            if (request.Count == 0)
            {
                CreateFolder($"{folderName}", service);
                request = listRequest.Execute().Files;
            }

            return request[0].Id;
        }

        public static IEnumerable<Google.Apis.Drive.v3.Data.File> GetFilesList(string folderId)
        {
            DriveService service = GetService();
            string folderID;

            // Не стал особо разбираться и налепил костылей. Вообще надо переделать функцию GetRootFolderId и использовать публично. Но уже лень.
            if (folderId == "root")
            {
                folderID = GetRootFolderId(service);
            }
            else
            {
                folderID = folderId;
            }

            // Параметры для запроса по поиску файлов.
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.Fields = "nextPageToken, files(id, name, mimeType, modifiedTime)";
            listRequest.Q = $"'{folderID}' in parents and trashed = false";

            var request = listRequest.Execute().Files; // Список найденых файлов.

            return request;
        }

        public static void UploadFile(string folderId, string path)
        {
            DriveService service = GetService();
            
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = Path.GetFileName(path),
                Parents = new List<string>
                {
                    folderId
                }
            };

            var folderFileId = GetFolderFiles(service, folderId, Path.GetFileName(path)); // Проверяю, есть ли этот файл на Google Drive.

            // Обновление файла если он существует.
            if (folderFileId.Count > 0) 
            {
                service.Files.Delete(folderFileId[0].Id).Execute(); // Просто обновить файл не получилось. Удаляю если файл с таким именем уже есть.
            }

            FilesResource.CreateMediaUpload request;

            using (var stream = new FileStream(path, FileMode.Open))
            {
                request = service.Files.Create(fileMetadata, stream, fileMetadata.MimeType);
                request.Fields = "id";
                request.Upload();
            }

            var file = request.ResponseBody;                                
        }

        public static void DeleteFiles(List<Google.Apis.Drive.v3.Data.File> listFiles)
        {
            DriveService service = GetService();

            foreach (var file in listFiles)
            {
                service.Files.Delete(file.Id).Execute();
            }
        }

        public static void DownloadFile(string fileId, string saveTo)
        {
            DriveService service = GetService();
            var request = service.Files.Get(fileId);
            var stream = new System.IO.MemoryStream();

            request.MediaDownloader.ProgressChanged += (IDownloadProgress progress) =>
            {
                switch (progress.Status)
                {
                    case DownloadStatus.Downloading:
                        {
                            Console.WriteLine(progress.BytesDownloaded);
                            break;
                        }
                    case DownloadStatus.Completed:
                        {
                            Console.WriteLine("Download complete.");
                            SaveStream(stream, saveTo);
                            break;
                        }
                    case DownloadStatus.Failed:
                        {
                            Console.WriteLine("Download failed.");
                            break;
                        }
                }};

            request.Download(stream);
        }

      
        private static DriveService GetService()
        {
            var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    new ClientSecrets
                    {
                        ClientId = clientId,
                        ClientSecret = clientSecret
                    },
                    Scopes,
                    Environment.UserName,
                    CancellationToken.None,
                    new FileDataStore(Resources.ApplicationName)).Result;

            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            return service;
        }

        private static string GetRootFolderId(DriveService service)
        {
            // Параметры для запроса по поиску файлов.
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.Fields = "nextPageToken, files(id, name, mimeType)";          
            listRequest.Q = $"mimeType = 'application/vnd.google-apps.folder' and name = '{ApplicationName}' and trashed = false"; // Тип - папка, имя - GameSaves, не находится в корзине.
          
            var request = listRequest.Execute().Files; // Список найденых файлов.

            if (request.Count == 0)
            {
                CreateFolder(ApplicationName, service);
                request = listRequest.Execute().Files;
            }   

            return request[0].Id;
        }

        private static void CreateFolder(string folderName, DriveService service)
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder"
            };

            if (folderName != ApplicationName)
            {
                fileMetadata.Parents = new List<string>
                {
                    GetRootFolderId(service)
                };
            }

            var request = service.Files.Create(fileMetadata);
            request.Fields = "id";
            var file = request.Execute();
        }

        private static IList<Google.Apis.Drive.v3.Data.File> GetFolderFiles(DriveService service, string folderId, string fileName)
        {
            // Параметры для запроса по поиску файлов.
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.Q = $"'{folderId}' in parents and name = '{fileName}' and trashed = false";
            listRequest.Fields = "nextPageToken, files(id, name)";

            return listRequest.Execute().Files; // Список найденых файлов. В идеале там должен быть только один элемент.
        }

        private static void SaveStream(MemoryStream stream, string saveTo)
        {
            using (FileStream file = new FileStream(saveTo, FileMode.Create, FileAccess.Write))
            {
                stream.WriteTo(file);
            }
        }
    }
}
