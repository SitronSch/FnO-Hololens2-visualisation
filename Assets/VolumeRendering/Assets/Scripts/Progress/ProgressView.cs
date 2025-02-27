namespace UnityVolumeRendering
{
    /// <summary>
    /// Interface for view of a progress.
    /// Can be attached to a ProgressHandler.
    /// Implement this interface if you wish you update your own GUI or execute some code based on current progress.
    /// </summary>
    public interface IProgressView
    {
        void StartProgress(string description,int numberOfParts);
        void FinishProgress(ProgressStatus status = ProgressStatus.Succeeded);
        void UpdateProgress(float progress, string description,int partNumber);
        void UpdateTotalNumberOfParts(int numberOfParts);
    }
}
