using System;
using System.Collections.Generic;
using System.Globalization;
using Martian.Save;
using UnityEngine;

namespace Martian.Save.Runtime
{
    /// <summary>
    /// 核心存档服务。实现三层存档架构的协调。
    ///
    /// 三层架构：
    /// 1. 数据层（ISaveSection）：各数据领域（经济、卡牌库等）定义自己的状态对象
    /// 2. 序列化层（ISaveSerializer）：将状态对象与 JSON 相互转换
    /// 3. 存储层（ISaveStorage）：将 JSON 写入文件或其他存储后端
    ///
    /// SaveService 的职责：
    /// - 协调各片段的状态捕获和应用
    /// - 构建/解析 SaveEnvelope（包含所有片段 + 元数据）
    /// - 验证数据完整性和合法性
    /// - 提供原子性保证（Save 全成功或全失败）
    ///
    /// 使用流程（保存）：
    /// 1. 调用 Save()，传入片段列表
    /// 2. SaveService 调用每个片段的 CaptureState()
    /// 3. SaveService 序列化所有状态为 JSON
    /// 4. SaveService 通过 Storage 写入文件
    ///
    /// 使用流程（加载）：
    /// 1. 调用 TryPrepareLoad()，返回已验证的状态列表
    /// 2. 调用 ApplyPreparedLoad()，应用状态到游戏世界
    /// 这样分离准备和应用，允许在加载前执行异步操作。
    /// </summary>
    public sealed class SaveService
    {
        /// <summary>序列化实现。负责对象与 JSON 的转换。</summary>
        private readonly ISaveSerializer _serializer;

        /// <summary>存储实现。负责文件 I/O。</summary>
        private readonly ISaveStorage _storage;

        /// <summary>运行时选项（模式、扩展名等）。</summary>
        private readonly SaveRuntimeOptions _options;

        /// <summary>
        /// 构造 SaveService。
        /// </summary>
        /// <param name="serializer">序列化实现（必需）</param>
        /// <param name="storage">存储实现（必需）</param>
        /// <param name="options">运行时选项，或 null 使用默认值</param>
        public SaveService(ISaveSerializer serializer, ISaveStorage storage, SaveRuntimeOptions options = null)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _options = options ?? new SaveRuntimeOptions();
        }

        /// <summary>
        /// 保存游戏。执行完整的保存流程：
        /// 1. 捕获各片段状态
        /// 2. 验证数据
        /// 3. 序列化为 JSON
        /// 4. 写入文件（带备份机制）
        /// </summary>
        /// <param name="relativePath">保存文件的相对路径（如 "Saves/slot1.json"）</param>
        /// <param name="slotId">存档槽 ID（唯一标识，如 "slot1"）</param>
        /// <param name="displayName">存档显示名称（如玩家自定义的名字）</param>
        /// <param name="sections">参与保存的数据片段列表</param>
        /// <param name="error">若保存失败，填入错误描述</param>
        /// <returns>保存成功返回 true</returns>
        public bool Save(string relativePath, string slotId, string displayName, IReadOnlyList<ISaveSection> sections, out string error)
        {
            error = null;

            // 第一步：构建信封（元数据 + 所有片段状态）
            if (!TryBuildEnvelope(slotId, displayName, sections, out var envelope, out error))
            {
                return false;
            }

            try
            {
                // 第二步：序列化
                var json = _serializer.Serialize(envelope, _options.prettyPrintJson);

                // 第三步：写入文件（Storage 负责原子性和备份）
                _storage.WriteAllText(relativePath, json, _options.backupFileExtension);
                return true;
            }
            catch (Exception exception)
            {
                error = $"Failed to save '{slotId}': {exception.Message}";
                return false;
            }
        }

        /// <summary>
        /// 准备加载：读取和验证存档文件，返回可应用的状态列表。
        ///
        /// 与 ApplyPreparedLoad 分离的好处：
        /// - 允许在加载前执行异步操作（如显示进度条）
        /// - 允许加载完成后再应用状态（可能涉及场景切换）
        /// - 分离验证逻辑和应用逻辑，便于测试
        ///
        /// 执行步骤：
        /// 1. 检查片段列表非空
        /// 2. 读取文件并反序列化 SaveEnvelope
        /// 3. 验证 Envelope 的基本合法性
        /// 4. 遍历每个注册片段：
        ///    - 如果在文件中找到对应记录，反序列化并验证
        ///    - 如果未找到且片段标记为 Required，返回失败
        ///    - 否则跳过该片段
        /// 5. 返回已准备的片段列表
        /// </summary>
        /// <param name="relativePath">存档文件相对路径</param>
        /// <param name="sections">当前定义的所有数据片段</param>
        /// <param name="envelope">读取的存档信封（元数据 + 所有片段记录）</param>
        /// <param name="preparedSections">已验证的片段状态列表（可直接应用）</param>
        /// <param name="error">若失败，填入错误描述</param>
        /// <returns>准备成功返回 true</returns>
        public bool TryPrepareLoad(
            string relativePath,
            IReadOnlyList<ISaveSection> sections,
            out SaveEnvelope envelope,
            out IReadOnlyList<PreparedSaveSectionState> preparedSections,
            out string error)
        {
            envelope = null;
            preparedSections = null;
            error = null;

            // 步骤 1：检查输入
            if (sections == null || sections.Count == 0)
            {
                error = "No save sections were provided.";
                return false;
            }

            if (!_storage.Exists(relativePath))
            {
                error = $"Save file not found: {relativePath}";
                return false;
            }

            // 步骤 2：读取和反序列化文件
            try
            {
                var rawJson = _storage.ReadAllText(relativePath);
                envelope = _serializer.Deserialize<SaveEnvelope>(rawJson);
            }
            catch (Exception exception)
            {
                error = $"Failed to read save file '{relativePath}': {exception.Message}";
                return false;
            }

            // 步骤 3：验证 Envelope 基本合法性
            if (!TryValidateEnvelope(envelope, out error))
            {
                return false;
            }

            // 步骤 4a：构建文件中存在的记录映射（检查重复）
            var recordLookup = new Dictionary<string, SaveSectionRecord>(StringComparer.Ordinal);
            for (int i = 0; i < envelope.sections.Count; i++)
            {
                var record = envelope.sections[i];
                if (record == null || string.IsNullOrWhiteSpace(record.key))
                {
                    error = $"Save file '{relativePath}' contains an invalid section entry.";
                    return false;
                }

                if (!recordLookup.TryAdd(record.key, record))
                {
                    error = $"Save file '{relativePath}' contains duplicate section '{record.key}'.";
                    return false;
                }
            }

            // 步骤 4b：遍历注册的片段，匹配文件中的记录
            var prepared = new List<PreparedSaveSectionState>(sections.Count);
            for (int i = 0; i < sections.Count; i++)
            {
                var section = sections[i];
                if (section == null)
                {
                    error = "Encountered a null save section.";
                    return false;
                }

                if (!recordLookup.TryGetValue(section.Key, out var record))
                {
                    // 文件中无此片段
                    if (section.IsRequired)
                    {
                        error = $"Missing required save section '{section.Key}'.";
                        return false;
                    }

                    // 可选片段，跳过
                    continue;
                }

                // 文件中有此片段，反序列化和验证
                object state;
                try
                {
                    state = _serializer.Deserialize(record.jsonPayload, section.StateType);
                }
                catch (Exception exception)
                {
                    error = $"Failed to deserialize section '{section.Key}': {exception.Message}";
                    return false;
                }

                if (state == null)
                {
                    error = $"Section '{section.Key}' could not be deserialized.";
                    return false;
                }

                if (!section.TryValidate(state, out error))
                {
                    error = string.IsNullOrWhiteSpace(error)
                        ? $"Section '{section.Key}' failed validation."
                        : error;
                    return false;
                }

                prepared.Add(new PreparedSaveSectionState(section, state));
            }

            preparedSections = prepared;
            return true;
        }

        /// <summary>
        /// 应用已准备的加载状态。
        /// 按顺序调用每个片段的 ApplyState()，将状态应用到游戏世界。
        /// </summary>
        /// <param name="preparedSections">已验证的片段状态列表（来自 TryPrepareLoad）</param>
        /// <param name="error">若应用失败，填入错误描述</param>
        /// <returns>应用成功返回 true</returns>
        public bool ApplyPreparedLoad(IReadOnlyList<PreparedSaveSectionState> preparedSections, out string error)
        {
            error = null;

            if (preparedSections == null)
            {
                error = "No prepared save sections were provided.";
                return false;
            }

            try
            {
                // 依次应用每个片段的状态
                for (int i = 0; i < preparedSections.Count; i++)
                {
                    var preparedSection = preparedSections[i];
                    preparedSection.Section.ApplyState(preparedSection.State);
                }

                return true;
            }
            catch (Exception exception)
            {
                error = $"Failed to apply save state: {exception.Message}";
                return false;
            }
        }

        /// <summary>
        /// 列出指定目录下的所有存档槽。
        /// 会扫描目录，读取每个文件的元数据（slotId、displayName、savedAt 等）。
        /// 结果按保存时间从新到旧排序。
        /// </summary>
        /// <param name="relativeDirectory">存档目录相对路径（如 "Saves"）</param>
        /// <returns>存档槽摘要列表（已排序）</returns>
        public IReadOnlyList<SaveSlotSummary> ListSlots(string relativeDirectory)
        {
            var files = _storage.ListFiles(relativeDirectory, _options.saveFileExtension);
            var results = new List<SaveSlotSummary>(files.Count);

            for (int i = 0; i < files.Count; i++)
            {
                // 尝试读取每个文件的信封，失败则跳过（可能是损坏的文件）
                if (!TryReadEnvelope(files[i], out var envelope, out _))
                {
                    continue;
                }

                results.Add(new SaveSlotSummary
                {
                    slotId = envelope.slotId,
                    displayName = string.IsNullOrWhiteSpace(envelope.displayName) ? envelope.slotId : envelope.displayName,
                    savedAtUtc = envelope.savedAtUtc,
                    schemaVersion = envelope.schemaVersion,
                    appVersion = envelope.appVersion
                });
            }

            // 按保存时间倒序排列（最新的在前）
            results.Sort((left, right) => CompareSavedTimes(right.savedAtUtc, left.savedAtUtc));
            return results;
        }

        /// <summary>
        /// 删除指定的存档文件及其备份。
        /// </summary>
        /// <param name="relativePath">存档文件相对路径</param>
        /// <param name="error">若删除失败，填入错误描述</param>
        /// <returns>删除成功返回 true</returns>
        public bool Delete(string relativePath, out string error)
        {
            error = null;

            try
            {
                _storage.Delete(relativePath, _options.backupFileExtension);
                return true;
            }
            catch (Exception exception)
            {
                error = $"Failed to delete save '{relativePath}': {exception.Message}";
                return false;
            }
        }

        /// <summary>
        /// 只读取存档的信封（元数据和片段记录）。
        /// 不加载或验证具体的片段状态。
        /// 用于列出存档列表时快速读取元数据。
        /// </summary>
        /// <param name="relativePath">存档文件相对路径</param>
        /// <param name="envelope">读取的信封</param>
        /// <param name="error">若失败，填入错误描述</param>
        /// <returns>读取成功返回 true</returns>
        public bool TryReadEnvelope(string relativePath, out SaveEnvelope envelope, out string error)
        {
            envelope = null;
            error = null;

            if (!_storage.Exists(relativePath))
            {
                error = $"Save file not found: {relativePath}";
                return false;
            }

            try
            {
                envelope = _serializer.Deserialize<SaveEnvelope>(_storage.ReadAllText(relativePath));
            }
            catch (Exception exception)
            {
                error = $"Failed to read save file '{relativePath}': {exception.Message}";
                return false;
            }

            return TryValidateEnvelope(envelope, out error);
        }

        private bool TryBuildEnvelope(string slotId, string displayName, IReadOnlyList<ISaveSection> sections, out SaveEnvelope envelope, out string error)
        {
            envelope = null;
            error = null;

            if (string.IsNullOrWhiteSpace(slotId))
            {
                error = "slotId must not be empty.";
                return false;
            }

            if (sections == null || sections.Count == 0)
            {
                error = "No save sections were provided.";
                return false;
            }

            envelope = new SaveEnvelope
            {
                schemaVersion = _options.currentSchemaVersion,
                savedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                slotId = slotId,
                displayName = string.IsNullOrWhiteSpace(displayName) ? slotId : displayName,
                appVersion = Application.version
            };

            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < sections.Count; i++)
            {
                var section = sections[i];
                if (section == null)
                {
                    error = "Encountered a null save section.";
                    return false;
                }

                if (!keys.Add(section.Key))
                {
                    error = $"Duplicate save section key '{section.Key}'.";
                    return false;
                }

                object state;
                try
                {
                    state = section.CaptureState();
                }
                catch (Exception exception)
                {
                    error = $"Failed to capture section '{section.Key}': {exception.Message}";
                    return false;
                }

                if (state == null)
                {
                    error = $"Section '{section.Key}' returned a null state.";
                    return false;
                }

                envelope.sections.Add(new SaveSectionRecord
                {
                    key = section.Key,
                    jsonPayload = _serializer.Serialize(state, _options.prettyPrintJson)
                });
            }

            return true;
        }

        private bool TryValidateEnvelope(SaveEnvelope envelope, out string error)
        {
            error = null;

            if (envelope == null)
            {
                error = "Save envelope is missing.";
                return false;
            }

            if (envelope.schemaVersion > _options.currentSchemaVersion)
            {
                error = $"Save schema {envelope.schemaVersion} is newer than supported schema {_options.currentSchemaVersion}.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(envelope.slotId))
            {
                error = "Save envelope is missing slotId.";
                return false;
            }

            if (envelope.sections == null)
            {
                error = "Save envelope is missing sections.";
                return false;
            }

            return true;
        }

        private static int CompareSavedTimes(string left, string right)
        {
            var hasLeft = DateTime.TryParse(left, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var leftTime);
            var hasRight = DateTime.TryParse(right, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var rightTime);

            if (!hasLeft && !hasRight)
            {
                return 0;
            }

            if (!hasLeft)
            {
                return -1;
            }

            if (!hasRight)
            {
                return 1;
            }

            return leftTime.CompareTo(rightTime);
        }
    }
}
