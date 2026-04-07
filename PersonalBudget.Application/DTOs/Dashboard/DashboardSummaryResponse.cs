namespace PersonalBudget.Application.DTOs.Dashboard;

public record DashboardSummaryResponse(
    int Month,
    int Year,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Balance,
    decimal FixedExpenses,
    decimal VariableExpenses,
    decimal InstallmentsThisMonth,
    decimal CreditCardInvoice,
    decimal CommittedIncomePercent,
    decimal SavingsPercent,
    int TransactionCount
);
