using Android.App;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.App;
using System;
using System.Collections.Generic;
using static Android.Content.PM.LaunchMode;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using Fragment = Android.Support.V4.App.Fragment;
using Java.Lang;

namespace ForgottenConqueror
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, LaunchMode = SingleTop)]
    public class MainActivity : AppCompatActivity
    {
        //ViewPager viewPager;
        //PagerTitleStrip pagerTitleStrip;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

#if LOGGED_RELEASE
            ForgottenConqueror.Instance.RequestPermission(this, ForgottenConqueror.PermissionCode.ReadWrite);
#endif

            FinishAndRemoveTask();

            //viewPager = FindViewById<ViewPager>(Resource.Id.viewpager);
            //pagerTitleStrip = FindViewById<PagerTitleStrip>(Resource.Id.viewpager_header);
            //PageAdapter adapter = new PageAdapter(SupportFragmentManager);
            //viewPager.Adapter = adapter;
            //viewPager.PageSelected += (sender, e) =>
            //{

            //};
        }

        private class PageAdapter : FragmentPagerAdapter
        {
            public static readonly string Instance = "Instance";
            public static readonly string Title = "Title";

            private static readonly List<Type> FragmentTypes = new List<Type>()
            {
                typeof(MainFragment),
                typeof(MainFragment),
            };

            public PageAdapter(FragmentManager fm) : base(fm)
            { }

            public override int Count => FragmentTypes.Count;

            public override Fragment GetItem(int position)
            {
                Type fragment = FragmentTypes[position];
                if (fragment.IsSubclassOf(typeof(Fragment)))
                {
                    return (fragment.GetProperty(Instance).GetValue(null, null) as Fragment);
                }
                return null;
            }

            public override ICharSequence GetPageTitleFormatted(int position)
            {
                Type fragment = FragmentTypes[position];
                if (fragment.IsSubclassOf(typeof(Fragment)))
                {
                    return new Java.Lang.String(fragment.GetProperty(Title).GetValue(null, null) as string);
                }
                return new Java.Lang.String("");
            }
        }
    }
}