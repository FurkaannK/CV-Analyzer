// CVScore — Global JS
(function () {
    'use strict';

    // Bootstrap tooltip init
    var tooltipEls = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipEls.forEach(function (el) {
        new bootstrap.Tooltip(el);
    });

    // Auto-dismiss alerts after 4 seconds
    setTimeout(function () {
        document.querySelectorAll('.alert.alert-success').forEach(function (el) {
            var bsAlert = bootstrap.Alert.getOrCreateInstance(el);
            if (bsAlert) bsAlert.close();
        });
    }, 4000);
})();
