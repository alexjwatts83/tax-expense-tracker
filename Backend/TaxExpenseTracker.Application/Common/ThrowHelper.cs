namespace TaxExpenseTracker.Application.Common;

public static class ThrowHelper
{
    public static void InvalidOperation(string message)
        => throw new InvalidOperationException(message);

    public static void ArgumentNull(string paramName)
        => throw new ArgumentNullException(paramName);
}