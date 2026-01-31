namespace ClassIsland.Core;

/// <summary>
/// 提供一个线程安全的包装器，用于对 <see cref="IDisposable"/> 对象进行引用计数管理。
/// <para>只有当引用计数归零时，底层资源才会被释放。</para>
/// </summary>
/// <typeparam name="T">实现了 <see cref="IDisposable"/> 的引用类型。</typeparam>
public class RefCounted<T> : IDisposable where T : class, IDisposable
{
    /// <summary>
    /// 存储的值是否已经被释放
    /// </summary>
    public bool IsValueDisposed => _value == null;
    
    /// <summary>
    /// 底层被管理的资源。
    /// </summary>
    private T? _value;

    /// <summary>
    /// 当前引用计数。
    /// 初始值为 1（代表 RefCounted 对象本身持有的所有权）。
    /// </summary>
    private int _refCount = 1;

    /// <summary>
    /// 用于同步状态变更（Rent/Release）的锁对象，防止并发时的“复活”竞争条件。
    /// </summary>
    private readonly object _lock = new object();

    /// <summary>
    /// 初始化 <see cref="RefCounted{T}"/> 的新实例。
    /// </summary>
    /// <param name="value">需要被管理的 Disposable 资源。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="value"/> 为 null 时抛出。</exception>
    public RefCounted(T value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// 申请资源的使用租约（Lease）。
    /// <para>调用此方法会将引用计数加 1。</para>
    /// </summary>
    /// <returns>
    /// 一个实现了 <see cref="IDisposable"/> 的租约对象。
    /// 通过该对象的 <see cref="Lease.Value"/> 属性可安全访问资源。
    /// 当租约被 Dispose 时，引用计数会自动减 1。
    /// </returns>
    /// <exception cref="ObjectDisposedException">如果底层资源已经被释放（引用计数已归零），则抛出此异常。</exception>
    public Lease Rent()
    {
        lock (_lock)
        {
            if (_value == null)
            {
                throw new ObjectDisposedException($"类型 {typeof(T).Name} 的资源已被释放，无法再获取租约。");
            }

            Interlocked.Increment(ref _refCount);

            return new Lease(this, _value);
        }
    }

    /// <summary>
    /// 内部方法：减少引用计数并尝试释放资源。
    /// </summary>
    private void Release()
    {
        lock (_lock)
        {
            int count = Interlocked.Decrement(ref _refCount);

            if (count == 0 && _value != null)
            {
                try
                {
                    _value.Dispose();
                }
                finally
                {
                    _value = null; // 标记为已销毁，防止 Rent() 再次分发
                }
            }
        }
    }

    /// <summary>
    /// 放弃 RefCounted 对象本身的持有权（引用计数减 1）。
    /// <para>注意：调用此方法并不一定会立即销毁资源，只有当所有租约（Lease）都归还后，资源才会被销毁。</para>
    /// </summary>
    public void Dispose()
    {
        // 释放创建者持有的那个 "1" 的计数
        Release();
    }

    /// <summary>
    /// 表示对共享资源的一次临时租用。
    /// 只要持有此对象，底层资源就保证不会被释放。
    /// </summary>
    public sealed class Lease : IDisposable
    {
        private RefCounted<T> _parent;
        private int _isDisposed = 0;

        /// <summary>
        /// 获取受保护的资源实例。
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// 仅供 RefCounted 内部创建实例
        /// </summary>
        internal Lease(RefCounted<T> parent, T value)
        {
            _parent = parent;
            Value = value;
        }

        /// <summary>
        /// 归还租约。这将导致引用计数减 1。
        /// </summary>
        public void Dispose()
        {
            // 使用 Interlocked 确保 Dispose 幂等性（即多次调用 Dispose 不会重复减少计数）
            if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
            {
                _parent?.Release();
                _parent = null;
            }
        }
    }
}