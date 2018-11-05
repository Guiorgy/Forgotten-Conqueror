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
            private int ItemLayout = Resource.Layout.widget_large_chapter_listitem;
            private int WidgetId = AppWidgetManager.InvalidAppwidgetId;
            private Realm RealmInstance;
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
                Realm other = Realm.GetInstance(DB.RealmConfiguration);
                if (!RealmInstance.IsSameInstance(other))
                {
                    RealmInstance.Dispose();
                    RealmInstance = other;
                    RealmBook = RealmInstance.Find<Book>(BookId);
                }

                RemoteViews page = new RemoteViews(context.PackageName, ItemLayout);

                page.SetTextViewText(Resource.Id.chapter_title, RealmBook.Chapters.ElementAtOrDefault(position).Title);

                return page;
            }

            public long GetItemId(int position)
            {
                return position;
            }

            public int Count {
                get
                {
                    Realm other = Realm.GetInstance(DB.RealmConfiguration);
                    if (!RealmInstance.IsSameInstance(other))
                    {
                        RealmInstance.Dispose();
                        RealmInstance = other;
                        RealmBook = RealmInstance.Find<Book>(BookId);
                    }
                    return RealmBook.Chapters.Count();
                }
            }

            public bool HasStableIds => true;

            public RemoteViews LoadingView => new RemoteViews(context.PackageName, Resource.Layout.widget_1cell_progress);

            public int ViewTypeCount => 1;
            
            public void OnCreate()
            {
                RealmInstance = Realm.GetInstance(DB.RealmConfiguration);
                RealmBook = RealmInstance.Find<Book>(BookId);
            }

            public void OnDataSetChanged()
            {
                Realm other = Realm.GetInstance(DB.RealmConfiguration);
                if (!RealmInstance.IsSameInstance(other))
                {
                    RealmInstance.Dispose();
                    RealmInstance = other;
                    RealmBook = RealmInstance.Find<Book>(BookId);
                }
            }

            public void OnDestroy()
            {
                RealmInstance.Dispose();
            }
        }
    }
}