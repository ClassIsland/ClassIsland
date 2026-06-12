using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Core.Attributes;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ClassIsland.Models.Actions;

namespace ClassIsland.Services.Automation.Actions
{
    [ActionInfo("classisland.DoSpeech", "语音播报", "\uee4a")]
    public class DoSpeechAction(ISpeechService speechService) : ActionBase<DoSpeechActionSettings>
    {
        protected override async Task OnInvoke()
        {
            await base.OnInvoke();
            var text = Settings?.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("语音播报内容不能为空");
            }
            else
            {
                speechService.EnqueueSpeechQueue(text);
            }
        }
    }
}