using Realms;
using System.Linq;

namespace ForgottenConqueror
{
    class DB
    {
        public static readonly RealmConfiguration RealmConfiguration = new RealmConfiguration("ForgottenConqueror.realm")
        {
            SchemaVersion = 3,
            MigrationCallback = (migration, oldSchema) =>
            {
                // 1 -> 2
                if (oldSchema == 1)
                {
                    // WidgetLargeParams
                    var newParams = migration.NewRealm.All<WidgetLargeParams>();
                    foreach (var p in newParams)
                    {
                        p.Descending = false;
                    }
                    oldSchema = 2;
                }
                // 2 -> 3
                if (oldSchema == 2)
                {
                    // Chapter
                    var newChapters = migration.NewRealm.All<Chapter>();
                    foreach (var c in newChapters)
                    {
                        c.ClearContent();
                        c.IsNew = false;
                    }
                    // WidgetParams
                    var newParams = migration.NewRealm.All<WidgetParams>();
                    foreach (var p in newParams)
                    {
                        p.DateFormat = "dd/MM/yyyy H:mm:ss";
                    }
                    oldSchema = 3;
                }
            },
            //EncryptionKey = System.Text.Encoding.ASCII.GetBytes("mYq3t6w9z$C&F)J@NcRfTjWnZr4u7x!A%D*G-KaPdSgVkXp2s5v8y/B?E(H+MbQe"),
            ShouldCompactOnLaunch = (totalBytes, usedBytes) =>
            {
                return totalBytes > 104857600 || (totalBytes > 52428800 && (double)usedBytes / totalBytes < 0.5);
            },

#if Debug
            ShouldDeleteIfMigrationNeeded = true,
#endif
        };
        private static object thislock = new object();
        private static DB instance;
        private DB() { }

        public static DB Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (thislock)
                    {
                        if (instance == null)
                        {
                            instance = new DB();
                        }
                    }
                }

                return instance;
            }
            private set { }
        }

        // Entities
        public class Chapter : RealmObject
        {
            [PrimaryKey]
            public int ID { get; set; }
            //public RealmInteger<int> Counter { get; set; }
            public int Count { get; set; }
            public string Title { get; set; }
            public string URL { get; set; }
            public bool IsNew { get; set; }

            public Book Book { get; set; }

            private byte[] CompressedContent { get; set; }
            [Ignored]
            public string Content
            {
                get
                {
                    return CompressedContent == null ? null : Data.Instance.Decompress(CompressedContent);
                }
                set
                {
                    CompressedContent = value == null ? null : Data.Instance.Compress(value);
                }
            }

            public void ClearContent()
            {
                CompressedContent = null;
            }
        }

        public class Book : RealmObject
        {
            [PrimaryKey]
            public int ID { get; set; }
            //public RealmInteger<int> Counter { get; set; }
            public int Count { get; set; }
            public string Title { get; set; }
            public string URL { get; set; }

            [Backlink(nameof(Chapter.Book))]
            public IQueryable<Chapter> Chapters { get; }
        }
      
        public class WidgetParams : RealmObject
        {
            [PrimaryKey]
            public int ID { get; set; }
            //public RealmInteger<int> Counter { get; set; }
            public bool IsRefreshing { get; set; }
            public int Cells { get; set; }
            public string DateFormat { get; set; }
        }

        public class WidgetLargeParams : RealmObject
        {
            [PrimaryKey]
            public int ID { get; set; }
            //public RealmInteger<int> Counter { get; set; }
            public bool IsRefreshing { get; set; }
            public int Book { get; set; }
            public bool Descending { get; set; }
        }

        public class WidgetLargeAltParams : RealmObject
        {
            [PrimaryKey]
            public int ID { get; set; }
            //public RealmInteger<int> Counter { get; set; }
            public bool IsRefreshing { get; set; }
            public int Book { get; set; }
            public bool Descending { get; set; }
            public bool OpenMenu { get; set; }
        }
    }
}