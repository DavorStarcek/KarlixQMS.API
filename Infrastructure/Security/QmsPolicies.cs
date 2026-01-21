namespace KarlixQMS.API.Infrastructure.Security;

public static class QmsPolicies
{
    // --- ACTIONS ---
    public const string ActionsRead = "Qms.Actions.Read";
    public const string ActionsWriteBasic = "Qms.Actions.WriteBasic";
    public const string ActionsVerify = "Qms.Actions.Verify";

    // --- CASES ---
    public const string CasesRead = "Qms.Cases.Read";

    // “Gate” policy: tko uopće smije pokušati write na slučajevima.
    // Pravu faznu kontrolu radimo u controlleru (EntityType+StatusCode -> perm).
    public const string CasesWriteBasic = "Qms.Cases.WriteBasic";
}

public static class QmsPerms
{
    // --- generic ---
    public const string Admin = "qms.admin";
    public const string Read = "qms.read";

    // --- actions ---
    public const string ActionsRead = "qms.actions.read";
    public const string ActionsWriteBasic = "qms.actions.write.basic";
    public const string ActionsVerify = "qms.actions.verify";
    public const string ActionsWriteAll = "qms.actions.write.all";

    // --- cases write perms (phase-based) ---
    public const string RinWriteReceived = "qms.rin.write.RECEIVED";
    public const string RinWriteInProgress = "qms.rin.write.IN_PROGRESS";
    public const string RinWriteClosed = "qms.rin.write.CLOSED";

    public const string UnWriteReceived = "qms.un.write.RECEIVED";
    public const string UnWriteInProgress = "qms.un.write.IN_PROGRESS";
    public const string UnWriteClosed = "qms.un.write.CLOSED";
}
