using System;
using System.Collections.Generic;
using System.Linq;
using BaoZuPo.Card;
using UnityEditor;
using UnityEngine;

namespace BaoZuPo.Editor
{
    /// <summary>
    /// 平衡模拟器：静态分析卡牌数值与贷款压力曲线。
    /// 菜单：BaoZuPo / Balance Simulator
    /// </summary>
    public class BalanceSimulatorWindow : EditorWindow
    {
        // ── 模拟参数 ─────────────────────────────────────────────
        private int _startingMoney    = 1000;
        private int _loanBase         = 500;
        private int _loanInterval     = 5;
        private float _loanGrowth     = 2f;
        private int _simulateTurns    = 20;
        private int _unlimitedRef     = 10; // 无限耐久卡的参考回合数

        // ── 数据 ─────────────────────────────────────────────────
        private List<CardStats> _stats = new();
        private bool _dirty = true;

        // ── UI 状态 ───────────────────────────────────────────────
        private Vector2 _cardScroll;
        private Vector2 _loanScroll;
        private int _sortCol = 10; // 默认按净收益排序
        private bool _sortAsc = false;
        private int _typeFilter = 0;          // 0=All, 1=Tenant, 2=Equipment, 3=Event, 4=Contract
        private int _rarityFilter = 0;        // 0=All, 1~4=各稀有度
        private bool _showOnlyNegative = false;

        private static readonly string[] TypeOptions    = { "全部", "Tenant", "Equipment", "Event", "Contract" };
        private static readonly string[] RarityOptions  = { "全部", "Common", "Rare", "Epic", "Legendary" };

        // ── 列定义 ───────────────────────────────────────────────
        private static readonly (string label, float width)[] Columns =
        {
            ("ID",    40f),
            ("名称",  110f),
            ("类型",  70f),
            ("稀有",  55f),
            ("费用",  50f),
            ("租金",  50f),
            ("耐久",  45f),
            ("即时",  50f),
            ("结算总", 62f),
            ("销毁罚", 62f),
            ("净收益", 65f),
            ("回本T",  55f),
            ("ROI%",   62f),
        };

        [MenuItem("BaoZuPo/Balance Simulator")]
        public static void Open()
        {
            var w = GetWindow<BalanceSimulatorWindow>("平衡模拟器");
            w.minSize = new Vector2(820, 560);
        }

        private void OnEnable() => _dirty = true;

        private void OnGUI()
        {
            DrawConfigBar();
            EditorGUILayout.Space(6);

            if (_dirty)
                Refresh();

            DrawLoanSection();
            EditorGUILayout.Space(8);
            DrawCardSection();
        }

        // ── 配置栏 ───────────────────────────────────────────────

        private void DrawConfigBar()
        {
            EditorGUILayout.LabelField("平衡模拟器", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                IntField("起始金钱", ref _startingMoney, 70, 70);
                IntField("贷款基础", ref _loanBase, 60, 70);
                IntField("间隔回合", ref _loanInterval, 60, 45);
                FloatField("增长倍率", ref _loanGrowth, 60, 45);
                IntField("模拟回合", ref _simulateTurns, 60, 45);
                IntField("∞参考T", ref _unlimitedRef, 50, 40);

                if (GUILayout.Button("刷新", GUILayout.Width(50)))
                    _dirty = true;
            }
        }

        // ── 贷款曲线 ──────────────────────────────────────────────

        private void DrawLoanSection()
        {
            EditorGUILayout.LabelField("贷款压力曲线", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                HeaderLabel("次数", 45);
                HeaderLabel("触发回合", 70);
                HeaderLabel("本次还款", 80);
                HeaderLabel("累计总还款", 90);
                HeaderLabel("累计/每回合", 100);
                HeaderLabel("距上次总收益需求", 130);
            }

            int safeLoanInterval = Mathf.Max(1, _loanInterval);
            float safeGrowth     = Mathf.Max(1f, _loanGrowth);
            int safeBase         = Mathf.Max(0, _loanBase);

            int payCount   = 0;
            int cumulative = 0;
            int prevTurn   = 0;

            _loanScroll = EditorGUILayout.BeginScrollView(_loanScroll, GUILayout.Height(108));

            for (int turn = safeLoanInterval; turn <= _simulateTurns; turn += safeLoanInterval)
            {
                int payment = Mathf.RoundToInt(safeBase * Mathf.Pow(safeGrowth, payCount));
                cumulative += payment;
                float avgPerTurn  = (float)cumulative / turn;
                int   interval    = turn - prevTurn;
                float neededThisInterval = (float)payment / interval;

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label((payCount + 1).ToString(), GUILayout.Width(45));
                    GUILayout.Label(turn.ToString(), GUILayout.Width(70));
                    GUILayout.Label(payment.ToString(), LoanPaymentStyle(payment), GUILayout.Width(80));
                    GUILayout.Label(cumulative.ToString(), GUILayout.Width(90));
                    GUILayout.Label(avgPerTurn.ToString("F1"), GUILayout.Width(100));
                    GUILayout.Label(neededThisInterval.ToString("F1") + $" × {interval}T", GUILayout.Width(130));
                }

                prevTurn = turn;
                payCount++;
            }

            EditorGUILayout.EndScrollView();
        }

        // ── 卡牌分析表 ────────────────────────────────────────────

        private void DrawCardSection()
        {
            EditorGUILayout.LabelField("卡牌数值分析", EditorStyles.boldLabel);

            // 过滤栏
            using (new EditorGUILayout.HorizontalScope())
            {
                _typeFilter   = EditorGUILayout.Popup("类型", _typeFilter, TypeOptions, GUILayout.Width(170));
                _rarityFilter = EditorGUILayout.Popup("稀有度", _rarityFilter, RarityOptions, GUILayout.Width(170));
                _showOnlyNegative = EditorGUILayout.ToggleLeft("只看亏损卡", _showOnlyNegative, GUILayout.Width(90));
                GUILayout.Label($"共 {CountFiltered()} 张", GUILayout.Width(80));
            }

            EditorGUILayout.Space(2);

            // 列标题（可排序）
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < Columns.Length; i++)
                {
                    var (label, width) = Columns[i];
                    string text = label + (_sortCol == i ? (_sortAsc ? " ▲" : " ▼") : "");
                    if (GUILayout.Button(text, EditorStyles.miniButton, GUILayout.Width(width)))
                    {
                        if (_sortCol == i) _sortAsc = !_sortAsc;
                        else { _sortCol = i; _sortAsc = false; }
                    }
                }
            }

            var filtered = GetFilteredSorted();

            _cardScroll = EditorGUILayout.BeginScrollView(_cardScroll);
            foreach (var s in filtered)
                DrawCardRow(s);
            EditorGUILayout.EndScrollView();
        }

        private void DrawCardRow(CardStats s)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(s.CardId.ToString(),      GUILayout.Width(40));
                GUILayout.Label(s.CardName,               GUILayout.Width(110));
                GUILayout.Label(s.TypeLabel,              GUILayout.Width(70));
                GUILayout.Label(s.RarityLabel,            RarityStyle(s.RarityValue), GUILayout.Width(55));
                GUILayout.Label(s.Cost.ToString(),        GUILayout.Width(50));
                GUILayout.Label(s.BaseRent.ToString(),    GUILayout.Width(50));
                GUILayout.Label(s.DurabilityLabel,        GUILayout.Width(45));
                GUILayout.Label(SignedOrDash(s.InstantIncome), GUILayout.Width(50));
                GUILayout.Label(s.SettleTotalLabel,       GUILayout.Width(62));
                GUILayout.Label(s.DestroyPenalty > 0 ? $"-{s.DestroyPenalty}" : "-", GUILayout.Width(62));
                GUILayout.Label(s.NetGain.ToString("+#;-#;0"), NetStyle(s.NetGain), GUILayout.Width(65));
                GUILayout.Label(s.BreakEvenLabel,         GUILayout.Width(55));
                GUILayout.Label(s.RoiLabel,               RoiStyle(s.Roi),  GUILayout.Width(62));
            }
        }

        // ── 数据刷新 ──────────────────────────────────────────────

        private void Refresh()
        {
            _dirty = false;
            var all = Resources.LoadAll<CardData>("Cards");
            _stats = all
                .Where(c => c != null)
                .Select(c => new CardStats(c, _unlimitedRef))
                .OrderBy(s => s.CardId)
                .ToList();
        }

        // ── 过滤 + 排序 ───────────────────────────────────────────

        private int CountFiltered() => GetFilteredSorted().Count;

        private List<CardStats> GetFilteredSorted()
        {
            IEnumerable<CardStats> q = _stats;

            if (_typeFilter > 0)
            {
                string t = TypeOptions[_typeFilter];
                q = q.Where(s => s.TypeLabel == t);
            }

            if (_rarityFilter > 0)
            {
                int r = _rarityFilter - 1;
                q = q.Where(s => s.RarityValue == r);
            }

            if (_showOnlyNegative)
                q = q.Where(s => s.NetGain < 0);

            Func<CardStats, IComparable> key = _sortCol switch
            {
                0  => s => (IComparable)s.CardId,
                1  => s => s.CardName,
                2  => s => s.TypeLabel,
                3  => s => (IComparable)s.RarityValue,
                4  => s => (IComparable)s.Cost,
                5  => s => (IComparable)s.BaseRent,
                6  => s => (IComparable)s.Durability,
                7  => s => (IComparable)s.InstantIncome,
                8  => s => (IComparable)s.SettleTotal,
                9  => s => (IComparable)s.DestroyPenalty,
                10 => s => (IComparable)s.NetGain,
                11 => s => (IComparable)s.BreakEvenTurn,
                12 => s => (IComparable)s.Roi,
                _  => s => (IComparable)s.CardId
            };

            q = _sortAsc ? q.OrderBy(key) : q.OrderByDescending(key);
            return q.ToList();
        }

        // ── 样式辅助 ──────────────────────────────────────────────

        private static GUIStyle LoanPaymentStyle(int amount)
        {
            var s = new GUIStyle(GUI.skin.label);
            if (amount >= 4000)      s.normal.textColor = Color.red;
            else if (amount >= 1000) s.normal.textColor = new Color(1f, 0.55f, 0f);
            else                     s.normal.textColor = new Color(0.4f, 0.85f, 0.4f);
            return s;
        }

        private static GUIStyle NetStyle(int net)
        {
            var s = new GUIStyle(GUI.skin.label);
            s.normal.textColor = net >= 0 ? new Color(0.35f, 0.85f, 0.35f) : new Color(1f, 0.3f, 0.3f);
            return s;
        }

        private static GUIStyle RoiStyle(float roi)
        {
            var s = new GUIStyle(GUI.skin.label);
            if      (roi >= 300) s.normal.textColor = new Color(1f, 0.85f, 0.1f);
            else if (roi >= 100) s.normal.textColor = new Color(0.35f, 0.85f, 0.35f);
            else if (roi >= 0)   s.normal.textColor = GUI.skin.label.normal.textColor;
            else                 s.normal.textColor = new Color(1f, 0.3f, 0.3f);
            return s;
        }

        private static GUIStyle RarityStyle(int rarity)
        {
            var s = new GUIStyle(GUI.skin.label);
            s.normal.textColor = rarity switch
            {
                1 => new Color(0.4f, 0.6f, 1f),           // Rare - 蓝
                2 => new Color(0.7f, 0.4f, 1f),           // Epic - 紫
                3 => new Color(1f,   0.8f, 0.15f),        // Legendary - 金
                _ => GUI.skin.label.normal.textColor
            };
            return s;
        }

        // ── 布局辅助 ──────────────────────────────────────────────

        private static void HeaderLabel(string text, float width)
            => GUILayout.Label(text, EditorStyles.boldLabel, GUILayout.Width(width));

        private static void IntField(string label, ref int val, float labelW, float fieldW)
        {
            GUILayout.Label(label, GUILayout.Width(labelW));
            val = EditorGUILayout.IntField(val, GUILayout.Width(fieldW));
        }

        private static void FloatField(string label, ref float val, float labelW, float fieldW)
        {
            GUILayout.Label(label, GUILayout.Width(labelW));
            val = EditorGUILayout.FloatField(val, GUILayout.Width(fieldW));
        }

        private static string SignedOrDash(int v) => v == 0 ? "-" : v.ToString("+#;-#;0");

        // ────────────────────────────────────────────────────────
        // 内部数据模型
        // ────────────────────────────────────────────────────────

        private class CardStats
        {
            public int    CardId;
            public string CardName;
            public string TypeLabel;
            public string RarityLabel;
            public int    RarityValue;
            public int    Cost;
            public int    BaseRent;
            public int    Durability;
            public string DurabilityLabel;
            public int    InstantIncome;   // 即时 AddMoney 收益
            public int    SettleTotal;     // 全生命周期结算净收益（∞卡为参考回合）
            public string SettleTotalLabel;
            public int    DestroyPenalty;  // destroyEffect ReduceMoney
            public int    NetGain;         // 净收益 = 即时 + 结算总 - 销毁罚 - 费用
            public int    BreakEvenTurn;   // 回本回合（-1=永不，0=即时）
            public string BreakEvenLabel;
            public float  Roi;             // (净收益/费用)*100，费用为0时=0
            public string RoiLabel;

            public CardStats(CardData card, int unlimitedRef)
            {
                CardId       = card.cardId;
                CardName     = card.cardName ?? "";
                TypeLabel    = card.cardType.ToString();
                RarityLabel  = card.rarity.ToString();
                RarityValue  = (int)card.rarity;
                Cost         = card.cost;
                BaseRent     = card.baseRent;
                Durability   = card.durability;
                DurabilityLabel = card.durability <= 0 ? "∞" : card.durability.ToString();

                // 即时 effect
                int instantAdd    = ParseFirstInt("AddMoney",    card.instantEffect);
                int instantReduce = ParseFirstInt("ReduceMoney", card.instantEffect);
                InstantIncome = instantAdd - instantReduce;

                // 结算 effect（每回合）
                int settleAdd    = ParseFirstInt("AddMoney",    card.settleEffect);
                int settleReduce = ParseFirstInt("ReduceMoney", card.settleEffect);
                int netPerSettle = BaseRent + settleAdd - settleReduce;

                bool unlimited = card.durability <= 0;
                int  lifespan  = unlimited ? unlimitedRef : card.durability;

                SettleTotal      = netPerSettle * lifespan;
                SettleTotalLabel = unlimited
                    ? $"{SettleTotal}({unlimitedRef}T)"
                    : SettleTotal.ToString();

                // 销毁 effect
                DestroyPenalty = ParseFirstInt("ReduceMoney", card.destroyEffect);
                int destroyBonus = ParseFirstInt("AddMoney",  card.destroyEffect);

                NetGain = InstantIncome + SettleTotal - DestroyPenalty + destroyBonus - Cost;
                Roi     = Cost > 0 ? (float)NetGain / Cost * 100f : 0f;
                RoiLabel = Cost > 0 ? Roi.ToString("F0") + "%" : "N/A";

                // 回本回合：费用需要多少结算回合来弥补
                int outflow = Cost - InstantIncome; // 实际首回合净支出（即时可能抵扣）
                if (outflow <= 0)
                {
                    BreakEvenTurn  = 0;
                    BreakEvenLabel = "即时";
                }
                else if (netPerSettle <= 0)
                {
                    BreakEvenTurn  = int.MaxValue;
                    BreakEvenLabel = "N/A";
                }
                else
                {
                    BreakEvenTurn  = Mathf.CeilToInt((float)outflow / netPerSettle);
                    BreakEvenLabel = BreakEvenTurn > lifespan && !unlimited
                        ? $"{BreakEvenTurn}(!)"  // 卡死了也回不了本
                        : BreakEvenTurn.ToString();
                }
            }

            /// <summary>
            /// 从 effect 字符串中解析第一个匹配效果的整数参数。
            /// 格式：EffectId;arg1;arg2|EffectId2;arg1 ...
            /// </summary>
            private static int ParseFirstInt(string effectId, string effectString)
            {
                if (string.IsNullOrWhiteSpace(effectString)) return 0;

                var segments = effectString.Split('|');
                foreach (var seg in segments)
                {
                    var parts = seg.Trim().Split(';');
                    if (parts.Length < 2) continue;
                    if (!string.Equals(parts[0].Trim(), effectId, StringComparison.OrdinalIgnoreCase)) continue;
                    if (int.TryParse(parts[1].Trim(), out int val)) return val;
                }

                return 0;
            }
        }
    }
}
