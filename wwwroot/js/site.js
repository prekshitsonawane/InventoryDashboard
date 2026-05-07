/**
 * site.js
 * Client-side logic for the Inventory Dashboard
 */

document.addEventListener('DOMContentLoaded', function () {
    
    // Auto-hide success toasts after 5 seconds
    var toastSuccess = document.getElementById('toast-success');
    if (toastSuccess) {
        var bsToast = new bootstrap.Toast(toastSuccess, { delay: 5000 });
        bsToast.show();
    }

    // Show error toasts
    var toastError = document.getElementById('toast-error');
    if (toastError) {
        var bsToastErr = new bootstrap.Toast(toastError, { autohide: false });
        bsToastErr.show();
    }

    // Session Timeout Logic (if user is authenticated)
    // The server auth cookie is valid for 8 hours. We show a warning 5 mins before.
    var sessionDurationHours = 8;
    var sessionDurationMs = sessionDurationHours * 60 * 60 * 1000;
    var warningLeadTimeMs = 5 * 60 * 1000; // 5 minutes before expiry
    var warningTimeMs = sessionDurationMs - warningLeadTimeMs;
    var sessionWarningTimer;
    var countdownInterval;

    var warningModalEl = document.getElementById('sessionWarningModal');
    if (warningModalEl) {
        var warningModal = new bootstrap.Modal(warningModalEl, { backdrop: 'static', keyboard: false });
        
        // Start timer
        startSessionTimer();

        // Extend Session Button
        document.getElementById('extendSessionBtn').addEventListener('click', function () {
            fetch(window.location.href, { method: 'HEAD' })
                .then(function() {
                    warningModal.hide();
                    clearInterval(countdownInterval);
                    startSessionTimer(); // Reset timers
                })
                .catch(function() {
                    // Fallback: reload page
                    window.location.reload();
                });
        });
    }

    function startSessionTimer() {
        clearTimeout(sessionWarningTimer);
        sessionWarningTimer = setTimeout(showSessionWarning, warningTimeMs);
    }

    function showSessionWarning() {
        if (!warningModalEl) return;
        warningModal.show();
        
        var countdownEl = document.getElementById('countdown');
        var remainingSeconds = warningLeadTimeMs / 1000;
        
        countdownInterval = setInterval(function () {
            remainingSeconds--;
            if (remainingSeconds <= 0) {
                clearInterval(countdownInterval);
                // Redirect to login or auto-logout
                var logoutForm = document.querySelector('form[action="/Account/Logout"]');
                if (logoutForm) logoutForm.submit();
                else window.location.href = '/Account/Login';
                return;
            }
            var m = Math.floor(remainingSeconds / 60);
            var s = remainingSeconds % 60;
            countdownEl.textContent = m + ":" + (s < 10 ? '0' : '') + s;
        }, 1000);
    }

    // Filter Form Auto-Submit (if dropdowns change)
    var filterForm = document.getElementById('filterForm');
    if (filterForm) {
        var selects = filterForm.querySelectorAll('select');
        selects.forEach(function(s) {
            s.addEventListener('change', function() {
                // Remove empty fields to keep URL clean
                var inputs = filterForm.querySelectorAll('input, select');
                inputs.forEach(function(i) {
                    if (!i.value) i.disabled = true;
                });
                filterForm.submit();
            });
        });
        
        // Prevent empty search from submitting
        filterForm.addEventListener('submit', function() {
            var inputs = filterForm.querySelectorAll('input, select');
            inputs.forEach(function(i) {
                if (!i.value) i.disabled = true;
            });
        });
    }
});
