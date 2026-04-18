using System;
using System.Collections.Generic;
using UnityEngine;


namespace Martian.EventBus
{
    /// <summary>
    /// 一个简单可复用的泛型事件总线系统，用于在项目中广播事件，实现系统之间解耦。
    /// 使用方式：
    ///     - 订阅事件：EventBus.Subscribe<YourEvent>(Handler)
    ///     - 取消订阅：EventBus.Unsubscribe<YourEvent>(Handler)
    ///     - 触发事件：EventBus.Publish(new YourEvent { ... })
    /// </summary>
    /// 
    /// <summary>
    /// 泛型事件总线（发布-订阅模式）。
    /// 用于在项目中解耦系统通信，允许多个系统对同一事件类型进行订阅和发布。
    ///
    /// 使用示例：
    ///   // 订阅：在 OnEnable 中
    ///   EventBus.Subscribe<GameEvents.MoneyChanged>(OnMoneyChanged);
    ///
    ///   // 发布：在需要通知其他系统时
    ///   EventBus.Publish(new GameEvents.MoneyChanged { OldValue = 100, NewValue = 150, Delta = 50 });
    ///
    ///   // 取消订阅：在 OnDisable 中
    ///   EventBus.Unsubscribe<GameEvents.MoneyChanged>(OnMoneyChanged);
    ///
    /// 线程安全：
    ///   - 本实现使用静态 Dictionary，仅支持主线程访问
    ///   - 如需多线程支持，需加锁保护 _eventTable
    ///
    /// Domain Reload 处理：
    ///   - Editor Play Mode 时 Domain Reload 会清空静态变量
    ///   - 本实现通过 RuntimeInitializeOnLoadMethod 自动清空旧订阅
    ///   - 确保每次 Play Mode 启动时不会有残留回调
    /// </summary>
    public static class EventBus
    {
        /// <summary>
        /// 内部事件表。
        /// Key：事件类型（例如 typeof(GameEvents.MoneyChanged)）
        /// Value：多播委托（可以有多个监听者，通过 Delegate.Combine 链接）
        /// </summary>
        private static readonly Dictionary<Type, Delegate> _eventTable = new();

        /// <summary>
        /// 订阅事件类型 T。
        /// 支持多个监听者，通过 Delegate.Combine 将回调链接到多播委托。
        /// </summary>
        public static void Subscribe<T>(Action<T> callback)
        {
            var eventType = typeof(T);

            if (_eventTable.TryGetValue(eventType, out var existingDelegate))
            {
                // 已有监听者，添加新监听到链表中（Delegate.Combine）
                _eventTable[eventType] = Delegate.Combine(existingDelegate, callback);
            }
            else
            {
                // 第一个监听者，直接添加
                _eventTable[eventType] = callback;
            }
        }

        /// <summary>
        /// 取消订阅事件类型 T。
        /// 如果移除后已无监听者，则从表中清除该事件类型。
        /// </summary>
        public static void Unsubscribe<T>(Action<T> callback)
        {
            var eventType = typeof(T);

            if (_eventTable.TryGetValue(eventType, out var existingDelegate))
            {
                // 从当前监听链表中移除该回调
                var newDelegate = Delegate.Remove(existingDelegate, callback);

                if (newDelegate == null)
                {
                    // 移除后已无监听者，清理整个事件类型
                    _eventTable.Remove(eventType);
                }
                else
                {
                    _eventTable[eventType] = newDelegate;
                }
            }
        }

        /// <summary>
        /// 发布事件：触发该类型所有订阅者的回调函数。
        /// 回调将按照订阅顺序依次调用。
        /// </summary>
        public static void Publish<T>(T eventData)
        {
            var eventType = typeof(T);

            if (_eventTable.TryGetValue(eventType, out var existingDelegate))
            {
                (existingDelegate as Action<T>)?.Invoke(eventData);
            }
        }

        /// <summary>
        /// 清除所有事件监听。
        /// 通常在场景切换时调用，防止旧场景的订阅污染新场景。
        /// </summary>
        public static void ClearAll()
        {
            _eventTable.Clear();
        }

        /// <summary>
        /// Runtime Initialization：进入 Play Mode 时自动清空残留订阅。
        /// 防止 Domain Reload 后旧回调引发异常。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnDomainReload()
        {
            _eventTable.Clear();
        }
    }
}