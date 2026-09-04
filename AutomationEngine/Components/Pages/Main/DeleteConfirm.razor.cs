using Microsoft.AspNetCore.Components;

namespace AutomationEngine.Components.Pages.Main
{
    public partial class DeleteConfirm : ComponentBase
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

        private bool isDeleting = false;

        private async Task OnConfirmClick()
        {
            try
            {
                isDeleting = true;
                await Task.Delay(200);
                await OnConfirm.InvokeAsync();
            }
            finally
            {
                isDeleting = false;
            }
        }

        private async Task OnCloseClick()
        {
            await OnClose.InvokeAsync();
        }
    }
}
