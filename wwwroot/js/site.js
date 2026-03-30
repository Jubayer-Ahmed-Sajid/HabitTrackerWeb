document.addEventListener("DOMContentLoaded", () => {
	setupMobileNav();
	setupThemeToggle();
	setupNotificationCenter();
	setupHabitCompletion();
	setupHabitLibraryFilter();
});

function setupMobileNav() {
	const toggle = document.getElementById("mobileNavToggle");
	const menu = document.getElementById("mobileNavMenu");
	const icon = document.getElementById("mobileNavIcon");

	if (!toggle || !menu) {
		return;
	}

	const closeMenu = () => {
		menu.classList.add("hidden");
		toggle.setAttribute("aria-expanded", "false");
		if (icon) {
			icon.classList.remove("is-open");
		}
		document.dispatchEvent(new CustomEvent("habit-mobile-nav-closed"));
	};

	const toggleMenu = () => {
		const isOpen = !menu.classList.contains("hidden");
		if (isOpen) {
			closeMenu();
			return;
		}

		menu.classList.remove("hidden");
		toggle.setAttribute("aria-expanded", "true");
		if (icon) {
			icon.classList.add("is-open");
		}
		document.dispatchEvent(new CustomEvent("habit-mobile-nav-opened"));
	};

	toggle.addEventListener("click", (event) => {
		event.stopPropagation();
		toggleMenu();
	});

	menu.querySelectorAll("a").forEach((link) => {
		link.addEventListener("click", () => closeMenu());
	});

	document.addEventListener("click", (event) => {
		if (!menu.classList.contains("hidden") && !menu.contains(event.target) && !toggle.contains(event.target)) {
			closeMenu();
		}
	});

	window.addEventListener("resize", () => {
		if (window.innerWidth >= 768) {
			closeMenu();
		}
	});

	document.addEventListener("habit-notification-panel-opened", () => {
		closeMenu();
	});
}

function setupThemeToggle() {
	const toggle = document.getElementById("themeToggle");
	const icon = document.getElementById("themeToggleIcon");
	if (!toggle) {
		return;
	}

	const refreshIcon = () => {
		const isDark = document.documentElement.classList.contains("dark");
		if (icon) {
			icon.src = isDark ? "/icons/sun-2-svgrepo-com.svg" : "/icons/moon-svgrepo-com.svg";
			icon.alt = isDark ? "Light mode" : "Dark mode";
		}
		toggle.setAttribute("aria-label", isDark ? "Switch to light mode" : "Switch to dark mode");
		toggle.title = isDark ? "Light mode" : "Dark mode";
	};

	refreshIcon();

	toggle.addEventListener("click", () => {
		document.documentElement.classList.toggle("dark");
		const isDark = document.documentElement.classList.contains("dark");
		localStorage.setItem("habit-theme", isDark ? "dark" : "light");
		refreshIcon();
	});
}

function setupNotificationCenter() {
	const host = document.getElementById("navNotifications");
	if (!host) {
		return;
	}

	const toggle = document.getElementById("notificationToggle");
	const panel = document.getElementById("notificationPanel");
	const list = document.getElementById("notificationList");
	const empty = document.getElementById("notificationEmpty");
	const badge = document.getElementById("notificationBadge");
	const pushToggle = document.getElementById("pushPreferenceToggle");
	const pushState = document.getElementById("pushPreferenceState");

	if (!toggle || !panel || !list || !empty || !badge || !pushToggle || !pushState) {
		return;
	}

	let isPanelOpen = false;
	let isInitialLoad = true;
	let latestItems = [];
	let knownKeys = loadSetFromSession("habit-known-notification-keys");
	let unreadKeys = loadSetFromSession("habit-unread-notification-keys");
	const pushControl = setupWebPushSubscription();
	let isToggleUpdating = false;
	let pushSupported = true;

	const syncPushToggleState = (enabled, text) => {
		pushToggle.checked = !!enabled;
		pushState.textContent = text || (enabled ? "Enabled" : "Disabled");
	};

	const setPushToggleBusy = (isBusy) => {
		isToggleUpdating = isBusy;
		pushToggle.disabled = isBusy || !pushSupported;
	};

	const updateBadge = () => {
		const unread = unreadKeys.size;
		if (unread <= 0) {
			badge.classList.add("hidden");
			badge.textContent = "";
			return;
		}

		badge.classList.remove("hidden");
		badge.textContent = unread > 99 ? "99+" : String(unread);
	};

	const renderItems = (items) => {
		list.innerHTML = "";

		if (!items.length) {
			empty.classList.remove("hidden");
			return;
		}

		empty.classList.add("hidden");

		items.forEach((item) => {
			const type = normalizeNotificationType(item.type);
			const link = document.createElement("a");
			link.className = "notification-item";
			link.href = item.actionUrl || "/Dashboard";
			link.innerHTML = `
				<span class="notification-icon ${type}">
					<i class="${getNotificationIconClass(type)}"></i>
				</span>
				<span class="notification-copy">
					<span class="notification-title">${escapeHtml(item.title || "Update")}</span>
					<span class="notification-message">${escapeHtml(item.message || "")}</span>
				</span>`;
			list.appendChild(link);
		});
	};

	const markAllRead = () => {
		latestItems.forEach((item) => {
			if (item?.key) {
				unreadKeys.delete(item.key);
			}
		});

		saveSetToSession("habit-unread-notification-keys", unreadKeys);
		updateBadge();
	};

	const openPanel = () => {
		document.dispatchEvent(new CustomEvent("habit-notification-panel-opened"));
		isPanelOpen = true;
		panel.classList.remove("hidden");
		toggle.setAttribute("aria-expanded", "true");
		markAllRead();
	};

	const closePanel = () => {
		if (!isPanelOpen) {
			return;
		}

		isPanelOpen = false;
		panel.classList.add("hidden");
		toggle.setAttribute("aria-expanded", "false");
	};

	const pollNotifications = async () => {
		try {
			const response = await fetch("/Notifications/Poll", {
				headers: {
					Accept: "application/json",
					"X-Requested-With": "XMLHttpRequest"
				}
			});

			if (!response.ok) {
				return;
			}

			const payload = await response.json();
			const items = Array.isArray(payload.items) ? payload.items : [];
			const newItems = items.filter((item) => item?.key && !knownKeys.has(item.key));

			latestItems = items;

			newItems.forEach((item) => {
				if (!item?.key) {
					return;
				}

				knownKeys.add(item.key);
				unreadKeys.add(item.key);
			});

			items.forEach((item) => {
				if (!item?.key) {
					return;
				}

				knownKeys.add(item.key);
			});

			knownKeys = trimSet(knownKeys, 120);
			unreadKeys = trimSet(unreadKeys, 120);

			saveSetToSession("habit-known-notification-keys", knownKeys);
			saveSetToSession("habit-unread-notification-keys", unreadKeys);

			renderItems(items);

			if (isPanelOpen) {
				markAllRead();
			} else {
				updateBadge();
			}

			if (!isInitialLoad && newItems.length > 0) {
				newItems.forEach((item) => {
					showNotificationToast(item);
					tryShowBrowserNotification(item);
				});
			}

			isInitialLoad = false;
		} catch {
			// Ignore transient polling issues; next cycle will retry.
		}
	};

	toggle.addEventListener("click", (event) => {
		event.stopPropagation();
		void pushControl.ensureWhenPanelOpens();
		if (isPanelOpen) {
			closePanel();
		} else {
			openPanel();
		}
	});

	pushToggle.addEventListener("change", async () => {
		if (isToggleUpdating) {
			return;
		}

		const desiredState = pushToggle.checked;
		setPushToggleBusy(true);

		try {
			const result = desiredState
				? await pushControl.enable()
				: await pushControl.disable();

			pushSupported = result.supported !== false;
			syncPushToggleState(result.enabled, result.message);

			if (!result.enabled && desiredState) {
				showToast(result.message || "Push notifications are unavailable.", "warning", "Push alerts");
			}
		} catch {
			syncPushToggleState(!desiredState, "Could not update right now");
			showToast("Could not update push preference.", "error", "Push alerts");
		} finally {
			setPushToggleBusy(false);
		}
	});

	document.addEventListener("click", (event) => {
		if (!host.contains(event.target)) {
			closePanel();
		}
	});

	document.addEventListener("keydown", (event) => {
		if (event.key === "Escape") {
			closePanel();
		}
	});

	document.addEventListener("habit-mobile-nav-opened", () => {
		closePanel();
	});

	pollNotifications();
	window.setInterval(pollNotifications, 25000);

	setPushToggleBusy(true);
	void pushControl.initialize()
		.then((state) => {
			pushSupported = state.supported !== false;
			syncPushToggleState(state.enabled, state.message);
		})
		.catch(() => {
			pushSupported = false;
			syncPushToggleState(false, "Unavailable");
		})
		.finally(() => {
			setPushToggleBusy(false);
		});
}

function setupWebPushSubscription() {
	if (!("serviceWorker" in navigator) || !("PushManager" in window) || !("Notification" in window)) {
		return {
			supported: false,
			initialize: async () => ({ supported: false, enabled: false, message: "Not supported in this browser" }),
			enable: async () => ({ supported: false, enabled: false, message: "Browser does not support push" }),
			disable: async () => ({ supported: false, enabled: false, message: "Browser does not support push" }),
			ensureWhenPanelOpens: async () => {}
		};
	}

	let registrationPromise;
	let subscriptionSyncPromise;

	const getRegistration = () => {
		registrationPromise ??= navigator.serviceWorker.register("/sw.js");
		return registrationPromise;
	};

	const fetchPreference = async () => {
		const response = await fetch("/Push/Preference", {
			headers: {
				Accept: "application/json",
				"X-Requested-With": "XMLHttpRequest"
			},
			credentials: "same-origin"
		});

		if (!response.ok) {
			return { available: false, enabled: false };
		}

		const payload = await response.json();
		return {
			available: payload.available !== false,
			enabled: payload.enabled !== false
		};
	};

	const setPreference = async (enabled) => {
		const response = await fetch("/Push/Preference", {
			method: "POST",
			headers: {
				"Content-Type": "application/json",
				Accept: "application/json",
				"X-Requested-With": "XMLHttpRequest"
			},
			credentials: "same-origin",
			body: JSON.stringify({ enabled: !!enabled })
		});

		return response.ok;
	};

	const unsubscribeCurrent = async () => {
		const registration = await getRegistration();
		const subscription = await registration.pushManager.getSubscription();
		if (!subscription) {
			return;
		}

		const serialized = subscription.toJSON();
		if (serialized?.endpoint) {
			await fetch("/Push/Unsubscribe", {
				method: "POST",
				headers: {
					"Content-Type": "application/json",
					Accept: "application/json",
					"X-Requested-With": "XMLHttpRequest"
				},
				credentials: "same-origin",
				body: JSON.stringify({ endpoint: serialized.endpoint })
			});
		}

		await subscription.unsubscribe();
	};

	const syncSubscription = async (promptForPermission) => {
		if (Notification.permission === "denied") {
			return false;
		}

		const registration = await getRegistration();
		let subscription = await registration.pushManager.getSubscription();

		if (!subscription) {
			if (Notification.permission === "default" && promptForPermission) {
				const permission = await Notification.requestPermission();
				if (permission !== "granted") {
					return false;
				}
			}

			if (Notification.permission !== "granted") {
				return false;
			}

			const vapidPublicKey = await fetchPushPublicKey();
			if (!vapidPublicKey) {
				return false;
			}

			subscription = await registration.pushManager.subscribe({
				userVisibleOnly: true,
				applicationServerKey: urlBase64ToUint8Array(vapidPublicKey)
			});
		}

		const serialized = subscription.toJSON();
		const keys = serialized.keys || {};

		if (!serialized.endpoint || !keys.p256dh || !keys.auth) {
			return false;
		}

		const response = await fetch("/Push/Subscribe", {
			method: "POST",
			headers: {
				"Content-Type": "application/json",
				Accept: "application/json",
				"X-Requested-With": "XMLHttpRequest"
			},
			credentials: "same-origin",
			body: JSON.stringify({
				endpoint: serialized.endpoint,
				p256Dh: keys.p256dh,
				auth: keys.auth
			})
		});

		return response.ok;
	};

	const ensureSync = async (promptForPermission) => {
		subscriptionSyncPromise ??= syncSubscription(promptForPermission)
			.catch(() => {
				// Errors here should not break the core notification center UX.
				return false;
			})
			.finally(() => {
				subscriptionSyncPromise = undefined;
			});

		return subscriptionSyncPromise;
	};

	return {
		supported: true,
		initialize: async () => {
			const preference = await fetchPreference();
			if (!preference.available) {
				return { supported: false, enabled: false, message: "Unavailable in this environment" };
			}

			if (!preference.enabled) {
				await unsubscribeCurrent();
				return { supported: true, enabled: false, message: "Disabled" };
			}

			const subscribed = await ensureSync(false);
			if (!subscribed) {
				return { supported: true, enabled: false, message: "Enable in browser permissions" };
			}

			return { supported: true, enabled: true, message: "Enabled" };
		},
		enable: async () => {
			if (!await setPreference(true)) {
				return { supported: true, enabled: false, message: "Could not save preference" };
			}

			const subscribed = await ensureSync(true);
			if (!subscribed) {
				await setPreference(false);
				return { supported: true, enabled: false, message: "Permission required to enable" };
			}

			return { supported: true, enabled: true, message: "Enabled" };
		},
		disable: async () => {
			await setPreference(false);
			await unsubscribeCurrent();
			return { supported: true, enabled: false, message: "Disabled" };
		},
		ensureWhenPanelOpens: async () => {
			const preference = await fetchPreference();
			if (!preference.available || !preference.enabled) {
				return;
			}

			await ensureSync(false);
		}
	};
}

async function fetchPushPublicKey() {
	try {
		const response = await fetch("/Push/PublicKey", {
			headers: {
				Accept: "text/plain",
				"X-Requested-With": "XMLHttpRequest"
			},
			credentials: "same-origin"
		});

		if (!response.ok) {
			return null;
		}

		const key = (await response.text()).trim();
		return key.length > 0 ? key : null;
	} catch {
		return null;
	}
}

function urlBase64ToUint8Array(value) {
	const padding = "=".repeat((4 - (value.length % 4)) % 4);
	const base64 = (value + padding).replaceAll("-", "+").replaceAll("_", "/");
	const raw = window.atob(base64);
	const bytes = new Uint8Array(raw.length);

	for (let i = 0; i < raw.length; i += 1) {
		bytes[i] = raw.charCodeAt(i);
	}

	return bytes;
}

function loadSetFromSession(key) {
	try {
		const raw = sessionStorage.getItem(key);
		if (!raw) {
			return new Set();
		}

		const parsed = JSON.parse(raw);
		if (!Array.isArray(parsed)) {
			return new Set();
		}

		return new Set(parsed.filter((item) => typeof item === "string"));
	} catch {
		return new Set();
	}
}

function saveSetToSession(key, values) {
	try {
		sessionStorage.setItem(key, JSON.stringify(Array.from(values)));
	} catch {
		// Storage can fail in privacy modes; safe to ignore.
	}
}

function trimSet(values, maxCount) {
	const list = Array.from(values);
	if (list.length <= maxCount) {
		return values;
	}

	return new Set(list.slice(list.length - maxCount));
}

function normalizeNotificationType(type) {
	if (type === "success" || type === "warning" || type === "info") {
		return type;
	}

	return "info";
}

function getNotificationIconClass(type) {
	if (type === "success") {
		return "fa-solid fa-circle-check";
	}

	if (type === "warning") {
		return "fa-solid fa-triangle-exclamation";
	}

	return "fa-solid fa-bolt";
}

function showNotificationToast(notification) {
	const type = normalizeNotificationType(notification.type);
	showToast(notification.message || "New update", type, notification.title || "Notification");
}

function tryShowBrowserNotification(notification) {
	if (!("Notification" in window)) {
		return;
	}

	if (document.hasFocus()) {
		return;
	}

	if (Notification.permission !== "granted") {
		return;
	}

	const browserNotification = new Notification(notification.title || "Habit Tracker", {
		body: notification.message || "You have a new update."
	});

	window.setTimeout(() => browserNotification.close(), 5000);
}

function escapeHtml(text) {
	const value = String(text || "");
	return value
		.replaceAll("&", "&amp;")
		.replaceAll("<", "&lt;")
		.replaceAll(">", "&gt;")
		.replaceAll('"', "&quot;")
		.replaceAll("'", "&#39;");
}

function setupHabitCompletion() {
	const dashboard = document.querySelector("[data-dashboard='true']");
	if (!dashboard) {
		return;
	}

	const tokenInput = document.querySelector("#habitLogTokenForm input[name='__RequestVerificationToken']");
	const token = tokenInput ? tokenInput.value : "";

	dashboard.querySelectorAll(".habit-toggle").forEach((button) => {
		button.addEventListener("click", async () => {
			if (button.disabled) {
				return;
			}

			const habitId = button.getAttribute("data-habit-id");
			if (!habitId) {
				return;
			}

			try {
				const response = await fetch("/Dashboard/Log", {
					method: "POST",
					headers: {
						"Content-Type": "application/x-www-form-urlencoded;charset=UTF-8",
						"RequestVerificationToken": token
					},
					body: new URLSearchParams({ HabitId: habitId })
				});

				const payload = await response.json();
				if (!response.ok || !payload.success) {
					showToast(payload.message || "Could not log habit.", "error");
					return;
				}

				markHabitCompleted(button, payload.streak || 0);
				showToast(payload.message || "Habit completed.", "success");
				updateSummaryCounters();
			} catch {
				showToast("Unexpected error while logging habit.", "error");
			}
		});
	});
}

function markHabitCompleted(button, streak) {
	const row = button.closest(".habit-row");
	if (!row) {
		return;
	}

	row.classList.add("is-complete", "bounce");
	setTimeout(() => row.classList.remove("bounce"), 400);

	button.disabled = true;
	button.classList.remove("border-slate-300", "bg-white", "text-slate-500");
	button.classList.add("border-emerald-400", "bg-emerald-500", "text-slate-900");
	button.innerHTML = '<i class="fa-solid fa-check"></i>';

	const streakBadge = row.querySelector("[data-streak-count]");
	if (streakBadge) {
		streakBadge.textContent = `${streak} day streak`;
	}
}

function updateSummaryCounters() {
	const completedElement = document.getElementById("completedHabitsCount");
	const percentElement = document.getElementById("completionPercent");

	if (!completedElement || !percentElement) {
		return;
	}

	const completed = Number.parseInt(completedElement.dataset.completed || "0", 10) || 0;
	const total = Number.parseInt(completedElement.dataset.total || "0", 10) || 0;
	const nextCompleted = Math.min(completed + 1, total);

	completedElement.dataset.completed = String(nextCompleted);
	completedElement.innerHTML = `${nextCompleted}<span class="text-lg text-slate-400">/${total}</span>`;

	const percent = total === 0 ? 0 : ((nextCompleted / total) * 100).toFixed(1);
	percentElement.textContent = `${percent}%`;
}

function showToast(message, type, title) {
	const toast = document.createElement("div");
	toast.className = `toast ${type || "info"}`;

	if (title) {
		const titleElement = document.createElement("p");
		titleElement.className = "toast-title";
		titleElement.textContent = title;

		const bodyElement = document.createElement("p");
		bodyElement.className = "toast-body";
		bodyElement.textContent = message;

		toast.appendChild(titleElement);
		toast.appendChild(bodyElement);
	} else {
		toast.textContent = message;
	}

	document.body.appendChild(toast);

	requestAnimationFrame(() => toast.classList.add("show"));
	setTimeout(() => toast.classList.remove("show"), 2400);
	setTimeout(() => toast.remove(), 2700);
}

function setupHabitLibraryFilter() {
	const library = document.querySelector("[data-habit-library='true']");
	if (!library) {
		return;
	}

	const searchInput = document.getElementById("habitSearch");
	const filterButtons = Array.from(document.querySelectorAll(".library-filter-btn"));
	const rows = Array.from(library.querySelectorAll("[data-habit-row='true']"));
	let activeFilter = "all";

	const runFilter = () => {
		const query = (searchInput?.value || "").trim().toLowerCase();

		rows.forEach((row) => {
			const status = row.getAttribute("data-status") || "active";
			const searchText = row.getAttribute("data-search") || "";

			const matchesStatus = activeFilter === "all" || status === activeFilter;
			const matchesSearch = query.length === 0 || searchText.includes(query);

			row.style.display = matchesStatus && matchesSearch ? "" : "none";
		});
	};

	searchInput?.addEventListener("input", runFilter);

	filterButtons.forEach((button) => {
		button.addEventListener("click", () => {
			activeFilter = button.getAttribute("data-filter") || "all";
			filterButtons.forEach((btn) => btn.classList.remove("is-active"));
			button.classList.add("is-active");
			runFilter();
		});
	});
}
