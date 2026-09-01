using Microsoft.AspNetCore.Components;

namespace AutomationEngine.Components.Pages.Main
{
    public partial class StopConfirm : ComponentBase
    {
        [Parameter]
        public bool IsVisible { get; set; } = false;

        [Parameter]
        public string JobId { get; set; } = string.Empty;

        [Parameter]
        public string JobDisplayName { get; set; } = string.Empty;

        [Parameter]
        public EventCallback OnConfirm { get; set; }

        [Parameter]
        public EventCallback OnClose { get; set; }

        private async Task OnConfirmClick()
        {
            await OnConfirm.InvokeAsync();
        }

        private async Task OnCloseClick()
        {
            await OnClose.InvokeAsync();
        }
    }
}
