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
