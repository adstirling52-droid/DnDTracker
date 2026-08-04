window.dndScrollRollRowIntoView = (elementId) => {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollIntoView({ block: 'center', behavior: 'smooth' });
    }
};

window.dndCopyToClipboard = async (text) => {
    if (!text) {
        return false;
    }

    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch {
        return false;
    }
};

const dndVisibilityHandlers = new WeakMap();

window.dndRegisterVisibilityRefresh = (dotNetRef) => {
    const handler = () => {
        if (document.visibilityState === 'visible') {
            dotNetRef.invokeMethodAsync('RefreshCurrentNpcsFromJsAsync');
        }
    };

    document.addEventListener('visibilitychange', handler);
    window.addEventListener('pageshow', handler);
    dndVisibilityHandlers.set(dotNetRef, handler);
};

window.dndUnregisterVisibilityRefresh = (dotNetRef) => {
    const handler = dndVisibilityHandlers.get(dotNetRef);
    if (!handler) {
        return;
    }

    document.removeEventListener('visibilitychange', handler);
    window.removeEventListener('pageshow', handler);
    dndVisibilityHandlers.delete(dotNetRef);
};
