(function () {
    const dbName = "pingme_cache";
    const storeName = "messages";
    const ttlMs = 5 * 60 * 1000;
    const maxMessages = 50;

    function openDb() {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open(dbName, 1);

            request.onupgradeneeded = () => {
                const db = request.result;
                if (!db.objectStoreNames.contains(storeName)) {
                    db.createObjectStore(storeName, { keyPath: "key" });
                }
            };

            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    }

    async function withStore(mode, action) {
        const db = await openDb();

        try {
            return await new Promise((resolve, reject) => {
                const tx = db.transaction(storeName, mode);
                const store = tx.objectStore(storeName);
                const result = action(store);

                tx.oncomplete = () => resolve(result);
                tx.onerror = () => reject(tx.error);
                tx.onabort = () => reject(tx.error);
            });
        } finally {
            db.close();
        }
    }

    window.pingmeCache = {
        getMessages: async function (conversationKey) {
            if (!conversationKey) {
                return null;
            }

            const entry = await withStore("readonly", (store) => {
                return new Promise((resolve, reject) => {
                    const request = store.get(conversationKey);
                    request.onsuccess = () => resolve(request.result || null);
                    request.onerror = () => reject(request.error);
                });
            });

            if (!entry || !entry.cachedAt || Date.now() - entry.cachedAt > ttlMs) {
                await window.pingmeCache.clearMessages(conversationKey);
                return null;
            }

            return entry.messages || [];
        },

        setMessages: async function (conversationKey, messages) {
            if (!conversationKey) {
                return;
            }

            const trimmed = (messages || [])
                .slice()
                .sort((a, b) => (b.id || 0) - (a.id || 0))
                .slice(0, maxMessages)
                .sort((a, b) => (a.id || 0) - (b.id || 0));

            await withStore("readwrite", (store) => {
                store.put({
                    key: conversationKey,
                    messages: trimmed,
                    cachedAt: Date.now()
                });
            });
        },

        clearMessages: async function (conversationKey) {
            if (!conversationKey) {
                return;
            }

            await withStore("readwrite", (store) => {
                store.delete(conversationKey);
            });
        }
    };
})();
