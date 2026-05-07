// ==================== Shopping ====================

// API URLs - defined in Index.cshtml via Razor
var shoppingApiUrls = window.shoppingApiUrls || {
    addShoppingItem: '/Api/AddShoppingItem'
};

document.getElementById('shoppingForm').addEventListener('submit', function(e) {
    e.preventDefault();
    var formData = new FormData(e.target);
    fetch(shoppingApiUrls.addShoppingItem, {
        method: 'POST',
        body: formData
    })
    .then(function(response) { return response.json(); })
    .then(function(data) {
        if (data.success) {
            showToast(data.message, 'success', e.target.querySelector('button[type="submit"]'));
            e.target.reset();
        } else {
            showToast(data.message, 'error', e.target.querySelector('button[type="submit"]'));
        }
    })
    .catch(function(err) { showToast('Error: ' + err, 'error', e.target.querySelector('button[type="submit"]')); });
});