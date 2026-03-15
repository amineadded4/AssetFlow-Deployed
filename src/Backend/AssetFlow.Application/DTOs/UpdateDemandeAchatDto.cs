// ============================================================
// AssetFlow.Application / DTOs / UpdateDemandeAchatDto.cs
// DTO pour la modification d'une demande d'achat
// ============================================================

namespace AssetFlow.Application.DTOs
{
    public class UpdateDemandeAchatDto
    {
        public string  NomProduit  { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<CreateLigneDemandeDto> Lignes { get; set; } = new();
    }
}