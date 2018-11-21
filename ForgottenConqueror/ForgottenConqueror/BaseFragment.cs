namespace ForgottenConqueror
{
    public abstract class BaseFragment : Android.Support.V4.App.Fragment
    {
        public override void OnDestroy()
        {
            base.OnDestroy();

            #if LOGGED_RELEASE
            var refWatcher = ForgottenConqueror.GetRefWatcher();
            refWatcher.Watch(this);
            #endif
        }
    }
}