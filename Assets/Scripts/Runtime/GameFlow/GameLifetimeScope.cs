using VContainer;
using VContainer.Unity;
using BaoZuPo.UI;

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
            builder.RegisterComponentInHierarchy<TurnManager>();
            builder.RegisterComponentInHierarchy<UICardDragController>();
        }
    }
}
