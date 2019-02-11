using System;
using System.Linq;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Widget;
using static Android.Content.PM.LaunchMode;
using Realms;
using static ForgottenConqueror.DB;
using Uri = Android.Net.Uri;
using System.Collections.Generic;
using R = Android.Resource;
using FR.Ganfra.Materialspinner;

namespace ForgottenConqueror
{
    [BroadcastReceiver(Label = "Last Chapter")]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/appwidgetprovider__small")]
    class WidgetSmall : AppWidgetProvider
    {
        private readonly static string OpenChapterClick = "OpenChapterClick";
        private readonly static string RefreshClick = "RefreshClick";
        private readonly static int[] Layouts =
        {
            Resource.Layout.widget__small,
            Resource.Layout.widget__small_1cell,
            Resource.Layout.widget__small_2cell,
            Resource.Layout.widget__small_3cell,
        };
        private readonly static int[] LayoutsRefreshing =
        {
            Resource.Layout.widget__small_progress,
            Resource.Layout.widget__small_1cell_progress,
            Resource.Layout.widget__small_2cell_progress,
            Resource.Layout.widget__small_3cell_progress,
        };

        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            Realm realm = Realm.GetInstance(DB.RealmConfiguration);
            foreach (int appWidgetId in appWidgetIds)
            {
                WidgetParams widgetParams = realm.Find<WidgetParams>(appWidgetId);
                if (widgetParams == null)
                {
                    // Widget created
                    realm.Write(() =>
                    {
                        widgetParams = new WidgetParams()
                        {
                            ID = appWidgetId,
                            IsRefreshing = true,
                            Cells = 3,
                            DateFormat = "dd/MM/yyyy H:mm:ss",
                        };
                        realm.Add<WidgetParams>(widgetParams);
                    });
                }
                else if (widgetParams.IsRefreshing)
                {
                    // Already updating
                    continue;
                }
                else realm.Write(() => widgetParams.IsRefreshing = true);

                DBController.Instance.ParseBooks(context);

                ComponentName appWidgetComponentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(WidgetSmall)).Name);
                appWidgetManager.UpdateAppWidget(appWidgetComponentName, BuildRemoteView(ref context, appWidgetId, ref widgetParams));
            }
            base.OnUpdate(context, appWidgetManager, appWidgetIds);
            realm.Dispose();
        }

        public override void OnAppWidgetOptionsChanged(Context context, AppWidgetManager appWidgetManager, int appWidgetId, Bundle newOptions)
        {
            Realm realm = Realm.GetInstance(DB.RealmConfiguration);

            Bundle options = appWidgetManager.GetAppWidgetOptions(appWidgetId);

            // Get min width and height.
            int minWidth = options.GetInt(AppWidgetManager.OptionAppwidgetMinWidth);
            //int minHeight = options.GetInt(AppWidgetManager.OptionAppwidgetMinHeight);

            WidgetParams widgetParams = realm.Find<WidgetParams>(appWidgetId);
            int cells = GetCellsForSize(minWidth);
            realm.Write(() => widgetParams.Cells = cells >= 1 && cells <= 3 ? cells : 0);

            // Obtain appropriate widget and update it.
            appWidgetManager.UpdateAppWidget(appWidgetId, BuildRemoteView(ref context, appWidgetId, ref widgetParams));
            base.OnAppWidgetOptionsChanged(context, appWidgetManager, appWidgetId, newOptions);

            realm.Dispose();
        }

        public override void OnReceive(Context context, Intent intent)
        {
            base.OnReceive(context, intent);

            string action = intent.Action;
            if (action == null) return;

            // Check if the click is to open chapter in browser
            if (action.Equals(RefreshClick))
            {
                Update(ref context);
                Update(ref context, intent.GetIntExtra(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId));
                return;
            }

            // Check if the click is to open chapter in browser
            if (action.Equals(OpenChapterClick))
            {
                try
                {
                    //read last chapter url
                    string url = Realm.GetInstance(DB.RealmConfiguration).All<Chapter>().OrderBy(c => c.ID).Last().URL;

                    // launch in browser
                    Uri uri = Uri.Parse(url);
                    Intent browser = new Intent(Intent.ActionView, uri);
                    context.StartActivity(browser);
                }
                catch
                {
                    // Something went wrong :(
                }
            }
        }

        private RemoteViews BuildRemoteView(ref Context context, int appWidgetId, ref WidgetParams widgetParams)
        {
            RemoteViews widgetView;

            int layout = widgetParams.IsRefreshing ? LayoutsRefreshing[widgetParams.Cells] : Layouts[widgetParams.Cells];

            widgetView = new RemoteViews(context.PackageName, layout);

            if (!widgetParams.IsRefreshing)
            {
                SetView(ref context, appWidgetId, ref widgetView, ref widgetParams);
            }

            return widgetView;
        }

        private static int GetCellsForSize(int size)
        {
            int n = 1;
            while (70 * n - 30 < size)
            {
                ++n;
            }
            return n - 1;
        }

        private void SetView(ref Context context, int appWidgetId, ref RemoteViews widgetView, ref WidgetParams widgetParams)
        {
            Realm realm = Realm.GetInstance(DB.RealmConfiguration);

            // Set TextViews
            string title = realm.All<Chapter>().OrderBy(c => c.ID).Last().Title;
            long lastUpdate = Data.Instance.ReadLong(ref context, Data.LastUpdateTime);

            widgetView.SetTextViewText(Resource.Id.chapter_title, title);
            widgetView.SetTextViewText(Resource.Id.last_update, new DateTime(lastUpdate).ToString(widgetParams.DateFormat));

            // Bind the click intent for the chapter on the widget
            Intent chapterIntent = new Intent(context, typeof(WidgetSmall));
            chapterIntent.SetAction(OpenChapterClick);
            chapterIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
            PendingIntent chapterPendingIntent = PendingIntent.GetBroadcast(context, appWidgetId, chapterIntent, 0);
            widgetView.SetOnClickPendingIntent(Resource.Id.container, chapterPendingIntent);

            // Bind the click intent for the refresh button on the widget
            Intent refreshIntent = new Intent(context, typeof(WidgetSmall));
            refreshIntent.SetAction(RefreshClick);
            refreshIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
            PendingIntent refreshPendingIntent = PendingIntent.GetBroadcast(context, appWidgetId, refreshIntent, PendingIntentFlags.UpdateCurrent);
            widgetView.SetOnClickPendingIntent(Resource.Id.btn_refresh, refreshPendingIntent);

            realm.Dispose();
        }

        public override void OnDeleted(Context context, int[] appWidgetIds)
        {
            base.OnDeleted(context, appWidgetIds);
            Realm realm = Realm.GetInstance(DB.RealmConfiguration);
            realm.Write(() =>
            {
                foreach (int appWidgetId in appWidgetIds)
                {
                    WidgetParams widgetParams = realm.Find<WidgetParams>(appWidgetId);
                    realm.Remove(widgetParams);
                }
            });

            realm.Dispose();
        }

        public override void OnDisabled(Context context)
        {
            base.OnDisabled(context);
            Realm realm = Realm.GetInstance(DB.RealmConfiguration);
            realm.Write(() => realm.RemoveAll<WidgetParams>());

            realm.Dispose();
        }

        public override void OnRestored(Context context, int[] oldWidgetIds, int[] newWidgetIds)
        {
            base.OnRestored(context, oldWidgetIds, newWidgetIds);
            Realm realm = Realm.GetInstance(DB.RealmConfiguration);
            realm.Write(() =>
            {
                for (int i = 0; i < oldWidgetIds.Length; i++)
                {
                    WidgetParams widgetParams = realm.Find<WidgetParams>(oldWidgetIds[i]);
                    widgetParams.ID = newWidgetIds[i];
                }
            });

            realm.Dispose();
        }

        public void UpdateAll(ref Context context)
        {
            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
            ComponentName appWidgetComponentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(WidgetSmall)).Name);
            int[] appWidgetIds = appWidgetManager.GetAppWidgetIds(appWidgetComponentName);
            OnUpdate(context, appWidgetManager, appWidgetIds);
        }

        public void Update(ref Context context, params int[] appWidgetIds)
        {
            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
            OnUpdate(context, appWidgetManager, appWidgetIds);
        }

        public void RedrawAll(ref Context context)
        {
            Realm realm = Realm.GetInstance(DB.RealmConfiguration);
            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
            ComponentName appWidgetComponentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(WidgetSmall)).Name);
            int[] appWidgetIds = appWidgetManager.GetAppWidgetIds(appWidgetComponentName);
            foreach (int appWidgetId in appWidgetIds)
            {
                WidgetParams p = realm.Find<WidgetParams>(appWidgetId);
                appWidgetManager.UpdateAppWidget(appWidgetId, BuildRemoteView(ref context, appWidgetId, ref p));
            }

            realm.Dispose();
        }

        public void Redraw(ref Context context, params int[] appWidgetIds)
        {
            Realm realm = Realm.GetInstance(DB.RealmConfiguration);
            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
            foreach (int appWidgetId in appWidgetIds)
            {
                WidgetParams p = realm.Find<WidgetParams>(appWidgetId);
                appWidgetManager.UpdateAppWidget(appWidgetId, BuildRemoteView(ref context, appWidgetId, ref p));
            }

            realm.Dispose();
        }
    }


    [Activity(Name = "forgottenconqueror.WidgetConfigurationActivity",
        Label = "@string/app_name", Theme = "@style/AppTheme", LaunchMode = SingleTop)]
    [IntentFilter(actions: new string[] { "android.appwidget.action.APPWIDGET_CONFIGURE" })]
    public class WidgetConfigurationActivity : AppCompatActivity
    {
        private int selected = 21;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.widget__small_configuration);

            Bundle extras = Intent.Extras;
            int appWidgetId = extras != null ? extras.GetInt(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId) : AppWidgetManager.InvalidAppwidgetId;

            if (appWidgetId == AppWidgetManager.InvalidAppwidgetId) this.Finish();

            Intent result = new Intent();
            result.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
            SetResult(Result.Canceled, result);

            void Finish()
            {
                // Save DB and finish
                Realm realm = Realm.GetInstance(DB.RealmConfiguration);
                realm.Write(() => realm.Add<WidgetParams>(
                    new WidgetParams()
                    {
                        ID = appWidgetId,
                        DateFormat = DateTimeFormats[selected].Item2,
                    }
                , true));

                WidgetSmall widget = new WidgetSmall();
                Context context = this;
                widget.Redraw(ref context, appWidgetId);

                SetResult(Result.Ok, result);
                FinishAndRemoveTask();
            }

            Button save = FindViewById<Button>(Resource.Id.btn_ok);
            save.Click += (sender, e) =>
            {
                Finish();
            };

            Button cancel = FindViewById<Button>(Resource.Id.btn_cancel);
            cancel.Click += (sender, e) =>
            {
                this.Finish();
            };
            
            MaterialSpinner spinner = FindViewById<MaterialSpinner>(Resource.Id.date_format_spinner);
            ArrayAdapter<string> adapter =
                new ArrayAdapter<string>(this, R.Layout.SimpleListItem1, R.Id.Text1, DateFormats);
            spinner.Adapter = adapter;

            spinner.ItemSelected += (object sender, AdapterView.ItemSelectedEventArgs e) =>
            {
                if(e.Position == -1)
                {
                    spinner.SetSelected(selected);
                    return;
                }
                selected = e.Position;
            };
        }

        private static readonly List<Tuple<string, string>> DateTimeFormats = new List<Tuple<string, string>>()
        {
            new Tuple<string, string>("Sat, Feb 21 2015", "ddd, MMM dd yyyy"),
            new Tuple<string, string>("Sat, 21 Feb 2015", "ddd, dd MMM yyyy"),
            new Tuple<string, string>("Sat, Feb-21-2015", "ddd, MMM-dd-yyyy"),
            new Tuple<string, string>("Sat, 21-Feb-2015", "ddd, dd-MMM-yyyy"),
            new Tuple<string, string>("Sat, 2 21 2015", "ddd, MM dd yyyy"),
            new Tuple<string, string>("Sat, 21 2 2015", "ddd, dd MM yyyy"),
            new Tuple<string, string>("Sat, 2-21-2015", "ddd, MM-dd-yyyy"),
            new Tuple<string, string>("Sat, 21-2-2015", "ddd, dd-MM-yyyy"),

            new Tuple<string, string>("2/21/2015", "MM/dd/yyyy"),
            new Tuple<string, string>("21/2/2015", "dd/MM/yyyy"),
            new Tuple<string, string>("2-21-2015", "MM-dd-yyyy"),
            new Tuple<string, string>("21-2-2015", "dd-MM-yyyy"),
            new Tuple<string, string>("2.21.2015", "MM.dd.yyyy"),
            new Tuple<string, string>("21.2.2015", "dd.MM.yyyy"),

            new Tuple<string, string>("2/21/2015 2:13 PM", "MM/dd/yyyy hh:mm tt"),
            new Tuple<string, string>("2/21/2015 14:13", "MM/dd/yyyy H:mm"),
            new Tuple<string, string>("21/2/2015 2:13 PM", "dd/MM/yyyy hh:mm tt"),
            new Tuple<string, string>("21/2/2015 14:13", "dd/MM/yyyy H:mm"),

            new Tuple<string, string>("2/21/2015 2:13:33 PM", "MM/dd/yyyy hh:mm:ss tt"),
            new Tuple<string, string>("2/21/2015 14:13:33", "MM/dd/yyyy H:mm:ss"),
            new Tuple<string, string>("21/2/2015 2:13:33 PM", "dd/MM/yyyy hh:mm:ss tt"),
            new Tuple<string, string>("21/2/2015 14:13:33", "dd/MM/yyyy H:mm:ss"),

            new Tuple<string, string>("Saturday 2:13 PM", "dddd hh:mm tt"),
            new Tuple<string, string>("Saturday 14:13", "dddd H:mm"),

            new Tuple<string, string>("Saturday 2:13:33 PM", "dddd hh:mm:ss tt"),
            new Tuple<string, string>("Saturday 14:13:33", "dddd H:mm:ss"),
        };
        private static readonly string[] DateFormats;

        static WidgetConfigurationActivity()
        {
            DateFormats = new string[DateTimeFormats.Count];
            for (int i = 0; i < DateTimeFormats.Count; i++)
            {
                DateFormats[i] = DateTimeFormats[i].Item1;
            }
        }
    }
}