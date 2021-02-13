using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Common.Apis;
using Java.Lang;
using System;
using Android.Content;

namespace PoCgDrive.Services
{
    public class GoogleOAuth : Java.Lang.Object, Android.Gms.Tasks.IOnSuccessListener
    {
        private const int RequestCodeGoogleSignIn = 112;
        private bool _googleAccountPopupOpened;
        private GoogleSignInClient _signInClient;
        private Fragment _fragment;
        private Activity _activity;
        private bool _fromActivity;

        public delegate void EventHandler(object sender, bool success);
        public event EventHandler Connected;

        public GoogleOAuth(Fragment fragment)
        {
            _fragment = fragment;
            _fromActivity = false;
        }

        public GoogleOAuth(Activity activity)
        {
            _activity = activity;
            _fromActivity = true;
        }

        public void Connect()
        {
            _signInClient = BuildSignInClient();

            if (_fromActivity)
            {
                _activity.StartActivityForResult(_signInClient.SignInIntent, RequestCodeGoogleSignIn);
            }
            else
            {
                _fragment.StartActivityForResult(_signInClient.SignInIntent, RequestCodeGoogleSignIn);
            }
        }

        public void Disconnect()
        {
            _googleAccountPopupOpened = false;

            if (_signInClient == null)
            {
                _signInClient = BuildSignInClient();
            }

            _signInClient.SignOut();
            //_googleApiClient.Disconnect();

            //App.State.BackupAccountEmail = string.Empty;
            //App.Settings.SaveSessionState();

            //Disconnected?.Invoke(this, success: true);
        }

        private GoogleSignInClient BuildSignInClient()
        {
            var signInOptions = new GoogleSignInOptions
                                       .Builder(GoogleSignInOptions.DefaultSignIn)
                                       //.RequestScopes(DriveClass.ScopeFile, DriveClass.ScopeAppfolder)
                                       .RequestProfile()
                                       .RequestEmail()
                                       .Build();

            var activity = _fromActivity ? _activity : _fragment.Activity;
            return GoogleSignIn.GetClient(activity, signInOptions);
        }

        public void OnSuccess(Java.Lang.Object result)
        {
            var googleAccount = result as GoogleSignInAccount;

            if (googleAccount != null)
            {
                _googleAccountPopupOpened = true;

                //TODO: pegar token, pois vai precisar ser validado do lado do server, pq senao qualquer um pode acessar os dados do outro

                //App.State.BackupAccountEmail = googleAccount.Email;
                //App.Settings.SaveSessionState();

                //ConnectDrive();
            }
            else
            {
                Connected?.Invoke(this, success: false);
            }
        }

        public void OnActivityResult(int requestCode, Result resultCode, object data)
        {
            switch (requestCode)
            {
                case RequestCodeGoogleSignIn:
                    if (resultCode == Result.Ok)
                    {
                        GoogleSignIn
                                .GetSignedInAccountFromIntent(data as Intent)
                                .AddOnSuccessListener(this);
                    }
                    break;

                default:
                    break;
            }
        }
    }
}