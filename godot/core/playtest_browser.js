/* global performance, document, navigator, addEventListener, indexedDB, AbortController, setTimeout, clearTimeout, fetch, TextDecoder */
/* The game envelope and outbox share one IndexedDB transaction. A completed
 * transaction, rather than Godot's asynchronous filesystem flush, gates POSTs. */
(() => {
  globalThis.AwakeningLab = {
    ready: false, writable: false, failed: false, saved: 0, raw: null,
    visibility: [], responses: [], request: null, db: null, release: null,
    pending: null, writing: false,
    now: () => Math.floor(performance.now()),
    status() { return JSON.stringify({ready:this.ready,writable:this.writable,failed:this.failed,saved:this.saved}); },
    load() { return this.raw; },
    recordVisibility() {
      const state={at:this.now(),focused:document.hasFocus(),visible:!document.hidden};
      if(this.lastVisibility?.focused===state.focused && this.lastVisibility?.visible===state.visible)return;
      this.lastVisibility=state;this.visibility.push(state);
    },
    takeVisibility() { this.recordVisibility();return JSON.stringify(this.visibility.splice(0)); },
    poll() { return JSON.stringify(this.responses.splice(0)); },
    init(key) {
      this.key = key;
      const record = () => this.recordVisibility();
      addEventListener('focus', record); addEventListener('blur', record);
      document.addEventListener('visibilitychange', record); record();
      if (!navigator.locks || !globalThis.indexedDB) { this.failed=true; this.ready=true; return; }
      navigator.locks.request('lucid-awakening:' + key, {ifAvailable:true}, async lock => {
        if (!lock) { this.ready=true; return; }
        try {
          this.db = await new Promise((resolve,reject) => {
            const req=indexedDB.open('lucid-awakening-save-v1',1);
            req.onupgradeneeded=()=>req.result.createObjectStore('snapshots');
            req.onsuccess=()=>resolve(req.result); req.onerror=()=>reject(req.error);
            req.onblocked=()=>reject(new Error('Storage blocked'));
          });
          this.db.onversionchange=()=>{this.failed=true;this.writable=false;this.db.close();};
          this.raw = await new Promise((resolve,reject)=>{
            const tx=this.db.transaction('snapshots','readonly');
            const req=tx.objectStore('snapshots').get(key);
            req.onsuccess=()=>resolve(req.result ?? null); req.onerror=()=>reject(req.error);
          });
          this.writable=true;
        } catch { this.failed=true; }
        this.ready=true;
        await new Promise(resolve=>this.release=resolve);
      }).catch(()=>{this.failed=true;this.ready=true;});
    },
    save(raw,generation) {
      if (!this.writable || this.failed) return;
      this.pending={raw,generation}; this.flush();
    },
    flush() {
      if (this.writing || !this.pending || this.failed) return;
      const entry=this.pending; this.pending=null; this.writing=true;
      try {
        const tx=this.db.transaction('snapshots','readwrite');
        tx.objectStore('snapshots').put(entry.raw,this.key);
        tx.oncomplete=()=>{this.saved=entry.generation;this.raw=entry.raw;this.writing=false;this.flush();};
        tx.onabort=tx.onerror=()=>{this.failed=true;this.writable=false;this.writing=false;};
      } catch { this.failed=true;this.writable=false;this.writing=false; }
    },
    send(id,url,body) {
      const controller=new AbortController(); this.request=controller;
      const timeout=setTimeout(()=>controller.abort(),10000);
      fetch(url,{method:'POST',headers:{'Content-Type':'application/json'},body,
        credentials:'omit',cache:'no-store',redirect:'error',signal:controller.signal})
        .then(async response=>{
          let text='',size=0; const reader=response.body?.getReader();
          if(reader) { const decoder=new TextDecoder(); for (;;) {
            const chunk=await reader.read(); if(chunk.done) break;
            size+=chunk.value.byteLength; if(size>16384) {await reader.cancel();throw new Error('Response too large');}
            text+=decoder.decode(chunk.value,{stream:true});
          } text+=decoder.decode(); }
          this.responses.push({id,status:response.status,body:text,retry_after:response.headers.get('Retry-After') ?? ''});
        }).catch(()=>this.responses.push({id,status:0,body:'',retry_after:''}))
        .finally(()=>{clearTimeout(timeout);if(this.request===controller)this.request=null;});
    },
    cancel() { this.request?.abort(); this.request=null;this.responses=[]; },
    dispose() { this.cancel();this.release?.();this.release=null;this.writable=false;this.db?.close(); }
  };
})();
