namespace KarlixQMS.API.Infrastructure.Security;

public static class QmsPolicies
{
    public const string ActionsRead = "Qms.Actions.Read";
    public const string ActionsWriteBasic = "Qms.Actions.WriteBasic";
    public const string ActionsVerify = "Qms.Actions.Verify";

    public const string CasesRead = "Qms.Cases.Read";
    public const string CasesWriteBasic = "Qms.Cases.WriteBasic";
}

public static class QmsPerms
{
    public const string ActionsRead = "qms.actions.read";
    public const string ActionsWriteBasic = "qms.actions.write.basic";
    public const string ActionsVerify = "qms.actions.verify";

    public const string CasesRead = "qms.cases.read";
    public const string CasesWriteBasic = "qms.cases.write.basic";
}
