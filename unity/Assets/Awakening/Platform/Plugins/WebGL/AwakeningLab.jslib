// Thin wrappers over globalThis.AwakeningLab (AwakeningLab.jspre) for C# [DllImport("__Internal")].
mergeInto(LibraryManager.library, {
  $AwakeningString: function (text) {
    if (text === null || text === undefined) return 0;
    var size = lengthBytesUTF8(text) + 1;
    var buffer = _malloc(size);
    stringToUTF8(text, buffer, size);
    return buffer;
  },
  AwakeningLabInit__deps: ['$AwakeningString'],
  AwakeningLabInit: function (key) { globalThis.AwakeningLab.init(UTF8ToString(key)); },
  AwakeningLabStatus__deps: ['$AwakeningString'],
  AwakeningLabStatus: function () { return AwakeningString(globalThis.AwakeningLab.status()); },
  AwakeningLabLoad__deps: ['$AwakeningString'],
  AwakeningLabLoad: function () { return AwakeningString(globalThis.AwakeningLab.load()); },
  AwakeningLabSave: function (raw, generation) { globalThis.AwakeningLab.save(UTF8ToString(raw), generation); },
  AwakeningLabNow: function () { return globalThis.AwakeningLab.now(); },
  AwakeningLabTakeVisibility__deps: ['$AwakeningString'],
  AwakeningLabTakeVisibility: function () { return AwakeningString(globalThis.AwakeningLab.takeVisibility()); },
  AwakeningLabSend: function (id, url, body) { globalThis.AwakeningLab.send(UTF8ToString(id), UTF8ToString(url), UTF8ToString(body)); },
  AwakeningLabPoll__deps: ['$AwakeningString'],
  AwakeningLabPoll: function () { return AwakeningString(globalThis.AwakeningLab.poll()); },
  AwakeningLabCancel: function () { globalThis.AwakeningLab.cancel(); },
  AwakeningLabDispose: function () { globalThis.AwakeningLab.dispose(); },
  AwakeningLabDownload: function (text, filename, type) { globalThis.AwakeningLab.download(UTF8ToString(text), UTF8ToString(filename), UTF8ToString(type)); },
  AwakeningLabTouchScreen: function () { return globalThis.AwakeningLab.touchScreen() ? 1 : 0; },
});
