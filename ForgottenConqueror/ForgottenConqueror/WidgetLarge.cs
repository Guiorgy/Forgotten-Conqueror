using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using Realms;
using System;
using System.Threading.Tasks;
using static ForgottenConqueror.DB;
using Uri = Android.Net.Uri;

namespace ForgottenConqueror
{
    [BroadcastReceiver(Label = "Forgotten Conqueror")]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/appwidgetprovider_large")]
    class WidgetLarge : AppWidgetProvider
    {
        private readonly static string NextClick = "NextClick";
        private readonly static string PreviousClick = "PreviousClick";
        private readonly static string RefreshClick = "RefreshClick";
        private readonly static int layouts = Resource.Layout.widget_large;
        private readonly static int layoutsRefreshing = Resource.Layout.widget_large_progress;

        private static readonly Task UpdateTask = new Task(() => throw new NotImplementedException());
        private void AsyncTask(Context context, AppWidgetManager appWidgetManager, int appWidgetId)
        {
            Realm realm = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
            WidgetLargeParams widgetLargeParams = realm.Find<WidgetLargeParams>(appWidgetId);
            DB db = DB.Instance;
            db.UpdateBooks(realm);
            Data.Instance.Write(context, Data.IsFirstUpdate, false);

            if (realm.IsClosed || !widgetLargeParams.IsValid)
            {
                realm = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
            }
            realm.Write(() =>
            {
                foreach (WidgetLargeParams wp in realm.All<WidgetLargeParams>())
                {
                    wp.IsRefreshing = false;
                }
            });
        }

        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            //Realm realm = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
            foreach(int appWidgetId in appWidgetIds)
            {
                //WidgetLargeParams widgetLargeParams = realm.Find<WidgetLargeParams>(appWidgetId);
                //if (widgetLargeParams == null)
                //{
                //    // Widget created
                //    realm.Write(() =>
                //    {
                //        widgetLargeParams = new WidgetLargeParams()
                //        {
                //            ID = appWidgetId,
                //            IsRefreshing = true,
                //            Book = 1,
                //        };
                //        realm.Add<WidgetLargeParams>(widgetLargeParams);
                //    });
                //}
                //else if (widgetLargeParams.IsRefreshing)
                //{
                //    // Already updating
                //    return;
                //}
                //else realm.Write(() => widgetLargeParams.IsRefreshing = true);
                
                //if (UpdateTask.IsCompleted)
                //{
                //    Task.Run(() => AsyncTask(context, appWidgetManager, appWidgetId))
                //        .ContinueWith((task) =>
                //        {
                //            Data.Instance.Write(context, Data.LastUpdate, DateTime.Now.Ticks);
                //            RedrawAll(context);
                //        });
                //}

                ComponentName appWidgetComponentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(WidgetLarge)).Name);
                appWidgetManager.UpdateAppWidget(appWidgetComponentName, BuildRemoteView(context, appWidgetId, null /*widgetLargeParams*/));
            }
            base.OnUpdate(context, appWidgetManager, appWidgetIds);
        }

        public override void OnReceive(Context context, Intent intent)
        {
            base.OnReceive(context, intent);

            if (intent.Action.Equals(NextClick))
            {
                // Next
            }
            if (intent.Action.Equals(PreviousClick))
            {
                // Previous
            }
            if (intent.Action.Equals(RefreshClick))
            {
                // Refresh
            }
        }

        private RemoteViews BuildRemoteView(Context context, int appWidgetId, WidgetLargeParams widgetLargeParams)
        {
            RemoteViews widgetView;
            //int layout = widgetLargeParams.IsRefreshing ? layoutsRefreshing : layouts;

            widgetView = new RemoteViews(context.PackageName, Resource.Layout.widget_large /*layout*/);

            if (!widgetLargeParams.IsRefreshing)
            {
                SetView(context, appWidgetId, widgetView);
            }

            return widgetView;
        }

        private void SetView(Context context, int appWidgetId, RemoteViews widgetView)
        {
            Intent intent = new Intent(context, typeof(WidgetLargeService));
			intent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
			intent.SetData(Uri.Parse(intent.ToUri(IntentUriType.Scheme)));
            widgetView.SetRemoteAdapter(Resource.Id.view_flipper, intent);

            // Bind the click intent for the next button on the widget
            Intent nextIntent = new Intent(context, typeof(WidgetLarge));
			nextIntent.SetAction(NextClick);
			nextIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
			PendingIntent nextPendingIntent = PendingIntent.GetBroadcast(context, 0, nextIntent, 0);
            widgetView.SetOnClickPendingIntent(Resource.Id.btn_next, nextPendingIntent);

            // Bind the click intent for the previous button on the widget
            Intent previousIntent = new Intent(context, typeof(WidgetLarge));
            previousIntent.SetAction(PreviousClick);
            previousIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
            PendingIntent previousPendingIntent = PendingIntent.GetBroadcast(context, 0, previousIntent, 0);
            widgetView.SetOnClickPendingIntent(Resource.Id.btn_previous, previousPendingIntent);

            // Bind the click intent for the refresh button on the widget
            Intent refreshIntent = new Intent(context, typeof(WidgetLarge));
            refreshIntent.SetAction(RefreshClick);
            refreshIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
            PendingIntent refreshPendingIntent = PendingIntent.GetBroadcast(context, 0, previousIntent, PendingIntentFlags.UpdateCurrent);
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
                    WidgetLargeParams widgetParams = realm.Find<WidgetLargeParams>(appWidgetId);
                    realm.Remove(widgetParams);
                }
            });
        }

        public override void OnDisabled(Context context)
        {
            base.OnDisabled(context);
            Realm realm = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
            realm.Write(() => realm.RemoveAll<WidgetLargeParams>());
        }

        public override void OnRestored(Context context, int[] oldWidgetIds, int[] newWidgetIds)
        {
            base.OnRestored(context, oldWidgetIds, newWidgetIds);
            Realm realm = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
            realm.Write(() =>
            {
                for (int i = 0; i < oldWidgetIds.Length; i++)
                {
                    WidgetLargeParams widgetLargeParams = realm.Find<WidgetLargeParams>(oldWidgetIds[i]);
                    widgetLargeParams.ID = newWidgetIds[i];
                }
            });
        }
        
        private void UpdateAll(Context context)
        {
            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
            ComponentName appWidgetComponentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(WidgetLarge)).Name);
            int[] appWidgetIds = appWidgetManager.GetAppWidgetIds(appWidgetComponentName);
            OnUpdate(context, appWidgetManager, appWidgetIds);
        }

        private void Update(Context context, int[] appWidgetIds)
        {
            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
            OnUpdate(context, appWidgetManager, appWidgetIds);
        }

        private void RedrawAll(Context context)
        {
            Realm realm = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
            ComponentName appWidgetComponentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(WidgetLarge)).Name);
            int[] appWidgetIds = appWidgetManager.GetAppWidgetIds(appWidgetComponentName);
            foreach (int appWidgetId in appWidgetIds)
            {
                appWidgetManager.UpdateAppWidget(appWidgetId, BuildRemoteView(context, appWidgetId, realm.Find<WidgetLargeParams>(appWidgetId)));
            }
        }

        private void Redraw(Context context, int[] appWidgetIds)
        {
            Realm realm = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
            foreach (int appWidgetId in appWidgetIds)
            {
                appWidgetManager.UpdateAppWidget(appWidgetId, BuildRemoteView(context, appWidgetId, realm.Find<WidgetLargeParams>(appWidgetId)));
            }
        }
    }
}