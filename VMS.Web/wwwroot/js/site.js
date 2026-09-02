window.vmsCloseNavDrawer = () => {
    const el = document.getElementById('vmsNavDrawer');
    if (!el) {
        return;
    }

    bootstrap.Offcanvas.getInstance(el)?.hide();
};
