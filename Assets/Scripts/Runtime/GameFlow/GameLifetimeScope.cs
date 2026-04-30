using BaoZuPo.Core;
using BaoZuPo.UI;
using VContainer;
using VContainer.Unity;

namespace BaoZuPo.GameFlow
{
    public sealed class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ICardPlayService, CardPlayService>(Lifetime.Scoped);
            builder.Register<ISettlementService, SettlementService>(Lifetime.Scoped);
            builder.Register<ISettlementPresentationMapper, SettlementPresentationMapper>(Lifetime.Scoped);
            builder.Register<ISettlementPresentationService, SettlementPresentationService>(Lifetime.Scoped);
            builder.Register<IRewardService, RewardService>(Lifetime.Scoped);
            builder.Register<IShopService, ShopService>(Lifetime.Scoped);
            builder.Register<ITurnFlowService, TurnFlowService>(Lifetime.Scoped);
            builder.RegisterComponentInHierarchy<GameManager>();
            builder.RegisterComponentInHierarchy<TurnManager>();
            builder.RegisterComponentInHierarchy<UICardDragController>();
            builder.RegisterEntryPoint<TurnFlowEntryPoint>(Lifetime.Scoped);
        }
    }
}
