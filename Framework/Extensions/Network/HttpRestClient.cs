using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Maple.Core;
using UnityEngine.Networking;

namespace Maple.Extensions
{
    public class HttpRestClient : IRequestResponseClient
    {
        private readonly ISerializer _serializer;
        private readonly int _timeoutSeconds;
        
        public HttpRestClient(ISerializer serializer, int timeoutSeconds = 30)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _timeoutSeconds = timeoutSeconds <= 0 ? 30 : timeoutSeconds;
        }

        public async Task<TResponse> SendAsync<TRequest, TResponse>(
            string url,
            TRequest request,
            CancellationToken cancellationToken = default)
        {
            byte[] bytes = _serializer.Serialize(request);
            var webRequest = new UnityWebRequest(url, "POST");
            webRequest.uploadHandler = new UploadHandlerRaw(bytes);
            webRequest.SetRequestHeader("Content-Type", "application/json");
            
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = _timeoutSeconds;
            
            // UnityWebRequest 没有原生的 async/await 支持，这里使用 UniTask 进行适配。
            await webRequest
                .SendWebRequest()
                .ToUniTask(cancellationToken: cancellationToken);

            // TODO: 根据需要添加更多 HTTP 错误处理（如超时、网络不可用等），目前只简单抛异常。
            try
            {
                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    long? code = webRequest.responseCode > 0 ? (long?)webRequest.responseCode : null;
                    throw new Exception($"HTTP Error ({code?.ToString() ?? "n/a"}): {webRequest.error}");
                }
                byte[] downloadHandlerData = webRequest.downloadHandler.data;
                
                try
                {
                    return _serializer.Deserialize<TResponse>(downloadHandlerData);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new Exception("Response parse failed.", ex);
                }
                
            }
            finally
            {
                webRequest.Dispose();
            }
            
        }
    }
}