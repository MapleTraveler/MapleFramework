using Maple.Core;

namespace Maple.Framework
{
    /// <summary>
    /// 通用游戏状态机，状态类型由游戏定义（泛型 TState，通常为 enum）。
    /// 不包含具体状态逻辑，仅维护当前状态并在 ChangeState 时发布 GameStateChangedEvent&lt;TState&gt;。
    /// 游戏可自行创建并注册到 ServiceHub，例如 ServiceHub.Register(gameStateManager)。
    /// </summary>
    public class GameStateManager<TState> where TState : struct
    {
        public TState CurrentState { get; private set; }

        public void ChangeState(TState newState)
        {
            TState oldState = CurrentState;
            CurrentState = newState;

            if (Eventer.IsReady)
                Eventer.Publish(new GameStateChangedEvent<TState> { OldState = oldState, NewState = newState });
        }
    }
}
