window.triggerDownload = async (url, fileName) => {
    try {
        const response = await fetch(url);
        if (!response.ok) {
            console.error('Download failed:', response.status, response.statusText);
            return;
        }
        const blob = await response.blob();
        const blobUrl = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = blobUrl;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        setTimeout(() => {
            document.body.removeChild(link);
            URL.revokeObjectURL(blobUrl);
        }, 5000);
    } catch (e) {
        console.error('Download error:', e);
    }
};

window.pinnedSlipsStorage = {
    get: function(key) {
        return localStorage.getItem(key) || '';
    },
    set: function(key, value) {
        localStorage.setItem(key, value);
    },
    remove: function(key) {
        localStorage.removeItem(key);
    }
};