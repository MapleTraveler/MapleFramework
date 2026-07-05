namespace Maple.Framework
{
    /// <summary>
    /// 状态变化时发布的事件，TState 由游戏定义（如 enum EGameState）。
    /// </summary>
    public struct GameStateChangedEvent<TState> where TState : struct
    {
        public TState OldState;
        public TState NewState;
    }
}
