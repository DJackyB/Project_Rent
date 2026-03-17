using System;
using System.Collections.Generic;
using System.IO;
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
    public static class EventBus
    {
        /// <summary>
        /// 内部事件表，key 是事件类型，value 是对应的多播委托（可以有多个监听者）
        /// </summary>
        private static readonly Dictionary<Type, Delegate> _eventTable = new();

        /// <summary>
        /// 订阅某个类型的事件（支持多个监听者）
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
        /// 取消订阅某个事件（如果是最后一个监听者，则从表中移除该事件）
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
        /// 广播事件：触发该类型所有订阅者的回调函数
        /// </summary>
        public static void Publish<T>(T eventData)
        {
            var eventType = typeof(T);

            if (_eventTable.TryGetValue(eventType, out var existingDelegate))
            {
                // 尝试转换为实际类型的委托
                (existingDelegate as Action<T>)?.Invoke(eventData);

                string targetName = existingDelegate.Target != null
                    ? existingDelegate.Target.ToString()
                    : $"<static:{existingDelegate.Method.DeclaringType.Name}>";

                /*Debug.Log(
                    $"{DebugHelperForEventBus.GetScriptNameWithColor()}" +
                    $"由 {DebugHelperForEventBus.ApllyColorToString(targetName, "cyan")}" +
                    $"发布事件 {DebugHelperForEventBus.ApllyColorToString(existingDelegate.Method.Name, "cyan")}"
                );*/

            }
        }

        /// <summary>
        /// 清除所有事件监听（比如切换场景时调用）
        /// </summary>
        public static void ClearAll()
        {
            _eventTable.Clear();
        }
        
    }
}