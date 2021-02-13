using Android.App;
using Android.Content;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Gms.Drive;
using Android.Gms.Drive.Query;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PoCgDrive.Services
{
    public class GoogleDrive : Java.Lang.Object, IResultCallback
    {
        private bool _googleAccountPopupOpened;
        private const int RequestCodeGoogleDriveRetryConnection = 111;

        private GoogleApiClient _googleApiClient;
        private Fragment _fragment;
        private Activity _activity;
        private bool _fromActivity;        

        public GoogleDrive(Fragment fragment)
        {
            _fragment = fragment;
            _fromActivity = false;
        }

        public GoogleDrive(Activity activity)
        {
            _activity = activity;
            _fromActivity = true;
        }

        public void Connect()
        {
            _googleApiClient = new GoogleApiClient
                                    .Builder(_activity)
                                    .AddApi(DriveClass.API)
                                    .AddScope(DriveClass.ScopeFile)
                                    .AddScope(DriveClass.ScopeAppfolder)
                                    .UseDefaultAccount()
                                    .AddConnectionCallbacks(OnConnected, OnConnectionSuspended)
                                    .AddOnConnectionFailedListener(OnConnectionFailed)
                                    .Build();

            _googleApiClient.Connect();
        }

        public void OnConnected(Bundle connectionHint)
        {
            //AccountEmail = App.State.BackupAccountEmail;

            if (!_googleAccountPopupOpened)
            {
                _googleApiClient.ClearDefaultAccountAndReconnect();
            }
            else
            {
                //Connected?.Invoke(this, success: true);
            }
        }

        public void OnConnectionSuspended(int cause)
        {
            //Connected?.Invoke(this, success: false);
        }

        public void OnConnectionFailed(ConnectionResult result)
        {
            if (result.HasResolution)
            {
                try
                {
                    _googleAccountPopupOpened = true;

                    if (_fromActivity)
                    {
                        _activity.StartIntentSenderForResult(result.Resolution.IntentSender, RequestCodeGoogleDriveRetryConnection, null, 0, 0, 0, null);
                    }
                    else
                    {
                        _fragment.StartIntentSenderForResult(result.Resolution.IntentSender, RequestCodeGoogleDriveRetryConnection, null, 0, 0, 0, null);
                    }
                }
                catch (IntentSender.SendIntentException ex)
                {
                    //Connected?.Invoke(this, success: false);
                }
            }
            else
            {
                var activity = _fromActivity ? _activity : _fragment.Activity;
                GooglePlayServicesUtil.GetErrorDialog(result.ErrorCode, activity, 0).Show();
            }
        }

        public void OnActivityResult(int requestCode, Result resultCode, object data)
        {
            switch (requestCode)
            {
                case RequestCodeGoogleDriveRetryConnection:
                    if (resultCode == Result.Ok)
                    {
                        _googleApiClient.Connect();
                    }
                    break;

                default:
                    break;
            }
        }

        public async Task<BackupMetadata> GetMetadata()
        {
            return await GetMetadataFromDrive();

            //Metadata = await GetMetadataFromDrive();

            //var success = Metadata != null;

            //MetadataStatus?.Invoke(this, success);
        }

        #region Save routines

        private byte[] _fileToUpload;

        public void Save(byte[] file)
        {
            _fileToUpload = file;

            // This routine trigger the callback => IResultCallback.OnResult(Java.Lang.Object result)
            DriveClass.DriveApi.NewDriveContents(_googleApiClient).SetResultCallback(this);
        }

        //https://forums.xamarin.com/discussion/102462/help-with-code-to-connect-with-google-drive
        async void IResultCallback.OnResult(Java.Lang.Object result)
        {
            var contentResults = (result).JavaCast<IDriveApiDriveContentsResult>();
            await SavedResult(contentResults);
        }

        //https://forums.xamarin.com/discussion/102462/help-with-code-to-connect-with-google-drive
        public async Task SavedResult(IDriveApiDriveContentsResult contentResults)
        {
            if (!contentResults.Status.IsSuccess)
            {
                //Saved?.Invoke(this, success: false);
                return;
            }

            var metadata = await GetMetadataFromDrive();
            var fileExist = metadata?.MetadataGms != null;

            var success = true;

            if (fileExist)
            {
                success = await UpdateFile(metadata.MetadataGms);
            }
            else
            {
                success = await CreateFile(contentResults);
            }

            //Saved?.Invoke(this, success);
        }

        // https://stackoverflow.com/questions/45725225/how-to-query-an-idriveresource-in-google-drive-for-xamarin-android-using-the-dri
        private async Task<BackupMetadata> GetMetadataFromDrive(int retry = 6)
        {
            try
            {
                var fileName = GetFileName();
                var folder = GetFolder();

                var query = new QueryClass.Builder()
                                .AddFilter(Filters.Contains(SearchableField.Title, fileName))
                                .Build();

                using (var queryResult = await folder.QueryChildrenAsync(_googleApiClient, query))
                {
                    if (queryResult?.MetadataBuffer == null)
                    {
                        Thread.Sleep(150);
                        return retry > 0 ? await GetMetadataFromDrive(--retry) : new BackupMetadata();
                    }

                    if (queryResult.MetadataBuffer.Count > 1)
                    {
                        //LogEvent(nameof(GetMetadataFromDrive), new System.Exception($"Warning: {queryResult.MetadataBuffer.Count} backup files found."));
                    }

                    var result = queryResult
                                            .MetadataBuffer
                                            .Select(x => x as Metadata)
                                            .Where(x => x != null)
                                            .Select(x => new BackupMetadata(x))
                                            .Where(x => x != null)
                                            .Where(x => x.HasValue)
                                            .OrderByDescending(x => x.LastBackup)
                                            .FirstOrDefault();

                    if (result == null)
                    {
                        Thread.Sleep(150);
                        return retry > 0 ? await GetMetadataFromDrive(--retry) : new BackupMetadata();
                    }

                    return result;
                }
            }
            catch (System.Exception ex)
            {
                //LogEvent(nameof(GetMetadataFromDrive), ex);
            }

            return null;
        }

        private async Task<bool> UpdateFile(Metadata metadata)
        {
            try
            {
                var file = metadata.DriveId.AsDriveFile();
                var openMode = DriveFile.ModeWriteOnly;

                using (var driveFile = await file.OpenAsync(_googleApiClient, openMode, listener: null))
                {
                    try
                    {
                        //var content = await GetDatabaseContent();
                        //var contentZipped = await TryZip(content);
                        var contentZipped = _fileToUpload;
                        await driveFile.DriveContents.OutputStream.WriteAsync(contentZipped);

                        var changeSet = GetChangeSet();
                        await driveFile.DriveContents.CommitAsync(_googleApiClient, changeSet);

                        return true;
                    }
                    catch (System.Exception ex)
                    {
                        //LogEvent(nameof(UpdateFile), ex);
                        return false;
                    }
                }
            }
            catch (System.Exception ex)
            {
                //LogEvent(nameof(UpdateFile), ex);
                return false;
            }
        }

        private async Task<bool> CreateFile(IDriveApiDriveContentsResult driveFile)
        {
            try
            {
                //var content = await GetDatabaseContent();
                //var contentZipped = await TryZip(content);
                var contentZipped = _fileToUpload;
                await driveFile.DriveContents.OutputStream.WriteAsync(contentZipped);

                var changeSet = GetChangeSet();
                var folder = GetFolder();

                await folder.CreateFileAsync(_googleApiClient, changeSet, driveFile.DriveContents);

                return true;
            }
            catch (System.Exception ex)
            {
                //LogEvent(nameof(CreateFile), ex);
                return false;
            }
        }

        #endregion

        #region Restore routines

        public async Task<byte[]> Restore()
        {
            return await GetFileContent();
        }

        // https://stackoverflow.com/questions/45725225/how-to-query-an-idriveresource-in-google-drive-for-xamarin-android-using-the-dri
        private async Task<byte[]> GetFileContent()
        {
            try
            {
                var metadata = await GetMetadataFromDrive();

                if (metadata?.MetadataGms == null)
                {
                    return null;
                }

                var file = metadata.MetadataGms.DriveId.AsDriveFile();
                var openMode = DriveFile.ModeReadOnly;

                using (var driveFile = await file.OpenAsync(_googleApiClient, openMode, listener: null))
                {
                    try
                    {
                        var content = await ReadFully(driveFile.DriveContents.InputStream);

                        //var contentUnzipped = await TryUnzip(content);
                        var contentUnzipped = content;

                        return contentUnzipped;
                    }
                    catch (System.Exception ex)
                    {
                        //LogEvent(nameof(GetFileContent), ex);
                    }
                }
            }
            catch (System.Exception ex)
            {
                //LogEvent(nameof(GetFileContent), ex);
            }

            return null;
        }

        private async Task<byte[]> ReadFully(Stream input)
        {
            using (var ms = new MemoryStream())
            {
                await input.CopyToAsync(ms);
                return ms.ToArray();
            }
        }

        #endregion

        private IDriveFolder GetFolder()
        {
            return DriveClass.DriveApi.GetRootFolder(_googleApiClient);
        }

        private string GetFileName()
        {
            return "sample_on_drive.txt";
        }

        private MetadataChangeSet GetChangeSet()
        {
            var fileName = GetFileName();
            var changeSet = new MetadataChangeSet.Builder()
                   .SetTitle(fileName)
                   .Build();
            return changeSet;
        }
    }
}