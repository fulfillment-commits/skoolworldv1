mergeInto(LibraryManager.library, {

    // ------------------------------------------------------------------
    // Internal: ensure window.__UIO exists with all helper methods.
    // Called inline at the top of every exported function.
    // window is the only storage visible across all jslib function calls.
    // ------------------------------------------------------------------

    UIO_Create: function (idPtr, urlPtr, zIndex, opacity, pointerEvents,
                          showCloseBtn, allowAutoplay, allowFullscreen,
                          allowPip, useSandbox, sandboxPtr) {
        try {
            // ---- init state on window ----
            if (!window.__UIO) {
                window.__UIO = {
                    inst: {},
                    rB: false,
                    getCanvas: function () {
                        if (typeof Module !== 'undefined' && Module.canvas) return Module.canvas;
                        var c = document.querySelector('#unity-canvas');
                        return c || document.querySelector('canvas') || null;
                    },
                    applyRect: function (e) {
                        if (!e || !e.wrap) return;
                        var cv = window.__UIO.getCanvas();
                        var cx=0, cy=0, cw=window.innerWidth, ch=window.innerHeight;
                        if (cv && cv.getBoundingClientRect) {
                            var r = cv.getBoundingClientRect();
                            cx=r.left; cy=r.top; cw=r.width; ch=r.height;
                        }
                        var rc=e.rect, x, y, w, h;
                        if (e.mode==='pct') {
                            x=cx+(rc.x/100)*cw; y=cy+(rc.y/100)*ch;
                            w=(rc.w/100)*cw;    h=(rc.h/100)*ch;
                        } else {
                            var uw=rc.uw>0?rc.uw:cw, uh=rc.uh>0?rc.uh:ch;
                            x=cx+rc.x*(cw/uw); y=cy+rc.y*(ch/uh);
                            w=rc.w*(cw/uw);    h=rc.h*(ch/uh);
                        }
                        e.wrap.style.left   = Math.round(x)+'px';
                        e.wrap.style.top    = Math.round(y)+'px';
                        e.wrap.style.width  = Math.round(w)+'px';
                        e.wrap.style.height = Math.round(h)+'px';
                    },
                    applyAll: function () {
                        var inst=window.__UIO.inst;
                        for (var id in inst)
                            if (Object.prototype.hasOwnProperty.call(inst,id))
                                window.__UIO.applyRect(inst[id]);
                    }
                };
            }
            if (!window.__UIO.rB) {
                window.addEventListener('resize',            window.__UIO.applyAll);
                window.addEventListener('orientationchange', window.__UIO.applyAll);
                window.__UIO.rB = true;
            }
            if (!window.__UIO.msgB) {
                window.addEventListener('message', function(ev) {
                    try {
                        if (!window.__UIO) return;
                        var d = ev.data;
                        if (typeof d === 'string') { try { d = JSON.parse(d); } catch(ex) { return; } }
                        if (!d || typeof d !== 'object') return;
                        var ended = (d.event === 'onStateChange' && d.info === 0) ||
                                    (d.event === 'finish');
                        if (!ended) return;
                        var inst = window.__UIO.inst;
                        for (var iid in inst) {
                            if (!Object.prototype.hasOwnProperty.call(inst, iid)) continue;
                            var ei = inst[iid];
                            if (!ei.cb_go || !ei.cb_fn) continue;
                            try {
                                if (ei.ifr.contentWindow === ev.source) {
                                    SendMessage(ei.cb_go, ei.cb_fn, iid);
                                    break;
                                }
                            } catch(ex) {}
                        }
                    } catch(err) { console.error('[UIO msg] '+err); }
                });
                window.__UIO.msgB = true;
            }
            // ---- create iframe ----
            var id         = UTF8ToString(idPtr);
            var url        = UTF8ToString(urlPtr);
            var sandboxStr = UTF8ToString(sandboxPtr);

            if (window.__UIO.inst[id]) {
                try { window.__UIO.inst[id].wrap.parentNode.removeChild(window.__UIO.inst[id].wrap); } catch(e){}
                delete window.__UIO.inst[id];
            }

            var wrap = document.createElement('div');
            wrap.style.cssText = 'position:fixed;left:0;top:0;width:0;height:0;' +
                'border:0;padding:0;margin:0;background:transparent;overflow:hidden;display:none;';
            wrap.style.zIndex        = String(zIndex|0);
            wrap.style.opacity       = String(opacity);
            wrap.style.pointerEvents = pointerEvents ? 'auto' : 'none';

            var ifr = document.createElement('iframe');
            ifr.style.cssText = 'position:absolute;left:0;top:0;width:100%;height:100%;border:0;';
            ifr.setAttribute('frameborder','0');
            ifr.setAttribute('scrolling','no');

            var allow=['encrypted-media','accelerometer','gyroscope','clipboard-write'];
            if (allowAutoplay)   allow.unshift('autoplay');
            if (allowFullscreen) allow.unshift('fullscreen');
            if (allowPip)        allow.push('picture-in-picture');
            ifr.setAttribute('allow', allow.join('; '));
            if (allowFullscreen) {
                ifr.setAttribute('allowfullscreen','true');
                ifr.setAttribute('webkitallowfullscreen','true');
                ifr.setAttribute('mozallowfullscreen','true');
            }
            if (useSandbox) {
                ifr.setAttribute('sandbox',
                    (sandboxStr && sandboxStr.length>0) ? sandboxStr
                    : 'allow-scripts allow-same-origin allow-popups allow-forms allow-presentation');
            }
            if (url && url.length>0) ifr.src = url;
            wrap.appendChild(ifr);

            var closeBtn = null;
            if (showCloseBtn) {
                closeBtn = document.createElement('button');
                closeBtn.type = 'button';
                closeBtn.innerHTML = '&times;';
                closeBtn.style.cssText =
                    'position:absolute;top:6px;right:6px;width:32px;height:32px;' +
                    'line-height:28px;font-size:22px;font-weight:bold;font-family:Arial,sans-serif;' +
                    'color:#fff;background:rgba(0,0,0,.65);border:0;border-radius:16px;' +
                    'cursor:pointer;z-index:2;padding:0;';
                closeBtn.addEventListener('click', function(){ wrap.style.display='none'; });
                wrap.appendChild(closeBtn);
            }

            document.body.appendChild(wrap);
            window.__UIO.inst[id] = {
                wrap: wrap, ifr: ifr, cb: closeBtn,
                rect: {x:0,y:0,w:0,h:0,uw:0,uh:0},
                mode: 'pct'
            };
        } catch(err) { console.error('[UIO_Create] '+err); }
    },

    UIO_SetRectPercent: function (idPtr, xPct, yPct, wPct, hPct) {
        try {
            if (!window.__UIO) return;
            var e = window.__UIO.inst[UTF8ToString(idPtr)]; if (!e) return;
            e.mode='pct'; e.rect.x=xPct; e.rect.y=yPct; e.rect.w=wPct; e.rect.h=hPct;
            window.__UIO.applyRect(e);
        } catch(err) { console.error('[UIO_SetRectPercent] '+err); }
    },

    UIO_SetRectPixels: function (idPtr, x, y, w, h, unityW, unityH) {
        try {
            if (!window.__UIO) return;
            var e = window.__UIO.inst[UTF8ToString(idPtr)]; if (!e) return;
            e.mode='px'; e.rect.x=x; e.rect.y=y; e.rect.w=w; e.rect.h=h;
            e.rect.uw=unityW; e.rect.uh=unityH;
            window.__UIO.applyRect(e);
        } catch(err) { console.error('[UIO_SetRectPixels] '+err); }
    },

    UIO_Show: function (idPtr) {
        try {
            if (!window.__UIO) return;
            var e = window.__UIO.inst[UTF8ToString(idPtr)]; if (!e) return;
            e.wrap.style.display = 'block';
            window.__UIO.applyRect(e);
        } catch(err) { console.error('[UIO_Show] '+err); }
    },

    UIO_Hide: function (idPtr) {
        try {
            if (!window.__UIO) return;
            var e = window.__UIO.inst[UTF8ToString(idPtr)]; if (!e) return;
            e.wrap.style.display = 'none';
        } catch(err) { console.error('[UIO_Hide] '+err); }
    },

    UIO_Destroy: function (idPtr) {
        try {
            if (!window.__UIO) return;
            var id = UTF8ToString(idPtr);
            var e = window.__UIO.inst[id]; if (!e) return;
            try { e.ifr.src = 'about:blank'; } catch(ex){}
            if (e.wrap && e.wrap.parentNode) e.wrap.parentNode.removeChild(e.wrap);
            delete window.__UIO.inst[id];
        } catch(err) { console.error('[UIO_Destroy] '+err); }
    },

    UIO_SetUrl: function (idPtr, urlPtr) {
        try {
            if (!window.__UIO) return;
            var e = window.__UIO.inst[UTF8ToString(idPtr)]; if (!e) return;
            e.ifr.src = UTF8ToString(urlPtr);
        } catch(err) { console.error('[UIO_SetUrl] '+err); }
    },

    UIO_SetZIndex: function (idPtr, z) {
        try {
            if (!window.__UIO) return;
            var e = window.__UIO.inst[UTF8ToString(idPtr)]; if (!e) return;
            e.wrap.style.zIndex = String(z|0);
        } catch(err) { console.error('[UIO_SetZIndex] '+err); }
    },

    UIO_SetOpacity: function (idPtr, opacity) {
        try {
            if (!window.__UIO) return;
            var e = window.__UIO.inst[UTF8ToString(idPtr)]; if (!e) return;
            e.wrap.style.opacity = String(opacity);
        } catch(err) { console.error('[UIO_SetOpacity] '+err); }
    },

    UIO_SetPointerEvents: function (idPtr, enabled) {
        try {
            if (!window.__UIO) return;
            var e = window.__UIO.inst[UTF8ToString(idPtr)]; if (!e) return;
            e.wrap.style.pointerEvents = enabled ? 'auto' : 'none';
        } catch(err) { console.error('[UIO_SetPointerEvents] '+err); }
    },

    UIO_Exists: function (idPtr) {
        try {
            if (!window.__UIO) return 0;
            return window.__UIO.inst[UTF8ToString(idPtr)] ? 1 : 0;
        } catch(err) { console.error('[UIO_Exists] '+err); return 0; }
    },

    UIO_IsVisible: function (idPtr) {
        try {
            if (!window.__UIO) return 0;
            var e = window.__UIO.inst[UTF8ToString(idPtr)]; if (!e) return 0;
            return (e.wrap.style.display !== 'none') ? 1 : 0;
        } catch(err) { console.error('[UIO_IsVisible] '+err); return 0; }
    },

    UIO_SetVideoEndCallback: function (idPtr, goNamePtr, methodNamePtr) {
        try {
            if (!window.__UIO) return;
            var e = window.__UIO.inst[UTF8ToString(idPtr)]; if (!e) return;
            e.cb_go = UTF8ToString(goNamePtr);
            e.cb_fn = UTF8ToString(methodNamePtr);
        } catch(err) { console.error('[UIO_SetVideoEndCallback] '+err); }
    }

});
