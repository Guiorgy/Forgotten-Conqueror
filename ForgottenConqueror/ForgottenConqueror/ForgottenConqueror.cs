using System;

using Android.App;
using Android.Runtime;

namespace ForgottenConqueror
{
    #if DEBUG
        [Application(Debuggable = true)]
    #else
        [Application(Debuggable = false)]
    #endif
    class ForgottenConqueror : Application
    {
        public static ForgottenConqueror Instance { get; private set; }

        public ForgottenConqueror(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {}

        public override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
        }

        public override void OnTerminate()
        {
            base.OnTerminate();
            Instance = null;
        }
    }
}