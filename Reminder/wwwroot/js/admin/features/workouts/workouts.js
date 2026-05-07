// ==================== Running Workouts ====================

// Store parsed workouts for saving
var parsedWorkouts = [];
var parsedIntro = '';

// API URLs - from window.apiUrls
var workoutsApiUrls = window.apiUrls.workouts;

function addRunningWorkout() {
    var input = document.getElementById('runningWorkoutInput');
    var workoutText = input.value.trim();
    
    if (!workoutText) {
        showToast('Введите задание', 'error', input);
        return;
    }
    
    // Call backend API to parse workouts
    fetch(workoutsApiUrls.parseWorkouts, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ text: workoutText })
    })
    .then(function(response) { return response.json(); })
    .then(function(data) {
        if (data.success) {
            // Convert API response to format expected by showWorkoutPreview
            var workouts = data.workouts.map(function(w) {
                return {
                    dayName: w.dayName,
                    dayNum: w.dayNum,
                    date: w.date,
                    dateFormatted: w.dateFormatted,
                    description: w.description
                };
            });
            // Store intro from API response
            parsedIntro = data.intro || '';
            showWorkoutPreview(workouts);
        } else {
            showToast(data.message || 'Не найдены дни недели в тексте', 'error', input);
        }
    })
    .catch(function(err) {
        showToast('Ошибка: ' + err, 'error', input);
    });
}

function showWorkoutPreview(workouts) {
    parsedWorkouts = workouts;
    
    var content = document.getElementById('workoutPreviewContent');
    
    var html = '';
    
    // Add intro section if present
    if (parsedIntro) {
        html += '<div class="mb-3 p-2 rounded" style="background-color: #e7f1ff; border-left: 3px solid #0d6efd;">';
        html += '<label class="small text-muted mb-1 d-block">';
        html += '<input class="form-check-input me-1" type="checkbox" checked id="workout_intro_check">';
        html += '<span class="fw-bold">Вступление</span>';
        html += '</label>';
        html += '<textarea class="form-control py-1" rows="3" style="font-size: 0.85rem;" id="workoutIntroDesc">' + parsedIntro.replace(/</g, '<').replace(/>/g, '>') + '</textarea>';
        html += '</div>';
    }
    
    // Add textareas with checkbox inline in label
    workouts.forEach(function(w, index) {
        html += '<div class="mb-2">';
        html += '<label class="small text-muted mb-1 d-block">';
        html += '<input class="form-check-input me-1" type="checkbox" checked data-index="' + index + '" id="workout_' + index + '">';
        html += '<span class="fw-bold">' + w.dayName + ' ' + w.dateFormatted + '</span>';
        html += '</label>';
        html += '<textarea class="form-control py-1" rows="4" style="font-size: 0.85rem;" data-index="' + index + '" id="workoutDesc_' + index + '">' + w.description + '</textarea>';
        html += '</div>';
    });
    
    content.innerHTML = html;
    
    var modal = new bootstrap.Modal(document.getElementById('workoutPreviewModal'));
    modal.show();
}

function saveWorkoutPreview() {
    // Collect selected workouts
    var selectedWorkouts = [];
    
    // Check if intro is selected
    var introCheckbox = document.getElementById('workout_intro_check');
    var introText = '';
    if (introCheckbox && introCheckbox.checked) {
        var introInput = document.getElementById('workoutIntroDesc');
        if (introInput) {
            introText = introInput.value;
        }
    }
    
    parsedWorkouts.forEach(function(w, index) {
        var checkbox = document.getElementById('workout_' + index);
        var descInput = document.getElementById('workoutDesc_' + index);
        
        if (checkbox && checkbox.checked) {
            selectedWorkouts.push({
                date: w.date,
                subject: w.dayName,
                description: descInput ? descInput.value : w.description
            });
        }
    });
    
    if (selectedWorkouts.length === 0 && !introText) {
        showToast('Выберите хотя бы один день или вступление', 'error');
        return;
    }
    
    // Show toast - next step will be saving to API
    var message = 'Найдено ' + selectedWorkouts.length + ' тренировок';
    if (introText) {
        message += ' (с вступлением)';
    }
    message += '. Сохранение...';
    showToast(message, 'success');
    
    // Close modal
    var modal = bootstrap.Modal.getInstance(document.getElementById('workoutPreviewModal'));
    modal.hide();
    
    // Save workouts to API (include intro if selected)
    saveWorkoutsToApi(selectedWorkouts, introText);
    
    // Clear input
    document.getElementById('runningWorkoutInput').value = '';
}

function saveWorkoutsToApi(workouts, introText) {
    // Build list of events to save
    var events = workouts.map(function(w) {
        return {
            date: w.date,
            subject: w.subject,
            description: w.description
        };
    });
    
    // Add intro as event on Sunday if provided
    if (introText) {
        var sundayDate = null;
        if (workouts.length > 0) {
            // Find the latest date and get that week's Sunday
            var latestDate = new Date(Math.max.apply(null, workouts.map(function(w) { return new Date(w.date); })));
            var dayOfWeek = latestDate.getDay();
            var daysToSunday = dayOfWeek === 0 ? 0 : 7 - dayOfWeek;
            sundayDate = new Date(latestDate);
            sundayDate.setDate(sundayDate.getDate() + daysToSunday);
        } else {
            // Use current week's Sunday
            var today = new Date();
            var dayOfWeek = today.getDay();
            var daysToSunday = dayOfWeek === 0 ? 0 : 7 - dayOfWeek;
            sundayDate = new Date(today);
            sundayDate.setDate(sundayDate.getDate() + daysToSunday);
        }
        
        var sundayStr = sundayDate.toISOString().split('T')[0];
        
        events.push({
            date: sundayStr,
            subject: 'Вступление',
            description: introText
        });
    }
    
    if (events.length === 0) {
        showToast('Нечего сохранять', 'error');
        return;
    }
    
    // Single request to save all events
    fetch(workoutsApiUrls.addWorkouts, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ workouts: events })
    })
    .then(function(response) { return response.json(); })
    .then(function(data) {
        if (data.success) {
            var msg = 'Сохранено ' + data.count + ' событий';
            if (introText) {
                msg += ' (включая вступление)';
            }
            showToast(msg, 'success');
        } else {
            showToast('Ошибка: ' + data.message, 'error');
        }
    })
    .catch(function(err) {
        showToast('Ошибка: ' + err, 'error');
    });
}

function clearRunningWorkout() {
    document.getElementById('runningWorkoutInput').value = '';
}