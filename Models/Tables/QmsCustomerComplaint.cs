using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class QmsCustomerComplaint
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Number { get; set; } = null!;

    public string SourceType { get; set; } = null!;

    public DateOnly ComplaintDate { get; set; }

    public DateOnly ReceivedDate { get; set; }

    public bool ChannelEmail { get; set; }

    public bool ChannelPhone { get; set; }

    public bool ChannelFax { get; set; }

    public bool ChannelMail { get; set; }

    public string? ChannelOther { get; set; }

    public string? CustomerCode { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerAddress { get; set; }

    public string? CustomerPhone { get; set; }

    public string? CustomerEmail { get; set; }

    public string? SalesPointCode { get; set; }

    public string? SalesPointName { get; set; }

    public DateOnly? OrderDate { get; set; }

    public DateOnly? DeliveryDate { get; set; }

    public string? ProductCode { get; set; }

    public string? ProductName { get; set; }

    public string? ProductLot { get; set; }

    public DateOnly? ProductExpiryDate { get; set; }

    public decimal? ProductQuantity { get; set; }

    public Guid? UnitOfMeasureId { get; set; }

    public Guid? ProductStateId { get; set; }

    public int? HealthRiskLevel { get; set; }

    public Guid? ComplaintFindingTypeId { get; set; }

    public Guid? ComplaintReasonId { get; set; }

    public Guid? OrgUnitId { get; set; }

    public string? Description { get; set; }

    public bool SampleAvailable { get; set; }

    public string? SampleDescription { get; set; }

    public string? Analysis { get; set; }

    public string? RootCause { get; set; }

    public string? RootCauseInvestigatedBy { get; set; }

    public bool? IsComplaintJustified { get; set; }

    public string? JustificationDetail { get; set; }

    public string? ImmediateCorrection { get; set; }

    public string? FeedbackToCustomer { get; set; }

    public string? FeedbackToSales { get; set; }

    public DateOnly? ClosedDate { get; set; }

    public Guid WorkflowStatusId { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedAt { get; set; }

    public string? LegacySystem { get; set; }

    public int? LegacyId { get; set; }

    public string? Title { get; set; }

    public virtual QmsComplaintFindingType? ComplaintFindingType { get; set; }

    public virtual QmsComplaintReason? ComplaintReason { get; set; }

    public virtual QmsOrgUnit? OrgUnit { get; set; }

    public virtual QmsProductState? ProductState { get; set; }

    public virtual ICollection<QmsIssueAction> QmsIssueActions { get; set; } = new List<QmsIssueAction>();

    public virtual ICollection<QmsIssue> QmsIssues { get; set; } = new List<QmsIssue>();

    public virtual QmsUnitOfMeasure? UnitOfMeasure { get; set; }

    public virtual QmsWorkflowStatus WorkflowStatus { get; set; } = null!;
}
