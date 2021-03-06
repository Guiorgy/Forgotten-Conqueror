using System;
using System.Linq;
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
        private readonly static int[] Layouts =
        {
            Resource.Layout.widget,
            Resource.Layout.widget_1cell,
            Resource.Layout.widget_2cell,
            Resource.Layout.widget_3cell,
        };
        private readonly static int[] LayoutsRefreshing =
        {
            Resource.Layout.widget_progress,
            Resource.Layout.widget_1cell_progress,
            Resource.Layout.widget_2cell_progress,
            Resource.Layout.widget_3cell_progress,
        };

        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            Realm realm = Realm.GetInstance(DB.RealmConfiguration);
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

                bool isFirstUpdate = Data.Instance.ReadBoolean(context, Data.IsFirstUpdate, true);
                DBController.Instance.ParseBooks(context, !isFirstUpdate);

                ComponentName appWidgetComponentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(Widget)).Name);
                appWidgetManager.UpdateAppWidget(appWidgetComponentName, BuildRemoteView(context, appWidgetId, widgetParams));
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
            appWidgetManager.UpdateAppWidget(appWidgetId, BuildRemoteView(context, appWidgetId, widgetParams));
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
                Update(context);
                Update(context, intent.GetIntExtra(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId));
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

        private RemoteViews BuildRemoteView(Context context, int appWidgetId, WidgetParams widgetParams)
        {
            RemoteViews widgetView;
            
            int layout = widgetParams.IsRefreshing ? LayoutsRefreshing[widgetParams.Cells] : Layouts[widgetParams.Cells];

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
            Realm realm = Realm.GetInstance(DB.RealmConfiguration);

            // Set TextViews
            string title = realm.All<Chapter>().OrderBy(c => c.ID).Last().Title;
            long lastUpdate = Data.Instance.ReadLong(context, Data.LastUpdateTime);

            widgetView.SetTextViewText(Resource.Id.chapter_title, title);
            widgetView.SetTextViewText(Resource.Id.last_update, string.Format("{0:MM/dd/yy H:mm:ss}", new DateTime(lastUpdate)));
            
            // Bind the click intent for the chapter on the widget
            Intent chapterIntent = new Intent(context, typeof(Widget));
            chapterIntent.SetAction(OpenChapterClick);
            chapterIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
            PendingIntent chapterPendingIntent = PendingIntent.GetBroadcast(context, appWidgetId, chapterIntent, 0);
            widgetView.SetOnClickPendingIntent(Resource.Id.container, chapterPendingIntent);
            
            // Bind the click intent for the refresh button on the widget
            Intent refreshIntent = new Intent(context, typeof(Widget));
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
                for(int i = 0; i < oldWidgetIds.Length; i++)
                {
                    WidgetParams widgetParams = realm.Find<WidgetParams>(oldWidgetIds[i]);
                    widgetParams.ID = newWidgetIds[i];
                }
            });

            realm.Dispose();
        }

        public void UpdateAll(Context context)
        {
            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
            ComponentName appWidgetComponentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(Widget)).Name);
            int[] appWidgetIds = appWidgetManager.GetAppWidgetIds(appWidgetComponentName);
            OnUpdate(context, appWidgetManager, appWidgetIds);
        }

        public void Update(Context context, params int[] appWidgetIds)
        {
            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
            OnUpdate(context, appWidgetManager, appWidgetIds);
        }

        public void RedrawAll(Context context)
        {
            Realm realm = Realm.GetInstance(DB.RealmConfiguration);
            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
            ComponentName appWidgetComponentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(Widget)).Name);
            int[] appWidgetIds = appWidgetManager.GetAppWidgetIds(appWidgetComponentName);
            foreach (int appWidgetId in appWidgetIds)
            {
                appWidgetManager.UpdateAppWidget(appWidgetId, BuildRemoteView(context, appWidgetId, realm.Find<WidgetParams>(appWidgetId)));
            }

            realm.Dispose();
        }

        public void Redraw(Context context, params int[] appWidgetIds)
        {
            Realm realm = Realm.GetInstance(DB.RealmConfiguration);
            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
            foreach (int appWidgetId in appWidgetIds)
            {
                appWidgetManager.UpdateAppWidget(appWidgetId, BuildRemoteView(context, appWidgetId, realm.Find<WidgetParams>(appWidgetId)));
            }

            realm.Dispose();
        }
    }
}