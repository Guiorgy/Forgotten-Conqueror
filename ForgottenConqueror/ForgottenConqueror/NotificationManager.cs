using Android.App;
using Android.Content;
using Android.OS;
using System.Collections.Generic;
using static ForgottenConqueror.DB;
using Builder = Android.Support.V4.App.NotificationCompat.Builder;
using NotificationManagerCompat = Android.App.NotificationManager;
using Notification = Android.Support.V4.App.NotificationCompat;
using Android.Net;

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
            Builder builder = new Builder(context);
            if(Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                Channel channel = Channels[(int)channelId];

                if (channel.NotificationChannel == null)
                {
                    // Create a NotificationChannel
                    NotificationChannel notificationChannel = new NotificationChannel(channel.ChannelId, channel.ChannelName, channel.Importance);
                    notificationChannel.Description = " ";
                    notificationChannel.EnableLights(channel.Lights);
                    notificationChannel.EnableVibration(channel.Vibration);
                    notificationChannel.SetBypassDnd(channel.DoNotDisturb);
                    notificationChannel.LockscreenVisibility = channel.Visibility;

                    // Register the new NotificationChannel
                    NotificationManagerCompat notificationManager = GetManager(context);
                    notificationManager.CreateNotificationChannel(notificationChannel);

                    channel.NotificationChannel = notificationChannel;
                }

                builder.SetChannelId(channel.ChannelId);
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

            Intent intent = new Intent(Intent.ActionView);
            intent.SetData(Uri.Parse(chapters[0].URL));
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
    }
}