namespace Tameru.Budgeting.Domain;

/// <summary>The three levels of the Budget → Category → Sub taxonomy (from the workbook).</summary>
public enum CategoryLevel
{
    Budget = 0,
    Category = 1,
    Sub = 2,
}

/// <summary>Which transaction flow a category classifies (BR-005).</summary>
public enum CategoryFlow
{
    Any = 0,
    Income = 1,
    Expense = 2,
    Transfer = 3,
}
