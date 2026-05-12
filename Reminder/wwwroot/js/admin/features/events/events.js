// ==================== Events Tab Logic ====================

var eventsData = [];
var showPastEvents = false; // Toggle: show today + past
var showAllFuture = false; // Toggle: show all future events (no pagination)
var displayedWeeks = 4; // Show first 4 weeks by default
var WEEKS_PER_PAGE = 4;

// API URLs
var apiUrls = {
    getEvents: '/events',
    addEvent: '/events/add',
    updateEvent: '/events/update',
    deleteEvent: '/events/delete'
};

// ==================== Events Tab Logic ====================

function loadEvents() {
    var eventsList = document.getElementById('eventsList');
    eventsList.innerHTML = '<div class="text-center text-muted py-4"><div class="spinner-border spinner-border-sm" role="status"></div> Загрузка событий...</div>';
    
    fetch(apiUrls.getEvents)
        .then(function(response) { return response.json(); })
        .then(function(data) {
            if (data.success) {
                eventsData = data.events;
                
                // Update badge if there are past events
                updatePastEventsBadge();
                
                // Clear list before rendering
                var eventsList = document.getElementById('eventsList');
                eventsList.innerHTML = '';
                renderEventsList(false);
            } else {
                eventsList.innerHTML = '<div class="text-danger p-3">Ошибка: ' + data.message + '</div>';
            }
        })
        .catch(function(err) {
            eventsList.innerHTML = '<div class="text-danger p-3">Ошибка загрузки: ' + err + '</div>';
        });
}

function renderEventsList(forceRefresh) {
    var eventsList = document.getElementById('eventsList');
    if (eventsData.length === 0) {
        eventsList.innerHTML = '<div class="text-muted p-3">Нет событий</div>';
        return;
    }
    
    // If force refresh, clear the list
    if (forceRefresh) {
        eventsList.innerHTML = '';
    }
    
    // Group events by date
    var groupedByDate = {};
    eventsData.forEach(function(event) {
        if (!groupedByDate[event.date]) groupedByDate[event.date] = [];
        groupedByDate[event.date].push(event);
    });
    
    // Sort dates
    var sortedDates = Object.keys(groupedByDate).sort();
    
    // Filter by past events toggle and pagination
    var today = new Date();
    today.setHours(0, 0, 0, 0);
    var startDate = new Date(today);
    startDate.setDate(startDate.getDate() - startDate.getDay() + 1); // Monday of current week
    var endDate = new Date(startDate);
    endDate.setDate(endDate.getDate() + displayedWeeks * 7); // End of displayed weeks
    
    var filteredDates = sortedDates;
    
    // Calculate date ranges
    var thirtyDaysFromNow = new Date(today);
    thirtyDaysFromNow.setDate(thirtyDaysFromNow.getDate() + 30);
    
    // Default: show only current (today to today+30)
    var currentDates = sortedDates.filter(function(dateStr) {
        var eventDate = new Date(dateStr);
        return eventDate >= today && eventDate <= thirtyDaysFromNow;
    });
    
    var futureDates = sortedDates.filter(function(dateStr) {
        var eventDate = new Date(dateStr);
        return eventDate > thirtyDaysFromNow;
    });
    
    var pastDates = sortedDates.filter(function(dateStr) {
        var eventDate = new Date(dateStr);
        return eventDate < today;
    });
    
    // Build filtered list based on toggles
    filteredDates = currentDates; // Start with current
    
    if (showPastEvents) {
        // Add past to current
        pastDates.forEach(function(dateStr) {
            if (filteredDates.indexOf(dateStr) === -1) {
                filteredDates.push(dateStr);
            }
        });
    }
    
    if (showAllFuture) {
        // Add future to current (or current + past if both toggles)
        futureDates.forEach(function(dateStr) {
            if (filteredDates.indexOf(dateStr) === -1) {
                filteredDates.push(dateStr);
            }
        });
    }
    
    // Sort the filtered dates
    filteredDates.sort();
    
    // Build HTML for new events
    var html = '';
    var daysRu = ['Вс', 'Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб'];
    
    filteredDates.forEach(function(dateStr) {
        // Skip if already rendered (to avoid duplicates when loading more)
        // But always render if force refresh
        if (!forceRefresh) {
            var existingDateHeader = eventsList.querySelector('[data-date="' + dateStr + '"]');
            if (existingDateHeader) return;
        }
        
        var eventsOnDate = groupedByDate[dateStr];
        
        // Sort events by time (events without time go last)
        eventsOnDate.sort(function(a, b) {
            if (!a.time && !b.time) return 0;
            if (!a.time) return 1;
            if (!b.time) return -1;
            return a.time.localeCompare(b.time);
        });
        
        var firstEvent = eventsOnDate[0];
        var date = new Date(firstEvent.date);
        var dayName = daysRu[date.getDay()];
        var day = String(date.getDate()).padStart(2, '0');
        var month = String(date.getMonth() + 1).padStart(2, '0');
        var dateFormatted = day + '.' + month;
        var isWeekend = date.getDay() === 6 || date.getDay() === 0;
        var isSaturday = date.getDay() === 6;
        
        // Date header - smaller and colored by weekend
        var headerBgStyle = 'background-color: rgba(0, 57, 166, 0.08);';
        if (isSaturday) headerBgStyle = 'background-color: rgba(255, 193, 7, 0.15);'; // Warning yellow tint
        else if (isWeekend) headerBgStyle = 'background-color: rgba(220, 53, 69, 0.12);'; // Danger red tint
        html += '<div class="date-group-header px-3 py-1" data-date="' + dateStr + '" style="' + headerBgStyle + ' border-bottom: 1px solid rgba(0, 57, 166, 0.1); font-size: 0.9rem;">';
        html += '<span class="fw-bold">' + dateFormatted + '</span>';
        html += ' <span class="text-muted">' + dayName + '</span>';
        if (eventsOnDate.length > 1) {
            html += ' <span class="badge bg-secondary ms-1" style="font-size: 0.75rem;">' + eventsOnDate.length + '</span>';
        }
        html += '</div>';
        
        // Events under this date
        eventsOnDate.forEach(function(event) {
            var importantBadge = event.important ? '<span class="me-1">⭐</span>' : '';
            var subjectText = event.subject || event.description || 'Без названия';
            var textContent = subjectText;
            if (event.subject && event.description) {
                // Time inside subject, description separate
                var subjectWithTime = event.time ? '<span class="text-muted me-2">' + event.time + '</span>' + importantBadge + event.subject : importantBadge + event.subject;
                textContent = '<div>' + subjectWithTime + '</div><div class="text-muted small" style="word-break: break-word; padding-left: 1.5rem;">' + event.description + '</div>';
            } else {
                // Only subject or only description
                if (event.time) {
                    textContent = '<span class="text-muted me-2">' + event.time + '</span>' + importantBadge + subjectText;
                } else if (event.important) {
                    textContent = importantBadge + subjectText;
                }
            }
            
            html += '<div class="d-flex align-items-start gap-2 py-2 px-3 border-bottom event-item" onclick="showEditEventDialog(\'' + event.key + '\')" style="cursor: pointer;">';
            
            // Event text
            html += '<div class="flex-grow-1 event-text" style="word-break: break-word; line-height: 1.4; white-space: pre-line;">';
            html += textContent + '</div>';
            
            // Edit button
            html += '<div class="flex-shrink-0">';
            html += '<button class="btn btn-sm btn-link text-muted p-0" onclick="showEditEventDialog(\'' + event.key + '\')">';
            html += '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">';
            html += '<path d="M12.146.146a.5.5 0 0 1 .708 0l3 3a.5.5 0 0 1 0 .708l-10 10a.5.5 0 0 1-.168.11l-5 2a.5.5 0 0 1-.65-.65l2-5a.5.5 0 0 1 .11-.168l10-10zM11.207 2.5 13.5 4.793 14.793 3.5 12.5 1.207 11.207 2.5zm1.586 3L10.5 3.207 4 9.707V10h.5a.5.5 0 0 1 .5.5v.5h.5a.5.5 0 0 1 .5.5v.5h.293l6.5-6.5zm-9.761 5.175-.106.106-1.528 3.821 3.821-1.528.106-.106A.5.5 0 0 1 5 12.5V12h-.5a.5.5 0 0 1-.5-.5V11h-.5a.5.5 0 0 1-.468-.325z"/>';
            html += '</svg>';
            html += '</button>';
            html += '</div>';
            
            html += '</div>';
        });
    });
    
    
    // Remove existing "Next" button if present
    var existingNextBtn = eventsList.querySelector('.load-more-btn');
    if (existingNextBtn) {
        existingNextBtn.remove();
    }
    
    // Remove existing "Add Event" button if present
    var existingAddBtn = eventsList.querySelector('.add-event-btn');
    if (existingAddBtn) {
        existingAddBtn.remove();
    }
    
    // Append new events to the list (before "Next" button position)
    if (html) {
        eventsList.insertAdjacentHTML('beforeend', html);
    }
    
    // Sync toggle states with state variables
    updateToggleStates();
    
    
}

function togglePastEvents() {
    var checkbox = document.getElementById('pastEventsToggle');
    showPastEvents = checkbox ? checkbox.checked : false;
    renderEventsList(true); // Force refresh
}

function toggleFutureEvents() {
    var checkbox = document.getElementById('futureEventsToggle');
    showAllFuture = checkbox ? checkbox.checked : false;
    renderEventsList(true); // Force refresh
}

// Sync checkboxes with state
function updateToggleStates() {
    var pastToggle = document.getElementById('pastEventsToggle');
    var futureToggle = document.getElementById('futureEventsToggle');
    if (pastToggle) pastToggle.checked = showPastEvents;
    if (futureToggle) futureToggle.checked = showAllFuture;
}

function updatePastEventsBadge() {
    var today = new Date();
    today.setHours(0, 0, 0, 0);
    
    // Count past events (date < today)
    var pastCount = eventsData.filter(function(event) {
        var eventDate = new Date(event.date);
        return eventDate < today;
    }).length;
    
    // Also count events from today (current)
    var currentCount = eventsData.filter(function(event) {
        var eventDate = new Date(event.date);
        return eventDate.getTime() >= today.getTime() && eventDate.getTime() < today.getTime() + 86400000;
    }).length;
    
    // If there are past events, show count next to Past label
    var countSpan = document.getElementById('pastCount');
    if (countSpan) {
        if (pastCount > 0) {
            countSpan.textContent = '(' + pastCount + ')';
            countSpan.style.display = 'inline-block';
        } else {
            countSpan.style.display = 'none';
        }
    }
}


// Context menu for editing
var editMenu = null;

function showEditMenu(e, key) {
    e.preventDefault();
    
    var event = eventsData.find(function(ev) { return ev.key === key; });
    if (!event) return;
    
    // Remove existing menu
    var existing = document.getElementById('editMenu');
    if (existing) existing.remove();
    
    // Create menu
    var menu = document.createElement('div');
    menu.id = 'editMenu';
    menu.className = 'dropdown-menu show p-3';
    menu.style.position = 'fixed';
    menu.style.left = Math.min(e.clientX, window.innerWidth - 200) + 'px';
    menu.style.top = Math.min(e.clientY, window.innerHeight - 150) + 'px';
    menu.style.minWidth = '200px';
    menu.style.zIndex = '9999';
    
    menu.innerHTML = '<div class="mb-2"><label class="form-label small text-muted">Название</label><input type="text" class="form-control" id="editSubject" value="' + (event.subject || '') + '"></div>' +
                     '<div class="mb-2"><label class="form-label small text-muted">Описание</label><textarea class="form-control" rows="5" id="editDesc">' + (event.description || '') + '</textarea></div>' +
                     '<button class="btn btn-sm btn-primary w-100" onclick="saveEventEdit(\'' + key + '\')">Сохранить</button>';
    
    document.body.appendChild(menu);
    editMenu = menu;
    
    // Make menu wider
    menu.style.minWidth = '400px';
    
    // Close on click outside (but not on menu itself)
    setTimeout(function() {
        document.addEventListener('click', function handler(e) {
            if (!menu.contains(e.target)) {
                closeEditMenu();
                document.removeEventListener('click', handler);
            }
        });
    }, 10);
}

function closeEditMenu() {
    var menu = document.getElementById('editMenu');
    if (menu) menu.remove();
    editMenu = null;
}

function saveEventEdit(key) {
    var subject = document.getElementById('editSubject-' + key).value;
    var description = document.getElementById('editDesc-' + key).value;
    var dateInput = document.getElementById('date-' + key);
    var timeInput = document.getElementById('time-' + key);
    var date = dateInput && dateInput.value ? dateInput.value : null;
    var time = timeInput && timeInput.value ? timeInput.value : null;
    
    var body = { key: key, subject: subject, description: description };
    if (date) body.date = date;
    if (time) body.time = time;
    
    fetch(apiUrls.updateEvent, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    })
    .then(function(response) { return response.json(); })
    .then(function(data) {
        if (data.success) {
            // Update local cache with new key if changed
            if (data.newKey) {
                var idx = eventsData.findIndex(function(e) { return e.key === key; });
                if (idx !== -1) {
                    eventsData[idx].key = data.newKey;
                    eventsData[idx].date = date;
                    eventsData[idx].time = time;
                }
            }
            // Update all fields in cache
            var event = eventsData.find(function(e) { return e.key === (data.newKey || key); });
            if (event) {
                event.subject = subject;
                event.description = description;
                event.date = date;
                event.time = time;
            }
            renderEventsList(true); // Force refresh after edit
            showToast('Сохранено', 'success');
        } else {
            showToast('Ошибка: ' + data.message, 'error');
        }
    })
    .catch(function(err) {
        showToast('Ошибка: ' + err, 'error');
    });
}

function deleteEvent(key) {
    fetch(apiUrls.deleteEvent, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ key: key })
    })
    .then(function(response) { return response.json(); })
    .then(function(data) {
        if (data.success) {
            showToast('Событие удалено', 'success');
            eventsData = eventsData.filter(function(e) { return e.key !== key; });
            renderEventsList(true); // Force refresh after delete
        } else {
            showToast('Ошибка: ' + data.message, 'error');
        }
    })
    .catch(function(err) {
        showToast('Ошибка: ' + err, 'error');
    });
}


// ==================== Add Event Modal ====================

function showAddEventDialog() {
    var today = new Date().toISOString().split('T')[0];
    document.getElementById('newEventDate').value = today;
    document.getElementById('newEventTime').value = '';
    document.getElementById('newEventSubject').value = '';
    document.getElementById('newEventDescription').value = '';
    document.getElementById('newEventImportant').checked = false;
    
    var modal = new bootstrap.Modal(document.getElementById('addEventModal'));
    modal.show();
}

function saveNewEvent() {
    var subject = document.getElementById('newEventSubject').value;
    var description = document.getElementById('newEventDescription').value;
    var dateInput = document.getElementById('newEventDate');
    var timeInput = document.getElementById('newEventTime');
    var date = dateInput && dateInput.value ? dateInput.value : null;
    var time = timeInput && timeInput.value ? timeInput.value : null;
    
    if (!subject || subject.trim() === '') {
        showToast('Введите название события', 'error');
        return;
    }
    
    var important = document.getElementById('newEventImportant').checked;
    var body = { subject: subject, description: description, important: important };
    if (date) body.date = date;
    if (time) body.time = time;
    
    fetch(apiUrls.addEvent, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    })
    .then(function(response) { return response.json(); })
    .then(function(data) {
        if (data.success) {
            showToast('Событие добавлено', 'success');
            var modal = bootstrap.Modal.getInstance(document.getElementById('addEventModal'));
            if (modal) modal.hide();
            loadEvents();
        } else {
            showToast('Ошибка: ' + data.message, 'error');
        }
    })
    .catch(function(err) {
        showToast('Ошибка: ' + err, 'error');
    });
}

// ==================== Edit Event Modal ====================

function showEditEventDialog(key) {
    var event = eventsData.find(function(e) { return e.key === key; });
    if (!event) return;
    
    // Clean up any existing backdrops first
    var existingBackdrop = document.querySelector('.modal-backdrop');
    if (existingBackdrop) {
        existingBackdrop.remove();
        document.body.classList.remove('modal-open');
        document.body.style.overflow = '';
    }
    
    document.getElementById('editEventKey').value = key;
    document.getElementById('editEventDate').value = event.date || '';
    document.getElementById('editEventTime').value = event.time || '';
    document.getElementById('editEventSubject').value = event.subject || '';
    document.getElementById('editEventDescription').value = event.description || '';
    document.getElementById('editEventImportant').checked = event.important || false;
    
    var modal = new bootstrap.Modal(document.getElementById('editEventModal'));
    modal.show();
}

function saveEditedEvent() {
    var key = document.getElementById('editEventKey').value;
    var subject = document.getElementById('editEventSubject').value;
    var description = document.getElementById('editEventDescription').value;
    var dateInput = document.getElementById('editEventDate');
    var timeInput = document.getElementById('editEventTime');
    var date = dateInput && dateInput.value ? dateInput.value : null;
    var time = timeInput && timeInput.value ? timeInput.value : null;
    
    if (!subject || subject.trim() === '') {
        showToast('Введите название события', 'error');
        return;
    }
    
    var important = document.getElementById('editEventImportant').checked;
    var body = { key: key, subject: subject, description: description, important: important };
    if (date) body.date = date;
    if (time) body.time = time;
    
    fetch(apiUrls.updateEvent, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    })
    .then(function(response) { return response.json(); })
    .then(function(data) {
        if (data.success) {
            showToast('Сохранено', 'success');
            var modal = bootstrap.Modal.getInstance(document.getElementById('editEventModal'));
            if (modal) modal.hide();
            // Clean up backdrop
            setTimeout(function() {
                var backdrop = document.querySelector('.modal-backdrop');
                if (backdrop) backdrop.remove();
                document.body.classList.remove('modal-open');
                document.body.style.overflow = '';
            }, 100);
            loadEvents();
        } else {
            showToast('Ошибка: ' + data.message, 'error');
        }
    })
    .catch(function(err) {
        showToast('Ошибка: ' + err, 'error');
    });
}

function deleteEventFromEditModal() {
    var key = document.getElementById('editEventKey').value;
    if (!key) return;
    
    if (!confirm('Удалить событие?')) return;
    
    fetch(apiUrls.deleteEvent, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ key: key })
    })
    .then(function(response) { return response.json(); })
    .then(function(data) {
        if (data.success) {
            showToast('Событие удалено', 'success');
            var modal = bootstrap.Modal.getInstance(document.getElementById('editEventModal'));
            if (modal) modal.hide();
            // Clean up backdrop
            setTimeout(function() {
                var backdrop = document.querySelector('.modal-backdrop');
                if (backdrop) backdrop.remove();
                document.body.classList.remove('modal-open');
                document.body.style.overflow = '';
            }, 100);
            loadEvents();
        } else {
            showToast('Ошибка: ' + data.message, 'error');
        }
    })
    .catch(function(err) {
        showToast('Ошибка: ' + err, 'error');
    });
}