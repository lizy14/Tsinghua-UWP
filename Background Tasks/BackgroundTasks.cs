using Windows.ApplicationModel.Background;
using TsinghuaUWP;
using System.Diagnostics;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;


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

    public sealed class Management
    {
        const int LOCAL_INTERVAL = 15; //minutes
        const int REMOTE_INTERVAL = 120; //minutes

        public static void register()
        {
            //local
            RegisterBackgroundTask(
                local_task_entry,
                local_task_name,
                new TimeTrigger(LOCAL_INTERVAL, false),
                new SystemCondition(SystemConditionType.UserPresent));

            //remote
            RegisterBackgroundTask(
                remote_task_entry,
                remote_task_name,
                new TimeTrigger(REMOTE_INTERVAL, false),
                new SystemCondition(SystemConditionType.InternetAvailable));

        }

        private const string local_task_name = "LocalUpdateTask";
        private const string local_task_entry = "BackgroundTasks.LocalUpdateTask";

        private const string remote_task_name = "RemoteUpdateTask";
        private const string remote_task_entry = "BackgroundTasks.RemoteUpdateTask";




        //helper function, reference: https://msdn.microsoft.com/en-us/windows/uwp/launch-resume/register-a-background-task

        static BackgroundTaskRegistration RegisterBackgroundTask(
            string taskEntryPoint,
            string taskName,
            IBackgroundTrigger trigger,
            IBackgroundCondition condition)
        {
            //
            // Check for existing registrations of this background task.
            //

            Debug.WriteLine("[BackgroundTasks] registering " + taskName);
            foreach (var cur in BackgroundTaskRegistration.AllTasks)
            {

                if (cur.Value.Name == taskName)
                {
                    //
                    // The task is already registered.
                    //
                    Debug.WriteLine("[BackgroundTasks] already registered");
                    return (BackgroundTaskRegistration)(cur.Value);
                }
            }

            //
            // Register the background task.
            //

            var builder = new BackgroundTaskBuilder();

            builder.Name = taskName;
            builder.TaskEntryPoint = taskEntryPoint;
            builder.SetTrigger(trigger);

            if (condition != null)
            {

                builder.AddCondition(condition);
            }

            BackgroundTaskRegistration task = builder.Register();

            Debug.WriteLine("[BackgroundTasks] successfully registered");
            return task;
        }
    }
}
