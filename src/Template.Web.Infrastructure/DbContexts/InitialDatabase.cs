using Template.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Template.Web.Infrastructure.DbContexts
{
    public class InitialDatabase
    {
        public InitialDatabase(
            ApiDbContext apiDbContext,
            ILogger<InitialDatabase> logger
            )
        {
            _apiDbContext = apiDbContext;
            _logger = logger;
        }

        private readonly ApiDbContext _apiDbContext;
        private readonly ILogger<InitialDatabase> _logger;

        public async Task Initialize()
        {
            if (!await _apiDbContext.Database.EnsureCreatedAsync())
            {
                _logger.LogInformation("database already created");
                //await _apiDbContext.Database.MigrateAsync();
            }
            var permissions = await _apiDbContext.Permissions
                .ToArrayAsync();
            var transaction = await _apiDbContext.Database.BeginTransactionAsync();
            try
            {
                //if (permissions.Length != Scope.Seeds.Length || permissions.Any(ss => Scope.Seeds.Contains(ss)))
                //{
                //    _apiDbContext.permissions.RemoveRange(permissions);
                //    await _apiDbContext.SaveChangesAsync();
                //    await _apiDbContext.AddRangeAsync(Scope.Seeds);
                //}
                //var count = await _apiDbContext.SaveChangesAsync();
                //await transaction.CommitAsync();
                //_logger.LogInformation($"scope table generate succeed, with {count} new permissions");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}