self.addEventListener("push", (event) => {
  let data = {};

  try {
    data = event.data ? event.data.json() : {};
  } catch {
    data = {
      title: "Habit Tracker",
      body: event.data ? event.data.text() : "You have a new notification."
    };
  }

  const title = data.title || "Habit Tracker";
  const options = {
    body: data.body || "You have a new update.",
    icon: data.icon || "/favicon.ico",
    badge: data.badge || "/favicon.ico",
    tag: data.tag || "habit-update",
    data: {
      url: data.url || "/Dashboard"
    }
  };

  event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener("notificationclick", (event) => {
  event.notification.close();

  const targetUrl = (event.notification.data && event.notification.data.url) || "/Dashboard";

  event.waitUntil((async () => {
    const clientsList = await clients.matchAll({ type: "window", includeUncontrolled: true });

    for (const client of clientsList) {
      if ("focus" in client) {
        client.navigate(targetUrl);
        return client.focus();
      }
    }

    if (clients.openWindow) {
      return clients.openWindow(targetUrl);
    }

    return undefined;
  })());
});
