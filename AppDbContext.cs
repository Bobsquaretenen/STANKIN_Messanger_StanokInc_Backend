using Microsoft.EntityFrameworkCore; 
namespace StankinMessengerApi;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<User> Users => Set<User>();
    public DbSet<GroupChat> GroupChats => Set<GroupChat>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<ChatMember> ChatMembers => Set<ChatMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatMember>().HasKey(cm => new { cm.IdUser, cm.IdChat }); 
    }
} 