window.dndScrollRollRowIntoView = (elementId) => {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollIntoView({ block: 'center', behavior: 'smooth' });
    }
};
