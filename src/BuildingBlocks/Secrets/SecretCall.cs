namespace SchoolOS.BuildingBlocks.Secrets;

internal static class SecretCall
{
    public static async Task<T> RunAsync<T>(
        string operation,
        SecretKey key,
        Func<Task<T>> call,
        Func<Exception, bool> isTransient,
        CancellationToken cancellationToken)
    {
        try
        {
            return await call();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (SecretProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SecretProviderException(operation, key, isTransient(ex), ex);
        }
    }

    public static Task RunAsync(
        string operation,
        SecretKey key,
        Func<Task> call,
        Func<Exception, bool> isTransient,
        CancellationToken cancellationToken) =>
        RunAsync(operation, key, async () =>
        {
            await call();
            return true;
        }, isTransient, cancellationToken);

    public static bool IsTransientStatus(int status) => status is 0 or 408 or 429 or >= 500;
}
