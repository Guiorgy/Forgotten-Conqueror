using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using HtmlAgilityPack;
using Realms;
using static ForgottenConqueror.DB;

namespace ForgottenConqueror
{
    class DBController
    {
        private static object thislock = new object();
        private static DBController instance;
        private DBController() { }

        public static DBController Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (thislock)
                    {
                        if (instance == null)
                        {
                            instance = new DBController();
                        }
                    }
                }

                return instance;
            }
            private set { }
        }

        private static Task UpdateTask = new Task(() => throw new NotImplementedException());
        private void AsyncTask(Context context, bool onlyLast)
        {
            Realm realm = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
            var list = realm.All<Chapter>();
            if (list.Count() != 0)
            {
                var last = list.OrderBy(c => c.ID).Last();
                if (last != null)
                {
                    Data.Instance.Write(context, Data.PreviouslyLastChapterId, last.ID);
                }
            }
            if (onlyLast)
            {
                Book book = realm.All<Book>().OrderBy(b => b.ID).Last();
                if (book != null) UpdateBook(realm, book);
                else UpdateBooks(realm);
            }
            else
            {
                UpdateBooks(realm);
            }
        }

        private void UpdateBook(Realm realm = null, Book book = null, int id = 0)
        {
            if (realm == null) realm = Realm.GetInstance(Realms.RealmConfiguration.DefaultConfiguration);

            if (book == null)
            {
                book = realm.Find<Book>(id);
                if (book == null) return;
            }
            Chapter first = book.Chapters.OrderBy(c => c.ID).First();
            int count = first == null ? 0 : first.ID;

            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load(book.URL);
            HtmlNodeCollection containers = doc.DocumentNode.SelectNodes("//div[@class='entry-content']/p[position()>2]");
            
            List<Chapter> chapters = new List<Chapter>();
            foreach (HtmlNode container in containers)
            {
                HtmlNodeCollection nodes = container.SelectNodes("./a");
                foreach (HtmlNode node in nodes)
                {
                    string title = HtmlEntity.DeEntitize(node.InnerText);
                    string url = node.Attributes["href"].Value;

                    Chapter chapter = new Chapter()
                    {
                        ID = count++,
                        Count = chapters.Count + 1,
                        Title = title,
                        URL = url,
                        Book = book,
                    };

                    chapters.Add(chapter);
                }
            }

            realm.Write(() =>
            {
                foreach (Chapter chapter in chapters)
                {
                    realm.Add<Chapter>(chapter, true);
                }
            });
        }

        private void UpdateBooks(Realm realm = null)
        {
            if (realm == null) realm = Realm.GetInstance(Realms.RealmConfiguration.DefaultConfiguration);
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load("http://forgottenconqueror.com/");

            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//div[@class='entry-content']/h1/strong/a");

            List<Book> books = new List<Book>();
            List<Chapter> chapters = new List<Chapter>();
            foreach (HtmlNode node in nodes)
            {
                string title = HtmlEntity.DeEntitize(node.InnerText);
                string url = node.Attributes["href"].Value;

                Book book = new Book()
                {
                    ID = books.Count,
                    Count = books.Count + 1,
                    Title = title,
                    URL = url,
                };

                books.Add(book);
                UpdateBooks_Book(realm, book, chapters);
            }

            realm.Write(() =>
            {
                foreach (Book book in books)
                {
                    realm.Add<Book>(book, true);
                }
                foreach (Chapter chapter in chapters)
                {
                    realm.Add<Chapter>(chapter, true);
                }
            });
        }

        private void UpdateBooks_Book(Realm realm, Book book, List<Chapter> chapters)
        {
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load(book.URL);

            HtmlNodeCollection containers = doc.DocumentNode.SelectNodes("//div[@class='entry-content']/p[position()>2]");

            int count = 1;
            foreach (HtmlNode container in containers)
            {
                HtmlNodeCollection nodes = container.SelectNodes("./a");
                foreach (HtmlNode node in nodes)
                {
                    string title = HtmlEntity.DeEntitize(node.InnerText);
                    string url = node.Attributes["href"].Value;

                    Chapter chapter = new Chapter()
                    {
                        ID = chapters.Count,
                        Count = count++,
                        Title = title,
                        URL = url,
                        Book = book,
                    };

                    chapters.Add(chapter);
                }
            }
        }
        
        private bool CanParse = true;
        public void ParseBooks(Context context, bool onlyLast)
        {
            if (CanParse && UpdateTask.Status != TaskStatus.Running)
            {
                CanParse = false;
                UpdateTask = new Task(() => AsyncTask(context, onlyLast));
                UpdateTask.ContinueWith((task) =>
                {
                    Realm realm = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
                    Chapter chapter = realm.All<Chapter>().OrderBy(c => c.ID).Last();
                    realm.Write(() =>
                    {
                        foreach (WidgetParams widgetParams in realm.All<WidgetParams>())
                        {
                            widgetParams.IsRefreshing = false;
                        }

                        foreach (WidgetLargeParams widgetLargeParams in realm.All<WidgetLargeParams>())
                        {
                            widgetLargeParams.IsRefreshing = false;
                        }
                    });
                    Data.Instance.Write(context, Data.LastUpdateTime, DateTime.Now.Ticks);
                    Data.Instance.Write(context, Data.IsFirstUpdate, false);

                    int lastId = Data.Instance.ReadInt(context, Data.PreviouslyLastChapterId, -1);
                    int currentId = realm.All<Chapter>().OrderBy(c => c.ID).Last().ID;
                    if (lastId != -1 && lastId < currentId)
                    {
                        List<Chapter> chapters = realm.All<Chapter>().Where(c => c.ID > lastId).ToList();
                        NotificationManager.Instance.NotifyNewChapters(context, chapters);
                    }

                    RedrawAllWidgets(context);
                    CanParse = true;
                }, TaskScheduler.FromCurrentSynchronizationContext());
                try
                {
                    UpdateTask.Start();
                } catch(Exception e)
                {
                    Realm realm = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
                    Chapter chapter = realm.All<Chapter>().OrderBy(c => c.ID).Last();
                    realm.Write(() =>
                    {
                        foreach (WidgetParams widgetParams in realm.All<WidgetParams>())
                        {
                            widgetParams.IsRefreshing = false;
                        }

                        foreach (WidgetLargeParams widgetLargeParams in realm.All<WidgetLargeParams>())
                        {
                            widgetLargeParams.IsRefreshing = false;
                        }
                    });
                    Data.Instance.Write(context, Data.LastUpdateTime, DateTime.Now.Ticks);
                    Data.Instance.Write(context, Data.IsFirstUpdate, false);

                    int lastId = Data.Instance.ReadInt(context, Data.PreviouslyLastChapterId, -1);
                    int currentId = realm.All<Chapter>().OrderBy(c => c.ID).Last().ID;
                    if (lastId != -1 && lastId < currentId)
                    {
                        List<Chapter> chapters = realm.All<Chapter>().Where(c => c.ID > lastId).ToList();
                        NotificationManager.Instance.NotifyNewChapters(context, chapters);
                    }

                    RedrawAllWidgets(context);
                    CanParse = true;
                }
            }
        }

        private static object parselock = new object();
        public void ParseBooksSafe(Context context, bool onlyLast)
        {
            if (CanParse && UpdateTask.Status != TaskStatus.Running)
            {
                lock (parselock)
                {
                    if (CanParse && UpdateTask.Status != TaskStatus.Running)
                    {
                        CanParse = false;
                        UpdateTask = new Task(() => AsyncTask(context, onlyLast));
                        UpdateTask.ContinueWith((task) =>
                        {
                            Realm realm = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
                            Chapter chapter = realm.All<Chapter>().OrderBy(c => c.ID).Last();
                            realm.Write(() =>
                            {
                                foreach (WidgetParams widgetParams in realm.All<WidgetParams>())
                                {
                                    widgetParams.IsRefreshing = false;
                                }

                                foreach (WidgetLargeParams widgetLargeParams in realm.All<WidgetLargeParams>())
                                {
                                    widgetLargeParams.IsRefreshing = false;
                                }
                            });
                            Data.Instance.Write(context, Data.LastUpdateTime, DateTime.Now.Ticks);
                            Data.Instance.Write(context, Data.IsFirstUpdate, false);

                            int lastId = Data.Instance.ReadInt(context, Data.PreviouslyLastChapterId, -1);
                            int currentId = realm.All<Chapter>().OrderBy(c => c.ID).Last().ID;
                            if(lastId != -1 && lastId < currentId)
                            {
                                List<Chapter> chapters = realm.All<Chapter>().Where(c => c.ID > lastId).ToList();
                                NotificationManager.Instance.NotifyNewChapters(context, chapters);
                            }
                            
                            RedrawAllWidgets(context);
                            CanParse = true;
                        }, TaskScheduler.FromCurrentSynchronizationContext());
                        try
                        {
                            UpdateTask.Start();
                        }
                        catch (Exception e)
                        {
                            Realm realm = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
                            Chapter chapter = realm.All<Chapter>().OrderBy(c => c.ID).Last();
                            realm.Write(() =>
                            {
                                foreach (WidgetParams widgetParams in realm.All<WidgetParams>())
                                {
                                    widgetParams.IsRefreshing = false;
                                }

                                foreach (WidgetLargeParams widgetLargeParams in realm.All<WidgetLargeParams>())
                                {
                                    widgetLargeParams.IsRefreshing = false;
                                }
                            });
                            Data.Instance.Write(context, Data.LastUpdateTime, DateTime.Now.Ticks);
                            Data.Instance.Write(context, Data.IsFirstUpdate, false);

                            int lastId = Data.Instance.ReadInt(context, Data.PreviouslyLastChapterId, -1);
                            int currentId = realm.All<Chapter>().OrderBy(c => c.ID).Last().ID;
                            if (lastId != -1 && lastId < currentId)
                            {
                                List<Chapter> chapters = realm.All<Chapter>().Where(c => c.ID > lastId).ToList();
                                NotificationManager.Instance.NotifyNewChapters(context, chapters);
                            }

                            RedrawAllWidgets(context);
                            CanParse = true;
                        }
                    }
                }
            }
        }

        private void RedrawAllWidgets(Context context)
        {
            // Widget
            Widget widget = Widget.instance;
            widget.RedrawAll(context);

            // WidgetLarge
            WidgetLarge widgetLarge = new WidgetLarge();
            widgetLarge.RedrawAll(context);
        }
    }
}