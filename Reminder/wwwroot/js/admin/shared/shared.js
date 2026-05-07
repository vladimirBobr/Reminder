// ==================== Shared Utilities ====================

// Toast notification function - used by all features
function showToast(message, type, anchorElement) {
    var toast = document.createElement('div');
    toast.className = 'toast-message toast-' + type;
    toast.textContent = message;
    
    if (anchorElement) {
        var rect = anchorElement.getBoundingClientRect();
        toast.style.position = 'fixed';
        toast.style.top = (rect.top - 10) + 'px';
        toast.style.right = (window.innerWidth - rect.right - 10) + 'px';
        toast.style.left = 'auto';
        toast.style.bottom = 'auto';
    }
    
    document.body.appendChild(toast);
    setTimeout(function() { toast.classList.add('show'); }, 10);
    setTimeout(function() {
        toast.classList.remove('show');
        setTimeout(function() { toast.remove(); }, 300);
    }, 3000);
}