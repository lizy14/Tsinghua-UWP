using Windows.ApplicationModel.Background;
using TsinghuaUWP;

namespace BackgroundTasks
{
    public sealed class UpdateTileTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
            Tile.update();
            deferral.Complete();
        }
    }
}