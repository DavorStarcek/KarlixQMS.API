using System;
using System.Collections.Generic;
using KarlixQMS.API.Models.Tables;
using Microsoft.EntityFrameworkCore;

namespace KarlixQMS.API.Data;

public partial class QmsDbContext : DbContext
{
    public QmsDbContext(DbContextOptions<QmsDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<QmsAction> QmsActions { get; set; }

    public virtual DbSet<QmsActionType> QmsActionTypes { get; set; }

    public virtual DbSet<QmsAttachment> QmsAttachments { get; set; }

    public virtual DbSet<QmsAudit> QmsAudits { get; set; }

    public virtual DbSet<QmsAuditAuditor> QmsAuditAuditors { get; set; }

    public virtual DbSet<QmsAuditStandard> QmsAuditStandards { get; set; }

    public virtual DbSet<QmsAuditType> QmsAuditTypes { get; set; }

    public virtual DbSet<QmsComplaintFindingType> QmsComplaintFindingTypes { get; set; }

    public virtual DbSet<QmsComplaintReason> QmsComplaintReasons { get; set; }

    public virtual DbSet<QmsCustomerComplaint> QmsCustomerComplaints { get; set; }

    public virtual DbSet<QmsEffectiveness> QmsEffectivenesses { get; set; }

    public virtual DbSet<QmsIssue> QmsIssues { get; set; }

    public virtual DbSet<QmsIssueAction> QmsIssueActions { get; set; }

    public virtual DbSet<QmsIssueAttachment> QmsIssueAttachments { get; set; }

    public virtual DbSet<QmsIssueLink> QmsIssueLinks { get; set; }

    public virtual DbSet<QmsNonconformity> QmsNonconformities { get; set; }

    public virtual DbSet<QmsNonconformityRelationType> QmsNonconformityRelationTypes { get; set; }

    public virtual DbSet<QmsOrgUnit> QmsOrgUnits { get; set; }

    public virtual DbSet<QmsProductState> QmsProductStates { get; set; }

    public virtual DbSet<QmsStandard> QmsStandards { get; set; }

    public virtual DbSet<QmsStandardRequirement> QmsStandardRequirements { get; set; }

    public virtual DbSet<QmsUnitOfMeasure> QmsUnitOfMeasures { get; set; }

    public virtual DbSet<QmsWorkflowStatus> QmsWorkflowStatuses { get; set; }

    public virtual DbSet<vw_QmsActionList> vw_QmsActionLists { get; set; }

    public virtual DbSet<vw_QmsActionOverview> vw_QmsActionOverviews { get; set; }

    public virtual DbSet<vw_QmsCustomerComplaintList> vw_QmsCustomerComplaintLists { get; set; }

    public virtual DbSet<vw_QmsCustomerComplaint_Analysis> vw_QmsCustomerComplaint_Analyses { get; set; }

    public virtual DbSet<vw_QmsDashboard> vw_QmsDashboards { get; set; }

    public virtual DbSet<vw_QmsIssueActionAnalysis> vw_QmsIssueActionAnalyses { get; set; }

    public virtual DbSet<vw_QmsIssueKpiMonthly> vw_QmsIssueKpiMonthlies { get; set; }

    public virtual DbSet<vw_QmsIssueList> vw_QmsIssueLists { get; set; }

    public virtual DbSet<vw_QmsIssue_Action> vw_QmsIssue_Actions { get; set; }

    public virtual DbSet<vw_QmsIssue_CombinedAnalysis> vw_QmsIssue_CombinedAnalyses { get; set; }

    public virtual DbSet<vw_QmsNonconformityList> vw_QmsNonconformityLists { get; set; }

    public virtual DbSet<vw_QmsNonconformity_Analysis> vw_QmsNonconformity_Analyses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QmsAction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QmsActio__3214EC0762EAA3FF");

            entity.ToTable("QmsAction");

            entity.HasIndex(e => e.ActionTypeId, "IX_QmsAction_ActionType");

            entity.HasIndex(e => e.DueDate, "IX_QmsAction_DueDate");

            entity.HasIndex(e => new { e.TenantId, e.EntityType, e.EntityId }, "IX_QmsAction_Tenant_Entity");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedBy).HasMaxLength(200);
            entity.Property(e => e.EntityType).HasMaxLength(30);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(200);
            entity.Property(e => e.Responsible).HasMaxLength(200);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.ActionType).WithMany(p => p.QmsActions)
                .HasForeignKey(d => d.ActionTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QmsAction_ActionType");

            entity.HasOne(d => d.Effectiveness).WithMany(p => p.QmsActions)
                .HasForeignKey(d => d.EffectivenessId)
                .HasConstraintName("FK_QmsAction_Effectiveness");

            entity.HasOne(d => d.OrgUnit).WithMany(p => p.QmsActions)
                .HasForeignKey(d => d.OrgUnitId)
                .HasConstraintName("FK_QmsAction_OrgUnit");
        });

        modelBuilder.Entity<QmsActionType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QmsActio__3214EC070C40CBFB");

            entity.ToTable("QmsActionType");

            entity.HasIndex(e => new { e.TenantId, e.Code }, "UQ_QmsActionType_Tenant_Code").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<QmsAttachment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QmsAttac__3214EC07B896BD9A");

            entity.ToTable("QmsAttachment");

            entity.HasIndex(e => new { e.TenantId, e.EntityType, e.EntityId }, "IX_QmsAttachment_Tenant_Entity");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.EntityType).HasMaxLength(30);
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UploadedBy).HasMaxLength(200);
        });

        modelBuilder.Entity<QmsAudit>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QmsAudit__3214EC077CE1C973");

            entity.ToTable("QmsAudit");

            entity.HasIndex(e => e.StartDate, "IX_QmsAudit_StartDate");

            entity.HasIndex(e => new { e.TenantId, e.Number }, "IX_QmsAudit_Tenant_Number");

            entity.HasIndex(e => e.WorkflowStatusId, "IX_QmsAudit_WorkflowStatus");

            entity.HasIndex(e => new { e.TenantId, e.Number }, "UQ_QmsAudit_Tenant_Number").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Auditee).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedBy).HasMaxLength(200);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(200);
            entity.Property(e => e.LeadAuditor).HasMaxLength(200);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Number).HasMaxLength(50);

            entity.HasOne(d => d.AuditType).WithMany(p => p.QmsAudits)
                .HasForeignKey(d => d.AuditTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QmsAudit_AuditType");

            entity.HasOne(d => d.OrgUnit).WithMany(p => p.QmsAudits)
                .HasForeignKey(d => d.OrgUnitId)
                .HasConstraintName("FK_QmsAudit_OrgUnit");

            entity.HasOne(d => d.WorkflowStatus).WithMany(p => p.QmsAudits)
                .HasForeignKey(d => d.WorkflowStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QmsAudit_WorkflowStatus");
        });

        modelBuilder.Entity<QmsAuditAuditor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QmsAudit__3214EC07CC4F5523");

            entity.ToTable("QmsAuditAuditor");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Role).HasMaxLength(100);

            entity.HasOne(d => d.Audit).WithMany(p => p.QmsAuditAuditors)
                .HasForeignKey(d => d.AuditId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QmsAuditAuditor_Audit");
        });

        modelBuilder.Entity<QmsAuditStandard>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QmsAudit__3214EC075F91D598");

            entity.ToTable("QmsAuditStandard");

            entity.HasIndex(e => new { e.AuditId, e.StandardId }, "UQ_QmsAuditStandard").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Audit).WithMany(p => p.QmsAuditStandards)
                .HasForeignKey(d => d.AuditId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QmsAuditStandard_Audit");

            entity.HasOne(d => d.Standard).WithMany(p => p.QmsAuditStandards)
                .HasForeignKey(d => d.StandardId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QmsAuditStandard_Standard");
        });

        modelBuilder.Entity<QmsAuditType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QmsAudit__3214EC073937C3A2");

            entity.ToTable("QmsAuditType");

            entity.HasIndex(e => new { e.TenantId, e.Code }, "UQ_QmsAuditType_Tenant_Code").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(10);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<QmsComplaintFindingType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QmsCompl__3214EC07A95AE7C8");

            entity.ToTable("QmsComplaintFindingType");

            entity.HasIndex(e => new { e.TenantId, e.Code }, "UQ_QmsComplaintFindingType_Tenant_Code").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<QmsComplaintReason>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QmsCompl__3214EC07982B15CE");

            entity.ToTable("QmsComplaintReason");

            entity.HasIndex(e => new { e.TenantId, e.Code }, "UQ_QmsComplaintReason_Tenant_Code").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<QmsCustomerComplaint>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QmsCusto__3214EC0712EA37F0");

            entity.ToTable("QmsCustomerComplaint");

            entity.HasIndex(e => e.ComplaintDate, "IX_QmsCustomerComplaint_ComplaintDate");

            entity.HasIndex(e => e.OrgUnitId, "IX_QmsCustomerComplaint_OrgUnit");

            entity.HasIndex(e => new { e.TenantId, e.ComplaintDate, e.ReceivedDate }, "IX_QmsCustomerComplaint_Tenant_Date");

            entity.HasIndex(e => new { e.TenantId, e.Number }, "IX_QmsCustomerComplaint_Tenant_Number");

            entity.HasIndex(e => e.WorkflowStatusId, "IX_QmsCustomerComplaint_WorkflowStatus");

            entity.HasIndex(e => new { e.TenantId, e.Number }, "UQ_QmsCustomerComplaint_Tenant_Number").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ChannelOther).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedBy).HasMaxLength(200);
            entity.Property(e => e.CustomerAddress).HasMaxLength(500);
            entity.Property(e => e.CustomerCode).HasMaxLength(50);
            entity.Property(e => e.CustomerEmail).HasMaxLength(200);
            entity.Property(e => e.CustomerName).HasMaxLength(200);
            entity.Property(e => e.CustomerPhone).HasMaxLength(50);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(200);
            entity.Property(e => e.LegacySystem).HasMaxLength(50);
            entity.Property(e => e.Number).HasMaxLength(50);
            entity.Property(e => e.ProductCode).HasMaxLength(50);
            entity.Property(e => e.ProductLot).HasMaxLength(100);
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.ProductQuantity).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.RootCauseInvestigatedBy).HasMaxLength(200);
            entity.Property(e => e.SalesPointCode).HasMaxLength(50);
            entity.Property(e => e.SalesPointName).HasMaxLength(200);
            entity.Property(e => e.SourceType).HasMaxLength(20);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.ComplaintFindingType).WithMany(p => p.QmsCustomerComplaints)
                .HasForeignKey(d => d.ComplaintFindingTypeId)
                .HasConstraintName("FK_QmsCustomerComplaint_FindingType");

            entity.HasOne(d => d.ComplaintReason).WithMany(p => p.QmsCustomerComplaints)
                .HasForeignKey(d => d.ComplaintReasonId)
                .HasConstraintName("FK_QmsCustomerComplaint_Reason");

            entity.HasOne(d => d.OrgUnit).WithMany(p => p.QmsCustomerComplaints)
                .HasForeignKey(d => d.OrgUnitId)
                .HasConstraintName("FK_QmsCustomerComplaint_OrgUnit");

            entity.HasOne(d => d.ProductState).WithMany(p => p.QmsCustomerComplaints)
                .HasForeignKey(d => d.ProductStateId)
                .HasConstraintName("FK_QmsCustomerComplaint_ProductState");

            entity.HasOne(d => d.UnitOfMeasure).WithMany(p => p.QmsCustomerComplaints)
                .HasForeignKey(d => d.UnitOfMeasureId)
                .HasConstraintName("FK_QmsCustomerComplaint_Uom");

            entity.HasOne(d => d.WorkflowStatus).WithMany(p => p.QmsCustomerComplaints)
                .HasForeignKey(d => d.WorkflowStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QmsCustomerComplaint_WorkflowStatus");
        });

        modelBuilder.Entity<QmsEffectiveness>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QmsEffec__3214EC07DE6C371D");

            entity.ToTable("QmsEffectiveness");

            entity.HasIndex(e => new { e.TenantId, e.Code }, "UQ_QmsEffectiveness_Tenant_Code").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<QmsIssue>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QmsIssue__3214EC07F6A50E7D");

            entity.ToTable("QmsIssue");

            entity.HasIndex(e => e.EntityType, "IX_QmsIssue_EntityType");

            entity.HasIndex(e => e.TenantId, "IX_QmsIssue_TenantId");

            entity.HasIndex(e => e.WorkflowStatusId, "IX_QmsIssue_WorkflowStatusId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.EntityType).HasMaxLength(50);

            entity.HasOne(d => d.Complaint).WithMany(p => p.QmsIssues)
                .HasForeignKey(d => d.ComplaintId)
                .HasConstraintName("FK_QmsIssue_Complaint");

            entity.HasOne(d => d.Nonconformity).WithMany(p => p.QmsIssues)
                .HasForeignKey(d => d.NonconformityId)
                .HasConstraintName("FK_QmsIssue_Nonconformity");

            entity.HasOne(d => d.WorkflowStatus).WithMany(p => p.QmsIssues)
                .HasForeignKey(d => d.WorkflowStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QmsIssue_WorkflowStatus");
        });

        modelBuilder.Entity<QmsIssueAction>(entity =>
        {
            entity.ToTable("QmsIssueAction");

            entity.HasIndex(e => e.IssueId, "IX_QmsIssueAction_IssueId");

            entity.HasIndex(e => new { e.TenantId, e.ActionTypeId }, "IX_QmsIssueAction_Tenant_ActionType");

            entity.HasIndex(e => new { e.TenantId, e.CustomerComplaintId }, "IX_QmsIssueAction_Tenant_Complaint");

            entity.HasIndex(e => new { e.TenantId, e.NonconformityId }, "IX_QmsIssueAction_Tenant_Nonconformity");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ResponsibleName).HasMaxLength(200);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasPrecision(3);

            entity.HasOne(d => d.ActionType).WithMany(p => p.QmsIssueActions)
                .HasForeignKey(d => d.ActionTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QmsIssueAction_ActionType");

            entity.HasOne(d => d.CustomerComplaint).WithMany(p => p.QmsIssueActions)
                .HasForeignKey(d => d.CustomerComplaintId)
                .HasConstraintName("FK_QmsIssueAction_CustomerComplaint");

            entity.HasOne(d => d.Effectiveness).WithMany(p => p.QmsIssueActions)
                .HasForeignKey(d => d.EffectivenessId)
                .HasConstraintName("FK_QmsIssueAction_Effectiveness");

            entity.HasOne(d => d.Issue).WithMany(p => p.QmsIssueActions)
                .HasForeignKey(d => d.IssueId)
                .HasConstraintName("FK_QmsIssueAction_Issue");

            entity.HasOne(d => d.Nonconformity).WithMany(p => p.QmsIssueActions)
                .HasForeignKey(d => d.NonconformityId)
                .HasConstraintName("FK_QmsIssueAction_Nonconformity");

            entity.HasOne(d => d.ResponsibleOrgUnit).WithMany(p => p.QmsIssueActions)
                .HasForeignKey(d => d.ResponsibleOrgUnitId)
                .HasConstraintName("FK_QmsIssueAction_OrgUnit");
        });

        modelBuilder.Entity<QmsIssueAttachment>(entity =>
        {
            entity.ToTable("QmsIssueAttachment");

            entity.HasIndex(e => e.IssueId, "IX_QmsIssueAttachment_Issue");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.StoragePath).HasMaxLength(1000);
            entity.Property(e => e.StorageProvider).HasMaxLength(50);

            entity.HasOne(d => d.Issue).WithMany(p => p.QmsIssueAttachments)
                .HasForeignKey(d => d.IssueId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QmsIssueAttachment_Issue");
        });

        modelBuilder.Entity<QmsIssueLink>(entity =>
        {
            entity.ToTable("QmsIssueLink");

            entity.HasIndex(e => e.FromIssueId, "IX_QmsIssueLink_FromIssue");

            entity.HasIndex(e => e.ToIssueId, "IX_QmsIssueLink_ToIssue");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.LinkType).HasMaxLength(50);
            entity.Property(e => e.Note).HasMaxLength(1000);

            entity.HasOne(d => d.FromIssue).WithMany(p => p.QmsIssueLinkFromIssues)
                .HasForeignKey(d => d.FromIssueId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QmsIssueLink_FromIssue");

            entity.HasOne(d => d.ToIssue).WithMany(p => p.QmsIssueLinkToIssues)
                .HasForeignKey(d => d.ToIssueId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QmsIssueLink_ToIssue");
        });

        modelBuilder.Entity<QmsNonconformity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QmsNonco__3214EC0703E5FE4F");

            entity.ToTable("QmsNonconformity");

            entity.HasIndex(e => e.OrgUnitId, "IX_QmsNonconformity_OrgUnit");

            entity.HasIndex(e => e.RaisedAt, "IX_QmsNonconformity_RaisedAt");

            entity.HasIndex(e => new { e.TenantId, e.Number }, "IX_QmsNonconformity_Tenant_Number");

            entity.HasIndex(e => e.WorkflowStatusId, "IX_QmsNonconformity_WorkflowStatus");

            entity.HasIndex(e => new { e.TenantId, e.Number }, "UQ_QmsNonconformity_Tenant_Number").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedBy).HasMaxLength(200);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(200);
            entity.Property(e => e.LegacySystem).HasMaxLength(50);
            entity.Property(e => e.Number).HasMaxLength(50);
            entity.Property(e => e.RaisedBy).HasMaxLength(200);
            entity.Property(e => e.RootCauseInvestigatedBy).HasMaxLength(200);
            entity.Property(e => e.Source).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(500);
            entity.Property(e => e.VerifiedBy).HasMaxLength(200);

            entity.HasOne(d => d.Audit).WithMany(p => p.QmsNonconformities)
                .HasForeignKey(d => d.AuditId)
                .HasConstraintName("FK_QmsNonconformity_Audit");

            entity.HasOne(d => d.Effectiveness).WithMany(p => p.QmsNonconformities)
                .HasForeignKey(d => d.EffectivenessId)
                .HasConstraintName("FK_QmsNonconformity_Effectiveness");

            entity.HasOne(d => d.OrgUnit).WithMany(p => p.QmsNonconformities)
                .HasForeignKey(d => d.OrgUnitId)
                .HasConstraintName("FK_QmsNonconformity_OrgUnit");

            entity.HasOne(d => d.RelationType).WithMany(p => p.QmsNonconformities)
                .HasForeignKey(d => d.RelationTypeId)
                .HasConstraintName("FK_QmsNonconformity_RelationType");

            entity.HasOne(d => d.StandardRequirement).WithMany(p => p.QmsNonconformities)
                .HasForeignKey(d => d.StandardRequirementId)
                .HasConstraintName("FK_QmsNonconformity_StandardRequirement");

            entity.HasOne(d => d.WorkflowStatus).WithMany(p => p.QmsNonconformities)
                .HasForeignKey(d => d.WorkflowStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QmsNonconformity_WorkflowStatus");
        });

        modelBuilder.Entity<QmsNonconformityRelationType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QmsNonco__3214EC072BF17E54");

            entity.ToTable("QmsNonconformityRelationType");

            entity.HasIndex(e => new { e.TenantId, e.Code }, "UQ_QmsNonconformityRelationType_Tenant_Code").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<QmsOrgUnit>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QmsOrgUn__3214EC07E53F200D");

            entity.ToTable("QmsOrgUnit");

            entity.HasIndex(e => new { e.TenantId, e.Code }, "UQ_QmsOrgUnit_Tenant_Code").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<QmsProductState>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QmsProdu__3214EC0788F83107");

            entity.ToTable("QmsProductState");

            entity.HasIndex(e => new { e.TenantId, e.Code }, "UQ_QmsProductState_Tenant_Code").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<QmsStandard>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QmsStand__3214EC0719B2B1BC");

            entity.ToTable("QmsStandard");

            entity.HasIndex(e => new { e.TenantId, e.Code }, "UQ_QmsStandard_Tenant_Code").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<QmsStandardRequirement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QmsStand__3214EC0759C71412");

            entity.ToTable("QmsStandardRequirement");

            entity.HasIndex(e => new { e.TenantId, e.StandardId, e.Code }, "UQ_QmsStandardRequirement_Tenant_Standard_Code").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Title).HasMaxLength(500);

            entity.HasOne(d => d.Standard).WithMany(p => p.QmsStandardRequirements)
                .HasForeignKey(d => d.StandardId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QmsStandardRequirement_Standard");
        });

        modelBuilder.Entity<QmsUnitOfMeasure>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QmsUnitO__3214EC074F3F9868");

            entity.ToTable("QmsUnitOfMeasure");

            entity.HasIndex(e => new { e.TenantId, e.Code }, "UQ_QmsUnitOfMeasure_Tenant_Code").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<QmsWorkflowStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__QmsWorkf__3214EC07E92E47A7");

            entity.ToTable("QmsWorkflowStatus");

            entity.HasIndex(e => new { e.TenantId, e.EntityType, e.Code }, "UQ_QmsWorkflowStatus_Tenant_EntityType_Code").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.EntityType).HasMaxLength(30);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<vw_QmsActionList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_QmsActionList");

            entity.Property(e => e.ActionTypeCode).HasMaxLength(50);
            entity.Property(e => e.ActionTypeName).HasMaxLength(200);
            entity.Property(e => e.EffectivenessCode).HasMaxLength(50);
            entity.Property(e => e.EffectivenessName).HasMaxLength(200);
            entity.Property(e => e.EntityNumber).HasMaxLength(50);
            entity.Property(e => e.EntityType).HasMaxLength(30);
            entity.Property(e => e.OrgUnitCode).HasMaxLength(50);
            entity.Property(e => e.OrgUnitName).HasMaxLength(200);
            entity.Property(e => e.Responsible).HasMaxLength(200);
            entity.Property(e => e.Title).HasMaxLength(200);
        });

        modelBuilder.Entity<vw_QmsActionOverview>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_QmsActionOverview");

            entity.Property(e => e.ActionStatus)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.ActionTitle).HasMaxLength(200);
            entity.Property(e => e.ActionTypeCode).HasMaxLength(50);
            entity.Property(e => e.ActionTypeName).HasMaxLength(200);
            entity.Property(e => e.EffectivenessCode).HasMaxLength(50);
            entity.Property(e => e.EffectivenessName).HasMaxLength(200);
            entity.Property(e => e.EntityType).HasMaxLength(50);
            entity.Property(e => e.IssueNumber).HasMaxLength(50);
            entity.Property(e => e.IssueStatusCode).HasMaxLength(50);
            entity.Property(e => e.IssueStatusName).HasMaxLength(200);
            entity.Property(e => e.IssueTitle).HasMaxLength(500);
            entity.Property(e => e.ResponsibleName).HasMaxLength(200);
            entity.Property(e => e.ResponsibleOrgUnitCode).HasMaxLength(50);
            entity.Property(e => e.ResponsibleOrgUnitName).HasMaxLength(200);
        });

        modelBuilder.Entity<vw_QmsCustomerComplaintList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_QmsCustomerComplaintList");

            entity.Property(e => e.ComplaintReasonCode).HasMaxLength(50);
            entity.Property(e => e.ComplaintReasonName).HasMaxLength(200);
            entity.Property(e => e.CustomerAddress).HasMaxLength(500);
            entity.Property(e => e.CustomerCode).HasMaxLength(50);
            entity.Property(e => e.CustomerEmail).HasMaxLength(200);
            entity.Property(e => e.CustomerName).HasMaxLength(200);
            entity.Property(e => e.CustomerPhone).HasMaxLength(50);
            entity.Property(e => e.Number).HasMaxLength(50);
            entity.Property(e => e.OrgUnitCode).HasMaxLength(50);
            entity.Property(e => e.OrgUnitName).HasMaxLength(200);
            entity.Property(e => e.ProductCode).HasMaxLength(50);
            entity.Property(e => e.ProductLot).HasMaxLength(100);
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.ProductQuantity).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.ProductStateCode).HasMaxLength(50);
            entity.Property(e => e.ProductStateName).HasMaxLength(200);
            entity.Property(e => e.RootCauseInvestigatedBy).HasMaxLength(200);
            entity.Property(e => e.SalesPointCode).HasMaxLength(50);
            entity.Property(e => e.SalesPointName).HasMaxLength(200);
            entity.Property(e => e.SourceType).HasMaxLength(20);
            entity.Property(e => e.StatusCode).HasMaxLength(50);
            entity.Property(e => e.StatusName).HasMaxLength(200);
            entity.Property(e => e.UnitOfMeasureCode).HasMaxLength(50);
            entity.Property(e => e.UnitOfMeasureName).HasMaxLength(200);
        });

        modelBuilder.Entity<vw_QmsCustomerComplaint_Analysis>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_QmsCustomerComplaint_Analysis");

            entity.Property(e => e.CustomerAddress).HasMaxLength(500);
            entity.Property(e => e.CustomerCode).HasMaxLength(50);
            entity.Property(e => e.CustomerName).HasMaxLength(200);
            entity.Property(e => e.CustomerPhone).HasMaxLength(50);
            entity.Property(e => e.LegacySystem).HasMaxLength(50);
            entity.Property(e => e.Number).HasMaxLength(50);
            entity.Property(e => e.SourceType).HasMaxLength(20);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.WorkflowCode).HasMaxLength(50);
            entity.Property(e => e.WorkflowEntityType).HasMaxLength(30);
            entity.Property(e => e.WorkflowName).HasMaxLength(200);
        });

        modelBuilder.Entity<vw_QmsDashboard>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_QmsDashboard");

            entity.Property(e => e.EntityType).HasMaxLength(20);
        });

        modelBuilder.Entity<vw_QmsIssueActionAnalysis>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_QmsIssueActionAnalysis");

            entity.Property(e => e.ActionTitle).HasMaxLength(200);
            entity.Property(e => e.ActionTypeCode).HasMaxLength(50);
            entity.Property(e => e.ActionTypeName).HasMaxLength(200);
            entity.Property(e => e.EffectivenessCode).HasMaxLength(50);
            entity.Property(e => e.EffectivenessName).HasMaxLength(200);
            entity.Property(e => e.EntityType).HasMaxLength(50);
            entity.Property(e => e.IssueNumber).HasMaxLength(50);
            entity.Property(e => e.IssueTitle).HasMaxLength(500);
            entity.Property(e => e.WorkflowStatusCode).HasMaxLength(50);
            entity.Property(e => e.WorkflowStatusName).HasMaxLength(200);
        });

        modelBuilder.Entity<vw_QmsIssueKpiMonthly>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_QmsIssueKpiMonthly");

            entity.Property(e => e.EntityType).HasMaxLength(20);
        });

        modelBuilder.Entity<vw_QmsIssueList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_QmsIssueList");

            entity.Property(e => e.CustomerName).HasMaxLength(200);
            entity.Property(e => e.EntityType).HasMaxLength(20);
            entity.Property(e => e.Number).HasMaxLength(50);
            entity.Property(e => e.StatusCode).HasMaxLength(50);
            entity.Property(e => e.StatusName).HasMaxLength(200);
            entity.Property(e => e.Title).HasMaxLength(500);
        });

        modelBuilder.Entity<vw_QmsIssue_Action>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_QmsIssue_Actions");

            entity.Property(e => e.ActionTitle).HasMaxLength(200);
            entity.Property(e => e.ActionTypeCode).HasMaxLength(50);
            entity.Property(e => e.ActionTypeName).HasMaxLength(200);
            entity.Property(e => e.ComplaintTitle).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasPrecision(3);
            entity.Property(e => e.EffectivenessCode).HasMaxLength(50);
            entity.Property(e => e.EffectivenessName).HasMaxLength(200);
            entity.Property(e => e.IssueKind)
                .HasMaxLength(13)
                .IsUnicode(false);
            entity.Property(e => e.IssueNumber).HasMaxLength(50);
            entity.Property(e => e.IssueTitle).HasMaxLength(500);
            entity.Property(e => e.NonconformityTitle).HasMaxLength(500);
            entity.Property(e => e.ResponsibleName).HasMaxLength(200);
            entity.Property(e => e.ResponsibleOrgUnitCode).HasMaxLength(50);
            entity.Property(e => e.ResponsibleOrgUnitName).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasPrecision(3);
        });

        modelBuilder.Entity<vw_QmsIssue_CombinedAnalysis>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_QmsIssue_CombinedAnalysis");

            entity.Property(e => e.CustomerAddress).HasMaxLength(500);
            entity.Property(e => e.CustomerCode).HasMaxLength(50);
            entity.Property(e => e.CustomerName).HasMaxLength(200);
            entity.Property(e => e.CustomerPhone).HasMaxLength(50);
            entity.Property(e => e.IssueKind)
                .HasMaxLength(13)
                .IsUnicode(false);
            entity.Property(e => e.Number).HasMaxLength(50);
            entity.Property(e => e.OrgUnitCode).HasMaxLength(50);
            entity.Property(e => e.OrgUnitName).HasMaxLength(200);
            entity.Property(e => e.SourceType).HasMaxLength(20);
            entity.Property(e => e.Title).HasMaxLength(500);
            entity.Property(e => e.WorkflowCode).HasMaxLength(50);
            entity.Property(e => e.WorkflowEntityType).HasMaxLength(30);
            entity.Property(e => e.WorkflowName).HasMaxLength(200);
        });

        modelBuilder.Entity<vw_QmsNonconformityList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_QmsNonconformityList");

            entity.Property(e => e.EffectivenessCode).HasMaxLength(50);
            entity.Property(e => e.EffectivenessName).HasMaxLength(200);
            entity.Property(e => e.Number).HasMaxLength(50);
            entity.Property(e => e.OrgUnitCode).HasMaxLength(50);
            entity.Property(e => e.OrgUnitName).HasMaxLength(200);
            entity.Property(e => e.RelationTypeCode).HasMaxLength(50);
            entity.Property(e => e.RelationTypeName).HasMaxLength(200);
            entity.Property(e => e.RequirementCode).HasMaxLength(50);
            entity.Property(e => e.RequirementTitle).HasMaxLength(500);
            entity.Property(e => e.Source).HasMaxLength(50);
            entity.Property(e => e.StandardCode).HasMaxLength(50);
            entity.Property(e => e.StandardName).HasMaxLength(200);
            entity.Property(e => e.StatusCode).HasMaxLength(50);
            entity.Property(e => e.StatusName).HasMaxLength(200);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.VerifiedBy).HasMaxLength(200);
        });

        modelBuilder.Entity<vw_QmsNonconformity_Analysis>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_QmsNonconformity_Analysis");

            entity.Property(e => e.EffectivenessCode).HasMaxLength(50);
            entity.Property(e => e.EffectivenessName).HasMaxLength(200);
            entity.Property(e => e.Number).HasMaxLength(50);
            entity.Property(e => e.OrgUnitCode).HasMaxLength(50);
            entity.Property(e => e.OrgUnitName).HasMaxLength(200);
            entity.Property(e => e.Title).HasMaxLength(500);
            entity.Property(e => e.WorkflowCode).HasMaxLength(50);
            entity.Property(e => e.WorkflowEntityType).HasMaxLength(30);
            entity.Property(e => e.WorkflowName).HasMaxLength(200);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
