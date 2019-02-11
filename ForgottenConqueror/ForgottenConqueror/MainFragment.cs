using Android.OS;
using Android.Views;
using Fragment = Android.Support.V4.App.Fragment;

namespace ForgottenConqueror
{
    public class MainFragment : Fragment
    {
        public static MainFragment Instance => new MainFragment();
        public static string Title => "Home";

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
        }

        public override void OnActivityCreated(Bundle inState)
        {
            base.OnViewStateRestored(inState);
            if (inState != null)
            {
                //
            }
        }

        public override void OnPause()
        {
            base.OnPause();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.main__fragment_main, container, false);
        }
        
        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
        }
    }
}