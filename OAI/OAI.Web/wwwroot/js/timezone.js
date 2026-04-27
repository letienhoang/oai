window.oaiTimeZone = {
    getTimeZone: function () {
        return Intl.DateTimeFormat().resolvedOptions().timeZone;
    }
};