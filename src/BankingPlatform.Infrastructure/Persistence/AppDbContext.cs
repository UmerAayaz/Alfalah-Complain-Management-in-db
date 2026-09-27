using BankingPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankingPlatform.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<ComplaintCategory> ComplaintCategories => Set<ComplaintCategory>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<DepartmentMembership> DepartmentMemberships => Set<DepartmentMembership>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<ComplaintEvent> ComplaintEvents => Set<ComplaintEvent>();
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowNode> WorkflowNodes => Set<WorkflowNode>();
    public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<WorkflowTask> WorkflowTasks => Set<WorkflowTask>();
    public DbSet<WorkflowNodeField> WorkflowNodeFields => Set<WorkflowNodeField>();
    public DbSet<WorkflowFieldAttachment> WorkflowFieldAttachments => Set<WorkflowFieldAttachment>();
    public DbSet<WorkflowNodeFieldAttachmentConfig> WorkflowNodeFieldAttachmentConfigs => Set<WorkflowNodeFieldAttachmentConfig>();
    public DbSet<WorkflowFieldResponse> WorkflowFieldResponses => Set<WorkflowFieldResponse>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ============================================================
        // INDEXES
        // ============================================================

        modelBuilder.Entity<Department>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<ComplaintCategory>().HasIndex(x => new { x.DepartmentId, x.Code }).IsUnique();
        modelBuilder.Entity<AppUser>().HasIndex(x => x.EmployeeCode).IsUnique();
        modelBuilder.Entity<AppUser>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<DepartmentMembership>().HasIndex(x => new { x.UserId, x.DepartmentId, x.RoleCode }).IsUnique();
        modelBuilder.Entity<Complaint>().HasIndex(x => x.ComplaintNumber).IsUnique();
        modelBuilder.Entity<WorkflowDefinition>().HasIndex(x => new { x.CategoryId, x.Version }).IsUnique();
        modelBuilder.Entity<WorkflowNode>().HasIndex(x => new { x.WorkflowDefinitionId, x.NodeKey }).IsUnique();
        modelBuilder.Entity<WorkflowTask>().HasIndex(x => new { x.Status, x.DueAtUtc });
        modelBuilder.Entity<WorkflowTask>().HasIndex(x => new { x.AssignedToUserId, x.Status });
        modelBuilder.Entity<ComplaintEvent>().HasIndex(x => new { x.ComplaintId, x.CreatedAtUtc });

        modelBuilder.Entity<WorkflowNodeField>()
            .HasIndex(x => new { x.WorkflowNodeId, x.FieldKey })
            .IsUnique();

        modelBuilder.Entity<WorkflowNodeField>()
            .HasIndex(x => new { x.WorkflowNodeId, x.DisplayOrder });

        modelBuilder.Entity<WorkflowFieldResponse>()
            .HasIndex(x => new { x.WorkflowTaskId, x.WorkflowNodeFieldId })
            .IsUnique();

        modelBuilder.Entity<WorkflowFieldResponse>()
            .HasIndex(x => x.ComplaintId);

        // ============================================================
        // PRECISION
        // ============================================================

        modelBuilder.Entity<WorkflowNode>().Property(x => x.PositionX).HasPrecision(18, 2);
        modelBuilder.Entity<WorkflowNode>().Property(x => x.PositionY).HasPrecision(18, 2);

        // ============================================================
        // RELATIONSHIPS — Workflow transitions
        // ============================================================

        modelBuilder.Entity<WorkflowTransition>()
            .HasOne(x => x.SourceNode)
            .WithMany()
            .HasForeignKey(x => x.SourceNodeId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WorkflowTransition>()
            .HasOne(x => x.TargetNode)
            .WithMany()
            .HasForeignKey(x => x.TargetNodeId)
            .OnDelete(DeleteBehavior.NoAction);

        // ============================================================
        // RELATIONSHIPS — Workflow instance / complaint
        // ============================================================

        modelBuilder.Entity<WorkflowInstance>()
            .HasOne(x => x.Complaint)
            .WithMany()
            .HasForeignKey(x => x.ComplaintId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Complaint>()
            .HasOne(x => x.CurrentWorkflowInstance)
            .WithMany()
            .HasForeignKey(x => x.CurrentWorkflowInstanceId)
            .OnDelete(DeleteBehavior.NoAction);

        // -----------------------------------------------------
        // Prevent SQL Server multiple cascade paths.
        // Core workflow/history records should never be
        // automatically deleted through parent records.
        // -----------------------------------------------------

        modelBuilder.Entity<WorkflowDefinition>()
            .HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WorkflowDefinition>()
            .HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WorkflowInstance>()
            .HasOne(x => x.WorkflowDefinition)
            .WithMany()
            .HasForeignKey(x => x.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WorkflowTask>()
            .HasOne(x => x.WorkflowInstance)
            .WithMany()
            .HasForeignKey(x => x.WorkflowInstanceId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WorkflowTask>()
            .HasOne(x => x.WorkflowNode)
            .WithMany()
            .HasForeignKey(x => x.WorkflowNodeId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WorkflowTask>()
            .HasOne(x => x.Complaint)
            .WithMany()
            .HasForeignKey(x => x.ComplaintId)
            .OnDelete(DeleteBehavior.NoAction);

        // ============================================================
        // RELATIONSHIPS — Workflow node fields
        // ============================================================

        modelBuilder.Entity<WorkflowNodeField>()
            .HasOne(x => x.WorkflowNode)
            .WithMany(x => x.Fields)
            .HasForeignKey(x => x.WorkflowNodeId)
            .OnDelete(DeleteBehavior.ClientCascade);

        // ============================================================
        // RELATIONSHIPS — Workflow field responses
        // ============================================================

        modelBuilder.Entity<WorkflowFieldResponse>()
            .HasOne(x => x.Complaint)
            .WithMany()
            .HasForeignKey(x => x.ComplaintId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WorkflowFieldResponse>()
            .HasOne(x => x.WorkflowTask)
            .WithMany()
            .HasForeignKey(x => x.WorkflowTaskId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WorkflowFieldResponse>()
            .HasOne(x => x.WorkflowNode)
            .WithMany()
            .HasForeignKey(x => x.WorkflowNodeId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WorkflowFieldResponse>()
            .HasOne(x => x.WorkflowNodeField)
            .WithMany()
            .HasForeignKey(x => x.WorkflowNodeFieldId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WorkflowFieldResponse>()
            .HasOne(x => x.SubmittedByUser)
            .WithMany()
            .HasForeignKey(x => x.SubmittedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // ============================================================
        // WORKFLOW FIELD ATTACHMENT — DB-only storage
        // ============================================================

        modelBuilder.Entity<WorkflowFieldAttachment>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<WorkflowFieldAttachment>()
            .Property(x => x.FileName)
            .HasMaxLength(255)
            .IsRequired();

        modelBuilder.Entity<WorkflowFieldAttachment>()
            .Property(x => x.ContentType)
            .HasMaxLength(150)
            .IsRequired();

        modelBuilder.Entity<WorkflowFieldAttachment>()
            .Property(x => x.Content)
            .HasColumnType("varbinary(max)")
            .IsRequired();

        modelBuilder.Entity<WorkflowFieldAttachment>()
            .Property(x => x.Sha256Hash)
            .HasMaxLength(64);

        modelBuilder.Entity<WorkflowFieldAttachment>()
            .HasIndex(x => x.UploadedByUserId);

        modelBuilder.Entity<WorkflowFieldAttachment>()
            .HasOne<WorkflowFieldResponse>()
            .WithMany()
            .HasForeignKey(x => x.WorkflowFieldResponseId)
            .OnDelete(DeleteBehavior.Cascade);

        // ============================================================
        // ATTACHMENT CONFIG — 1:1 with WorkflowNodeField
        // ============================================================

        modelBuilder.Entity<WorkflowNodeFieldAttachmentConfig>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<WorkflowNodeFieldAttachmentConfig>()
            .Property(x => x.AllowedFileTypesJson)
            .HasMaxLength(500);

        modelBuilder.Entity<WorkflowNodeFieldAttachmentConfig>()
            .HasIndex(x => x.WorkflowNodeFieldId)
            .IsUnique();

        modelBuilder.Entity<WorkflowNodeFieldAttachmentConfig>()
            .HasOne(x => x.WorkflowNodeField)
            .WithOne(x => x.AttachmentConfig)
            .HasForeignKey<WorkflowNodeFieldAttachmentConfig>(x => x.WorkflowNodeFieldId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}