using BaoZuPo.GameFlow;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace BaoZuPo.NodeActions
{
    [Category("BaoZuPo/Turn Flow")]
    [Name("Check Game Over")]
    [Description("Check whether the game has entered the GameOver state.")]
    public class Condition_CheckGameOver : ConditionTask
    {
        protected override bool OnCheck()
        {
            return TurnManager.Instance.IsGameOver;
        }
    }
}
