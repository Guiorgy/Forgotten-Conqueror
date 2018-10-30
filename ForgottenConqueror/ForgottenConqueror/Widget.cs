using System;
using System.Linq;
using System.Threading;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Widget;
using Realms;
using static ForgottenConqueror.DB;
using Uri = Android.Net.Uri;

namespace ForgottenConqueror
{
    [BroadcastReceiver(Label = "Last Chapter")]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/appwidgetprovider")]
    class Widget : AppWidgetProvider
    {
        private readonly static string OpenChapterClick = "OpenChapterClick";
        private readonly static string RefreshClick = "RefreshClick";
        private readonly static int[] layouts =
        {
            Resource.Layout.widget,
            Resource.Layout.widget_1cell,
            Resource.Layout.widget_2cell,
            Resource.Layout.widget_3cell,
        };
        private readonly static int[] layoutsRefreshing =
        {
            Resource.Layout.widget_progress,
            Resource.Layout.widget_1cell_progress,
            Resource.Layout.widget_2cell_progress,
            Resource.Layout.widget_3cell_progress,
        };

        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            Realm realm = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
            foreach(int appWidgetId in appWidgetIds)
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

                new Thread(() =>
                {
                    // update
                    Realm aRealm = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
                    WidgetParams aWidgetParams = aRealm.Find<WidgetParams>(appWidgetId);
                    DB db = DB.Instance;
                    bool isFirstUpdate = Data.Instance.ReadBoolean(context, Data.IsFirstUpdate, true);
                    if (isFirstUpdate)
                    {
                        db.UpdateBooks(aRealm);
                    }
                    else
                    {
                        Book book = aRealm.All<Book>().Last();
                        db.UpdateBook(aRealm, book);
                        Data.Instance.Write(context, Data.IsFirstUpdate, false);
                    }
                    if (aRealm.IsClosed || !aWidgetParams.IsValid)
                    {
                        Realm r = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
                        r.Write(() => r.Find<WidgetParams>(appWidgetId).IsRefreshing = false);
                        return;
                    }
                    aRealm.Write(() => aWidgetParams.IsRefreshing = false);
                    Data.Instance.Write(context, Data.LastUpdate, DateTime.Now.Ticks);
                    ComponentName provider = new ComponentName(context, Java.Lang.Class.FromType(typeof(Widget)).Name);
                    appWidgetManager.UpdateAppWidget(provider, BuildRemoteViews(context, appWidgetId, aWidgetParams));
                }).Start();

                ComponentName appWidgetComponentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(Widget)).Name);
                appWidgetManager.UpdateAppWidget(appWidgetComponentName, BuildRemoteViews(context, appWidgetId, widgetParams));
            }
            base.OnUpdate(context, appWidgetManager, appWidgetIds);
        }

        public override void OnAppWidgetOptionsChanged(Context context, AppWidgetManager appWidgetManager, int appWidgetId, Bundle newOptions)
        {
            Bundle options = appWidgetManager.GetAppWidgetOptions(appWidgetId);

            // Get min width and height.
            int minWidth = options.GetInt(AppWidgetManager.OptionAppwidgetMinWidth);
            //int minHeight = options.GetInt(AppWidgetManager.OptionAppwidgetMinHeight);

            Realm realm = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
            WidgetParams widgetParams = realm.Find<WidgetParams>(appWidgetId);
            int cells = GetCellsForSize(minWidth);
            realm.Write(() => widgetParams.Cells = cells >= 1 && cells <= 3 ? cells : 0);

            // Obtain appropriate widget and update it.
            appWidgetManager.UpdateAppWidget(appWidgetId, BuildRemoteViews(context, appWidgetId, widgetParams));
            base.OnAppWidgetOptionsChanged(context, appWidgetManager, appWidgetId, newOptions);
        }

        public override void OnReceive(Context context, Intent intent)
        {
            base.OnReceive(context, intent);

            // Check if the click is to open chapter in browser
            if (intent.Action.Equals(RefreshClick))
            {
                //Toast.MakeText(context, "REFRESH CLICKED", ToastLength.Long).Show();
                for(int i = 0; i < 100; i++)
                {
                    Console.WriteLine("REFRESH CLICKED" + i);
                }
                return;
            }

            // Check if the click is to open chapter in browser
            if (intent.Action.Equals(OpenChapterClick))
            {
                try
                {
                    //read last chapter url
                    string url = Realm.GetInstance(RealmConfiguration.DefaultConfiguration).All<Chapter>().Last().URL;

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

        private RemoteViews BuildRemoteViews(Context context, int appWidgetId, WidgetParams widgetParams)
        {
            RemoteViews widgetView;
            
            int layout = widgetParams.IsRefreshing ? layoutsRefreshing[widgetParams.Cells] : layouts[widgetParams.Cells];

            widgetView = new RemoteViews(context.PackageName, layout);

            if (!widgetParams.IsRefreshing)
            {
                SetView(context, appWidgetId, widgetView);
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

        private void SetView(Context context, int appWidgetId, RemoteViews widgetView)
        {
            // Set TextViews
            string title = Realm.GetInstance(RealmConfiguration.DefaultConfiguration).All<Chapter>().Last().Title;
            long lastUpdate = Data.Instance.ReadLong(context, Data.LastUpdate);

            widgetView.SetTextViewText(Resource.Id.chapter_title, title);
            widgetView.SetTextViewText(Resource.Id.last_update, string.Format("{0:MM/dd/yy H:mm:ss}", new DateTime(lastUpdate)));
            
            // Bind the click intent for the chapter on the widget
            Intent chapterIntent = new Intent(context, typeof(Widget));
            chapterIntent.SetAction(RefreshClick);
            chapterIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
            PendingIntent chapterPendingIntent = PendingIntent.GetBroadcast(context, 0, chapterIntent, 0);
            widgetView.SetOnClickPendingIntent(Resource.Id.container, chapterPendingIntent);
            
            // Bind the click intent for the refresh button on the widget
            Intent refreshIntent = new Intent(context, typeof(Widget));
            refreshIntent.SetAction(RefreshClick);
            refreshIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
            PendingIntent refreshPendingIntent = PendingIntent.GetBroadcast(context, 0, refreshIntent, PendingIntentFlags.UpdateCurrent);
            widgetView.SetOnClickPendingIntent(Resource.Id.btn_refresh, refreshPendingIntent);
        }

        public override void OnDeleted(Context context, int[] appWidgetIds)
        {
            base.OnDeleted(context, appWidgetIds);
            Realm realm = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
            realm.Write(() =>
            {
                foreach (int appWidgetId in appWidgetIds)
                {
                    WidgetParams widgetParams = realm.Find<WidgetParams>(appWidgetId);
                    realm.Remove(widgetParams);
                }
            });
        }

        public override void OnDisabled(Context context)
        {
            base.OnDisabled(context);
            Realm realm = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
            realm.Write(() => realm.RemoveAll<WidgetParams>());
        }

        public override void OnRestored(Context context, int[] oldWidgetIds, int[] newWidgetIds)
        {
            base.OnRestored(context, oldWidgetIds, newWidgetIds);
            Realm realm = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
            realm.Write(() =>
            {
                for(int i = 0; i < oldWidgetIds.Length; i++)
                {
                    WidgetParams widgetParams = realm.Find<WidgetParams>(oldWidgetIds[i]);
                    widgetParams.ID = newWidgetIds[i];
                }
            });
        }

        private void UpdateAll(Context context)
        {
            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
            ComponentName appWidgetComponentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(Widget)).Name);
            int[] appWidgetIds = appWidgetManager.GetAppWidgetIds(appWidgetComponentName);
            OnUpdate(context, appWidgetManager, appWidgetIds);
        }

        private void Update(Context context, int[] appWidgetIds)
        {
            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
            OnUpdate(context, appWidgetManager, appWidgetIds);
        }
    }
}