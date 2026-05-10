(function () {
    window.oaiTooltips = {
        initialize: function () {
            if (!window.bootstrap || !window.bootstrap.Tooltip) {
                return;
            }

            document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (element) {
                window.bootstrap.Tooltip.getInstance(element)?.dispose();
                new window.bootstrap.Tooltip(element);
            });
        },

        dispose: function () {
            if (!window.bootstrap || !window.bootstrap.Tooltip) {
                return;
            }

            document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (element) {
                window.bootstrap.Tooltip.getInstance(element)?.dispose();
            });
        }
    };
})();
