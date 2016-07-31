using Windows.ApplicationModel.Background;
using TsinghuaUWP;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Storage;
using System.Net.NetworkInformation;

namespace BackgroundTasks
{
    public sealed class LocalUpdateTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("[LocalUpdateTask] launched");
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            Notification.update();
            Appointment.updateDeadlines();

            deferral.Complete();
            Debug.WriteLine("[LocalUpdateTask] finished");

        }
    }

    public sealed class RemoteUpdateTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("[RemoteUpdateTask] launched");
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            DataAccess.getAllDeadlines(forceRemote: true);

            deferral.Complete();
            Debug.WriteLine("[RemoteUpdateTask] finished");

        }
    }

    public sealed class UnifiedUpdateTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
            Debug.WriteLine("[UnifiedUpdateTask] launched at " + DateTime.Now);

            Debug.WriteLine("[UnifiedUpdateTask] local");
            Notification.update();
            Appointment.updateDeadlines();

            bool goRemote = false;
            string key = "last_successful_remote_task";
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                if (DataAccess.getLocalSettings()[key] == null)
                    goRemote = true;
                else {
                    var delta = DateTime.Now - DateTime.Parse(DataAccess.getLocalSettings()[key].ToString());
                    if (delta.TotalHours > .5 /*TODO: MAGIC*/)
                        goRemote = true;
                }
                if (goRemote) {
                    Debug.WriteLine("[UnifiedUpdateTask] remote");
                    await DataAccess.getAllDeadlines(forceRemote: true); //hope this can finish in 30 seconds
                    Notification.update();
                    Appointment.updateDeadlines();
                }
            }

            deferral.Complete();

            if(goRemote)
                DataAccess.setLocalSettings(key, DateTime.Now.ToString());

            Debug.WriteLine("[UnifiedUpdateTask] finished");
        }
    }

    public sealed class TaskManager
    {
        const int LOCAL_INTERVAL = 15; //minutes
        const int REMOTE_INTERVAL = 120; //minutes

        public static async void register()
        {   
            await RegisterBackgroundTask(
                unified_task_entry,
                unified_task_name,
                new TimeTrigger(LOCAL_INTERVAL, false),
                new SystemCondition(SystemConditionType.UserPresent));

            return;
        }

        private const string unified_task_name = "UnifiedUpdateTask";
        private const string unified_task_entry = "BackgroundTasks.UnifiedUpdateTask";

        static async Task<BackgroundTaskRegistration> RegisterBackgroundTask(
            string taskEntryPoint,
            string taskName,
            IBackgroundTrigger trigger,
            IBackgroundCondition condition)
        {

            Debug.WriteLine("[BackgroundTasks] registering " + taskName);

            var backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();
            if (!(backgroundAccessStatus == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity ||
                backgroundAccessStatus == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity))
                throw new Exception();

            foreach (var cur in BackgroundTaskRegistration.AllTasks)
            {
                if (cur.Value.Name == taskName)
                {
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
