using System;
using System.Collections.Concurrent;
using System.Threading;
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

    public static class AsyncConsole
    {
        #if DEBUG
            private static BlockingCollection<string> ConsoleQueue = new BlockingCollection<string>();
        #endif

        static AsyncConsole()
        {
            #if DEBUG
                var thread = new Thread(() =>
                    {
                        while (true) Console.WriteLine(ConsoleQueue.Take());
                    });
                thread.IsBackground = true;
                thread.Start();
            #endif
        }

        public static void WriteLine(string value)
        {
            #if DEBUG
                ConsoleQueue.Add(value);
            #endif
        }
    }
}