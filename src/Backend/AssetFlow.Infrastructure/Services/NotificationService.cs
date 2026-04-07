using AssetFlow.Application.DTOs;
using AssetFlow.Application.Interfaces;
using AssetFlow.Domain.Entities;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _db;

        public NotificationService(AppDbContext db) => _db = db;

        // ── Récupérer les notifications ──────────────────────────────────────
        public async Task<List<NotificationDto>> GetNotificationsAsync(
            string? role = null, bool nonLuesSeulement = false)
        {
            // Génère d'abord les nouvelles notifications d'expirations
            await GenererNotificationsAffectationsExpireesAsync();

            var query = _db.Notifications
                .Include(n => n.Utilisateur)
                .Include(n => n.Affectation)
                    .ThenInclude(a => a != null ? a.Materiel : null)
                .AsNoTracking()
                .AsQueryable();

            // if (!string.IsNullOrWhiteSpace(role))
            //     query = query.Where(n => n.RoleDestinataire == null || n.RoleDestinataire == role);

            if (nonLuesSeulement)
                query = query.Where(n => !n.EstLue);

            var notifications = await query
                .OrderByDescending(n => n.DateCreation)
                .Take(50)
                .ToListAsync();

            return notifications.Select(MapToDto).ToList();
        }

        // ── Compteur non lues ────────────────────────────────────────────────
        public async Task<int> GetNombreNonLuesAsync(string? role = null)
        {
            await GenererNotificationsAffectationsExpireesAsync();

            var query = _db.Notifications.AsNoTracking()
                .Where(n => !n.EstLue);

            // if (!string.IsNullOrWhiteSpace(role))
            //     query = query.Where(n => n.RoleDestinataire == null || n.RoleDestinataire == role);

            return await query.CountAsync();
        }

        // ── Marquer une notification comme lue ──────────────────────────────
        public async Task MarquerCommeLueAsync(int notificationId)
        {
            var notif = await _db.Notifications.FindAsync(notificationId);
            if (notif == null || notif.EstLue) return;

            notif.EstLue      = true;
            notif.DateLecture = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // ── Marquer toutes comme lues ────────────────────────────────────────
        public async Task MarquerToutesCommeLuesAsync(string? role = null)
        {
            var query = _db.Notifications.Where(n => !n.EstLue);

            // if (!string.IsNullOrWhiteSpace(role))
            //     query = query.Where(n => n.RoleDestinataire == null || n.RoleDestinataire == role);

            var nonLues = await query.ToListAsync();
            var maintenant = DateTime.UtcNow;

            foreach (var n in nonLues)
            {
                n.EstLue      = true;
                n.DateLecture = maintenant;
            }

            await _db.SaveChangesAsync();
        }

        // ── Génération automatique des notifications d'expiration ────────────
        public async Task GenererNotificationsAffectationsExpireesAsync()
        {
            var maintenant = DateTime.UtcNow;

            // ── 1. Affectations courantes avec date de retour dépassée ────────
            var affectationsExpirees = await _db.Affectations
                .Include(a => a.Materiel)
                .Include(a => a.Utilisateur)
                .Where(a =>
                    a.Etat == EtatAffectation.Courante &&
                    a.DateRetour.HasValue &&
                    a.DateRetour.Value < maintenant)
                .AsNoTracking()
                .ToListAsync();

            foreach (var aff in affectationsExpirees)
            {
                // FIX : on ne filtre plus sur !EstLue — une notif par affectation par 24h suffit,
                // qu'elle soit lue ou non. Cela évite la duplication après "marquer comme lu".
                var dejaPresente = await _db.Notifications.AnyAsync(n =>
                    n.AffectationId == aff.Id &&
                    n.Type == TypeNotification.AffectationExpiree &&
                    n.DateCreation >= maintenant.AddDays(-1)); // max 1 notif/24h

                if (dejaPresente) continue;

                var joursRetard = (int)(maintenant - aff.DateRetour!.Value).TotalDays;
                var nomEmploye  = aff.Utilisateur != null
                    ? $"{aff.Utilisateur.FirstName} {aff.Utilisateur.LastName}"
                    : "Projet";

                var niveau = joursRetard > 14 ? NiveauNotification.Critique
                           : joursRetard > 3  ? NiveauNotification.Avertissement
                           : NiveauNotification.Info;

                _db.Notifications.Add(new Notification
                {
                    Titre            = $"Retour en retard · {aff.Materiel.Designation}",
                    Message          = $"{nomEmploye} doit retourner « {aff.Materiel.Designation} » " +
                                       $"depuis {joursRetard} jour(s). " +
                                       $"Date prévue : {aff.DateRetour.Value:dd/MM/yyyy}.",
                    Type             = TypeNotification.AffectationExpiree,
                    Niveau           = niveau,
                    AffectationId    = aff.Id,
                    UtilisateurId    = aff.UtilisateurId,
                    RoleDestinataire = "IT",
                    DateCreation     = maintenant
                });
            }

            // ── 2. Affectations qui vont expirer dans moins de 3 jours ────────
            var bientotExpirees = await _db.Affectations
                .Include(a => a.Materiel)
                .Include(a => a.Utilisateur)
                .Where(a =>
                    a.Etat == EtatAffectation.Courante &&
                    a.DateRetour.HasValue &&
                    a.DateRetour.Value >= maintenant &&
                    a.DateRetour.Value <= maintenant.AddDays(3))
                .AsNoTracking()
                .ToListAsync();

            foreach (var aff in bientotExpirees)
            {
                // Déjà cohérent : pas de filtre EstLue ici, fenêtre 24h
                var dejaPresente = await _db.Notifications.AnyAsync(n =>
                    n.AffectationId == aff.Id &&
                    n.Type == TypeNotification.RetourEnRetard &&
                    n.DateCreation >= maintenant.AddDays(-1));

                if (dejaPresente) continue;

                var joursRestants = (int)(aff.DateRetour!.Value - maintenant).TotalDays;
                var nomEmploye    = aff.Utilisateur != null
                    ? $"{aff.Utilisateur.FirstName} {aff.Utilisateur.LastName}"
                    : "Projet";

                _db.Notifications.Add(new Notification
                {
                    Titre            = $"Retour imminent · {aff.Materiel.Designation}",
                    Message          = $"L'affectation de « {aff.Materiel.Designation} » pour {nomEmploye} " +
                                       $"expire dans {joursRestants} jour(s) ({aff.DateRetour.Value:dd/MM/yyyy}).",
                    Type             = TypeNotification.RetourEnRetard,
                    Niveau           = NiveauNotification.Avertissement,
                    AffectationId    = aff.Id,
                    UtilisateurId    = aff.UtilisateurId,
                    RoleDestinataire = "IT",
                    DateCreation     = maintenant
                });
            }

            if (_db.ChangeTracker.HasChanges())
                await _db.SaveChangesAsync();
        }

        // ── Nettoyage ────────────────────────────────────────────────────────
        public async Task NettoyerAnciennesNotificationsAsync()
        {
            var limite = DateTime.UtcNow.AddDays(-30);
            var anciennes = await _db.Notifications
                .Where(n => n.EstLue && n.DateLecture < limite)
                .ToListAsync();

            _db.Notifications.RemoveRange(anciennes);
            await _db.SaveChangesAsync();
        }

        // ── Mapper ───────────────────────────────────────────────────────────
        private static NotificationDto MapToDto(Notification n) => new()
        {
            Id                  = n.Id,
            Titre               = n.Titre,
            Message             = n.Message,
            Type                = n.Type.ToString(),
            Niveau              = n.Niveau.ToString(),
            DateCreation        = n.DateCreation,
            EstLue              = n.EstLue,
            AffectationId       = n.AffectationId,
            UtilisateurId       = n.UtilisateurId,
            NomEmploye          = n.Utilisateur != null
                ? $"{n.Utilisateur.FirstName} {n.Utilisateur.LastName}"
                : null,
            DesignationMateriel = n.Affectation?.Materiel?.Designation,
            DateRetourPrevue    = n.Affectation?.DateRetour,
            JoursRetard         = n.Affectation?.DateRetour.HasValue == true
                ? (int)(DateTime.UtcNow - n.Affectation.DateRetour!.Value).TotalDays
                : null
        };
    }
}