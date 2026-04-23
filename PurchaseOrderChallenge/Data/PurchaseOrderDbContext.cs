using Microsoft.EntityFrameworkCore;
using PurchaseOrderChallenge.Models;

namespace PurchaseOrderChallenge.Data;

public class PurchaseOrderDbContext(DbContextOptions<PurchaseOrderDbContext> options) : DbContext(options)
{
    public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();
    public DbSet<PurchaseRequestItem> PurchaseRequestItems => Set<PurchaseRequestItem>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();
    public DbSet<PurchaseRequestHistory> PurchaseRequestHistories => Set<PurchaseRequestHistory>();

    /// <summary>
    /// Configura o modelo relacional usado pelo Entity Framework Core,
    /// incluindo chaves, tamanhos de campos, conversão de enums e relacionamentos.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PurchaseRequest>(entity =>
        {
            entity.HasKey(purchaseRequest => purchaseRequest.Id);

            entity.Property(purchaseRequest => purchaseRequest.RequesterName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(purchaseRequest => purchaseRequest.TotalAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(purchaseRequest => purchaseRequest.PurchaseRequestStatus)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.HasMany(purchaseRequest => purchaseRequest.Items)
                .WithOne(item => item.PurchaseRequest)
                .HasForeignKey(item => item.PurchaseRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(purchaseRequest => purchaseRequest.ApprovalSteps)
                .WithOne(step => step.PurchaseRequest)
                .HasForeignKey(step => step.PurchaseRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(purchaseRequest => purchaseRequest.History)
                .WithOne(history => history.PurchaseRequest)
                .HasForeignKey(history => history.PurchaseRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PurchaseRequestItem>(entity =>
        {
            entity.HasKey(item => item.Id);

            entity.Property(item => item.ProductName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(item => item.UnitPrice)
                .HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<ApprovalStep>(entity =>
        {
            entity.HasKey(step => step.Id);

            entity.Property(step => step.ApproverRole)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(step => step.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(step => step.ActionBy)
                .HasMaxLength(150);

            entity.Property(step => step.Comments)
                .HasMaxLength(500);
        });

        modelBuilder.Entity<PurchaseRequestHistory>(entity =>
        {
            entity.HasKey(history => history.Id);

            entity.Property(history => history.ActionType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(history => history.PerformedBy)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(history => history.PerformedByRole)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(history => history.Comments)
                .HasMaxLength(500)
                .IsRequired();
        });
    }
}
