// QuizMaster: confirm dialogs and the quiz countdown. Plain JavaScript, no libraries.
(function () {
	'use strict';

	// A form with data-confirm="Question?" asks before it is submitted.
	document.addEventListener('submit', function (event) {
		var form = event.target;
		var message = form && form.getAttribute ? form.getAttribute('data-confirm') : null;
		if (message && !window.confirm(message)) {
			event.preventDefault();
		}
	});

	// A form with data-busy-text is slow (for example letting the AI write a quiz). While it is being sent, its
	// buttons are disabled, so it can't be sent twice, and the text is shown in its .busy-text element.
	document.addEventListener('submit', function (event) {
		var form = event.target;
		var busyText = form && form.getAttribute ? form.getAttribute('data-busy-text') : null;
		if (!busyText || event.defaultPrevented) {
			return;
		}
		form.querySelectorAll('button[type="submit"]').forEach(function (button) {
			button.disabled = true;
		});
		var status = form.querySelector('.busy-text');
		if (status) {
			status.textContent = busyText;
			status.classList.remove('d-none');
		}
	});

	// Back to a page kept in the browser's cache: the busy forms work again.
	window.addEventListener('pageshow', function (event) {
		if (!event.persisted) {
			return;
		}
		document.querySelectorAll('form[data-busy-text]').forEach(function (form) {
			form.querySelectorAll('button[type="submit"]').forEach(function (button) {
				button.disabled = false;
			});
			var status = form.querySelector('.busy-text');
			if (status) {
				status.classList.add('d-none');
			}
		});
	});

	// The countdown on the Take page. The server sends the number of seconds left, because the services compare
	// times with the server's clock; the browser never parses the deadline (its clock or time zone may differ).
	// The countdown is only a display: the server enforces the limit. At zero it submits the Finish form.
	var timer = document.getElementById('countdown');
	if (!timer) {
		return;
	}
	var secondsLeft = parseInt(timer.getAttribute('data-seconds-left'), 10);
	var finishForm = document.getElementById(timer.getAttribute('data-finish-form'));
	if (isNaN(secondsLeft) || !finishForm) {
		return;
	}

	// performance.now() only measures time passed on this page, so changing the computer's clock doesn't matter.
	var loadedAt = performance.now();
	var submitted = false;

	function pad(value) {
		return (value < 10 ? '0' : '') + value;
	}

	// Same format as the server's FormatDuration: mm:ss, or h:mm:ss from one hour.
	function format(totalSeconds) {
		var hours = Math.floor(totalSeconds / 3600);
		var minutes = Math.floor((totalSeconds % 3600) / 60);
		var seconds = totalSeconds % 60;
		return hours > 0
			? hours + ':' + pad(minutes) + ':' + pad(seconds)
			: pad(minutes) + ':' + pad(seconds);
	}

	function tick() {
		var passed = Math.floor((performance.now() - loadedAt) / 1000);
		var left = Math.max(0, secondsLeft - passed);
		timer.textContent = format(left);
		if (left <= 60) {
			timer.classList.add('text-danger');
		}
		if (left === 0 && !submitted) {
			submitted = true;
			window.clearInterval(handle);
			finishForm.submit();
		}
	}

	var handle = window.setInterval(tick, 250);
	tick();
})();
