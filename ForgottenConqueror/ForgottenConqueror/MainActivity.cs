using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Widget;
using Realms;
using static ForgottenConqueror.DB;

namespace ForgottenConqueror
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Toast.MakeText(this, "Long-press the homescreen to add the widget", ToastLength.Long).Show();
            
            Realm realm = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
            realm.Write(() => realm.RemoveAll());
            Data.Instance.Write(this, Data.IsFirstUpdate, true);

            Finish();
        }
    }
}