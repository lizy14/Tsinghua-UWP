using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace TsinghuaUWP
{
    class BackgroundTaskRegister
    {
        public static async void RegisterBackgroundTask()
        {

            var backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();
            if (backgroundAccessStatus == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity ||
                backgroundAccessStatus == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity)
            {
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == taskName)
                    {
                        task.Value.Unregister(true);
                        Debug.WriteLine("Background task unregistered");
                    }
                }

                BackgroundTaskBuilder taskBuilder = new BackgroundTaskBuilder();
                taskBuilder.Name = taskName;
                taskBuilder.TaskEntryPoint = taskEntryPoint;

                taskBuilder.SetTrigger(new TimeTrigger(15, false));

                taskBuilder.AddCondition(new SystemCondition(SystemConditionType.UserPresent));
                //taskBuilder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
                var registration = taskBuilder.Register();

                Debug.WriteLine("Background task registered");
            }
        }

        private const string taskName = "UpdateTileTask";
        private const string taskEntryPoint = "BackgroundTasks.UpdateTileTask";
    }
}
