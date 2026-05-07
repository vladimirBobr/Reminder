// ==================== Digest Buttons ====================

document.querySelectorAll('.digest-btn').forEach(function(btn) {
    btn.addEventListener('click', function() {
        var action = btn.getAttribute('data-action');
        fetch(action, { method: 'POST' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                if (data.success) {
                    showToast(data.message, 'success', btn);
                } else {
                    showToast(data.message, 'error', btn);
                }
            })
            .catch(function(err) { showToast('Error: ' + err, 'error', btn); });
    });
});