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

window.dndInitUserGuideAnchors = () => {
    const container = document.querySelector('.user-guide');
    if (!container || container.dataset.anchorsInit === 'true') {
        return;
    }

    container.dataset.anchorsInit = 'true';

    const scrollToId = (id) => {
        if (!id) {
            return false;
        }

        const target = document.getElementById(id);
        if (!target) {
            return false;
        }

        target.scrollIntoView({ behavior: 'smooth', block: 'start' });
        history.replaceState(null, '', `/user-guide#${id}`);
        return true;
    };

    container.addEventListener('click', (event) => {
        const link = event.target.closest('a[href^="#"]');
        if (!link) {
            return;
        }

        const id = decodeURIComponent(link.getAttribute('href').slice(1));
        if (scrollToId(id)) {
            event.preventDefault();
        }
    });

    const hash = window.location.hash;
    if (hash.length > 1) {
        scrollToId(decodeURIComponent(hash.slice(1)));
    }
};
