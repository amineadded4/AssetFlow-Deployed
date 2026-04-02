using AssetFlow.BlazorUI.Services;
using Microsoft.AspNetCore.Components;

namespace AssetFlow.BlazorUI.Pages.Mobile
{
    public partial class FicheMobile
    {
        [Inject] private EmployeService EmployeService { get; set; } = default!;

        // Paramètre URL : /fiche/{AffectationId}
        [Parameter] public int AffectationId { get; set; }
        [Parameter] public int ArticleId { get; set; } = 0;

        private EquipementAffecteDto? Equipement { get; set; }
        private bool IsLoading { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                // Réutilise GetEquipementDetailAsync — endpoint public
                Equipement = await EmployeService.GetEquipementDetailAsync(AffectationId, ArticleId);
            }
            catch
            {
                Equipement = null;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string GetStatutLabel(string statut) => statut switch
        {
            "EnCours"   => "En Service",
            "Retourne"  => "Retourné",
            "Perdu"     => "Perdu",
            "Endommage" => "Endommagé",
            _           => statut
        };
    }
}