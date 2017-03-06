using Windows.ApplicationModel.Background;
using TsinghuaUWP;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

namespace BackgroundTasks {
    public sealed class LocalUpdateTask : IBackgroundTask {
        public async void Run(IBackgroundTaskInstance taskInstance) {
            Debug.WriteLine("[LocalUpdateTask] launched");
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            Notification.update();
            Appointment.updateDeadlines();

            deferral.Complete();
            Debug.WriteLine("[LocalUpdateTask] finished");

        }
    }

    public sealed class RemoteUpdateTask : IBackgroundTask {
        public async void Run(IBackgroundTaskInstance taskInstance) {
            Debug.WriteLine("[RemoteUpdateTask] launched");
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            DataAccess.getAllDeadlines(forceRemote: true);

            deferral.Complete();
            Debug.WriteLine("[RemoteUpdateTask] finished");

        }
    }

    public sealed class UnifiedUpdateTask : IBackgroundTask {

        private const double REMOTE_INTERVAL_HOURS = 1.9;
        private double remoteIntervalHours() {
            if (DataAccess.getLocalSettings()["remote_interval"] == null)
                return REMOTE_INTERVAL_HOURS;
            return double.Parse(DataAccess.getLocalSettings()["remote_interval"].ToString());
        }


        public async void Run(IBackgroundTaskInstance taskInstance) {
            try {
                BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
                Debug.WriteLine("[UnifiedUpdateTask] launched at " + DateTime.Now);
#if DEBUG
                var start = DateTime.Now;
#endif



                bool goRemote = false;
                string key = "last_successful_remote_task";
                if (NetworkInterface.GetIsNetworkAvailable()) {
                    if (DataAccess.getLocalSettings()[key] == null)
                        goRemote = true;
                    else {
                        var delta = DateTime.Now - DateTime.Parse(DataAccess.getLocalSettings()[key].ToString());
                        if (delta.TotalHours >= remoteIntervalHours())
                            goRemote = true;
                    }
                }

                if (goRemote) {
                    Debug.WriteLine("[UnifiedUpdateTask] remote");
                    try {
                        await DataAccess.getAllDeadlines(forceRemote: true); //hope this can finish in 30 seconds
                    } catch (Exception) { }
                }

                try{
                    await Notification.update();
                } catch (Exception) { }
                try {
                    await Appointment.updateDeadlines();
                } catch (Exception) { }
                try {
                    if (goRemote) { await Appointment.updateLectures(); }
                } catch (Exception) { }
                

                deferral.Complete();

                if (goRemote)
                    DataAccess.setLocalSettings(key, DateTime.Now.ToString());

                Debug.WriteLine("[UnifiedUpdateTask] finished at" + DateTime.Now);
#if DEBUG
                Debug.WriteLine("[UnifiedUpdateTask] seconds elapsed " + (DateTime.Now - start).TotalSeconds);
#endif
            } catch (Exception) { }
        }
    }

    public sealed class TaskManager {
        public static async void register() {
            await RegisterBackgroundTask(
                unified_task_entry,
                unified_task_name,
                new TimeTrigger(15 /*the minimal possible value*/, false),
                new SystemCondition(SystemConditionType.UserPresent));

            return;
        }

        private const string unified_task_name = "UnifiedUpdateTask";
        private const string unified_task_entry = "BackgroundTasks.UnifiedUpdateTask";

        private static async Task<BackgroundTaskRegistration> RegisterBackgroundTask(
            string taskEntryPoint,
            string taskName,
            IBackgroundTrigger trigger,
            IBackgroundCondition condition) {

            Debug.WriteLine("[BackgroundTasks] registering " + taskName);

            var backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();
            try {
                if (!(
                backgroundAccessStatus == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity ||
                backgroundAccessStatus == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity ||
                backgroundAccessStatus == BackgroundAccessStatus.AlwaysAllowed ||
                backgroundAccessStatus == BackgroundAccessStatus.AllowedSubjectToSystemPolicy)) {
                    throw new Exception();
                }
            } catch { }

            foreach (var cur in BackgroundTaskRegistration.AllTasks) {
                if (cur.Value.Name == taskName) {
                    Debug.WriteLine("[BackgroundTasks] already registered");
                    cur.Value.Unregister(true);
                }
            }

            var builder = new BackgroundTaskBuilder();

            builder.Name = taskName;
            builder.TaskEntryPoint = taskEntryPoint;
            builder.SetTrigger(trigger);
            if (condition != null)
                builder.AddCondition(condition);
            BackgroundTaskRegistration task = builder.Register();

            Debug.WriteLine("[BackgroundTasks] successfully registered");
            return task;
        }
    }
}
