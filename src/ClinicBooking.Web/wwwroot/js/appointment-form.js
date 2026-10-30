(() => {
    const doctor = document.getElementById('DoctorId');
    const date = document.getElementById('AppointmentDate');
    const time = document.getElementById('AppointmentTime');
    const message = document.getElementById('availability-message');
    const timeError = document.querySelector('[data-valmsg-for="AppointmentTime"]');
    let request;
    let requestNumber = 0;

    function showMessage(text) {
        message.textContent = text;
        message.hidden = !text;
    }

    async function loadTimes() {
        const currentRequest = ++requestNumber;
        request?.abort();
        time.replaceChildren(new Option('Select an available time', ''));
        time.disabled = true;
        time.classList.remove('input-validation-error');
        timeError.textContent = '';
        showMessage('');

        if (!doctor.value || !date.value) {
            return;
        }

        if (date.min && date.value < date.min) {
            showMessage('Appointment date cannot be in the past.');
            return;
        }

        request = new AbortController();
        showMessage('Loading available times...');

        try {
            const query = new URLSearchParams({ doctorId: doctor.value, date: date.value });
            const response = await fetch(`${time.dataset.slotsUrl}?${query}`, {
                signal: request.signal,
                cache: 'no-store'
            });
            const result = await response.json();

            // Ignore responses for a doctor or date that has since changed.
            if (currentRequest !== requestNumber) {
                return;
            }

            if (!response.ok) {
                showMessage(result.message || 'Unable to load available times. Please try again.');
                return;
            }

            result.slots.forEach(slot => time.add(new Option(slot.text, slot.value)));
            time.disabled = result.slots.length === 0;
            showMessage(result.message);
        } catch (error) {
            if (error.name !== 'AbortError' && currentRequest === requestNumber) {
                showMessage('Unable to load available times. Please try again.');
            }
        }
    }

    doctor.addEventListener('change', loadTimes);
    date.addEventListener('change', loadTimes);

    if (doctor.value && date.value && time.options.length === 1) {
        loadTimes();
    }
})();
