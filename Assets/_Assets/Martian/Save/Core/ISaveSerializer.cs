using System;

namespace Martian.Save
{
    /// <summary>
    /// 存档序列化接口。负责数据对象与 JSON 字符串的相互转换。
    ///
    /// 三层架构中的第二层（序列化层）：
    /// - SaveService 调用此接口序列化/反序列化状态对象
    /// - 实现可使用 Unity JsonUtility、Newtonsoft.Json 等库
    ///
    /// 实现必须支持：
    /// - 基本类型（int、string 等）
    /// - 列表、字典（如果使用简单实现，可能不支持）
    /// - 自定义数据类
    /// </summary>
    public interface ISaveSerializer
    {
        /// <summary>
        /// 将对象序列化为 JSON 字符串。
        /// </summary>
        /// <param name="value">要序列化的对象</param>
        /// <param name="prettyPrint">是否格式化输出（便于调试）</param>
        /// <returns>JSON 字符串</returns>
        string Serialize(object value, bool prettyPrint = false);

        /// <summary>
        /// 将 JSON 字符串反序列化为指定类型的对象。
        /// 泛型版本，推荐在类型已知时使用。
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="json">JSON 字符串</param>
        /// <returns>反序列化后的对象</returns>
        T Deserialize<T>(string json);

        /// <summary>
        /// 将 JSON 字符串反序列化为指定类型的对象。
        /// 非泛型版本，用于运行时类型。
        /// </summary>
        /// <param name="json">JSON 字符串</param>
        /// <param name="type">目标类型</param>
        /// <returns>反序列化后的对象</returns>
        object Deserialize(string json, Type type);
    }
}
