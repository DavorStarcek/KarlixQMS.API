using System;

namespace KarlixQMS.API.Models.Views
{
    // Keyless entity za dbo.vw_QmsActionOverview
    public class VwQmsActionOverview
    {
        public Guid ActionId { get; set; }
        public Guid? IssueId { get; set; }
        public string? EntityType { get; set; }
        public string? IssueNumber { get; set; }
        public string? IssueTitle { get; set; }
        public DateTime? IssueDate { get; set; }

        public string? ActionTitle { get; set; }
        public string? ActionDescription { get; set; }

        public string? ActionTypeCode { get; set; }
        public string? ActionTypeName { get; set; }

        public string? ResponsibleName { get; set; }
        public Guid? ResponsibleOrgUnitId { get; set; }
        public string? ResponsibleOrgUnitName { get; set; }

        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        public string? EffectivenessCode { get; set; }
        public string? EffectivenessName { get; set; }

        public string? ActionStatus { get; set; }

        public DateTime? VerificationDate { get; set; }
        public string? VerificationNotes { get; set; }

        public Guid? WorkflowStatusId { get; set; }
        public string? IssueStatusCode { get; set; }
        public string? IssueStatusName { get; set; }

        public Guid TenantId { get; set; }
        public bool IsActive { get; set; }
    }
}
