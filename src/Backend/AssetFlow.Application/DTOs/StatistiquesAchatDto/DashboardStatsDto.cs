using System.Globalization;
namespace AssetFlow.Application.DTOs
{
    public class DashboardStatsDto
    {
        // KPIs
        public int TotalMateriels       { get; set; }
        public int TotalCommandes       { get; set; }
        public int TotalArticles        { get; set; }
        public int TotalDemandesActives { get; set; }

        // Données pour graphes
        public AffectationMaterielDto         AffectationMateriel  { get; set; } = new();
        public List<ArticlesParMaterielDto>   ArticlesParMateriel  { get; set; } = new();
        public List<ArticlesParCategorieDto>  ArticlesParCategorie { get; set; } = new();

        public List<DemandeRawDto>            DemandesRaw          { get; set; } = new();
        public EtatDemandesDto GetEtatDemandes(int annee, int mois)
        {
            var q = DemandesRaw.Where(d => d.DateCreation.Year == annee);
            if (mois > 0)
                q = q.Where(d => d.DateCreation.Month == mois);
            var list = q.ToList();
            return new EtatDemandesDto
            {
                EnAttente = list.Count(d => d.Statut == "en_attente"),
                Commande  = list.Count(d => d.Statut == "commande"),
                Traite    = list.Count(d => d.Statut == "traite"),
                Refuse    = list.Count(d => d.Statut == "refuse"),
            };
        }
        public List<DemandesParSemaineDto> GetDemandesParSemaine(
            DateTime debut, DateTime fin, int nbSemaines = 8)
        {
            // Ajuster si la plage est trop grande : garder les N dernières semaines
            var realDebut = fin.AddDays(-(nbSemaines * 7 - 1));
            if (debut > realDebut) realDebut = debut;

            var result = new List<DemandesParSemaineDto>();
            var cursor = realDebut.Date;

            for (int i = 0; i < nbSemaines; i++)
            {
                var weekStart = cursor;
                var weekEnd   = cursor.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);
                if (weekStart > fin.Date) break;

                var sem  = DemandesRaw
                    .Where(d => d.DateCreation.Date >= weekStart &&
                                d.DateCreation.Date <= weekEnd.Date)
                    .ToList();

                int wn = ISOWeek.GetWeekOfYear(weekStart);
                result.Add(new DemandesParSemaineDto
                {
                    Label     = $"S{wn:D2}",
                    EnAttente = sem.Count(d => d.Statut == "en_attente"),
                    Commande  = sem.Count(d => d.Statut == "commande"),
                    Traite    = sem.Count(d => d.Statut == "traite"),
                });

                cursor = cursor.AddDays(7);
            }

            return result;
        }
        public List<DemandesParSemaineDto> GetDemandesSemaineDuMois(int annee, int mois)
        {
            var result = new List<DemandesParSemaineDto>();
            var ranges = new[]
            {
                (1,  7,  "Semaine 1"),
                (8,  14, "Semaine 2"),
                (15, 21, "Semaine 3"),
                (22, DateTime.DaysInMonth(annee, mois), "Semaine 4"),
            };

            foreach (var (jourDebut, jourFin, label) in ranges)
            {
                var d1 = new DateTime(annee, mois, jourDebut);
                var d2 = new DateTime(annee, mois, Math.Min(jourFin, DateTime.DaysInMonth(annee, mois)));

                var sem = DemandesRaw
                    .Where(d => d.DateCreation.Date >= d1 && d.DateCreation.Date <= d2)
                    .ToList();

                result.Add(new DemandesParSemaineDto
                {
                    Label     = label,
                    EnAttente = sem.Count(d => d.Statut == "en_attente"),
                    Commande  = sem.Count(d => d.Statut == "commande"),
                    Traite    = sem.Count(d => d.Statut == "traite"),
                });
            }
            return result;
        }
    }
}