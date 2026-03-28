// ============================================================
// AssetFlow.Application / DTOs / SentimentDtos.cs
// ============================================================

namespace AssetFlow.Application.DTOs
{
    /// <summary>Résultat de l'analyse de sentiment pour un matériel</summary>
    public class SentimentMaterielDto
    {
        public int    MaterielId        { get; set; }
        public string MaterielRef       { get; set; } = string.Empty;
        public string MaterielNom       { get; set; } = string.Empty;

        public int    TotalCommentaires { get; set; }
        public int    Positifs          { get; set; }   // nombre d'avis positifs
        public int    Negatifs          { get; set; }   // nombre d'avis négatifs
        public int    Neutres           { get; set; }   // nombre d'avis neutres

        public double PourcentagePositif { get; set; }  // 0-100
        public double PourcentageNegatif { get; set; }  // 0-100
        public double PourcentageNeutre  { get; set; }  // 0-100

        /// <summary>Résumé synthétique généré par l'IA</summary>
        public string Resume            { get; set; } = string.Empty;

        /// <summary>Score de satisfaction global de 1 à 5</summary>
        public double ScoreGlobal       { get; set; }

        /// <summary>Sentiment dominant : "Positif", "Négatif", "Neutre", "Mitigé"</summary>
        public string SentimentDominant { get; set; } = string.Empty;
    }

    /// <summary>Requête d'analyse</summary>
    public class AnalyseSentimentRequestDto
    {
        public int MaterielId { get; set; }
    }

    /// <summary>Payload interne envoyé à l'IA</summary>
    public class SentimentCommentairePayload
    {
        public int    Id      { get; set; }
        public string Contenu { get; set; } = string.Empty;
    }
}
