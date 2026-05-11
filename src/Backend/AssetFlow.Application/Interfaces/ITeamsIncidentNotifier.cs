namespace AssetFlow.Application.Interfaces
{
    public interface ITeamsIncidentNotifier
    {
        Task NotifierIncidentResoluAsync(int incidentId, string? commentaireResolution);
    }
}