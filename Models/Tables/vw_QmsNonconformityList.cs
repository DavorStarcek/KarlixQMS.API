using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class vw_QmsNonconformityList
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Number { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateOnly RaisedAt { get; set; }

    public string Source { get; set; } = null!;

    public string? RootCause { get; set; }

    public DateOnly? VerificationDate { get; set; }

    public string? VerifiedBy { get; set; }

    public string? OrgUnitCode { get; set; }

    public string? OrgUnitName { get; set; }

    public string? RelationTypeCode { get; set; }

    public string? RelationTypeName { get; set; }

    public string? StandardCode { get; set; }

    public string? StandardName { get; set; }

    public string? RequirementCode { get; set; }

    public string? RequirementTitle { get; set; }

    public string? EffectivenessCode { get; set; }

    public string? EffectivenessName { get; set; }

    public string? StatusCode { get; set; }

    public string? StatusName { get; set; }
}
