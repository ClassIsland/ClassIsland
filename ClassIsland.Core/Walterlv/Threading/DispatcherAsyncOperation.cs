// ReSharper disable CheckNamespace
namespace Walterlv.Threading;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Windows.Threading;

/// <summary>
/// 表示可以等待一个主要运行在 UI 线程的异步操作。
/// </summary>
/// <typeparam name="T">异步等待 UI 操作结束后的返回值类型。</typeparam>
public class DispatcherAsyncOperation<T> : DispatcherObject,
    IAwaitable<DispatcherAsyncOperation<T>, T>, IAwaiter<T>
{
    /// <summary>
    /// 创建 <see cref="DispatcherAsyncOperation{T}"/> 的新实例。
    /// </summary>
    private DispatcherAsyncOperation()
    {
    }

    /// <summary>
    /// 获取一个可用于 await 关键字异步等待的异步等待对象。
    /// 此方法会被编译器自动调用。
    /// </summary>
    /// <returns>返回自身，用于异步等待返回值。</returns>
    public DispatcherAsyncOperation<T> GetAwaiter()
    {
        return this;
    }

    /// <summary>
    /// 获取一个状态，该状态表示正在异步等待的操作已经完成（成功完成或发生了异常）。
    /// 此状态会被编译器自动调用。
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// 获取此异步等待操作的返回值。
    /// 与 <see cref="System.Threading.Tasks.Task{TResult}"/> 不同的是，
    /// 如果操作没有完成或发生了异常，此实例会返回 <typeparamref name="T"/> 的默认值，
    /// 而不是阻塞线程直至任务完成。
    /// </summary>
    public T Result { get; private set; }

    /// <summary>
    /// 获取此异步等待操作的返回值，此方法会被编译器在 await 结束时自动调用以获取返回值。
    /// 与 <see cref="System.Threading.Tasks.Task{TResult}"/> 不同的是，
    /// 如果操作没有完成，此实例会返回 <typeparamref name="T"/> 的默认值，而不是阻塞线程直至任务完成。
    /// 但是，如果异步操作中发生了异常，调用此方法会抛出这个异常。
    /// </summary>
    /// <returns>
    /// 异步操作的返回值。
    /// </returns>
    public T GetResult()
    {
        if (_exception != null)
        {
            ExceptionDispatchInfo.Capture(_exception).Throw();
        }
        return Result;
    }

    /// <summary>
    /// 使用 Builder 模式配置此异步操作执行完后，后续任务执行采用的优先级。
    /// 不配置时，使用的是 <see cref="DispatcherPriority.Normal"/>。
    /// </summary>
    /// <param name="priority">使用 <see cref="Dispatcher"/> 调度的后续任务的优先级。</param>
    /// <returns>实例自身。</returns>
    public DispatcherAsyncOperation<T> ConfigurePriority(DispatcherPriority priority)
    {
        _priority = priority;
        return this;
    }

    /// <summary>
    /// 当使用此类型执行异步任务的方法执行完毕后，编译器会自动调用此方法。
    /// 也就是说，此方法会在调用方所在的线程执行，用于通知调用方所在线程的代码已经执行完毕，请求执行 await 后续任务。
    /// 在此类型中，后续任务是通过 <see cref="Dispatcher.InvokeAsync(Action, DispatcherPriority)"/> 来执行的。
    /// </summary>
    /// <param name="continuation">
    /// 被异步任务状态机包装的后续任务。当执行时，会让状态机继续往下走一步。
    /// </param>
    public void OnCompleted(Action continuation)
    {
        if (IsCompleted)
        {
            // 如果 await 开始时任务已经执行完成，则直接执行 await 后面的代码。
            // 注意，即便 _continuation 有值，也无需关心，因为报告结束的时候就会将其执行。
            continuation?.Invoke();
        }
        else
        {
            // 当使用多个 await 关键字等待此同一个 awaitable 实例时，此 OnCompleted 方法会被多次执行。
            // 当任务真正结束后，需要将这些所有的 await 后面的代码都执行。
            _continuation += continuation;
        }
    }

    /// <summary>
    /// 调用此方法以报告任务结束，并指定返回值和异步任务中的异常。
    /// 当使用 <see cref="Create"/> 静态方法创建此类型的实例后，调用方可以通过方法参数中传出的委托来调用此方法。
    /// </summary>
    /// <param name="result">异步返回值。</param>
    /// <param name="exception">异步操作中的异常。</param>
    private void ReportResult(T result, Exception exception)
    {
        Result = result;
        _exception = exception;
        IsCompleted = true;

        // _continuation 可能为 null，说明任务已经执行完毕，但没有任何一处 await 了这个任务。
        if (_continuation != null)
        {
            // 无论此方法执行时所在线程关联的 Dispatcher 是否等于此类型创建时的 Dispatcher；
            // 都 Invoke 到创建时的 Dispatcher 上，以便对当前执行上下文造成影响在不同线程执行下都一致（如异常）。
            Dispatcher.InvokeAsync(_continuation, _priority);
        }
    }

    /// <summary>
    /// 临时保存 await 后后续任务的包装，用于报告任务完成后能够继续执行。
    /// </summary>
    private Action _continuation;

    /// <summary>
    /// 临时保存异步任务执行过程中发生的异常。它会在异步等待结束后抛出，以报告异步执行过程中发生的错误。
    /// </summary>
    private Exception _exception;

    /// <summary>
    /// 储存恢复 await 后续任务时需要使用的优先级。
    /// </summary>
    private DispatcherPriority _priority = DispatcherPriority.Normal;

    /// <summary>
    /// 创建 <see cref="DispatcherAsyncOperation{T}"/> 的新实例，并得到一个可以用于报告操作执行完毕的委托。
    /// </summary>
    /// <param name="reportResult">一个委托。调用此委托可以报告任务已经执行完毕，并给定返回值和异常信息。</param>
    /// <returns>
    /// 创建好的 <see cref="DispatcherAsyncOperation{T}"/> 的新实例，将此返回值作为方法的返回值可以让方法支持 await 异步等待。
    /// </returns>
    public static DispatcherAsyncOperation<T> Create([NotNull] out Action<T, Exception> reportResult)
    {
        var asyncOperation = new DispatcherAsyncOperation<T>();
        reportResult = asyncOperation.ReportResult;
        return asyncOperation;
    }
}