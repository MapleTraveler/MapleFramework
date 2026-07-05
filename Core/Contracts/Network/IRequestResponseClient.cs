using System.Threading;
using System.Threading.Tasks;

namespace Maple.Core
{
    /// <summary>
    /// 请求-响应式通讯契约（如 REST、LLM API）。发请求，异步拿响应，支持取消。
    /// </summary>
    public interface IRequestResponseClient
    {
        Task<TResponse> SendAsync<TRequest, TResponse>(
            string url,
            TRequest request,
            CancellationToken cancellationToken = default);
    }
}