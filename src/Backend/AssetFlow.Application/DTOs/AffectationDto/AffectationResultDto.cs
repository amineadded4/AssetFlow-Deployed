namespace AssetFlow.Application.DTOs
{
    public class AffectationResultDto
    {
        public bool   Succes        { get; set; }
        public string Message       { get; set; } = string.Empty;
        public int    AffectationId { get; set; }
    }
}