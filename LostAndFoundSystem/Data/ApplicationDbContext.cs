using Microsoft.EntityFrameworkCore;
using LostAndFoundSystem.Models;

namespace LostAndFoundSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<ItemClaim> Claims { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== Recycle Bin: global query filters =====
            // Soft-deleted records are hidden from every normal query automatically.
            // The Recycle Bin page uses .IgnoreQueryFilters() to see them.
            modelBuilder.Entity<Item>()
                .HasQueryFilter(i => !i.IsDeleted);

            modelBuilder.Entity<Notification>()
                .HasQueryFilter(n => !n.IsDeleted);

            // Claims of a deleted item are hidden too (and come back when the item is restored).
            modelBuilder.Entity<ItemClaim>()
                .HasQueryFilter(c => !c.Item!.IsDeleted);

            // Users -> Items -> Claims cascade chain.
            // Second cascade path (Users -> Claims) is not allowed by SQL Server.
            modelBuilder.Entity<ItemClaim>()
                .HasOne(c => c.ClaimedByUser)
                .WithMany()
                .HasForeignKey(c => c.ClaimedByUserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.Student)
                .WithMany()
                .HasForeignKey(c => c.StudentUserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderUserID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}