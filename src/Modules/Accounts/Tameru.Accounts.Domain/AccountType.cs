namespace Tameru.Accounts.Domain;

/// <summary>The kind of money container an account represents (from the workbook's account list).</summary>
public enum AccountType
{
    Cash = 0,
    Bank = 1,
    EWallet = 2,
    Investment = 3,
    Blocked = 4,
}
