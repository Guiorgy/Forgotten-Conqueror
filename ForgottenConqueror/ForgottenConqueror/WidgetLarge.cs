using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using Realms;
using System;
using System.Threading;
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

        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            Realm realm = Realm.GetInstance(Realms.RealmConfiguration.DefaultConfiguration);
            foreach(int appWidgetId in appWidgetIds)
            {
                WidgetLargeParams widgetLargeParams = realm.Find<WidgetLargeParams>(appWidgetId);
                if (widgetLargeParams == null)
                {
                    // Widget created
                    realm.Write(() =>
                    {
                        widgetLargeParams = new WidgetLargeParams()
                        {
                            ID = appWidgetId,
                            IsRefreshing = true,
                            Book = 1,
                        };
                        realm.Add<WidgetLargeParams>(widgetLargeParams);
                    });
                }
                else if (widgetLargeParams.IsRefreshing)
                {
                    // Already updating
                    return;
                }
                else realm.Write(() => widgetLargeParams.IsRefreshing = true);

                new Thread(() =>
                {
                    // update
                    Realm aRealm = Realm.GetInstance(Realms.RealmConfiguration.DefaultConfiguration);
                    WidgetLargeParams aWidgetLargeParams = aRealm.Find<WidgetLargeParams>(appWidgetId);
                    DB db = DB.Instance;
                    db.UpdateBooks();
                    aRealm.Write(() => aWidgetLargeParams.IsRefreshing = false);
                    Data.Instance.Write(context, Data.LastUpdate, DateTime.Now.Ticks);
                    ComponentName provider = new ComponentName(context, Java.Lang.Class.FromType(typeof(Widget)).Name);
                    appWidgetManager.UpdateAppWidget(provider, BuildRemoteViews(context, appWidgetIds, aWidgetLargeParams));
                }).Start();

                ComponentName me = new ComponentName(context, Java.Lang.Class.FromType(typeof(WidgetLarge)).Name);
                appWidgetManager.UpdateAppWidget(me, BuildRemoteViews(context, appWidgetIds, widgetLargeParams));
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

        private RemoteViews BuildRemoteViews(Context context, int[] appWidgetIds, WidgetLargeParams widgetLargeParams)
        {
            RemoteViews widgetView;
            int layout = widgetLargeParams.IsRefreshing ? layoutsRefreshing : layouts;

            widgetView = new RemoteViews(context.PackageName, layout);

            if (!widgetLargeParams.IsRefreshing)
            {
                SetView(context, appWidgetIds, widgetView);
            }

            return widgetView;
        }

        private void SetView(Context context, int[] appWidgetIds, RemoteViews widgetView)
        {
            Intent intent = new Intent(context, typeof(WidgetLargeService));
			intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, appWidgetIds);
			intent.SetData(Uri.Parse(intent.ToUri(IntentUriType.Scheme)));
            widgetView.SetRemoteAdapter(Resource.Id.view_flipper, intent);

            // Bind the click intent for the next button on the widget
            Intent nextIntent = new Intent(context, typeof(WidgetLarge));
			nextIntent.SetAction(NextClick);
			nextIntent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, appWidgetIds);
			PendingIntent nextPendingIntent = PendingIntent.GetBroadcast(context, 0, nextIntent, 0);
            widgetView.SetOnClickPendingIntent(Resource.Id.btn_next, nextPendingIntent);

            // Bind the click intent for the previous button on the widget
            Intent previousIntent = new Intent(context, typeof(WidgetLarge));
            previousIntent.SetAction(PreviousClick);
			PendingIntent previousPendingIntent = PendingIntent.GetBroadcast(context, 0, previousIntent, 0);
            widgetView.SetOnClickPendingIntent(Resource.Id.btn_previous, previousPendingIntent);

            // Bind the click intent for the refresh button on the widget
            Intent refreshIntent = new Intent(context, typeof(WidgetLarge));
            refreshIntent.SetAction(RefreshClick);
            PendingIntent refreshPendingIntent = PendingIntent.GetBroadcast(context, 0, previousIntent, PendingIntentFlags.UpdateCurrent);
            widgetView.SetOnClickPendingIntent(Resource.Id.btn_refresh, refreshPendingIntent);
        }

        public override void OnDeleted(Context context, int[] appWidgetIds)
        {
            base.OnDeleted(context, appWidgetIds);
            Realm realm = Realm.GetInstance(Realms.RealmConfiguration.DefaultConfiguration);
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
            Realm realm = Realm.GetInstance(Realms.RealmConfiguration.DefaultConfiguration);
            realm.Write(() => realm.RemoveAll<WidgetLargeParams>());
        }

        public override void OnRestored(Context context, int[] oldWidgetIds, int[] newWidgetIds)
        {
            base.OnRestored(context, oldWidgetIds, newWidgetIds);
            Realm realm = Realm.GetInstance(Realms.RealmConfiguration.DefaultConfiguration);
            realm.Write(() =>
            {
                for (int i = 0; i < oldWidgetIds.Length; i++)
                {
                    WidgetLargeParams widgetLargeParams = realm.Find<WidgetLargeParams>(oldWidgetIds[i]);
                    widgetLargeParams.ID = newWidgetIds[i];
                }
            });
        }
    }
}