namespace Thesis.Core
{
    public interface IHubModeRunner
    {
        string ModeId { get; }
        bool HasActiveSession { get; }

        void HubStart();
        void HubAddFake();
        void HubEnd();
        void HubExport();
    }
}
