using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using Realms;
using static ForgottenConqueror.DB;
using Uri = Android.Net.Uri;

namespace ForgottenConqueror
{
    [BroadcastReceiver(Label = "Forgotten Conqueror")]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/appwidgetprovider_large_alt")]
    class WidgetLargeAlt : AppWidgetProvider
    {
        private readonly static string MenuClick = "OpenMenu";
        private readonly static string MenuOutsideClick = "CloseMenu";
        private readonly static string ReverseClick = "ReverseClick";
        private readonly static string RefreshClick = "RefreshClick";
        private readonly static int Layout = Resource.Layout.widget_large_alt;
        private readonly static int LayoutRefreshing = Resource.Layout.widget_large_alt_progress;

        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            Realm realm = Realm.GetInstance(DB.RealmConfiguration);
            foreach (int appWidgetId in appWidgetIds)
            {
                WidgetLargeAltParams widgetLargeAltParams = realm.Find<WidgetLargeAltParams>(appWidgetId);
                if (widgetLargeAltParams == null)
                {
                    // Widget created
                    realm.Write(() =>
                    {
                        widgetLargeAltParams = new WidgetLargeAltParams()
                        {
                            ID = appWidgetId,
                            IsRefreshing = true,
                            Book = 0,
                            Descending = false,
                        };
                        realm.Add<WidgetLargeAltParams>(widgetLargeAltParams);
                    });
                }
                else if (widgetLargeAltParams.IsRefreshing)
                {
                    // Already updating
                    return;
                }
                else realm.Write(() => widgetLargeAltParams.IsRefreshing = true);

                DBController.Instance.ParseBooks(context);

                ComponentName appWidgetComponentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(WidgetLargeAlt)).Name);
                appWidgetManager.UpdateAppWidget(appWidgetComponentName, BuildRemoteView(context, appWidgetId, widgetLargeAltParams));
            }
            base.OnUpdate(context, appWidgetManager, appWidgetIds);
            realm.Dispose();
        }

        public override void OnReceive(Context context, Intent intent)
        {
            base.OnReceive(context, intent);

            string action = intent.Action;
            if (action == null) return;

            int appWidgetId = intent.GetIntExtra(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId);
            if (appWidgetId == AppWidgetManager.InvalidAppwidgetId) return;

            Realm realm = Realm.GetInstance(DB.RealmConfiguration);
            WidgetLargeAltParams widgetLargeAltParams = realm.Find<WidgetLargeAltParams>(appWidgetId);
            if (action.Equals(MenuClick))
            {
                // Show Book list
                realm.Write(() => widgetLargeAltParams.OpenMenu = !widgetLargeAltParams.OpenMenu);
                Redraw(context, appWidgetId);
            }
            if (action.Equals(MenuOutsideClick))
            {
                // Hide Book list
                realm.Write(() => widgetLargeAltParams.OpenMenu = false);
                Redraw(context, appWidgetId);
            }
            if (action.Equals(ReverseClick))
            {
                // Reverse order
                realm.Write(() => widgetLargeAltParams.Descending = !widgetLargeAltParams.Descending);
                Redraw(context, appWidgetId);
            }
            if (action.Equals(RefreshClick))
            {
                // Refresh
                Update(context, appWidgetId);
            }
            if (action.Equals(RemoteBookAdapter.SelectBook))
            {
                int bookId = intent.GetIntExtra(RemoteBookAdapter.ExtraBookId, 0);
                realm.Write(() =>
                {
                    widgetLargeAltParams.Book = bookId;
                    widgetLargeAltParams.OpenMenu = false;
                });
                Redraw(context, appWidgetId);
            }
            realm.Dispose();
        }

        private RemoteViews BuildRemoteView(Context context, int appWidgetId, WidgetLargeAltParams widgetLargeAltParams)
        {
            RemoteViews widgetView;
            int layout = widgetLargeAltParams.IsRefreshing ? LayoutRefreshing : Layout;

            widgetView = new RemoteViews(context.PackageName, layout);

            SetView(context, appWidgetId, widgetView, widgetLargeAltParams);
            
            return widgetView;
        }

        private void SetView(Context context, int appWidgetId, RemoteViews widgetView, WidgetLargeAltParams widgetLargeAltParams)
        {
            if (!widgetLargeAltParams.IsRefreshing)
            {
                Realm realm = Realm.GetInstance(DB.RealmConfiguration);

                // Bind the RemoteViewsService (adapter) for the Chapters list
                Intent intent = new Intent(context, typeof(RemoteChapterAdapter));
			    intent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
                intent.PutExtra(RemoteChapterAdapter.ExtraBookId, widgetLargeAltParams.Book);
                intent.PutExtra(RemoteChapterAdapter.ExtraSortOrder, widgetLargeAltParams.Descending);
                intent.SetData(Uri.Parse(intent.ToUri(IntentUriType.Scheme)));
                widgetView.SetRemoteAdapter(Resource.Id.list_chapters, intent);

                // Set Chapter list click intent template
                Intent chapterClickIntentTemplate = new Intent(Intent.ActionView);
                PendingIntent chapterClickPendingIntentTemplate = TaskStackBuilder.Create(context)
                        .AddNextIntentWithParentStack(chapterClickIntentTemplate)
                        .GetPendingIntent(appWidgetId, PendingIntentFlags.UpdateCurrent);
                widgetView.SetPendingIntentTemplate(Resource.Id.list_chapters, chapterClickPendingIntentTemplate);
                
                // Bind the click intent for the refresh button on the widget
                Intent refreshIntent = new Intent(context, typeof(WidgetLargeAlt));
                refreshIntent.SetAction(RefreshClick);
                refreshIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
                PendingIntent refreshPendingIntent = PendingIntent.GetBroadcast(context, appWidgetId, refreshIntent, PendingIntentFlags.UpdateCurrent);
                widgetView.SetOnClickPendingIntent(Resource.Id.btn_refresh, refreshPendingIntent);

                realm.Dispose();
            }

            if (widgetLargeAltParams.OpenMenu)
            {
                widgetView.SetViewVisibility(Resource.Id.menu, Android.Views.ViewStates.Visible);

                // Bind the click intent for the reverse button on the widget
                Intent CloseIntent = new Intent(context, typeof(WidgetLargeAlt));
                CloseIntent.SetAction(MenuOutsideClick);
                CloseIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
                PendingIntent closePendingIntent = PendingIntent.GetBroadcast(context, appWidgetId, CloseIntent, PendingIntentFlags.UpdateCurrent);
                widgetView.SetOnClickPendingIntent(Resource.Id.menu_outside, closePendingIntent);

                if (!widgetLargeAltParams.IsRefreshing)
                {
                    // Bind the RemoteViewsService (adapter) for the Book list
                    Intent intent = new Intent(context, typeof(RemoteBookAdapter));
                    intent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
                    intent.PutExtra(RemoteBookAdapter.ExtraBookId, widgetLargeAltParams.Book);
                    intent.SetData(Uri.Parse(intent.ToUri(IntentUriType.Scheme)));
                    widgetView.SetRemoteAdapter(Resource.Id.list_books, intent);

                    // Set Book list click intent template
                    Intent bookClickIntentTemplate = new Intent(context, typeof(WidgetLargeAlt));
                    PendingIntent bookClickPendingIntentTemplate = PendingIntent.GetBroadcast(context, appWidgetId, bookClickIntentTemplate, PendingIntentFlags.UpdateCurrent);
                    widgetView.SetPendingIntentTemplate(Resource.Id.list_books, bookClickPendingIntentTemplate);
                }
            }
            else
            {
                widgetView.SetViewVisibility(Resource.Id.menu, Android.Views.ViewStates.Invisible);
            }

            // Bind the click intent for the menu button on the widget
            Intent menuIntent = new Intent(context, typeof(WidgetLargeAlt));
            menuIntent.SetAction(MenuClick);
            menuIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
            PendingIntent menuPendingIntent = PendingIntent.GetBroadcast(context, appWidgetId, menuIntent, PendingIntentFlags.UpdateCurrent);
            widgetView.SetOnClickPendingIntent(Resource.Id.btn_menu, menuPendingIntent);

            // Bind the click intent for the reverse button on the widget
            Intent reverseIntent = new Intent(context, typeof(WidgetLargeAlt));
            reverseIntent.SetAction(ReverseClick);
            reverseIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
            PendingIntent reversePendingIntent = PendingIntent.GetBroadcast(context, appWidgetId, reverseIntent, PendingIntentFlags.UpdateCurrent);
            widgetView.SetOnClickPendingIntent(Resource.Id.btn_reverse, reversePendingIntent);
        }

        public override void OnDeleted(Context context, int[] appWidgetIds)
        {
            base.OnDeleted(context, appWidgetIds);
            Realm realm = Realm.GetInstance(DB.RealmConfiguration);
            realm.Write(() =>
            {
                foreach (int appWidgetId in appWidgetIds)
                {
                    WidgetLargeAltParams widgetParams = realm.Find<WidgetLargeAltParams>(appWidgetId);
                    realm.Remove(widgetParams);
                }
            });

            realm.Dispose();
        }

        public override void OnDisabled(Context context)
        {
            base.OnDisabled(context);
            Realm realm = Realm.GetInstance(DB.RealmConfiguration);
            realm.Write(() => realm.RemoveAll<WidgetLargeAltParams>());
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
                    WidgetLargeAltParams widgetLargeAltParams = realm.Find<WidgetLargeAltParams>(oldWidgetIds[i]);
                    widgetLargeAltParams.ID = newWidgetIds[i];
                }
            });

            realm.Dispose();
        }

        public void UpdateAll(Context context)
        {
            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
            ComponentName appWidgetComponentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(WidgetLargeAlt)).Name);
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
            ComponentName appWidgetComponentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(WidgetLargeAlt)).Name);
            int[] appWidgetIds = appWidgetManager.GetAppWidgetIds(appWidgetComponentName);
            foreach (int appWidgetId in appWidgetIds)
            {
                appWidgetManager.UpdateAppWidget(appWidgetId, BuildRemoteView(context, appWidgetId, realm.Find<WidgetLargeAltParams>(appWidgetId)));
            }

            realm.Dispose();
        }

        public void Redraw(Context context, params int[] appWidgetIds)
        {
            Realm realm = Realm.GetInstance(DB.RealmConfiguration);
            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
            foreach (int appWidgetId in appWidgetIds)
            {
                appWidgetManager.UpdateAppWidget(appWidgetId, BuildRemoteView(context, appWidgetId, realm.Find<WidgetLargeAltParams>(appWidgetId)));
            }

            realm.Dispose();
        }
    }
}