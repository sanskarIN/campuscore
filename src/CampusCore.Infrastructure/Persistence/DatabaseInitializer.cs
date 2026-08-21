using CampusCore.Domain.Entities;
using CampusCore.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusCore.Infrastructure.Persistence;

public sealed class DatabaseInitializer(ApplicationDbContext db, RoleManager<IdentityRole> roleManager)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);

        foreach (var role in CampusRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded) throw new InvalidOperationException($"Could not create role {role}: {string.Join(", ", result.Errors.Select(x => x.Description))}");
            }
        }

        if (!await db.InstitutionSettings.AnyAsync(cancellationToken))
            db.InstitutionSettings.Add(new InstitutionSettings());

        if (!await db.GradeScales.AnyAsync(cancellationToken))
        {
            db.GradeScales.AddRange(
                new GradeScale { Name = "Default", MinimumPercentage = 90, MaximumPercentage = 100, Grade = "A+", Description = "Outstanding" },
                new GradeScale { Name = "Default", MinimumPercentage = 80, MaximumPercentage = 89.99m, Grade = "A", Description = "Excellent" },
                new GradeScale { Name = "Default", MinimumPercentage = 70, MaximumPercentage = 79.99m, Grade = "B", Description = "Good" },
                new GradeScale { Name = "Default", MinimumPercentage = 60, MaximumPercentage = 69.99m, Grade = "C", Description = "Satisfactory" },
                new GradeScale { Name = "Default", MinimumPercentage = 50, MaximumPercentage = 59.99m, Grade = "D", Description = "Pass" },
                new GradeScale { Name = "Default", MinimumPercentage = 0, MaximumPercentage = 49.99m, Grade = "F", Description = "Needs improvement" });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
