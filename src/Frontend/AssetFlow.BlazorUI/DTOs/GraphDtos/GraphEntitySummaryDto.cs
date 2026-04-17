namespace AssetFlow.BlazorUI.DTOs
{
    public class GraphEntitySummaryDto
    {
        public string Id     { get; set; } = string.Empty;
        public string Label  { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public string Type   { get; set; } = string.Empty;
        public string Status { get; set; } = "normal";
        public int    Count  { get; set; } = 0;
    }
}