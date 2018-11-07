using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Widget;
using Realms;
using System.Collections.Generic;
using static Android.Content.PM.LaunchMode;
using static ForgottenConqueror.DB;

namespace ForgottenConqueror
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, LaunchMode = SingleTop)]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Toast.MakeText(this, "Long-press the homescreen to add the widget", ToastLength.Long).Show();
            
            FinishAndRemoveTask();
        }
    }
}