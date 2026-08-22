window.stockSenseApi = {
    jsonTransfers: new Map(),
    nextJsonTransferId: 1,
    getXsrfToken: function() {
        try {
            const cookies = document.cookie ? document.cookie.split(';') : [];
            for (let c of cookies) {
                const trimmed = c.trim();
                const eq = trimmed.indexOf('=');
                if (eq < 0) continue;
                const k = trimmed.substring(0, eq);
                const v = trimmed.substring(eq + 1);
                if (k === 'XSRF-TOKEN' || k.startsWith('.AspNetCore.Antiforgery') || k === '__RequestVerificationToken') {
                    return decodeURIComponent(v);
                }
            }
            const el = document.querySelector('input[name="__RequestVerificationToken"]');
            if (el && el.value) return el.value;
        } catch {}
        return null;
    },
    getXsrfHeaders: function(extra) {
        const h = extra ? { ...extra } : {};
        const tok = window.stockSenseApi.getXsrfToken();
        if (tok) h['X-XSRF-TOKEN'] = tok;
        return h;
    },
    get: async function(url) {
        const response = await fetch(url, {
            credentials: 'same-origin',
            headers: window.stockSenseApi.getXsrfHeaders({ 'Accept': 'application/json' })
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
            headers: window.stockSenseApi.getXsrfHeaders({ 'Accept': 'application/json' })
        });

        if (!response.ok) {
            throw new Error('Request failed');
        }

        return await response.json();
    },
    beginJsonTransfer: async function(url) {
        const response = await fetch(url, {
            credentials: 'same-origin',
            headers: window.stockSenseApi.getXsrfHeaders({ 'Accept': 'application/json' })
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
            headers: window.stockSenseApi.getXsrfHeaders({
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            }),
            body: JSON.stringify(value)
        });

        return {
            ok: response.ok,
            status: response.status,
            body: await response.text()
        };
    },
    postJsonBase64: async function(url, payloadBase64) {
        const response = await fetch(url, {
            method: 'POST',
            credentials: 'same-origin',
            headers: window.stockSenseApi.getXsrfHeaders({
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            }),
            body: atob(payloadBase64)
        });

        return JSON.stringify({
            ok: response.ok,
            status: response.status,
            body: await response.text()
        });
    },
    put: async function(url) {
        const response = await fetch(url, {
            method: 'PUT',
            credentials: 'same-origin',
            headers: window.stockSenseApi.getXsrfHeaders({ 'Accept': 'application/json' })
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