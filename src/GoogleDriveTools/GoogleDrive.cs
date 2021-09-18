using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GoogleDriveTools
{
    public class GoogleDrive
    {
        private DriveService _service = null;
        private DriveService Service
        {
            get
            {
                if (_service == null)
                {
                    _service = GetService();
                }
                return _service;
            }
        }
        private DriveService GetService()
        {
            var config = _options.Value;
            var tokenResponse = new TokenResponse
            {
                AccessToken = config.AccessToken,
                RefreshToken = config.RefreshToken,
            };


            var applicationName = config.ApplicationName; // Use the name of the project in Google Cloud
            var username = config.Username;// Use your email


            var apiCodeFlow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = config.ClientId,
                    ClientSecret = config.ClientSecret
                },
                Scopes = new[] { DriveService.Scope.Drive },
                DataStore = new FileDataStore(applicationName)
            }); ;


            var credential = new UserCredential(apiCodeFlow, username, tokenResponse);


            var service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName
            });
            return service;
        }
        private readonly IOptionsSnapshot<GoogleDriveConfig> _options;
        public GoogleDrive(IOptionsSnapshot<GoogleDriveConfig> options)
        {
            _options = options;
        }

        public async Task<string> CreateFolder(string parent, string folderName)
        {
            var folder = new Google.Apis.Drive.v3.Data.File
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder",
                Parents = new[] { parent }
            };
            var command = Service.Files.Create(folder);
            var result = await command.ExecuteAsync().ConfigureAwait(false);
            return result.Id;
        }

        public async Task<string> UploadFile(Stream fileStream, string fileName, string fileMime, string folder, string fileDescription)
        {
            var file = new Google.Apis.Drive.v3.Data.File
            {
                Name = fileName,
                Description = fileDescription,
                MimeType = fileMime,
                Parents = new[] { folder }
            };
            var request = Service.Files.Create(file, fileStream, fileMime);
            request.Fields = "id";

            var response = await request.UploadAsync().ConfigureAwait(false);
            if (response.Status != Google.Apis.Upload.UploadStatus.Completed)
            {
                throw response.Exception;
            }
            return request.ResponseBody.Id;
        }

        public Task<string> DeleteFile(string fileId)
        {
            var request = Service.Files.Delete(fileId);
            return request.ExecuteAsync();
        }

        public async IAsyncEnumerable<Google.Apis.Drive.v3.Data.File> GetFiles(string folder)
        {
            var request = Service.Files.List();
            request.Q = $"mimeType!='application/vnd.google-apps.folder' and '{folder}' in parents";
            request.Fields = "nextPageToken, files(id, name, size, mimeType)";

            string pageToken = null;
            do
            {
                request.PageToken = pageToken;
                var result = await request.ExecuteAsync().ConfigureAwait(false);
                var files = result.Files;
                pageToken = result.NextPageToken;
                foreach (var file in files)
                {
                    yield return file;
                }
            } while (pageToken != null);
        }
    }
}
