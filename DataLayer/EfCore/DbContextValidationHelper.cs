using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.EfCore
{
    public static class DbContextValidationHelper
    {
        public static async Task<ImmutableList<ValidationResult>> SaveChangesWithValidationAsync(this DbContext context)
        {
            var result = context.ExecuteValidation();
            if (result.Any()) return result;

            context.ChangeTracker.AutoDetectChangesEnabled = false;
            try
            {
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
            finally
            {
                context.ChangeTracker.AutoDetectChangesEnabled = true;
            }

            return result;
        }


        public static ImmutableList<ValidationResult>
            SaveChangesWithValidation(this DbContext context)
        {
            var result = context.ExecuteValidation();
            if (result.Any()) return result;


            context.ChangeTracker.AutoDetectChangesEnabled = false;
            try
            {
                context.SaveChanges();
            }
            finally
            {
                context.ChangeTracker.AutoDetectChangesEnabled = true;
            }

            return result;
        }

        private static ImmutableList<ValidationResult>
            ExecuteValidation(this DbContext context)
        {
            var result = new List<ValidationResult>();
            foreach (var entry in
                     context.ChangeTracker.Entries()
                         .Where(e =>
                             e.State is EntityState.Added or EntityState.Modified))
            {
                var entity = entry.Entity;
                var valProvider = new
                    ValidationDbContextServiceProvider(context);
                var valContext = new
                    ValidationContext(entity, valProvider, null);
                var entityErrors = new List<ValidationResult>();
                if (!Validator.TryValidateObject(
                        entity, valContext, entityErrors, true))
                {
                    result.AddRange(entityErrors);
                }
            }

            return result.ToImmutableList();
        }
    }
}