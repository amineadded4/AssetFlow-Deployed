// AssetFlow.Application/Services/OffreSelectionService.cs

using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;

namespace AssetFlow.Infrastructure.Services
{
    public class OffreSelectionService : IOffreSelectionService
    {
        private readonly IRedisOffreService _redis;
        private readonly IOffreAchatService _offres;

        public OffreSelectionService(
            IRedisOffreService redis,
            IOffreAchatService offres)
        {
            _redis  = redis;
            _offres = offres;
        }

        public async Task<(bool Success, string? Error)> ConfirmSelectionAsync(OffreSelectionDto dto)
        {
            // 1. Sauvegarder infos OCR de l'offre choisie
            await _offres.SauvegarderInfosOcrAsync(
                dto.OffreId,
                dto.PrixTotal,
                dto.FraisLivraison,
                dto.DelaiLivraison,
                dto.Garantie);

            // 2. Sauvegarder infos OCR des autres offres analysées
            foreach (var autre in dto.AutresOffres)
            {
                await _offres.SauvegarderInfosOcrAsync(
                    autre.OffreId, autre.PrixTotal, autre.FraisLivraison,
                    autre.DelaiLivraison, autre.Garantie);
            }

            // 3. Marquer EstChoisie = true
            var success = await _offres.ChoisirOffreAsync(dto.OffreId, dto.IdDemande);
            if (!success) return (false, "Offre introuvable.");

            // 4. Supprimer TOUS les caches OCR de la demande
            var toutesLesOffres = await _offres.GetByDemandeIdAsync(dto.IdDemande);
            foreach (var offre in toutesLesOffres)
                await _redis.DeleteOffreSelectionAsync($"ocr_cache:{offre.IdOffre}");

            await _redis.DeleteOffreSelectionAsync($"chat_offre:{dto.UserId}:{dto.IdDemande}");
            await _redis.DeleteOffreSelectionAsync($"chat_offre_rec:{dto.UserId}:{dto.IdDemande}");

            return (true, null);
        }
    }
}