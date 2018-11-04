using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using Realms;
using static ForgottenConqueror.DB;

namespace ForgottenConqueror
{
    [Service(Permission = Android.Manifest.Permission.BindRemoteviews, Exported = false)]
    class WidgetLargeService : RemoteViewsService
    {
        public static readonly string ExtraBookId = "ExtraBookId";
        public override IRemoteViewsFactory OnGetViewFactory(Intent intent)
        {
            return new ViewFactory(ApplicationContext, intent);
        }

        private class ViewFactory : Java.Lang.Object, IRemoteViewsFactory
        {
            private Context context;
            private int ItemLayout = Android.Resource.Layout.SimpleExpandableListItem1;
            private int WidgetId = AppWidgetManager.InvalidAppwidgetId;
            private Realm RealmInsrance;
            private int BookId;
            private Book RealmBook;

            public ViewFactory(Context context, Intent intent)
            {
                this.context = context;
                WidgetId = intent.GetIntExtra(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId);
                BookId = intent.GetIntExtra(ExtraBookId, 0);
            }

            public RemoteViews GetViewAt(int position)
            {
                RemoteViews page = new RemoteViews(context.PackageName, ItemLayout);

                page.SetTextViewText(Android.Resource.Id.Text1, RealmBook.Chapters.ElementAtOrDefault(position).Title);

                return page;
            }

            public long GetItemId(int position)
            {
                return position;
            }

            public int Count => RealmBook.Chapters.Count();

            public bool HasStableIds => true;

            public RemoteViews LoadingView => new RemoteViews(context.PackageName, Resource.Layout.widget_1cell_progress);

            public int ViewTypeCount => 1;

            public void OnCreate()
            {
                RealmInsrance = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
                RealmBook = RealmInsrance.Find<Book>(BookId);
            }

            public void OnDataSetChanged()
            {
                RealmInsrance.Refresh();
                RealmBook = RealmInsrance.Find<Book>(BookId);
            }

            public void OnDestroy()
            {
                RealmInsrance.Dispose();
            }
        }
    }
}