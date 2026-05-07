// ==================== Shopping ====================

// API URLs
var shoppingApiUrls = {
    addShoppingItem: '/Api/AddShoppingItem',
    deleteShoppingItem: '/Api/DeleteShoppingItem'
};
var eventsApiUrls = {
    getEvents: '/events'
};
var shoppingLoaded = false;

// Load shopping list
function loadShoppingList() {
    fetch(eventsApiUrls.getEvents)
    .then(function(response) { return response.json(); })
    .then(function(data) {
        if (data.success && data.shoppingItems) {
            renderShoppingList(data.shoppingItems);
        }
    })
    .catch(function(err) { console.error('Error loading shopping list:', err); });
}

// Render shopping list
function renderShoppingList(items) {
    var container = document.getElementById('shoppingList');
    if (!container) return;
    
    if (!items || items.length === 0) {
        container.innerHTML = '<div class="text-muted text-center py-3">Список покупок пуст</div>';
        return;
    }
    
    container.innerHTML = items.map(function(item) {
        return '<div class="list-group-item d-flex justify-content-between align-items-center">' +
            '<span>' + escapeHtml(item) + '</span>' +
            '<button class="btn btn-sm btn-outline-danger delete-shopping-item" data-item="' + escapeHtml(item) + '">✕</button>' +
        '</div>';
    }).join('');
    
    // Add delete event listeners
    container.querySelectorAll('.delete-shopping-item').forEach(function(btn) {
        btn.addEventListener('click', function() {
            deleteShoppingItem(this.dataset.item);
        });
    });
}

// Delete shopping item
function deleteShoppingItem(item) {
    var formData = new FormData();
    formData.append('item', item);
    
    fetch(shoppingApiUrls.deleteShoppingItem, {
        method: 'POST',
        body: formData
    })
    .then(function(response) { return response.json(); })
    .then(function(data) {
        if (data.success) {
            showToast(data.message, 'success');
            loadShoppingList();
        } else {
            showToast(data.message, 'error');
        }
    })
    .catch(function(err) { showToast('Error: ' + err, 'error'); });
}

// Escape HTML to prevent XSS
function escapeHtml(text) {
    var div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Form submit handler
document.getElementById('shoppingForm').addEventListener('submit', function(e) {
    e.preventDefault();
    
    var formData = new FormData(e.target);
    var item = formData.get('item');
    
    fetch(shoppingApiUrls.addShoppingItem, {
        method: 'POST',
        body: formData
    })
    .then(function(response) { return response.json(); })
    .then(function(data) {
        if (data.success) {
            showToast(data.message, 'success', e.target.querySelector('button[type="submit"]'));
            e.target.reset();
            loadShoppingList();
        } else {
            showToast(data.message, 'error', e.target.querySelector('button[type="submit"]'));
        }
    })
    .catch(function(err) { showToast('Error: ' + err, 'error', e.target.querySelector('button[type="submit"]')); });
});

// Load shopping list when tab is shown
document.getElementById('shopping-tab').addEventListener('shown.bs.tab', function() {
    if (!shoppingLoaded) {
        loadShoppingList();
        shoppingLoaded = true;
    }
});