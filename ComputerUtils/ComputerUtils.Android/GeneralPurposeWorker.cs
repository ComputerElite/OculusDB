using System;
using System.Collections.Generic;
using Android.Content;
using Android.Runtime;
using AndroidX.Work;
using Java.Util.Concurrent;
using JetBrains.Annotations;
using Logger = ComputerUtils.Android.Logging.Logger;

namespace ComputerUtils.Android
{
    public class GeneralPurposeWorker : Worker
    {
        public const string tag = "ComputerUtilsGeneralPurposeWorker";
        public static Dictionary<string, Action> actions = new Dictionary<string, Action>();
        public static bool initialized = true;
        public string actionToUse;
        public override Result DoWork()
        {
            actions[actionToUse].Invoke();
            return Result.InvokeSuccess();
        }

        public static void ExecuteWork(Action work)
        {
            Logger.Log("Adding action to work");
            string actionId = DateTime.Now.Ticks.ToString();
            actions.Add(actionId, work);
            if (!initialized)
            {
                ComputerUtils.Android.Logging.Logger.Log("Initializing WorkManager...");
                WorkManager.Initialize(
                    AndroidCore.context,
                    new Configuration.Builder()
                        .SetExecutor(Executors.NewFixedThreadPool(4))
                        .Build());
                initialized = true;
            }
            ComputerUtils.Android.Logging.Logger.Log("Creating request");
            OneTimeWorkRequest request = new OneTimeWorkRequest.Builder(typeof(GeneralPurposeWorker))
                .SetInputData(new AndroidX.Work.Data(new Dictionary<string, object> {{"action", actionId}}))
                .Build();
            ComputerUtils.Android.Logging.Logger.Log("Enqueuing request");
            WorkManager.GetInstance(AndroidCore.context).BeginWith(request).Enqueue();
        }

        public GeneralPurposeWorker(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public GeneralPurposeWorker([NotNull] Context context, [NotNull] WorkerParameters workerParams) : base(context, workerParams)
        {
            actionToUse = workerParams.InputData.GetString("action");
        }
    }
}