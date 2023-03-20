export function debounceEvent(htmlElement, eventName, delay) {
    registerEvent(htmlElement, eventName, delay, debounce);
}

export function throttleEvent(htmlElement, eventName, delay) {
    registerEvent(htmlElement, eventName, delay, throttle);
}

function registerEvent(htmlElement, eventName, delay, filterFunction) {
    let raisingEvent = false;
    let eventHandler = filterFunction(function (e) {
        raisingEvent = true;
        try {
            htmlElement.dispatchEvent(e);
        } finally {
            raisingEvent = false;
        }
    }, delay);

    htmlElement.addEventListener(eventName, e => {
        if (!raisingEvent) {
            e.stopImmediatePropagation();
            eventHandler(e);
        }
    });
}

function debounce(func, wait) {
    let timer;
    return (...args) => {
        clearTimeout(timer);
        timer = setTimeout(() => { func.apply(this, args); }, wait);
    };
}

function throttle(func, wait) {
    var context, args, result;
    var timeout = null;
    var previous = 0;
    var later = function () {
        previous = Date.now();
        timeout = null;
        result = func.apply(context, args);
        if (!timeout) context = args = null;
    };
    return function () {
        var now = Date.now();
        if (!previous) previous = now;
        var remaining = wait - (now - previous);
        context = this;
        args = arguments;

        if (!timeout) {
            timeout = setTimeout(later, remaining);
        }
        return result;
    };
};