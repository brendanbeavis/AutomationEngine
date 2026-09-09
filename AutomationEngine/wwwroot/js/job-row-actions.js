window.jobRowActions = window.jobRowActions || {};

(function () {
    const state = {
        activeMenu: null
    };

    function positionMenu(triggerElement, menuElement) {
        if (!triggerElement || !menuElement) {
            return;
        }

        const triggerRect = triggerElement.getBoundingClientRect();

        menuElement.style.position = 'fixed';
        menuElement.style.zIndex = '2001';
        menuElement.style.display = 'block';
        menuElement.style.visibility = 'hidden';
        menuElement.style.left = '0px';
        menuElement.style.top = '0px';

        const menuRect = menuElement.getBoundingClientRect();
        const viewportWidth = window.innerWidth;
        const viewportHeight = window.innerHeight;
        const gap = 6;
        const padding = 8;

        let left = triggerRect.right - menuRect.width;
        if (left < padding) {
            left = padding;
        }
        if (left + menuRect.width > viewportWidth - padding) {
            left = Math.max(padding, viewportWidth - menuRect.width - padding);
        }

        const spaceBelow = viewportHeight - triggerRect.bottom;
        const spaceAbove = triggerRect.top;
        let top;

        if (spaceBelow >= menuRect.height + gap || spaceBelow >= spaceAbove) {
            top = triggerRect.bottom + gap;
        } else {
            top = triggerRect.top - menuRect.height - gap;
            if (top < padding) {
                top = padding;
            }
        }

        if (top + menuRect.height > viewportHeight - padding) {
            top = Math.max(padding, viewportHeight - menuRect.height - padding);
        }

        menuElement.style.left = `${left}px`;
        menuElement.style.top = `${top}px`;
        menuElement.style.visibility = 'visible';
    }

    window.jobRowActions.openMenu = function (menuId, dotNetRef, triggerElement, menuElement) {
        if (!triggerElement || !menuElement) {
            return;
        }

        if (state.activeMenu && state.activeMenu.id !== menuId) {
            const previousMenu = state.activeMenu;
            state.activeMenu = null;
            previousMenu.dotNetRef?.invokeMethodAsync('CloseMenuFromJs').catch(() => { });
        }

        state.activeMenu = {
            id: menuId,
            dotNetRef: dotNetRef,
            triggerElement: triggerElement,
            menuElement: menuElement
        };

        positionMenu(triggerElement, menuElement);
    };

    window.jobRowActions.unregister = function (menuId) {
        if (state.activeMenu && state.activeMenu.id === menuId) {
            state.activeMenu = null;
        }
    };
})();
