using System.Linq;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Graphics;
using Android.Widget;
using Realms;
using static ForgottenConqueror.DB;

namespace ForgottenConqueror
{
    [Service(Permission = Android.Manifest.Permission.BindRemoteviews, Exported = false)]
    class RemoteBookAdapter : RemoteViewsService
    {
        public static readonly string SelectBook = "SelectBook";
        public static readonly string ExtraBookId = "ExtraBookId";
        public override IRemoteViewsFactory OnGetViewFactory(Intent intent)
        {
            return new ViewFactory(ApplicationContext, intent);
        }

        private class ViewFactory : Java.Lang.Object, IRemoteViewsFactory
        {
            private static readonly int ItemLayout = Resource.Layout.widget__large_book_listitem;
            private Context context;
            private int WidgetId = AppWidgetManager.InvalidAppwidgetId;
            private int BookId;

            public ViewFactory(Context context, Intent intent)
            {
                this.context = context;
                WidgetId = intent.GetIntExtra(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId);
                BookId = intent.GetIntExtra(ExtraBookId, 0);
            }

            public RemoteViews GetViewAt(int position)
            {
                Realm realm = Realm.GetInstance(DB.RealmConfiguration);

                RemoteViews page = new RemoteViews(context.PackageName, ItemLayout);

                Book book = realm.Find<Book>(position);
                if (book == null) return page;

                page.SetTextViewText(Resource.Id.book_title, book.Title);

                Intent bookClick = new Intent();
                bookClick.SetAction(SelectBook);
                bookClick.PutExtra(AppWidgetManager.ExtraAppwidgetId, WidgetId);
                bookClick.PutExtra(ExtraBookId, book.ID);
                page.SetOnClickFillInIntent(Resource.Id.root, bookClick);

                if (book.ID == BookId)
                {
                    page.SetInt(Resource.Id.root, "setBackgroundColor", Color.ParseColor("#d0d0d0"));
                }
                else
                {
                    page.SetInt(Resource.Id.root, "setBackgroundColor", Color.ParseColor("#f0f0f0"));
                }

                return page;
            }

            public long GetItemId(int position)
            {
                return position;
            }

            public int Count { get; private set; }

            public bool HasStableIds => true;

            public RemoteViews LoadingView => new RemoteViews(context.PackageName, Resource.Layout.widget__small_1cell_progress);

            public int ViewTypeCount => 1;
            
            public void OnCreate()
            {
                Realm realm = Realm.GetInstance(DB.RealmConfiguration);
                Count = realm.All<Book>().Count();
            }

            public void OnDataSetChanged()
            {
                Realm realm = Realm.GetInstance(DB.RealmConfiguration);
                Count = realm.All<Book>().Count();
            }

            public void OnDestroy()
            {
                Realm.GetInstance(DB.RealmConfiguration).Dispose();
            }
        }
    }
}