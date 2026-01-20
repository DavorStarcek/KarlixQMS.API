namespace KarlixQMS.API.Infrastructure.Security;

public static class QmsPolicies
{
    public const string ActionsRead = "Qms.Actions.Read";
    public const string ActionsWriteBasic = "Qms.Actions.WriteBasic";
    public const string ActionsVerify = "Qms.Actions.Verify";
}

public static class QmsPerms
{
    public const string ActionsRead = "qms.actions.read";
    public const string ActionsWriteBasic = "qms.actions.write.basic";
    public const string ActionsVerify = "qms.actions.verify";
}
