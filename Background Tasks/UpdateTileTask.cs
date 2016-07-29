using Windows.ApplicationModel.Background;
using TsinghuaUWP;
using System.Diagnostics;

namespace BackgroundTasks
{
    public sealed class UpdateTileTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("[UpdateTileTask] launched");

            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
            Notification.update(true);
            Appointment.updateDeadlines();
            deferral.Complete();
            Debug.WriteLine("[UpdateTileTask] finished");

        }
    }
}
