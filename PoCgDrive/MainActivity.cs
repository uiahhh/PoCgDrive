﻿using System;
using System.IO;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using PoCgDrive.Services;

namespace PoCgDrive
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private GoogleOAuth _oauth;
        private GoogleDrive _drive;
        private bool _connected;

        //clientid 892288556023-j6qknjd8jlgteb5sv9fcj0pvlierlrl7.apps.googleusercontent.com
        //keystore password 123456

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            var swt_connect = FindViewById<Switch>(Resource.Id.swt_connect);
            swt_connect.Click += Swt_connect_Click;

            var btn_upload = FindViewById<Button>(Resource.Id.btn_upload);
            btn_upload.Click += Btn_upload_Click;

            var btn_download = FindViewById<Button>(Resource.Id.btn_download);
            btn_download.Click += Btn_download_Click;

            var btn_getfilemetadata = FindViewById<Button>(Resource.Id.btn_getfilemetadata);
            btn_getfilemetadata.Click += Btn_getfilemetadata_Click;

            _oauth = new GoogleOAuth(this);
            _drive = new GoogleDrive(this);            
        }

        private async void Btn_getfilemetadata_Click(object sender, EventArgs e)
        {
            var metadata = await _drive.GetMetadata();

            var txt_metadata = FindViewById<Button>(Resource.Id.txt_metadata);
            txt_metadata.Text = $"File properties: size {metadata.GetSizeDescription()}";
        }

        private void Swt_connect_Click(object sender, EventArgs e)
        {
            if (_connected)
            {
                Toast.MakeText(this, $"Disconnecting...", ToastLength.Long).Show();

                _oauth.Disconnect();
            }
            else
            {
                Toast.MakeText(this, $"Connecting...", ToastLength.Long).Show();

                _oauth.Connect();                
            }

            _connected = !_connected;
        }

        private void Btn_upload_Click(object sender, EventArgs e)
        {
            Toast.MakeText(this, $"Uploading...", ToastLength.Long).Show();

            var file = GetFile();

            _drive.Connect();
            _drive.Save(file);
        }

        private byte[] GetFile()
        {
            using (var stream = this.Assets.Open("sample.txt"))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }

        private async void Btn_download_Click(object sender, EventArgs e)
        {
            Toast.MakeText(this, "Downloading...", ToastLength.Long).Show();

            var dataBytes = await _drive.Restore();
            var dataText = string.Empty;

            using (var stream = new MemoryStream(dataBytes))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        dataText += line;
                    }
                }
            }

            var txt_data = FindViewById<Button>(Resource.Id.txt_data);
            txt_data.Text = $"File content: {dataText}";
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            _oauth.OnActivityResult(requestCode, resultCode, data);
            _drive.OnActivityResult(requestCode, resultCode, data);
        }

        #region generated by xamarin template

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        #endregion
    }
}
