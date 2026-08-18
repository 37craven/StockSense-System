window.stockSenseApi = {
    jsonTransfers: new Map(),
    nextJsonTransferId: 1,
    get: async function(url) {
        const response = await fetch(url, {
            credentials: 'same-origin',
            headers: { 'Accept': 'application/json' }
        });

        return {
            ok: response.ok,
            status: response.status,
            body: await response.text()
        };
    },
    getJson: async function(url) {
        const response = await fetch(url, {
            credentials: 'same-origin',
            headers: { 'Accept': 'application/json' }
        });

        if (!response.ok) {
            throw new Error('Request failed');
        }

        return await response.json();
    },
    beginJsonTransfer: async function(url) {
        const response = await fetch(url, {
            credentials: 'same-origin',
            headers: { 'Accept': 'application/json' }
        });

        if (!response.ok) {
            throw new Error('Request failed');
        }

        const body = await response.text();
        const id = String(window.stockSenseApi.nextJsonTransferId++);
        window.stockSenseApi.jsonTransfers.set(id, body);
        return { id: id, length: body.length };
    },
    readJsonTransfer: function(id, offset, count) {
        const body = window.stockSenseApi.jsonTransfers.get(id);
        if (body === undefined) {
            throw new Error('JSON transfer expired');
        }

        return body.substring(offset, offset + count);
    },
    releaseJsonTransfer: function(id) {
        window.stockSenseApi.jsonTransfers.delete(id);
    },
    postJson: async function(url, value) {
        const response = await fetch(url, {
            method: 'POST',
            credentials: 'same-origin',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(value)
        });

        return {
            ok: response.ok,
            status: response.status,
            body: await response.text()
        };
    },
    put: async function(url) {
        const response = await fetch(url, {
            method: 'PUT',
            credentials: 'same-origin',
            headers: { 'Accept': 'application/json' }
        });

        return {
            ok: response.ok,
            status: response.status,
            body: await response.text()
        };
    },
    observeLoadMore: function(element, dotNetRef) {
        if (!element || element.__ssLoadMoreObserver) return;
        const observer = new IntersectionObserver(function(entries) {
            entries.forEach(function(entry) {
                if (entry.isIntersecting) {
                    dotNetRef.invokeMethodAsync('LoadMore');
                }
            });
        }, { rootMargin: '300px' });
        observer.observe(element);
        element.__ssLoadMoreObserver = observer;
    }
};