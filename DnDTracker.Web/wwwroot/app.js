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

const dndScrollUserGuideToId = (id) => {
    if (!id || !document.querySelector('.user-guide')) {
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

const dndInitUserGuideAnchors = () => {
    const hash = window.location.hash;
    if (hash.length > 1) {
        requestAnimationFrame(() => {
            dndScrollUserGuideToId(decodeURIComponent(hash.slice(1)));
        });
    }
};

document.addEventListener('click', (event) => {
    const link = event.target.closest('.user-guide a[href^="#"], .user-guide a[href^="/user-guide#"]');
    if (!link) {
        return;
    }

    const href = link.getAttribute('href') ?? '';
    const hashIndex = href.indexOf('#');
    if (hashIndex < 0) {
        return;
    }

    const id = decodeURIComponent(href.slice(hashIndex + 1));
    if (dndScrollUserGuideToId(id)) {
        event.preventDefault();
        event.stopPropagation();
    }
}, true);

document.addEventListener('DOMContentLoaded', dndInitUserGuideAnchors);
document.addEventListener('blazor:enhanced:load', dndInitUserGuideAnchors);
