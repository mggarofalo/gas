namespace GasTracker.Core.Interfaces;

public interface IYnabClient
{
    Task<List<YnabPlan>> GetPlansAsync(string token);
    Task<List<YnabAccount>> GetAccountsAsync(string token, string planId);
    Task<List<YnabCategory>> GetCategoriesAsync(string token, string planId);
    Task<YnabTransactionResult> CreateTransactionAsync(string token, string planId, YnabTransaction tx);
    Task<YnabTransactionPage> GetTransactionsAsync(string token, string planId, string? accountId = null, DateOnly? sinceDate = null, long? lastServerKnowledge = null);
}

public record YnabPlan(string Id, string Name, string? LastModifiedOn);

public record YnabAccount(string Id, string Name, string Type, long Balance);

public record YnabCategory(string Id, string Name, string CategoryGroupName);

public record YnabTransaction(
    string AccountId,
    string Date,
    long Amount,
    string PayeeName,
    string? CategoryId,
    string? Memo,
    string Cleared = "cleared",
    bool Approved = true,
    string? ImportId = null);

public record YnabTransactionResult(string? TransactionId, bool IsDuplicate);

public record YnabTransactionRead(
    string Id,
    string Date,
    long Amount,
    string? PayeeName,
    string? Memo,
    string? CategoryId,
    string AccountId,
    string? ImportId);

public record YnabTransactionPage(List<YnabTransactionRead> Transactions, long ServerKnowledge);
