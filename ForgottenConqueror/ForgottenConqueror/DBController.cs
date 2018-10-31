using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
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

        private static readonly Task UpdateTask = new Task(() => throw new NotImplementedException());
        private void AsyncTask(Context context, bool onlyLast)
        {
            Toast.MakeText(context, "HTML", ToastLength.Short).Show();
            //Thread.Sleep(3000);
            //Realm realm = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
            //realm.Write(() =>
            //{
            //    Book book = new Book()
            //    {
            //        ID = 0,
            //        Count = 1,
            //        Title = "Book 1",
            //        URL = "http://google.com/",
            //    };
            //    realm.Add<Book>(book, true);
            //    realm.Add<Chapter>(new Chapter()
            //    {
            //        ID = 0,
            //        Count = 1,
            //        Title = "Chapter 1",
            //        URL = "http://google.com/",
            //        Book = book,
            //    }, true);
            //});
            //Realm realm = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
            //if (onlyLast)
            //{
            //    Book book = realm.All<Book>().Last();
            //    if (book != null) UpdateBook(realm, book);
            //    else UpdateBooks(realm);
            //}
            //else
            //{
            //    UpdateBooks(realm);
            //}
            Realm realm = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load("http://forgottenconqueror.com/book-1/");

            HtmlNodeCollection containers = doc.DocumentNode.SelectNodes("//div[@class='entry-content']/p[position()>2]");
            int count = 0;
            foreach (HtmlNode container in containers)
            {
                HtmlNodeCollection nodes = container.SelectNodes("./a");
                realm.Write(() =>
                {
                    foreach (HtmlNode node in nodes)
                    {
                        string title = HtmlEntity.DeEntitize(node.InnerText);
                        string url = node.Attributes["href"].Value;

                        Chapter chapter = new Chapter()
                        {
                            ID = count++,
                            Count = count,
                            Title = title,
                            URL = url,
                            Book = null,
                        };

                        realm.Add<Chapter>(chapter);
                    }
                });
            }
            Thread.Sleep(3000);
        }

        private void UpdateBook(Realm realm = null, Book book = null, int id = 0)
        {
            if (realm == null) realm = Realm.GetInstance(Realms.RealmConfiguration.DefaultConfiguration);
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load(book.URL);

            HtmlNodeCollection containers = doc.DocumentNode.SelectNodes("//div[@class='entry-content']/p[position()>2]");

            if (book == null)
            {
                book = realm.Find<Book>(id);
            }
            Chapter first = book.Chapters.First();
            int count = first == null ? 0 : first.ID;

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
                        Count = book.Chapters.Count() + 1,
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
                Task.Run(() => AsyncTask(context, onlyLast))
                    .ContinueWith((task) =>
                    {
                        Realm realm = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
                        Chapter chapter = realm.All<Chapter>().Last();
                        Toast.MakeText(context, chapter.ID + " " + chapter.Title + " " + chapter.URL, ToastLength.Long).Show();
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
                        Data.Instance.Write(context, Data.LastUpdate, DateTime.Now.Ticks);
                        Data.Instance.Write(context, Data.IsFirstUpdate, false);
                        RedrawAllWidgets(context);
                        CanParse = true;
                    }, TaskScheduler.FromCurrentSynchronizationContext());
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