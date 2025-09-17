using AvailabilityApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AvailabilityApi.Infrastructure;

public class AvailabilityContext : DbContext
{
    public AvailabilityContext(DbContextOptions<AvailabilityContext> options): base(options)
    {
    }
    
    public DbSet<Resource> Resources { get; set; }
}