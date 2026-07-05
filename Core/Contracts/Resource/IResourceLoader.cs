using System;

namespace Maple.Core
{
    public interface IResourceLoader
    {
        T Load<T>(string path) where T : class;
        void LoadAsync<T>(string path, Action<T> onComplete) where T : class;
        void Release(string path);
        void ReleaseAll();
    }
}
