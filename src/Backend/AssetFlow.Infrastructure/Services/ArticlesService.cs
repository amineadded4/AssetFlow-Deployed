using AssetFlow.Application.Interfaces;
using AssetFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetFlow.Application.Services
{
    public class ArticleService : IArticleService
    {
        private readonly AppDbContext _db;
        public ArticleService(AppDbContext db) => _db = db;

        public async Task<(bool Success, string Message, string? NumeroSerie)> UpdateNumeroSerieAsync(int id, string? numeroSerie)
        {
            var article = await _db.ArticlesIndividuels.FindAsync(id);
            if (article is null)
                return (false, "Article introuvable.", null);

            if (!string.IsNullOrWhiteSpace(numeroSerie))
            {
                var ns = numeroSerie.Trim();
                var existe = await _db.ArticlesIndividuels
                    .AnyAsync(a => a.NumeroSerie == ns && a.Id != id);
                if (existe)
                    return (false, "Ce numéro de série est déjà utilisé.", null);

                article.NumeroSerie = ns;
            }
            else
            {
                article.NumeroSerie = null;
            }

            await _db.SaveChangesAsync();
            return (true, "Numéro de série mis à jour.", article.NumeroSerie);
        }
    }
}