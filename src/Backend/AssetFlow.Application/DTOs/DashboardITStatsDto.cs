namespace AssetFlow.Application.DTOs
{
    // ── KPIs ─────────────────────────────────────────────────

    public class IncidentStatutCountDto
    {
        public int EnAttente   { get; set; }
        public int EnCours     { get; set; }
        public int Resolu      { get; set; }
        public int Cloture     { get; set; }
    }

    public class IncidentTypeCountDto
    {
        public string Type  { get; set; } = string.Empty;
        public int    Count { get; set; }
    }

    public class ArticleStatutDto
    {
        public int Disponible   { get; set; }
        public int Affecte      { get; set; }
        public int HorsService  { get; set; }
        public int EnReparation { get; set; }
    }

    public class AffectationDepartementDto
    {
        public string Departement { get; set; } = string.Empty;
        public int    Count       { get; set; }
    }

    public class CategorieEquipementDto
    {
        public string Categorie { get; set; } = string.Empty;
        public int    Total     { get; set; }
        public int    Affectes  { get; set; }
    }

    public class IncidentSemaineDto
    {
        public string Label    { get; set; } = string.Empty;
        public int    EnAttente { get; set; }
        public int    EnCours  { get; set; }
        public int    Resolu   { get; set; }
    }

    public class ResolutionTempsDto
    {
        public string Label        { get; set; } = string.Empty;
        public double MoyenneHeures { get; set; }
    }

    public class IncidentRawDto
    {
        public DateTime DateIncident    { get; set; }
        public DateTime? DateResolution { get; set; }
        public string   Statut          { get; set; } = string.Empty;
        public string   TypeIncident    { get; set; } = string.Empty;
        public int      Urgence         { get; set; }
    }


    public class DashboardITStatsDto
    {
        // ── KPIs ────────────────────────────────────────────
        public int TotalMateriels          { get; set; }
        public int TotalArticles           { get; set; }
        public int IncidentsActifs         { get; set; }
        public int AffectationsEnCours     { get; set; }
        public int ArticlesHorsService     { get; set; }
        public int DemandesAchatEnAttente  { get; set; }

        // ── Données graphes ─────────────────────────────────
        public IncidentStatutCountDto          IncidentParStatut      { get; set; } = new();
        public List<IncidentTypeCountDto>      IncidentsParType       { get; set; } = new();
        public ArticleStatutDto                ArticlesParStatut      { get; set; } = new();
        public List<AffectationDepartementDto> AffectationsParDept    { get; set; } = new();
        public List<CategorieEquipementDto>    EquipementsParCategorie{ get; set; } = new();
        public List<ResolutionTempsDto>        TendanceResolution     { get; set; } = new();

        public List<IncidentRawDto> IncidentsRaw { get; set; } = new();

        public List<IncidentSemaineDto> GetIncidentsParSemaine(DateTime debut, DateTime fin, int nbSemaines = 8)
        {
            var realDebut = fin.AddDays(-(nbSemaines * 7 - 1));
            if (debut > realDebut) realDebut = debut;

            var result = new List<IncidentSemaineDto>();
            var cursor = realDebut.Date;

            for (int i = 0; i < nbSemaines; i++)
            {
                var weekStart = cursor;
                var weekEnd   = cursor.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);
                if (weekStart > fin.Date) break;

                var sem = IncidentsRaw
                    .Where(d => d.DateIncident.Date >= weekStart && d.DateIncident.Date <= weekEnd.Date)
                    .ToList();

                int wn = System.Globalization.ISOWeek.GetWeekOfYear(weekStart);
                result.Add(new IncidentSemaineDto
                {
                    Label     = $"S{wn:D2}",
                    EnAttente = sem.Count(d => d.Statut == "EnAttente"),
                    EnCours   = sem.Count(d => d.Statut == "EnCours"),
                    Resolu    = sem.Count(d => d.Statut is "Resolu" or "Cloture"),
                });
                cursor = cursor.AddDays(7);
            }
            return result;
        }

        public Dictionary<string, double> GetUrgenceMoyenneParType()
        {
            return IncidentsRaw
                .Where(i => !string.IsNullOrEmpty(i.TypeIncident))
                .GroupBy(i => i.TypeIncident)
                .ToDictionary(g => g.Key, g => g.Average(i => i.Urgence));
        }
    }
}
