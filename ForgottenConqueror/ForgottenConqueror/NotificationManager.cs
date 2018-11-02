using Android.App;
using Android.Content;
using Android.OS;
using System.Collections.Generic;
using static ForgottenConqueror.DB;
using Builder = Android.Support.V4.App.NotificationCompat.Builder;
using NotificationManagerCompat = Android.App.NotificationManager;
using Notification = Android.Support.V4.App.NotificationCompat;
using Android.Net;
using Android.Widget;

namespace ForgottenConqueror
{
    class NotificationManager
    {
        private static object thislock = new object();
        private static NotificationManager instance;
        private NotificationManager() { }

        public static NotificationManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (thislock)
                    {
                        if (instance == null)
                        {
                            instance = new NotificationManager();
                        }
                    }
                }

                return instance;
            }
            private set { }
        }

        private static object managerlock = new object();
        private NotificationManagerCompat notificationManager;
        public NotificationManagerCompat GetManager(Context context)
        {
            if (notificationManager == null)
            {
                lock (managerlock)
                {
                    if (notificationManager == null)
                    {
                        notificationManager = (NotificationManagerCompat)context.GetSystemService(Context.NotificationService);
                    }
                }
            }

            return notificationManager;
        }

        public Builder GetBuilder(Context context, ChannelId channelId)
        {
            Builder builder;
            if(Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                Channel channel = Channels[(int)channelId];

                if (channel.NotificationChannel == null)
                {
                    // Create a NotificationChannel
                    NotificationChannel notificationChannel = new NotificationChannel(channel.ChannelId, channel.ChannelName, channel.Importance);
                    notificationChannel.EnableLights(channel.Lights);
                    notificationChannel.EnableVibration(channel.Vibration);
                    notificationChannel.SetBypassDnd(channel.DoNotDisturb);
                    notificationChannel.Group = channel.ChannelName;
                    notificationChannel.LockscreenVisibility = channel.Visibility;

                    // Register the new NotificationChannel
                    NotificationManagerCompat notificationManager = GetManager(context);
                    notificationManager.CreateNotificationChannel(notificationChannel);
                }

                builder = new Builder(context, channel.ChannelId);
            }
            else
            {
                builder = new Builder(context);
            }
            return builder;
        }
        
        public enum ChannelId
        {
            NaN = 0,
            NewChapter = 1,
        }
        
        private class Channel
        {
            public NotificationChannel NotificationChannel = null;
            public string ChannelId = "Za1d3.ForgottenConqueror";
            public string ChannelName = "Forgotten Conqueror";
            public NotificationImportance Importance = NotificationImportance.Default;
            public bool Lights = false;
            public bool Vibration = false;
            public bool DoNotDisturb = false;
            public NotificationVisibility Visibility = NotificationVisibility.Private;
        }

        private Channel[] Channels = new Channel[]
        {
            new Channel()
            {
                ChannelId = "Za1d3.ForgottenConqueror.NaN",
                ChannelName = "NaN",
                Importance = NotificationImportance.Min,
            },
            new Channel()
            {
                ChannelId = "Za1d3.ForgottenConqueror.NewChapter",
                ChannelName = "New Chapters",
                Importance = NotificationImportance.High,
                Visibility = NotificationVisibility.Public,
            },
        };

        public void NotifyNewChapters(Context context, List<Chapter> chapters)
        {
            if (chapters.Count < 1) return;

            Builder builder = GetBuilder(context, ChannelId.NewChapter);
            builder.SetAutoCancel(true);
            builder.SetNumber(4);
            builder.SetOnlyAlertOnce(true);
            builder.SetContentTitle("1 New Chapter");
            builder.SetContentText(chapters[0].Title);

            Intent intent = new Intent(context, typeof(MainActivity));
            intent.SetAction(OpenNewChapter);
            intent.PutExtra("URL", chapters[0].URL);
            intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
            PendingIntent pendingIntent = PendingIntent.GetService(context, (int)ChannelId.NewChapter, intent, PendingIntentFlags.OneShot);
            builder.SetContentIntent(pendingIntent);

            if (chapters.Count > 1)
            {
                builder.SetContentTitle($"{chapters.Count} New Chapters");
                builder.SetSubText($"and {chapters.Count - 1} more");

                var inboxStyle = new Notification.InboxStyle(GetBuilder(context, ChannelId.NewChapter));
                inboxStyle.SetBigContentTitle($"{chapters.Count} New Chapters");

                foreach(Chapter chapter in chapters)
                {
                    inboxStyle.AddLine(chapter.Title);
                }

                builder.SetStyle(inboxStyle);
            }
            //Bitmap icon = BitmapFactory.DecodeResource(context.Resources, Resource.Drawable.new_chapter_icon);
            builder.SetSmallIcon(Resource.Drawable.new_chapter_icon);
            
            NotificationManagerCompat notificationManager = GetManager(context);
            notificationManager.Notify((int)ChannelId.NewChapter, builder.Build());
        }

        public void NotifySimple(Context context, string title, string message, int icon)
        {
            Builder builder = GetBuilder(context, ChannelId.NaN);
            builder.SetAutoCancel(true)
                .SetNumber(4)
                .SetOnlyAlertOnce(true)
                .SetTicker(title)
                .SetContentTitle(title)
                .SetContentText(message)
                .SetSubText(message)
                .SetSmallIcon(icon);

            Intent intent = new Intent(context, typeof(MainActivity));
            intent.SetAction(UseWidgetInstead);
            intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
            PendingIntent pendingIntent = PendingIntent.GetService(context, (int)ChannelId.NewChapter, intent, PendingIntentFlags.OneShot);
            builder.SetContentIntent(pendingIntent);

            NotificationManagerCompat notificationManager = GetManager(context);
            notificationManager.Notify((int)ChannelId.NaN, builder.Build());
        }

        private static string UseWidgetInstead = "UseWidgetInstead";
        private static string OpenNewChapter = "OpenNewChapter";
        public void HandleIntent(Context context, Intent intent)
        {
            string action = intent.Action;
            for (int i = 0; i < 50; i++)
                {
                    System.Console.WriteLine(action + " " + OpenNewChapter);
                }
            if (action == null) return;
            
            if (action.Equals(UseWidgetInstead))
            {
                Toast.MakeText(context, "Long-press the homescreen to add the widget", ToastLength.Long).Show();
                return;
            }

            if (action.Equals(OpenNewChapter))
            {
                string url = intent.GetStringExtra("URL");
                
                if (url == null) return;
                Uri uri = Uri.Parse(url);
                Intent browser = new Intent(Intent.ActionView, uri);
                context.StartActivity(browser);
                return;
            }
        }
    }
}