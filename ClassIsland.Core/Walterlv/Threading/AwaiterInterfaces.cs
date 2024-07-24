using System.Runtime.CompilerServices;
// ReSharper disable CheckNamespace

namespace Walterlv.Threading
{
    /// <summary>
    /// 表示一个可等待对象，如果一个方法返回此类型的实例，则此方法可以使用 `await` 异步等待。
    /// </summary>
    /// <typeparam name="TAwaiter">用于给 await 确定返回时机的 IAwaiter 的实例。</typeparam>
    public interface IAwaitable<out TAwaiter> where TAwaiter : IAwaiter
    {
        /// <summary>
        /// 获取一个可用于 await 关键字异步等待的异步等待对象。
        /// 此方法会被编译器自动调用。
        /// </summary>
        TAwaiter GetAwaiter();
    }

    /// <summary>
    /// 表示一个包含返回值的可等待对象，如果一个方法返回此类型的实例，则此方法可以使用 `await` 异步等待返回值。
    /// </summary>
    /// <typeparam name="TAwaiter">用于给 await 确定返回时机的 IAwaiter{<typeparamref name="TResult"/>} 的实例。</typeparam>
    /// <typeparam name="TResult">异步返回的返回值类型。</typeparam>
    public interface IAwaitable<out TAwaiter, out TResult> where TAwaiter : IAwaiter<TResult>
    {
        /// <summary>
        /// 获取一个可用于 await 关键字异步等待的异步等待对象。
        /// 此方法会被编译器自动调用。
        /// </summary>
        TAwaiter GetAwaiter();
    }

    /// <summary>
    /// 用于给 await 确定异步返回的时机。
    /// </summary>
    public interface IAwaiter : INotifyCompletion
    {
        /// <summary>
        /// 获取一个状态，该状态表示正在异步等待的操作已经完成（成功完成或发生了异常）；此状态会被编译器自动调用。
        /// 在实现中，为了达到各种效果，可以灵活应用其值：可以始终为 true，或者始终为 false。
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// 此方法会被编译器在 await 结束时自动调用以获取返回状态（包括异常）。
        /// </summary>
        void GetResult();
    }

    /// <summary>
    /// 当执行关键代码（此代码中的错误可能给应用程序中的其他状态造成负面影响）时，
    /// 用于给 await 确定异步返回的时机。
    /// </summary>
    public interface ICriticalAwaiter : IAwaiter, ICriticalNotifyCompletion
    {
    }

    /// <summary>
    /// 用于给 await 确定异步返回的时机，并获取到返回值。
    /// </summary>
    /// <typeparam name="TResult">异步返回的返回值类型。</typeparam>
    public interface IAwaiter<out TResult> : INotifyCompletion
    {
        /// <summary>
        /// 获取一个状态，该状态表示正在异步等待的操作已经完成（成功完成或发生了异常）；此状态会被编译器自动调用。
        /// 在实现中，为了达到各种效果，可以灵活应用其值：可以始终为 true，或者始终为 false。
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// 获取此异步等待操作的返回值，此方法会被编译器在 await 结束时自动调用以获取返回值（包括异常）。
        /// </summary>
        /// <returns>异步操作的返回值。</returns>
        TResult GetResult();
    }

    /// <summary>
    /// 当执行关键代码（此代码中的错误可能给应用程序中的其他状态造成负面影响）时，
    /// 用于给 await 确定异步返回的时机，并获取到返回值。
    /// </summary>
    /// <typeparam name="TResult">异步返回的返回值类型。</typeparam>
    public interface ICriticalAwaiter<out TResult> : IAwaiter<TResult>, ICriticalNotifyCompletion
    {
    }
}