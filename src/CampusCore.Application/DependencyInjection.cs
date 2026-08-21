using CampusCore.Application.Academics;
using CampusCore.Application.Dashboard;
using CampusCore.Application.Reports;
using CampusCore.Application.Students;
using Microsoft.Extensions.DependencyInjection;

namespace CampusCore.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<StudentService>();
        services.AddScoped<AcademicService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<ReportService>();
        return services;
    }
}
