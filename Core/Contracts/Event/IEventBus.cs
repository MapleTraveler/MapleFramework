using System;

namespace Maple.Core
{
    public interface IEventBus
    {
        EventToken Subscribe<T>(Action<T> handler);
        void Publish<T>(T evt);
    }
}
