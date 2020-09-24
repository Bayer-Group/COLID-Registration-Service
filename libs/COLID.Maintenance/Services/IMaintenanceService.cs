namespace COLID.Maintenance.Services
{
    public interface IMaintenanceService
    {
        bool IsInMaintenance();

        void UpdateInMaintenanceMode(bool inMaintenanceMode);

        string RetryAfterInSeconds();

        string ContentType();

        object DefaultResponse();
    }
}
