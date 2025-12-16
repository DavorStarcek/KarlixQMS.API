namespace KarlixQMS.API.Models.Dashboard;

public class DashboardDto
{
    public DashboardSummaryDto Summary { get; set; } = new();
    public List<DashboardRecentCaseDto> RecentCases { get; set; } = new();
    public List<DashboardOpenActionDto> OpenActions { get; set; } = new();
    public List<DashboardMonthlyTrendDto> MonthlyTrend { get; set; } = new();
}

public class DashboardSummaryDto
{
    public int TotalCases { get; set; }
    public int ActiveCases { get; set; }
    public int ComplaintsLast30Days { get; set; }
    public int NonconformitiesLast30Days { get; set; }
}

public class DashboardRecentCaseDto
{
    public string? Number { get; set; }
    public string? Type { get; set; }     // "RIN"/"UN" ili entityType
    public string? Title { get; set; }
    public string? Status { get; set; }
    public DateTime? Date { get; set; }
}

public class DashboardOpenActionDto
{
    public string? CaseNumber { get; set; }
    public string? Title { get; set; }
    public string? Responsible { get; set; }
    public DateTime? DueDate { get; set; }
}

public class DashboardMonthlyTrendDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Total { get; set; }
    public int Closed { get; set; }
    public int Open { get; set; }
}
