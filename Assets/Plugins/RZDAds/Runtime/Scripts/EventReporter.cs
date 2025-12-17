using System;
using Cysharp.Threading.Tasks;
using Plugins.RZDAds.Runtime.Scripts.ApiSystem;

namespace Plugins.RZDAds.Runtime.Scripts
{
    public class EventReporter
    {
        private readonly Api _api;
        private readonly ILogger _logger;

        public EventReporter(Api api, ILogger logger = null)
        {
            _api = api;
            _logger = logger;
        }

        public async UniTask ReportShown(uint id, float duration)
            => await Report(id, "show", duration);

        public async UniTask ReportClicked(uint id)
            => await Report(id, "click", 0);

        private async UniTask Report(uint id, string type, float duration)
        {
            try
            {
                var result = await _api.ReportEvent(id, type, duration);
                _logger?.Log($"[Reporter] Report [{type}] id=({id}) ok=({result.isDone})");
            }
            catch (Exception e)
            {
                _logger?.Log($"[Reporter] Report [{type}] id=({id}) error: {e}");
            }
        }
    }
}