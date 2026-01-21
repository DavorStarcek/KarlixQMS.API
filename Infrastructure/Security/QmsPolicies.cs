namespace KarlixQMS.API.Infrastructure.Security;

public static class QmsPolicies
{
    // ACTIONS
    public const string ActionsRead = "Qms.Actions.Read";
    public const string ActionsWriteBasic = "Qms.Actions.WriteBasic";
    public const string ActionsVerify = "Qms.Actions.Verify";

    // CASES
    public const string CasesRead = "Qms.Cases.Read";
    public const string CasesWriteBasic = "Qms.Cases.WriteBasic";
}

public static class QmsPerms
{
    // “root”
    public const string QmsRead = "qms.read";
    public const string QmsAdmin = "qms.admin";

    // ACTIONS
    public const string ActionsRead = "qms.actions.read";
    public const string ActionsWriteBasic = "qms.actions.write.basic";
    public const string ActionsVerify = "qms.actions.verify";
    public const string ActionsWriteAll = "qms.actions.write.all";

    // CASES (po fazama – ovo ćemo koristiti kad krenemo na edit slučaja)
    public const string RinWriteReceived = "qms.rin.write.RECEIVED";
    public const string RinWriteInProgress = "qms.rin.write.IN_PROGRESS";
    public const string RinWriteClosed = "qms.rin.write.CLOSED";

    public const string UnWriteReceived = "qms.un.write.RECEIVED";
    public const string UnWriteInProgress = "qms.un.write.IN_PROGRESS";
    public const string UnWriteClosed = "qms.un.write.CLOSED";
}
