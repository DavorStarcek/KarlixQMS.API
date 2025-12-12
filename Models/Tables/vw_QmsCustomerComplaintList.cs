using System;
using System.Collections.Generic;

namespace KarlixQMS.API.Models.Tables;

public partial class vw_QmsCustomerComplaintList
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Number { get; set; } = null!;

    public string SourceType { get; set; } = null!;

    public DateOnly ComplaintDate { get; set; }

    public DateOnly ReceivedDate { get; set; }

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

    public string? UnitOfMeasureCode { get; set; }

    public string? UnitOfMeasureName { get; set; }

    public string? ProductStateCode { get; set; }

    public string? ProductStateName { get; set; }

    public string? ComplaintReasonCode { get; set; }

    public string? ComplaintReasonName { get; set; }

    public int? HealthRiskLevel { get; set; }

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

    public string? OrgUnitCode { get; set; }

    public string? OrgUnitName { get; set; }

    public string? StatusCode { get; set; }

    public string? StatusName { get; set; }
}
