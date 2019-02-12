using Android.Support.V7.App;
using FR.Ganfra.Materialspinner;
using System.Collections.Generic;

namespace ForgottenConqueror
{
    public static class Extensions
    {
        public static void SetSelected(this MaterialSpinner spinner, int position)
        {
            spinner.SetSelection(position + 1);
        }

        //public static bool IsEmpty<T>(this IEnumerable<T> collection)
        //{
        //    return collection.Count() == 0;
        //}

        public static bool IsEmpty<T>(this List<T> collection)
        {
            return collection.Count == 0;
        }

        public static void RequestPermissions(this AppCompatActivity activity, string[] permissions, byte requestCode)
        {
            activity.RequestPermissions(permissions, requestCode);
        }

        public static void RequestPermission(this AppCompatActivity activity, string permission, byte requestCode)
        {
            string[] permissions = { permission };
            activity.RequestPermissions(permissions, requestCode);
        }
    }
}