namespace KarlixQMS.API.Infrastructure.Security;

public static class QmsPermissions
{
    // Baseline
    public const string Admin = "qms.admin";
    public const string Read = "qms.read";

    // Actions
    public const string ActionsRead = "qms.actions.read";
    public const string ActionsWriteBasic = "qms.actions.write.basic";
    public const string ActionsVerify = "qms.actions.verify";
    public const string ActionsWriteAll = "qms.actions.write.all";

    // (Kasnije: RIN/UN phase perms kad radimo Cases endpoint-e)
    public const string RinWriteReceived = "qms.rin.write.RECEIVED";
    public const string RinWriteInProgress = "qms.rin.write.IN_PROGRESS";
    public const string RinWriteClosed = "qms.rin.write.CLOSED";

    public const string UnWriteReceived = "qms.un.write.RECEIVED";
    public const string UnWriteInProgress = "qms.un.write.IN_PROGRESS";
    public const string UnWriteClosed = "qms.un.write.CLOSED";
}
