// wwwroot/js/chat.js

window.scrollToBottom = function(element) {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};

window.getLocalStorage = function(key) {
    return localStorage.getItem(key);
};

window.setLocalStorage = function(key, value) {
    localStorage.setItem(key, value);
};

window.removeLocalStorage = function(key) {
    localStorage.removeItem(key);
};

window.scrollToElement = function(elementId) {
    var element = document.getElementById(elementId);
    if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'end' });
    }
};

window.focusElement = function(elementId) {
    var element = document.getElementById(elementId);
    if (element) {
        element.focus();
    }
};