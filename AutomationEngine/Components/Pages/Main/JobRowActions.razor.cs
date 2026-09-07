using AutomationEngine.Dto;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AutomationEngine.Components.Pages.Main
{
    public partial class JobRowActions
    {
        [Parameter]
        public JobDto? Job { get; set; }

        [Parameter]
        public EventCallback OnEdit { get; set; }

        [Parameter]
        public EventCallback OnHistory { get; set; }

        [Parameter]
        public EventCallback OnDuplicate { get; set; }

        [Parameter]
        public EventCallback OnDelete { get; set; }

        [Parameter]
        public EventCallback OnStop { get; set; }

        [Parameter]
        public EventCallback OnToggleEnabled { get; set; }

        [Parameter]
        public bool IsDeleteDisabled { get; set; }

        [Parameter]
        public bool IsStopDisabled { get; set; }

        private readonly string menuId = Guid.NewGuid().ToString("N");
        private bool isMenuOpen;
        private ElementReference triggerRef;
        private ElementReference menuRef;
        private DotNetObjectReference<JobRowActions>? dotNetRef;

        private async Task ToggleMenu()
        {
            if (isMenuOpen)
            {
                await CloseMenuAsync();
                return;
            }

            isMenuOpen = true;
            await InvokeAsync(StateHasChanged);
        }

        private async Task CloseMenuAsync()
        {
            if (!isMenuOpen)
            {
                return;
            }

            isMenuOpen = false;
            await JS.InvokeVoidAsync("jobRowActions.unregister", menuId);
            await InvokeAsync(StateHasChanged);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (isMenuOpen)
            {
                dotNetRef ??= DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("jobRowActions.openMenu", menuId, dotNetRef, triggerRef, menuRef);
            }
        }

        [JSInvokable]
        public async Task CloseMenuFromJs()
        {
            if (!isMenuOpen)
            {
                return;
            }

            isMenuOpen = false;
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleEdit()
        {
            await OnEdit.InvokeAsync();
            await CloseMenuAsync();
        }

        private async Task HandleHistory()
        {
            await OnHistory.InvokeAsync();
            await CloseMenuAsync();
        }

        private async Task HandleDuplicate()
        {
            await OnDuplicate.InvokeAsync();
            await CloseMenuAsync();
        }

        private async Task HandleDelete()
        {
            if (IsDeleteDisabled)
            {
                return;
            }

            await OnDelete.InvokeAsync();
            await CloseMenuAsync();
        }

        private async Task HandleStop()
        {
            if (IsStopDisabled)
            {
                return;
            }

            await OnStop.InvokeAsync();
            await CloseMenuAsync();
        }

        private async Task HandleToggleEnabled()
        {
            await OnToggleEnabled.InvokeAsync();
            await CloseMenuAsync();
        }
    }
}
