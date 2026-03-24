using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Martian.EventBus;

namespace BaoZuPo.UI.Common
{
    /// <summary>
    /// 运行时事件总线桥接器。
    /// 允许通过事件类型名，在不直接引用具体事件类型的情况下完成订阅和取消订阅。
    /// </summary>
    public static class RuntimeEventBusBridge
    {
        public static bool TrySubscribe(string eventTypeName, object target, string handlerMethodName, out Type eventType, out Delegate subscriber)
        {
            eventType = ResolveType(eventTypeName);
            subscriber = null;

            if (eventType == null || target == null || string.IsNullOrWhiteSpace(handlerMethodName))
            {
                return false;
            }

            var handler = target.GetType().GetMethod(handlerMethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (handler == null)
            {
                return false;
            }

            subscriber = CreateActionDelegate(target, handler, eventType);
            if (subscriber == null)
            {
                return false;
            }

            InvokeGenericEventBusMethod("Subscribe", eventType, subscriber);
            return true;
        }

        public static void TryUnsubscribe(Type eventType, Delegate subscriber)
        {
            if (eventType == null || subscriber == null)
            {
                return;
            }

            InvokeGenericEventBusMethod("Unsubscribe", eventType, subscriber);
        }

        private static Delegate CreateActionDelegate(object target, MethodInfo handler, Type eventType)
        {
            var parameter = Expression.Parameter(eventType, "evt");
            var converted = Expression.Convert(parameter, typeof(object));
            var body = Expression.Call(Expression.Constant(target), handler, converted);
            var delegateType = typeof(Action<>).MakeGenericType(eventType);
            return Expression.Lambda(delegateType, body, parameter).Compile();
        }

        private static void InvokeGenericEventBusMethod(string methodName, Type eventType, Delegate subscriber)
        {
            var method = typeof(EventBus)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == methodName && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);

            if (method == null)
            {
                return;
            }

            var generic = method.MakeGenericMethod(eventType);
            generic.Invoke(null, new object[] { subscriber });
        }

        private static Type ResolveType(string eventTypeName)
        {
            if (string.IsNullOrWhiteSpace(eventTypeName))
            {
                return null;
            }

            var direct = Type.GetType(eventTypeName, false);
            if (direct != null)
            {
                return direct;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(eventTypeName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
